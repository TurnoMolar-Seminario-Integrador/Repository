namespace DTOs
{
    public class FacturaDTO
    {
        public int CodPago { get; set; }
        public int Id { get => CodPago; set => CodPago = value; }
        public int? CodAtencion { get; set; }
        public int? TurnoId { get => CodAtencion; set => CodAtencion = value; }
        public string PacienteTipoDoc { get; set; } = "DNI";
        public int PacienteNroDoc { get; set; }
        public int PacienteId { get => PacienteNroDoc; set => PacienteNroDoc = value; }
        public string? PacienteNombre { get; set; }
        public string? ObraSocialNombre { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public decimal Total { get => Monto; set => Monto = value; }
        public decimal Subtotal { get => Monto; set => Monto = value; }
        public decimal DescuentoObraSocial { get; set; }
        public decimal MontoAPagarPaciente { get => Monto; set => Monto = value; }
        public bool EstadoPago { get; set; } = true;
        public string TipoMetodoPago { get; set; } = "EFECTIVO";
        public string MetodoPago { get => TipoMetodoPago; set => TipoMetodoPago = value; }
        public DateTime FechaYHoraPago { get; set; } = DateTime.Now;
        public DateTime FechaEmision { get => FechaYHoraPago; set => FechaYHoraPago = value; }
        public List<ItemFacturaDTO> Items { get; set; } = new();
    }
}
