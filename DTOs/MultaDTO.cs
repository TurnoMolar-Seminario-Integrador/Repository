namespace DTOs
{
    public class MultaDTO
    {
        public int Id { get; set; }
        public int PacienteId { get; set; }
        public string? PacienteNombre { get; set; }
        public decimal Monto { get; set; }
        public bool EstadoPago { get; set; }
        public DateTime FechaEmision { get; set; } = DateTime.Now;
        public DateTime? FechaPago { get; set; }
        public string Motivo { get; set; } = "Ausencia no justificada";
    }
}
