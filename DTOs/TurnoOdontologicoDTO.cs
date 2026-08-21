namespace DTOs
{
    public class TurnoOdontologicoDTO
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; } = DateTime.MinValue;
        public TimeOnly HorarioTurno { get; set; } = TimeOnly.MinValue;
        public string EstadoTurno { get; set; } = "RESERVADO";
        public string? MotivoCancelacion { get; set; }
        public string? ModalidadPago { get; set; } = "PARTICULAR";
        public int? CodEspecialidad { get; set; }
        public int? OdontologoNroDoc { get; set; }
        public int? PacienteNroDoc { get; set; }
        public string? NombrePaciente { get; set; }
        public string? NombreOdontologo { get; set; }
        public string? NombreEspecialidad { get; set; }
        public DateTime? FechaCancelacion { get; set; }
        public DateTime? FechaSolicitudReprogramacion { get; set; }
    }
}
