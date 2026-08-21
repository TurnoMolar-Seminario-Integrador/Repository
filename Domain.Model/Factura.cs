namespace Domain.Model
{
    // Alias / Compatibilidad con código previo
    public class Factura : Pago
    {
        public int Id => CodPago;
        public decimal Subtotal => Monto;
        public decimal Total => Monto;

        public Factura() : base() { }

        public Factura(int id, int codAtencion, DateTime fecha, decimal monto, string metodoPago)
            : base(id, codAtencion, fecha, monto, metodoPago)
        {
        }
    }
}