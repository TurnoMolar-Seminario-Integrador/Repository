using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    public interface IReportesService
    {
        Task<ReporteTurnosDiaDTO> GetReporteTurnosDiaAsync(DateTime fecha);
        Task<ReporteAusentismoDTO> GetReporteAusentismoAsync(DateTime fechaDesde, DateTime fechaHasta);
        Task<ReporteFacturacionDTO> GetReporteFacturacionAsync(DateTime fechaDesde, DateTime fechaHasta);
        Task<HistoriaClinicaDTO?> GetHistoriaClinicaPacienteAsync(string tipoDoc, int nroDoc);
    }

    public class ReportesService : IReportesService
    {
        private readonly ITurnoRepository _turnoRepository;
        private readonly IFacturaRepository _facturaRepository;
        private readonly IMultaRepository _multaRepository;
        private readonly IConsultaRepository _consultaRepository;
        private readonly IPacienteRepository _pacienteRepository;
        private readonly IHistoriaClinicaRepository _historiaClinicaRepository;

        public ReportesService(
            ITurnoRepository turnoRepository,
            IFacturaRepository facturaRepository,
            IMultaRepository multaRepository,
            IConsultaRepository consultaRepository,
            IPacienteRepository pacienteRepository,
            IHistoriaClinicaRepository historiaClinicaRepository)
        {
            _turnoRepository = turnoRepository;
            _facturaRepository = facturaRepository;
            _multaRepository = multaRepository;
            _consultaRepository = consultaRepository;
            _pacienteRepository = pacienteRepository;
            _historiaClinicaRepository = historiaClinicaRepository;
        }

        public async Task<ReporteTurnosDiaDTO> GetReporteTurnosDiaAsync(DateTime fecha)
        {
            var turnos = (await _turnoRepository.GetByFechaAsync(fecha)).ToList();

            return new ReporteTurnosDiaDTO
            {
                Fecha = fecha.Date,
                TotalTurnos = turnos.Count,
                TurnosPendientes = turnos.Count(t => t.EstadoTurno == "RESERVADO"),
                TurnosPresentes = turnos.Count(t => t.EstadoTurno == "CONFIRMADO"),
                TurnosAtendidos = turnos.Count(t => t.EstadoTurno == "ATENDIDO"),
                TurnosCancelados = turnos.Count(t => t.EstadoTurno == "CANCELADO"),
                TurnosAusentes = turnos.Count(t => t.EstadoTurno == "AUSENTE"),
                DetalleTurnos = turnos.Select(t => new TurnoOdontologicoDTO
                {
                    Id = t.NroTurno,
                    Fecha = t.FechaHoraTurno.Date,
                    HorarioTurno = TimeOnly.FromDateTime(t.FechaHoraTurno),
                    EstadoTurno = t.EstadoTurno,
                    MotivoCancelacion = t.MotivoCancelacion,
                    PacienteNroDoc = int.TryParse(t.NroDocumentoPaciente, out var pacDoc) ? pacDoc : 0,
                    NombrePaciente = t.Paciente != null ? $"{t.Paciente.Apellido}, {t.Paciente.Nombre}" : $"Paciente #{t.NroDocumentoPaciente}",
                    OdontologoNroDoc = int.TryParse(t.NroDocumentoOdontologo, out var docDoc) ? docDoc : 0,
                    NombreOdontologo = t.Odontologo != null ? $"Dr/a. {t.Odontologo.Apellido}, {t.Odontologo.Nombre}" : $"Odontólogo #{t.NroDocumentoOdontologo}",
                    CodEspecialidad = t.IdEspecialidad,
                    NombreEspecialidad = t.Especialidad?.Nombre ?? "Odontología"
                }).ToList()
            };
        }

        public async Task<ReporteAusentismoDTO> GetReporteAusentismoAsync(DateTime fechaDesde, DateTime fechaHasta)
        {
            var turnos = (await _turnoRepository.GetAllAsync())
                .Where(t => t.FechaHoraTurno.Date >= fechaDesde.Date && t.FechaHoraTurno.Date <= fechaHasta.Date)
                .ToList();

            var multas = (await _multaRepository.GetAllAsync())
                .Where(m => m.FechaPago.Date >= fechaDesde.Date && m.FechaPago.Date <= fechaHasta.Date)
                .ToList();

            int totalProgramados = turnos.Count;
            int totalAusencias = turnos.Count(t => t.EstadoTurno == "AUSENTE");
            double porcentaje = totalProgramados > 0 ? ((double)totalAusencias / totalProgramados) * 100 : 0;

            return new ReporteAusentismoDTO
            {
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                TotalTurnosProgramados = totalProgramados,
                TotalAusencias = totalAusencias,
                PorcentajeAusentismo = Math.Round(porcentaje, 2),
                TotalMultasGeneradas = (decimal)multas.Sum(m => m.Monto),
                TotalMultasCobradas = (decimal)multas.Where(m => m.EstadoPago).Sum(m => m.Monto)
            };
        }

        public async Task<ReporteFacturacionDTO> GetReporteFacturacionAsync(DateTime fechaDesde, DateTime fechaHasta)
        {
            var pagos = (await _facturaRepository.GetByRangoFechasAsync(fechaDesde, fechaHasta)).ToList();

            return new ReporteFacturacionDTO
            {
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                TotalFacturado = pagos.Sum(p => p.Monto),
                TotalCobradoPacientes = pagos.Sum(p => p.Monto),
                TotalLiquidadoObrasSociales = 0m,
                CantidadConsultasAtendidas = pagos.Count,
                Facturas = pagos.Select(p => new FacturaDTO
                {
                    CodPago = p.CodPago,
                    CodAtencion = p.CodAtencion,
                    Monto = p.Monto,
                    TipoMetodoPago = p.TipoMetodoPago,
                    FechaYHoraPago = p.FechaYHoraPago
                }).ToList()
            };
        }

        public async Task<HistoriaClinicaDTO?> GetHistoriaClinicaPacienteAsync(string tipoDoc, int nroDoc)
        {
            var paciente = await _pacienteRepository.GetAsync(nroDoc);
            if (paciente == null) return null;

            var hc = await _historiaClinicaRepository.GetByPacienteDocAsync(tipoDoc, nroDoc);
            var atenciones = await _consultaRepository.GetByPacienteDocAsync(tipoDoc, nroDoc);

            return new HistoriaClinicaDTO
            {
                Id = hc?.NroHC ?? 0,
                NumeroHistoriaClinica = hc?.NroHC ?? (1000 + nroDoc),
                PacienteId = nroDoc,
                PacienteNombre = $"{paciente.Apellido}, {paciente.Nombre}",
                FechaAlta = hc?.FechaCreacion ?? DateTime.Today,
                AntecedentesMedicos = hc?.AntecedentesMedicos ?? "Sin antecedentes relevantes registrados",
                Alergias = hc?.Alergias ?? "Ninguna reportada",
                ObservacionesGenerales = hc?.ObservacionesGeneral ?? "Sin observaciones",
                ConsultasPrevias = atenciones.Select(a => new ConsultaDTO
                {
                    CodAtencion = a.CodAtencion,
                    CodTurno = a.CodTurno,
                    PacienteTipoDoc = a.PacienteTipoDoc,
                    PacienteNroDoc = int.TryParse(a.PacienteNroDoc, out var doc) ? doc : 0,
                    PacienteNombre = $"{paciente.Apellido}, {paciente.Nombre}",
                    OdontologoNombre = a.Turno?.Odontologo != null ? $"Dr/a. {a.Turno.Odontologo.Apellido}, {a.Turno.Odontologo.Nombre}" : "Profesional",
                    Observaciones = a.Observaciones,
                    FechaYHoraAtencionInicio = a.FechaYHoraAtencionInicio,
                    FechaYHoraAtencionFin = a.FechaYHoraAtencionFin
                }).ToList()
            };
        }
    }
}
