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
        Task<HistoriaClinicaDTO?> GetHistoriaClinicaPacienteAsync(int pacienteId);
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
                TurnosPendientes = turnos.Count(t => t.EstadoTurno == TurnoOdontologico.EstadoTurnoEnum.Pendiente),
                TurnosPresentes = turnos.Count(t => t.EstadoTurno == TurnoOdontologico.EstadoTurnoEnum.Presente),
                TurnosAtendidos = turnos.Count(t => t.EstadoTurno == TurnoOdontologico.EstadoTurnoEnum.Atendido),
                TurnosCancelados = turnos.Count(t => t.EstadoTurno == TurnoOdontologico.EstadoTurnoEnum.Cancelado),
                TurnosAusentes = turnos.Count(t => t.EstadoTurno == TurnoOdontologico.EstadoTurnoEnum.NoAsistido),
                DetalleTurnos = turnos.Select(t => new TurnoOdontologicoDTO
                {
                    Id = t.Id,
                    Fecha = t.Fecha,
                    HorarioTurno = t.HorarioTurno,
                    EstadoTurno = t.EstadoTurno.ToString(),
                    MotivoCancelacion = t.MotivoCancelacion,
                    PacienteId = t.PacienteId,
                    PacienteNombre = t.Paciente != null ? $"{t.Paciente.Apellido}, {t.Paciente.Nombre}" : $"Paciente #{t.PacienteId}",
                    OdontologoId = t.OdontologoId,
                    OdontologoNombre = t.Odontologo != null ? $"Dr/a. {t.Odontologo.Apellido}, {t.Odontologo.Nombre}" : $"Odontólogo #{t.OdontologoId}",
                    EspecialidadId = t.EspecialidadId,
                    EspecialidadNombre = t.Especialidad?.Nombre ?? "Odontología",
                    MontoEstimado = t.MontoEstimado
                }).ToList()
            };
        }

        public async Task<ReporteAusentismoDTO> GetReporteAusentismoAsync(DateTime fechaDesde, DateTime fechaHasta)
        {
            var criteria = new TurnoCriteria { FechaDesde = fechaDesde, FechaHasta = fechaHasta };
            var turnos = (await _turnoRepository.GetByCriteriaAsync(criteria)).ToList();

            var multas = (await _multaRepository.GetAllAsync())
                .Where(m => m.FechaEmision.Date >= fechaDesde.Date && m.FechaEmision.Date <= fechaHasta.Date)
                .ToList();

            int totalProgramados = turnos.Count;
            int totalAusencias = turnos.Count(t => t.EstadoTurno == TurnoOdontologico.EstadoTurnoEnum.NoAsistido);
            double porcentaje = totalProgramados > 0 ? ((double)totalAusencias / totalProgramados) * 100 : 0;

            return new ReporteAusentismoDTO
            {
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                TotalTurnosProgramados = totalProgramados,
                TotalAusencias = totalAusencias,
                PorcentajeAusentismo = Math.Round(porcentaje, 2),
                TotalMultasGeneradas = multas.Sum(m => m.Monto),
                TotalMultasCobradas = multas.Where(m => m.EstadoPago).Sum(m => m.Monto)
            };
        }

        public async Task<ReporteFacturacionDTO> GetReporteFacturacionAsync(DateTime fechaDesde, DateTime fechaHasta)
        {
            var facturas = (await _facturaRepository.GetByRangoFechasAsync(fechaDesde, fechaHasta)).ToList();

            return new ReporteFacturacionDTO
            {
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                TotalFacturado = facturas.Sum(f => f.Total),
                TotalCobradoPacientes = facturas.Where(f => f.EstadoPago).Sum(f => f.MontoAPagarPaciente),
                TotalLiquidadoObrasSociales = facturas.Sum(f => f.DescuentoObraSocial),
                CantidadConsultasAtendidas = facturas.Count,
                Facturas = facturas.Select(f => new FacturaDTO
                {
                    Id = f.Id,
                    TurnoId = f.TurnoId,
                    PacienteId = f.PacienteId,
                    PacienteNombre = f.Paciente != null ? $"{f.Paciente.Apellido}, {f.Paciente.Nombre}" : $"Paciente #{f.PacienteId}",
                    ObraSocialNombre = f.Paciente?.ObraSocial != null ? $"{f.Paciente.ObraSocial.Nombre} ({f.Paciente.ObraSocial.Plan})" : "Particular",
                    Descripcion = f.Descripcion,
                    Subtotal = f.Subtotal,
                    DescuentoObraSocial = f.DescuentoObraSocial,
                    Total = f.Total,
                    MontoAPagarPaciente = f.MontoAPagarPaciente,
                    EstadoPago = f.EstadoPago,
                    MetodoPago = f.MetodoPago,
                    FechaEmision = f.FechaEmision
                }).ToList()
            };
        }

        public async Task<HistoriaClinicaDTO?> GetHistoriaClinicaPacienteAsync(int pacienteId)
        {
            var paciente = await _pacienteRepository.GetAsync(pacienteId);
            if (paciente == null) return null;

            var hc = await _historiaClinicaRepository.GetByPacienteIdAsync(pacienteId);
            var consultas = await _consultaRepository.GetByPacienteIdAsync(pacienteId);

            return new HistoriaClinicaDTO
            {
                Id = hc?.Id ?? 0,
                NumeroHistoriaClinica = hc?.NumeroHistoriaClinica ?? (1000 + pacienteId),
                PacienteId = pacienteId,
                PacienteNombre = $"{paciente.Apellido}, {paciente.Nombre}",
                FechaAlta = hc?.FechaAlta ?? DateTime.Today,
                AntecedentesMedicos = hc?.AntecedentesMedicos ?? "Sin antecedentes relevantes registrados",
                Alergias = hc?.Alergias ?? "Ninguna reportada",
                ObservacionesGenerales = hc?.ObservacionesGenerales ?? "Sin observaciones",
                ConsultasPrevias = consultas.Select(c => new ConsultaDTO
                {
                    Id = c.Id,
                    TurnoId = c.TurnoId,
                    PacienteId = pacienteId,
                    PacienteNombre = $"{paciente.Apellido}, {paciente.Nombre}",
                    OdontologoNombre = c.Turno?.Odontologo != null ? $"Dr/a. {c.Turno.Odontologo.Apellido}, {c.Turno.Odontologo.Nombre}" : "Profesional",
                    Diagnostico = c.Diagnostico,
                    Tratamiento = c.Tratamiento,
                    Observaciones = c.Observaciones,
                    AnestesiaLocal = c.AnestesiaLocal,
                    Radiografias = c.Radiografias,
                    Valoracion = c.Valoracion,
                    CalificacionEstrellas = c.CalificacionEstrellas,
                    Fecha = c.Fecha
                }).ToList()
            };
        }
    }
}
