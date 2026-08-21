namespace Domain.Model
{
    public class DetalleInsumoUtilizado
    {
        public int CodInsumo { get; private set; }
        public int CodAtencion { get; private set; }
        public int CantidadUtilizada { get; private set; }
        public decimal CostoUnitarioAlMomento { get; private set; }

        public virtual Insumo Insumo { get; private set; }
        public virtual AtencionOdontologica Atencion { get; private set; }

        protected DetalleInsumoUtilizado() { }

        public DetalleInsumoUtilizado(int codInsumo, int codAtencion, int cantidadUtilizada, decimal costoUnitarioAlMomento)
        {
            CodInsumo = codInsumo;
            CodAtencion = codAtencion;
            SetCantidadUtilizada(cantidadUtilizada);
            SetCostoUnitarioAlMomento(costoUnitarioAlMomento);
        }

        public void SetCantidadUtilizada(int cantidad)
        {
            if (cantidad <= 0)
                throw new ArgumentException("La cantidad utilizada debe ser mayor a 0.", nameof(cantidad));
            CantidadUtilizada = cantidad;
        }

        public void SetCostoUnitarioAlMomento(decimal costo)
        {
            if (costo < 0)
                throw new ArgumentException("El costo unitario al momento no puede ser negativo.", nameof(costo));
            CostoUnitarioAlMomento = costo;
        }
    }
}
