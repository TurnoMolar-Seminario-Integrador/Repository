namespace Domain.Model
{
    public class TurnoCriteria
    {
        public DateTime? Fecha { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public int? OdontologoId { get; set; }
        public int? PacienteId { get; set; }
        public int? EspecialidadId { get; set; }
        public string? EstadoTurno { get; set; }

        public TurnoCriteria() { }
    }
}
