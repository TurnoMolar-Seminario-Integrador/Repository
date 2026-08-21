namespace DTOs
{
    public class ReporteTurnosDiaDTO
    {
        public DateTime Fecha { get; set; }
        public int TotalTurnos { get; set; }
        public int TurnosPendientes { get; set; }
        public int TurnosPresentes { get; set; }
        public int TurnosAtendidos { get; set; }
        public int TurnosCancelados { get; set; }
        public int TurnosAusentes { get; set; }
        public List<TurnoOdontologicoDTO> DetalleTurnos { get; set; } = new();
    }

    public class ReporteAusentismoDTO
    {
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
        public int TotalTurnosProgramados { get; set; }
        public int TotalAusencias { get; set; }
        public double PorcentajeAusentismo { get; set; }
        public decimal TotalMultasGeneradas { get; set; }
        public decimal TotalMultasCobradas { get; set; }
    }

    public class ReporteFacturacionDTO
    {
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
        public decimal TotalFacturado { get; set; }
        public decimal TotalCobradoPacientes { get; set; }
        public decimal TotalLiquidadoObrasSociales { get; set; }
        public int CantidadConsultasAtendidas { get; set; }
        public List<FacturaDTO> Facturas { get; set; } = new();
    }

    public class HistoriaClinicaDTO
    {
        public int Id { get; set; }
        public int NumeroHistoriaClinica { get; set; }
        public int PacienteId { get; set; }
        public string PacienteNombre { get; set; } = string.Empty;
        public DateTime FechaAlta { get; set; }
        public string AntecedentesMedicos { get; set; } = string.Empty;
        public string Alergias { get; set; } = string.Empty;
        public string ObservacionesGenerales { get; set; } = string.Empty;
        public List<ConsultaDTO> ConsultasPrevias { get; set; } = new();
    }
}
