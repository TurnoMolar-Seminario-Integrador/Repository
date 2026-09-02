namespace Domain.Model
{
    public class DetalleInsumoUtilizado
    {
        public int IdAtencion { get; private set; }
        public int IdInsumo { get; private set; }
        public int CantidadUtilizada { get; private set; }
        public decimal CostoUnitarioAlMomento { get; private set; }

        public virtual AtencionOdontologica Atencion { get; private set; } = null!;
        public virtual Insumo Insumo { get; private set; } = null!;

        // Aliases para retrocompatibilidad
        public int CodAtencion => IdAtencion;
        public int CodInsumo => IdInsumo;

        protected DetalleInsumoUtilizado() { }

        public DetalleInsumoUtilizado(int idAtencion, int idInsumo, int cantidadUtilizada, decimal costoUnitarioAlMomento)
        {
            IdAtencion = idAtencion;
            IdInsumo = idInsumo;
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
