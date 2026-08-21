namespace DTOs
{
    public class FacturaDTO
    {
        public int Id { get; set; }
        public int? TurnoId { get; set; }
        public int PacienteId { get; set; }
        public string? PacienteNombre { get; set; }
        public string? ObraSocialNombre { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal DescuentoObraSocial { get; set; }
        public decimal Total { get; set; }
        public decimal MontoAPagarPaciente { get; set; }
        public bool EstadoPago { get; set; }
        public string MetodoPago { get; set; } = "Efectivo";
        public DateTime FechaEmision { get; set; } = DateTime.Now;
        public List<ItemFacturaDTO> Items { get; set; } = new();
    }

    public class ItemFacturaDTO
    {
        public int Id { get; set; }
        public int FacturaId { get; set; }
        public int InsumoId { get; set; }
        public string? InsumoNombre { get; set; }
        public int CantidadInsumo { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal => CantidadInsumo * PrecioUnitario;
    }
}
