namespace DTOs
{
    public class InsumoDTO
    {
        public int CodInsumo { get; set; }
        public int Id { get => CodInsumo; set => CodInsumo = value; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal CostoUnitario { get; set; }
        public decimal Precio { get => CostoUnitario; set => CostoUnitario = value; }
        public int StockDisponible { get; set; }
        public int Stock { get => StockDisponible; set => StockDisponible = value; }
    }
}
