using Data;
using DTOs;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    // =========================================================================
    // CUF05 - CONSULTAR VALORACIONES (E4.R01, v1.02)
    // Actor primario: Odontólogo. Otros: Responsable de la clínica (alt 1.b).
    // Camino básico: 1. el odontólogo consulta las valoraciones de sus atenciones (nroTurno, fecha y hora
    //   de inicio de la atención, especialidad, paciente, calificación y observaciones);
    //   2. selecciona una y el sistema muestra el detalle.
    // Alt 1.a: sin valoraciones -> mensajeSinValoraciones(d).
    // Alt 1.b: el responsable consulta todas las valoraciones de la clínica (agrega el odontólogo).
    // =========================================================================
    public interface IConsultarValoracionesService
    {
        Task<ConsultaValoracionesResultDTO> ConsultarPorOdontologoAsync(string tipoDocumento, string nroDocumento);
        Task<ConsultaValoracionesResultDTO> ConsultarClinicaAsync();
        Task<ValoracionListadoDTO?> ObtenerDetalleAsync(int nroTurno, string? tipoDocumentoOdontologo = null, string? nroDocumentoOdontologo = null);
    }

    public class ConsultarValoracionesService : IConsultarValoracionesService
    {
        public const string MensajeSinValoraciones = "No hay valoraciones disponibles para mostrar.";

        private readonly TurnoMolarDbContext _contexto;

        public ConsultarValoracionesService(TurnoMolarDbContext contexto) => _contexto = contexto;

        // Paso 1: sListadoValoraciones del odontólogo autenticado.
        public Task<ConsultaValoracionesResultDTO> ConsultarPorOdontologoAsync(string tipoDocumento, string nroDocumento) =>
            ConsultarAsync(Consulta(tipoDocumento, nroDocumento));

        // Alt 1.b: sListadoValoracionesClinica.
        public Task<ConsultaValoracionesResultDTO> ConsultarClinicaAsync() => ConsultarAsync(Consulta(null, null));

        // Paso 2: sDetalleValoracion. Si se indica el odontólogo, solo puede ver las suyas.
        public Task<ValoracionListadoDTO?> ObtenerDetalleAsync(int nroTurno, string? tipoDocumentoOdontologo = null, string? nroDocumentoOdontologo = null) =>
            Consulta(tipoDocumentoOdontologo, nroDocumentoOdontologo).FirstOrDefaultAsync(v => v.NroTurno == nroTurno);

        private static async Task<ConsultaValoracionesResultDTO> ConsultarAsync(IQueryable<ValoracionListadoDTO> consulta)
        {
            var lista = await consulta.ToListAsync();
            lista = lista.OrderByDescending(v => v.FechaYHoraAtencionInicio).ToList();
            return new ConsultaValoracionesResultDTO
            {
                Valoraciones = lista,
                Mensaje = lista.Count == 0 ? MensajeSinValoraciones : null // Alt 1.a.1
            };
        }

        private IQueryable<ValoracionListadoDTO> Consulta(string? tipoDocumento, string? nroDocumento)
        {
            var q = _contexto.Valoraciones.AsNoTracking().AsQueryable();
            if (nroDocumento != null)
                q = q.Where(v => v.Atencion.Turno.TipoDocumentoOdontologo == tipoDocumento &&
                                 v.Atencion.Turno.NroDocumentoOdontologo == nroDocumento);
            return q.Select(v => new ValoracionListadoDTO
            {
                NroTurno = v.Atencion.NroTurno,
                FechaYHoraAtencionInicio = v.Atencion.FechaHoraAtencionInicio,
                NombreEspecialidad = v.Atencion.Turno.Especialidad.Nombre,
                NombrePaciente = v.Atencion.Turno.Paciente.Nombre,
                ApellidoPaciente = v.Atencion.Turno.Paciente.Apellido,
                NombreOdontologo = v.Atencion.Turno.Odontologo.Nombre,
                ApellidoOdontologo = v.Atencion.Turno.Odontologo.Apellido,
                Calificacion = v.Calificacion,
                Observaciones = v.Observaciones
            });
        }
    }
}
