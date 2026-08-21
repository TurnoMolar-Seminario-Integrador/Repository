namespace Domain.Model
{
    public class Insumo
    {
        public int CodInsumo { get; private set; }
        public string Nombre { get; private set; } = string.Empty;
        public decimal CostoUnitario { get; private set; }
        public int StockDisponible { get; private set; }

        protected Insumo() { }

        public Insumo(int codInsumo, string nombre, decimal costoUnitario, int stockDisponible)
        {
            CodInsumo = codInsumo;
            SetNombre(nombre);
            SetCostoUnitario(costoUnitario);
            SetStockDisponible(stockDisponible);
        }

        public void SetNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre del insumo no puede ser vacío.", nameof(nombre));
            Nombre = nombre.Trim();
        }

        public void SetCostoUnitario(decimal costo)
        {
            if (costo < 0)
                throw new ArgumentException("El costo unitario no puede ser negativo.", nameof(costo));
            CostoUnitario = costo;
        }

        public void SetStockDisponible(int stock)
        {
            if (stock < 0)
                throw new ArgumentException("El stock disponible no puede ser negativo.", nameof(stock));
            StockDisponible = stock;
        }

        public void DescontarStock(int cantidad)
        {
            if (cantidad <= 0)
                throw new ArgumentException("La cantidad a descontar debe ser mayor a 0.");
            if (cantidad > StockDisponible)
                throw new InvalidOperationException("Stock insuficiente para el insumo " + Nombre);
            StockDisponible -= cantidad;
        }
    }
}