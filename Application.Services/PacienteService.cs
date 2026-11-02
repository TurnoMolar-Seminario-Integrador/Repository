using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    public class PacienteService : IPacienteService
    {
        private readonly IPacienteRepository pacienteRepository;
        private readonly IIncorporarPacienteService incorporarPacienteService;
        private readonly IRectificarDatosPacienteService rectificarDatosPacienteService;
        private readonly ICoberturaPacienteService coberturaPacienteService;

        public PacienteService(
            IPacienteRepository pacienteRepository,
            IIncorporarPacienteService incorporarPacienteService,
            IRectificarDatosPacienteService rectificarDatosPacienteService,
            ICoberturaPacienteService coberturaPacienteService)
        {
            this.pacienteRepository = pacienteRepository;
            this.incorporarPacienteService = incorporarPacienteService;
            this.rectificarDatosPacienteService = rectificarDatosPacienteService;
            this.coberturaPacienteService = coberturaPacienteService;
        }

        // CUF01 (Incorporar Paciente) desde la WebAPI. Antes armaba el Paciente acá, con el estado y el
        // tipo de documento que mandara el cliente, y no creaba la historia clínica. Ahora el alta es la
        // del caso de uso: datos validados, paciente Habilitado y sin deuda, historia clínica creada.
        // Sin IdentificadorOS el paciente es particular (RN6); con uno, debe tener acuerdo activo. La
        // contraseña (Password) la establece quien se registra (RN3) y es obligatoria. El correo no puede ser
        // el de otra persona (otra clave primaria): lo controla el caso de uso, no este servicio.
        public async Task<PacienteDTO> AddAsync(PacienteDTO dto)
        {
            var resultado = await incorporarPacienteService.IncorporarAsync(new IncorporarPacienteRequestDTO
            {
                DatosPersonales = ADatosPersonales(dto),
                ObraSocial = string.IsNullOrWhiteSpace(dto.IdentificadorOS) ? "Particular" : dto.IdentificadorOS,
                Password = dto.Password
            });

            if (resultado.Resultado != ResultadoIncorporarPaciente.Incorporado)
                throw new ArgumentException(MensajeDeError(resultado.Mensaje, resultado.Errores));

            var paciente = await pacienteRepository.GetAsync(resultado.TipoDocumento!, resultado.NroDocumento!)
                ?? throw new InvalidOperationException("El paciente se registró pero no pudo recuperarse.");
            return MapToDTO(paciente);
        }

        public async Task<bool> DeleteAsync(int nroDocumento)
        {
            return await pacienteRepository.DeleteAsync(nroDocumento);
        }

        public async Task<PacienteDTO?> GetAsync(int nroDocumento)
        {
            var paciente = await pacienteRepository.GetAsync(nroDocumento);
            return paciente == null ? null : MapToDTO(paciente);
        }

        public async Task<IEnumerable<PacienteDTO>> GetAllAsync()
        {
            var pacientes = await pacienteRepository.GetAllAsync();
            return pacientes.Select(MapToDTO).ToList();
        }

        // CUF02 (Rectificar Datos del Paciente) desde la WebAPI. El paciente se identifica por el tipo y
        // número de documento del DTO. Antes se armaba un paciente nuevo desde el DTO y se lo guardaba
        // entero: pisaba la contraseña con vacío (el paciente dejaba de poder ingresar), la deuda con 0 y
        // el estado con el que mandara el cliente. Ahora solo se rectifican los datos personales y la
        // obra social (sin afectar el estado de habilitación), con las validaciones del caso de uso.
        // Devuelve false si el paciente no existe. Concurrencia: el DTO debe traer la VersionDatos que devolvió
        // el GET; si otro usuario modificó los datos personales desde entonces se lanza
        // ConflictoConcurrenciaException (la API responde 409) y no se cambia nada, tampoco la obra social.
        public async Task<bool> UpdateAsync(PacienteDTO dto)
        {
            var tipoDocumento = (dto.TipoDocumento ?? "DNI").Trim().ToUpperInvariant();
            var nroDocumento = (dto.NroDocumento ?? string.Empty).Trim().ToUpperInvariant();

            if (await pacienteRepository.GetAsync(tipoDocumento, nroDocumento) == null)
                return false;

            if (string.IsNullOrWhiteSpace(dto.VersionDatos))
                throw new ArgumentException("Falta VersionDatos: enviá la que devolvió GET /pacientes para el paciente que se modifica.");

            // La versión se controla antes de tocar la obra social: si el conflicto apareciera recién al
            // rectificar los datos, la cobertura ya habría cambiado y el paciente quedaría a medio modificar.
            var vigentes = await rectificarDatosPacienteService.ConsultarDatosAsync(tipoDocumento, nroDocumento);
            if (!VersionDeDatos.Coincide(dto.VersionDatos, vigentes.VersionDatos))
                throw new ConflictoConcurrenciaException(RectificarDatosPacienteService.MensajeConflictoConcurrencia);

            var datos = ADatosPersonales(dto);
            datos.TipoDocumento = tipoDocumento;
            datos.NroDocumento = nroDocumento;

            // Se validan los datos antes de tocar nada: si la obra social se cambiara primero y los datos
            // resultaran inválidos, el paciente quedaría a medio modificar.
            var errores = DatosPersonalesPacienteValidador.Validar(DatosPersonalesPacienteValidador.Normalizar(datos));
            if (errores.Count > 0)
                throw new ArgumentException(MensajeDeError(IncorporarPacienteService.MensajeDatosInvalidos, errores));

            // Obra social (alt 2.a y 2.b). Sin IdentificadorOS no se modifica (un cliente que solo corrige un
            // teléfono no debe dejar al paciente sin cobertura: eso cambiaría el precio de sus turnos). Con uno
            // se cambia a esa obra social, que debe tener acuerdo activo, o a particular con "PARTICULAR". El
            // cliente manda el valor final deseado, de modo que pasar a particular no requiere la
            // confirmación interactiva.
            if (!string.IsNullOrWhiteSpace(dto.IdentificadorOS))
            {
                var cobertura = await coberturaPacienteService.ActualizarObraSocialAsync(
                    tipoDocumento, nroDocumento, dto.IdentificadorOS, confirmaPasarAParticular: true);
                if (cobertura.Resultado == ResultadoActualizarCobertura.ConflictoConcurrencia)
                    throw new ConflictoConcurrenciaException(cobertura.Mensaje);
                if (cobertura.Resultado != ResultadoActualizarCobertura.Actualizada)
                    throw new ArgumentException(cobertura.Mensaje);
            }

            var rectificacion = await rectificarDatosPacienteService.RectificarDatosAsync(
                tipoDocumento, nroDocumento, datos, dto.VersionDatos);
            switch (rectificacion.Resultado)
            {
                case ResultadoRectificarDatos.Actualizada:
                case ResultadoRectificarDatos.SinCambios:
                    return true;
                case ResultadoRectificarDatos.PacienteNoEncontrado:
                    return false;
                case ResultadoRectificarDatos.ConflictoConcurrencia:
                    throw new ConflictoConcurrenciaException(rectificacion.Mensaje);
                default:
                    throw new ArgumentException(MensajeDeError(rectificacion.Mensaje, rectificacion.Errores));
            }
        }

        public async Task<IEnumerable<PacienteDTO>> GetByCriteriaAsync(PacienteCriteriaDTO criteriaDTO)
        {
            var criteria = new PacienteCriteria(criteriaDTO.Texto);
            var pacientes = await pacienteRepository.GetByCriteriaAsync(criteria);
            return pacientes.Select(MapToDTO).ToList();
        }

        private static DatosPersonalesPacienteDTO ADatosPersonales(PacienteDTO dto)
        {
            return new DatosPersonalesPacienteDTO
            {
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                TipoDocumento = dto.TipoDocumento,
                NroDocumento = dto.NroDocumento,
                FechaNacimiento = dto.FechaNacimiento,
                Telefono = dto.Telefono,
                Email = dto.Email,
                Domicilio = dto.Domicilio
            };
        }

        // Con errores por campo se informan todos (la API no tiene campos "marcados en rojo").
        private static string MensajeDeError(string mensaje, Dictionary<string, string> errores)
        {
            return errores.Count > 0 ? string.Join(" ", errores.Values) : mensaje;
        }

        private static PacienteDTO MapToDTO(Paciente p)
        {
            return new PacienteDTO
            {
                TipoDocumento = p.TipoDocumento,
                NroDocumento = p.NroDocumento,
                Nombre = p.Nombre,
                Apellido = p.Apellido,
                Telefono = p.Telefono,
                Email = p.Email,
                Domicilio = p.Domicilio,
                FechaNacimiento = p.FechaNacimiento,
                EstadoPaciente = p.EstadoPaciente,
                IdentificadorOS = p.IdentificadorOS,
                NombreObraSocial = p.ObraSocial?.NombreOS,
                VersionDatos = VersionDeDatos.DePaciente(p)
            };
        }
    }
}
