namespace DTOs
{
    public class TurnoOdontologicoDTO
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; } = DateTime.MinValue;
        public TimeOnly HorarioTurno { get; set; } = TimeOnly.MinValue;
        public string EstadoTurno { get; set; } = string.Empty;
        public string MotivoCancelacion { get; set; } = string.Empty;
    }
}


