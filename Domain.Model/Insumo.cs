namespace Domain.Model
{
    public class Insumo
    {
        public int IdInsumo { get; private set; }
        public int CodInsumo
        {
            get => IdInsumo;
            private set => IdInsumo = value;
        }

        public string Nombre { get; private set; } = string.Empty;
        public decimal CostoUnitario { get; private set; }
        public int StockDisponible { get; private set; }

        // CUF09 alt 3.c / 3.d: estadoInsumo(d) = [ "Habilitado" | "Deshabilitado" ].
        public string EstadoInsumo { get; private set; } = EstadosInsumo.Habilitado;
        public bool EstaHabilitado => EstadosInsumo.EsHabilitado(EstadoInsumo);

        public virtual ICollection<DetalleInsumoUtilizado> DetallesInsumos { get; private set; } = new List<DetalleInsumoUtilizado>();

        protected Insumo() { }

        public Insumo(int idInsumo, string nombre, decimal costoUnitario, int stockDisponible)
        {
            IdInsumo = idInsumo;
            SetNombre(nombre);
            SetCostoUnitario(costoUnitario);
            SetStockDisponible(stockDisponible);
        }

        public Insumo(string nombre, decimal costoUnitario, int stockDisponible)
        {
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

        // CUF09 alt 3.a.1: "El sistema suma la cantidad ingresada al stock disponible e informa el nuevo total."
        public void ReponerStock(int cantidadIncorporada)
        {
            if (cantidadIncorporada <= 0)
                throw new ArgumentException("La cantidad a incorporar debe ser mayor a 0.", nameof(cantidadIncorporada));
            StockDisponible += cantidadIncorporada;
        }

        // CUF09 alt 3.c.1: deja de ofrecerse en las atenciones, conservando su stock y su historial.
        public void Deshabilitar() => EstadoInsumo = EstadosInsumo.Deshabilitado;

        // CUF09 alt 3.d.1: vuelve a ofrecerse en las atenciones.
        public void Rehabilitar() => EstadoInsumo = EstadosInsumo.Habilitado;

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