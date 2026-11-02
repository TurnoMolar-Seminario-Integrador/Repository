using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    // =========================================================================
    // CUF02 - RECTIFICAR DATOS DEL PACIENTE (E4.R01, v1.02)
    // Actor primario: Paciente. Otros: Responsable de la clínica. Meta: permitir la consulta y modificación
    // de los datos personales y de obra social de un paciente ya registrado, sin afectar su estado de
    // habilitación.
    //
    // Camino básico:
    //   1. El paciente ingresa a sus datos personales; el sistema muestra nombre, apellido, tipo y número
    //      de documento, fecha de nacimiento, teléfono, email y domicilio  -> ConsultarDatosAsync.
    //   2. Selecciona el tipo de dato a modificar e ingresa el nuevo valor (eDatoAModificar).
    //   3. Confirma; el sistema valida, actualiza SIN MODIFICAR EL ESTADO DE HABILITACIÓN y confirma
    //      ("Sus datos fueron actualizados correctamente.")                  -> RectificarDatosAsync.
    // Alt 2.a / 2.b (baja y alta de obra social, con su confirmación y la validación del acuerdo activo):
    //   ver ICoberturaPacienteService.ActualizarObraSocialAsync.
    //
    // El diccionario de datos admite modificar cualquiera de los ocho datos, incluidos el tipo y el número
    // de documento (campoModificado). Antes solo se podían cambiar el teléfono, el correo y el domicilio, y
    // la pantalla mostraba el número de documento como int (un pasaporte con letras salía como 0).
    //
    // El responsable de la clínica (otro actor) usa este mismo servicio desde su pantalla "Rectificar datos
    // del paciente", e identifica al paciente por tipo y número de documento.
    //
    // Defecto corregido en la WebAPI (PUT /pacientes): armaba un paciente nuevo desde el DTO y se lo pasaba
    // al repositorio, que pisaba la contraseña con vacío (el paciente dejaba de poder ingresar), la deuda
    // con 0 y el estado con el que mandara el cliente. Ahora PacienteService.UpdateAsync delega acá: solo
    // se rectifican los datos personales y la obra social, con sus validaciones.
    //
    // Concurrencia (optimista): dos personas pueden tener abierta la misma ficha (el paciente desde su
    // portal y el responsable desde su consola). El formulario lleva la huella de los datos que mostró
    // (VersionDatos); si al guardar ya no coincide con la base, otro los modificó en el medio y no se guarda
    // nada (ConflictoConcurrencia: hay que volver a cargar los datos vigentes y reaplicar el cambio). Los
    // tokens de concurrencia de EF Core cubren además la carrera dentro del propio guardado.
    //
    // Correo electrónico: es de una sola persona, identificada por su clave primaria (tipo y número de
    // documento), y es el mismo en todos sus roles. Un correo en uso por otra persona se rechaza; si el
    // paciente es además odontólogo o responsable de la clínica (misma clave), el correo nuevo se replica en
    // esos roles. Ver IPersonaRepository. Cambiar el documento hace del paciente otra persona a estos
    // efectos: no se replica nada y el correo se valida contra la clave nueva.
    // =========================================================================
    public interface IRectificarDatosPacienteService
    {
        // Paso 1. Lanza ArgumentException si el paciente no existe.
        Task<DatosActualesPacienteDTO> ConsultarDatosAsync(string tipoDocumento, string nroDocumento);

        // Pasos 2 y 3. tipoDocumentoActual / nroDocumentoActual identifican al paciente (el que figura hoy);
        // nuevosDatos trae todos los valores con los que debe quedar (los que no cambian, iguales).
        // versionEsperada: la VersionDatos que devolvió ConsultarDatosAsync cuando se mostraron los datos; sin
        // ella (o si ya no coincide) no se guarda nada y el resultado es ConflictoConcurrencia.
        Task<RectificarDatosResultDTO> RectificarDatosAsync(
            string tipoDocumentoActual, string nroDocumentoActual, DatosPersonalesPacienteDTO nuevosDatos,
            string? versionEsperada);
    }

    public class RectificarDatosPacienteService : IRectificarDatosPacienteService
    {
        public const string MensajePacienteNoEncontrado = "No se encontró el paciente indicado.";

        public const string MensajeSinCambios = "No se detectaron cambios en los datos ingresados.";

        public const string MensajeDocumentoYaRegistrado =
            "Ya existe otro paciente registrado con ese tipo y número de documento. No se realizó la modificación.";

        public const string MensajeConflictoConcurrencia =
            "Los datos del paciente fueron modificados por otro usuario mientras los editabas. No se guardó ningún cambio: revisá los datos vigentes y volvé a aplicar tu modificación.";

        private readonly IPacienteRepository _pacienteRepository;
        private readonly IPersonaRepository _personaRepository;

        public RectificarDatosPacienteService(IPacienteRepository pacienteRepository, IPersonaRepository personaRepository)
        {
            _pacienteRepository = pacienteRepository;
            _personaRepository = personaRepository;
        }

        public async Task<DatosActualesPacienteDTO> ConsultarDatosAsync(string tipoDocumento, string nroDocumento)
        {
            var paciente = await _pacienteRepository.GetAsync(Normalizar(tipoDocumento), Normalizar(nroDocumento))
                ?? throw new ArgumentException(MensajePacienteNoEncontrado);

            return new DatosActualesPacienteDTO
            {
                Nombre = paciente.Nombre,
                Apellido = paciente.Apellido,
                TipoDocumento = paciente.TipoDocumento,
                NroDocumento = paciente.NroDocumento,
                FechaNacimiento = paciente.FechaNacimiento,
                Telefono = paciente.Telefono,
                Email = paciente.Email,
                Domicilio = paciente.Domicilio,
                EstadoPaciente = paciente.EstadoPaciente,
                VersionDatos = VersionDeDatos.DePaciente(paciente),
                IdentificadorOS = paciente.IdentificadorOS,
                NombreObraSocial = paciente.ObraSocial?.NombreOS
            };
        }

        public async Task<RectificarDatosResultDTO> RectificarDatosAsync(
            string tipoDocumentoActual, string nroDocumentoActual, DatosPersonalesPacienteDTO nuevosDatos,
            string? versionEsperada)
        {
            var paciente = await _pacienteRepository.GetAsync(Normalizar(tipoDocumentoActual), Normalizar(nroDocumentoActual));
            if (paciente == null)
            {
                return new RectificarDatosResultDTO
                {
                    Resultado = ResultadoRectificarDatos.PacienteNoEncontrado,
                    Mensaje = MensajePacienteNoEncontrado
                };
            }

            // Identificación y nombre vigentes: se toman ahora, antes de aplicar los cambios en memoria, para
            // informarlos tal cual están en la base si el guardado termina en conflicto.
            var tipoVigente = paciente.TipoDocumento;
            var nroVigente = paciente.NroDocumento;
            var nombreVigente = $"{paciente.Nombre} {paciente.Apellido}";
            var correoVigente = paciente.Email;

            // Concurrencia: los datos que vio quien edita tienen que ser los que hay en la base.
            if (!VersionDeDatos.Coincide(versionEsperada, VersionDeDatos.DePaciente(paciente)))
            {
                return Conflicto(tipoVigente, nroVigente, nombreVigente);
            }

            // Paso 3: "el sistema valida los datos ingresados".
            var datos = DatosPersonalesPacienteValidador.Normalizar(nuevosDatos);
            var errores = DatosPersonalesPacienteValidador.Validar(datos);
            if (errores.Count > 0)
            {
                return Resultado(paciente, ResultadoRectificarDatos.DatosInvalidos, IncorporarPacienteService.MensajeDatosInvalidos, errores);
            }

            var tipoDocumento = datos.TipoDocumento!;
            var nroDocumento = datos.NroDocumento!;
            var cambiaDocumento = tipoDocumento != paciente.TipoDocumento || nroDocumento != paciente.NroDocumento;

            // El documento es la identidad del paciente: no puede pasar a ser el de otro paciente.
            if (cambiaDocumento && await _pacienteRepository.GetAsync(tipoDocumento, nroDocumento) != null)
            {
                return Resultado(paciente, ResultadoRectificarDatos.DocumentoYaRegistrado, MensajeDocumentoYaRegistrado,
                    new Dictionary<string, string> { ["NroDocumento"] = MensajeDocumentoYaRegistrado });
            }

            // El correo es de una sola persona (clave primaria). Se controla cuando cambia el correo o cuando
            // cambia el documento (la persona pasa a ser otra y su correo se valida contra la clave nueva).
            var cambiaCorreo = !string.Equals(datos.Email, paciente.Email, StringComparison.OrdinalIgnoreCase);
            if ((cambiaCorreo || cambiaDocumento)
                && await _personaRepository.CorreoEnUsoPorOtraPersonaAsync(datos.Email!, tipoDocumento, nroDocumento))
            {
                var mensajeCorreo = IncorporarPacienteService.MensajeCorreoDeOtraPersona;
                return Resultado(paciente, ResultadoRectificarDatos.DatosInvalidos, IncorporarPacienteService.MensajeDatosInvalidos,
                    new Dictionary<string, string> { ["Email"] = mensajeCorreo });
            }

            var hayCambios = cambiaDocumento
                || datos.Nombre != paciente.Nombre
                || datos.Apellido != paciente.Apellido
                || datos.FechaNacimiento!.Value.Date != paciente.FechaNacimiento.Date
                || datos.Telefono != paciente.Telefono
                || datos.Email != paciente.Email
                || datos.Domicilio != paciente.Domicilio;
            if (!hayCambios)
            {
                return Resultado(paciente, ResultadoRectificarDatos.SinCambios, MensajeSinCambios, new Dictionary<string, string>());
            }

            // Solo datos personales: el estado de habilitación, la deuda, la contraseña y la obra social no
            // se tocan ("sin afectar su estado de habilitación").
            paciente.SetNombre(datos.Nombre!);
            paciente.SetApellido(datos.Apellido!);
            paciente.SetFechaNacimiento(datos.FechaNacimiento!.Value);
            paciente.SetTelefono(datos.Telefono!);
            paciente.SetEmail(datos.Email!);
            paciente.SetDomicilio(datos.Domicilio!);

            try
            {
                if (cambiaDocumento)
                {
                    await _pacienteRepository.CambiarDocumentoAsync(paciente, tipoDocumento, nroDocumento);
                }
                else
                {
                    await _pacienteRepository.UpdateAsync(paciente);
                }
            }
            catch (ConflictoConcurrenciaException)
            {
                // Otro usuario guardó la ficha entre la lectura y este guardado: el repositorio ya descartó
                // los cambios en memoria y no se escribió nada.
                return Conflicto(tipoVigente, nroVigente, nombreVigente);
            }

            // Un mismo usuario tiene el mismo correo en todos sus roles: si el paciente es también odontólogo
            // o responsable de la clínica (misma clave primaria), el correo nuevo se replica en esas filas.
            if (!cambiaDocumento && datos.Email != correoVigente)
            {
                await _personaRepository.SincronizarCorreoEnRolesDelPersonalAsync(tipoDocumento, nroDocumento, datos.Email!);
            }

            return new RectificarDatosResultDTO
            {
                Resultado = ResultadoRectificarDatos.Actualizada,
                // Diccionario, paso 3: sConfirmacionModificacion = mensajeModificacionExitosa(d).
                Mensaje = CoberturaPacienteService.MensajeDatosActualizados,
                TipoDocumento = tipoDocumento,
                NroDocumento = nroDocumento,
                NombreCompleto = $"{datos.Nombre} {datos.Apellido}",
                CambioElDocumento = cambiaDocumento
            };
        }

        private static RectificarDatosResultDTO Conflicto(string tipoDocumento, string nroDocumento, string nombreCompleto)
        {
            return new RectificarDatosResultDTO
            {
                Resultado = ResultadoRectificarDatos.ConflictoConcurrencia,
                Mensaje = MensajeConflictoConcurrencia,
                TipoDocumento = tipoDocumento,
                NroDocumento = nroDocumento,
                NombreCompleto = nombreCompleto,
                CambioElDocumento = false
            };
        }

        private static RectificarDatosResultDTO Resultado(
            Paciente paciente, ResultadoRectificarDatos resultado, string mensaje, Dictionary<string, string> errores)
        {
            return new RectificarDatosResultDTO
            {
                Resultado = resultado,
                Mensaje = mensaje,
                Errores = errores,
                TipoDocumento = paciente.TipoDocumento,
                NroDocumento = paciente.NroDocumento,
                NombreCompleto = $"{paciente.Nombre} {paciente.Apellido}",
                CambioElDocumento = false
            };
        }

        // El tipo y el número de documento se guardan en mayúsculas (Persona).
        private static string Normalizar(string? valor) => (valor ?? string.Empty).Trim().ToUpperInvariant();
    }
}
