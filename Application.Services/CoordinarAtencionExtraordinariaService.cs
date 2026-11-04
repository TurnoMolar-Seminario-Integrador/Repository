using Data;
using Domain.Model;
using DTOs;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    // =========================================================================
    // CUF11 - COORDINAR ATENCIÓN EXTRAORDINARIA (E4.R01, v1.01)
    // Actor primario: Responsable de la clínica. RN13.
    // Camino básico: 1. ingresa el documento del paciente; 2. el sistema valida que esté Habilitado y
    //   no tenga un turno pendiente de atención; 3. ingresa especialidad, odontólogo (Habilitado),
    //   fecha y hora acordadas, descripción del material y modalidad de pago; 4. el sistema registra
    //   el turno en estado "Reservado" sin aplicar la grilla de disponibilidad estándar y emite el
    //   comprobante con la descripción del material.
    // Alt 2.a: deuda pendiente -> mensajeDeudaPendiente(d). Alt 2.b: turno pendiente.
    // Alt 3.a: el responsable cancela la operación (no se registra nada: basta con no invocar Registrar).
    // RN19 (antelación mínima) NO aplica a este circuito: es la coordinación fuera del circuito estándar
    // (RN13). Ver docs/DESVIOS.md.
    // =========================================================================
    public interface ICoordinarAtencionExtraordinariaService
    {
        Task<ResultadoOperacionDTO> ValidarPacienteAsync(string tipoDocumento, string nroDocumento);
        Task<AtencionExtraordinariaResultDTO> RegistrarAsync(AtencionExtraordinariaRequestDTO solicitud);
    }

    public class CoordinarAtencionExtraordinariaService : ICoordinarAtencionExtraordinariaService
    {
        public const string MensajeDeudaPendiente = "El paciente posee una deuda pendiente. Debe regularizar su situación para poder coordinar una nueva atención.";
        public const string MensajeTurnoPendiente = "El paciente ya posee un turno pendiente de atención.";
        public const string MensajePacienteInexistente = "No se encontró un paciente con ese tipo y número de documento.";
        public const string MensajePacienteHabilitado = "El paciente está habilitado para coordinar una atención extraordinaria.";
        public const string MensajeOdontologoInexistente = "No se encontró el odontólogo indicado.";
        public const string MensajeOdontologoInhabilitado = "El odontólogo seleccionado se encuentra inhabilitado y no puede recibir turnos.";
        public const string MensajeNoAtiendeEspecialidad = "El odontólogo seleccionado no atiende la especialidad indicada.";
        public const string MensajeFechaPasada = "La fecha y hora acordadas deben ser posteriores al momento actual.";
        public const string MensajeMaterialRequerido = "Describí el material que el paciente debe traer o que se va a utilizar.";
        public const string MensajeSinConvenio = "La obra social del paciente no tiene un convenio vigente para esa especialidad. Seleccioná otra forma de abonar.";
        public const string MensajeChoqueAgenda = "El odontólogo ya tiene un turno en ese horario.";
        public const string MensajeRegistrado = "Atención extraordinaria registrada. Se emitió el comprobante del turno.";

        private readonly TurnoMolarDbContext _contexto;

        public CoordinarAtencionExtraordinariaService(TurnoMolarDbContext contexto) => _contexto = contexto;

        // Pasos 1-2.
        public async Task<ResultadoOperacionDTO> ValidarPacienteAsync(string tipoDocumento, string nroDocumento)
        {
            var paciente = await _contexto.Pacientes.AsNoTracking()
                .FirstOrDefaultAsync(p => p.TipoDocumento == tipoDocumento && p.NroDocumento == nroDocumento);
            if (paciente == null)
                return ResultadoOperacionDTO.Error("1", MensajePacienteInexistente);
            if (paciente.EstadoPaciente != "HABILITADO")
                return ResultadoOperacionDTO.Error("2.a.1", MensajeDeudaPendiente);
            if (await TieneTurnoPendienteAsync(tipoDocumento, nroDocumento))
                return ResultadoOperacionDTO.Error("2.b.1", MensajeTurnoPendiente);
            return ResultadoOperacionDTO.Ok(MensajePacienteHabilitado, "2");
        }

        public async Task<AtencionExtraordinariaResultDTO> RegistrarAsync(AtencionExtraordinariaRequestDTO s)
        {
            // Se revalida el paso 2 por si cambió el estado entre la búsqueda y la confirmación.
            var validacion = await ValidarPacienteAsync(s.TipoDocumentoPaciente, s.NroDocumentoPaciente);
            if (!validacion.Exito)
                return Error(validacion.Codigo, validacion.Mensaje);

            var paciente = await _contexto.Pacientes
                .FirstAsync(p => p.TipoDocumento == s.TipoDocumentoPaciente && p.NroDocumento == s.NroDocumentoPaciente);

            // Paso 3: eDatosAtencionExtraordinaria.
            var odontologo = await _contexto.Odontologos.Include(o => o.DisponibilidadesHorarias)
                .FirstOrDefaultAsync(o => o.TipoDocumento == s.OdontologoTipoDocumento && o.NroDocumento == s.OdontologoNroDocumento);
            if (odontologo == null)
                return Error("3", MensajeOdontologoInexistente);
            if (!odontologo.EstaHabilitado)
                return Error("3", MensajeOdontologoInhabilitado);
            // DisponibilidadHoraria es la clase asociativa Odontólogo-Especialidad: se usa solo para
            // saber qué especialidades atiende, no para validar día y hora.
            if (!odontologo.DisponibilidadesHorarias.Any(d => d.IdEspecialidad == s.IdEspecialidad))
                return Error("3", MensajeNoAtiendeEspecialidad);
            // CUF12 E4.R02 alt 1.d.2: una especialidad deshabilitada ya no se ofrece.
            var especialidadElegida = await _contexto.Especialidades.FindAsync(s.IdEspecialidad);
            if (especialidadElegida != null && !especialidadElegida.EstaHabilitada)
                return Error("3", AdministrarEspecialidadService.MensajeEspecialidadDeshabilitada);
            if (s.FechaHoraTurno <= DateTime.Now)
                return Error("3", MensajeFechaPasada);
            if (string.IsNullOrWhiteSpace(s.DescripcionMaterial))
                return Error("3", MensajeMaterialRequerido);

            string modalidad;
            try { modalidad = ModalidadesPago.Requerir(s.ModalidadPago); }
            catch (ArgumentException ex) { return Error("3", ex.Message); }

            if (modalidad == ModalidadesPago.ObraSocial)
            {
                var id = paciente.IdentificadorOS ?? string.Empty;
                var obra = await _contexto.ObrasSociales.Include(o => o.Convenios)
                    .FirstOrDefaultAsync(o => o.IdentificadorOS == id);
                if (obra == null || !obra.EstaDisponible || !obra.Convenios.Any(c => c.IdEspecialidad == s.IdEspecialidad))
                    return Error("3", MensajeSinConvenio);
            }

            // Choque con otro turno del odontólogo (dos turnos de 30 min se superponen).
            var desde = s.FechaHoraTurno.AddMinutes(-AgendaTurnoService.DuracionTurnoMinutos);
            var hasta = s.FechaHoraTurno.AddMinutes(AgendaTurnoService.DuracionTurnoMinutos);
            var choque = await _contexto.Turnos.AnyAsync(t =>
                t.TipoDocumentoOdontologo == odontologo.TipoDocumento && t.NroDocumentoOdontologo == odontologo.NroDocumento &&
                t.EstadoTurno != "CANCELADO" && t.EstadoTurno != "REPROGRAMADO" &&
                t.FechaHoraTurno > desde && t.FechaHoraTurno < hasta);
            if (choque)
                return Error("3", MensajeChoqueAgenda);

            // Paso 4: turno "Reservado" con descripcionMaterial + comprobante.
            var turno = new Turno(0, s.FechaHoraTurno, modalidad, s.IdEspecialidad,
                odontologo.TipoDocumento, odontologo.NroDocumento, paciente.TipoDocumento, paciente.NroDocumento,
                "RESERVADO", s.DescripcionMaterial.Trim());
            _contexto.Turnos.Add(turno);
            await _contexto.SaveChangesAsync();

            var comprobante = new ComprobanteDeTurno(0, turno.NroTurno, DateTime.Now);
            _contexto.ComprobantesTurnos.Add(comprobante);
            await _contexto.SaveChangesAsync();

            var especialidad = await _contexto.Especialidades.FindAsync(s.IdEspecialidad);

            // sComprobanteTurnoExtraordinario
            return new AtencionExtraordinariaResultDTO
            {
                Exito = true,
                Codigo = "4",
                Mensaje = MensajeRegistrado,
                NroTurno = turno.NroTurno,
                DescripcionMaterial = turno.DescripcionMaterial,
                Comprobante = new ComprobanteTurnoDTO
                {
                    NroComprobante = comprobante.NroComprobante,
                    FechaHoraEmision = comprobante.FechaHoraEmision,
                    FechaHoraTurno = turno.FechaHoraTurno,
                    NombrePaciente = paciente.Nombre,
                    ApellidoPaciente = paciente.Apellido,
                    NombreEspecialidad = especialidad?.Nombre ?? string.Empty,
                    NombreOdontologo = odontologo.Nombre,
                    ApellidoOdontologo = odontologo.Apellido
                }
            };
        }

        // RN8: turno pendiente de atención = Reservado, Presente o Atención Registrada.
        private Task<bool> TieneTurnoPendienteAsync(string tipo, string nro) =>
            _contexto.Turnos.AnyAsync(t => t.TipoDocumentoPaciente == tipo && t.NroDocumentoPaciente == nro &&
                (t.EstadoTurno == "RESERVADO" || t.EstadoTurno == "PRESENTE" || t.EstadoTurno == "ATENCION_REGISTRADA"));

        private static AtencionExtraordinariaResultDTO Error(string codigo, string mensaje) =>
            new() { Exito = false, Codigo = codigo, Mensaje = mensaje };
    }
}
