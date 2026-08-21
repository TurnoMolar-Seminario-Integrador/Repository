namespace DTOs
{
    public class MultaDTO
    {
        public int Id { get; set; }
        public string PacienteTipoDoc { get; set; } = "DNI";
        public int PacienteNroDoc { get; set; }
        public int PacienteId { get => PacienteNroDoc; set => PacienteNroDoc = value; }
        public string? PacienteNombre { get; set; }
        public float Monto { get; set; }
        public bool EstadoPago { get; set; }
        public DateTime FechaPago { get; set; }
        public DateTime FechaEmision { get; set; } = DateTime.Now;
        public string Motivo { get; set; } = "Ausencia no justificada";
    }
}
