namespace Domain.Model
{
    public class Pago
    {
        public int CodPago { get; private set; }
        public int CodAtencion { get; private set; }
        public DateTime FechaYHoraPago { get; private set; }
        public decimal Monto { get; private set; }
        public string TipoMetodoPago { get; private set; } = "EFECTIVO";
        public decimal? AportePaciente { get; private set; }
        public decimal? AporteObraSocial { get; private set; }

        public virtual AtencionOdontologica Atencion { get; private set; }

        public string ResponsablePago => AporteObraSocial.HasValue && AporteObraSocial.Value > 0 ? "Obra Social" : "Particular";

        protected Pago() { }

        public Pago(int codPago, int codAtencion, DateTime fechaYHoraPago, decimal monto, string tipoMetodoPago, decimal? aportePaciente = null, decimal? aporteObraSocial = null)
        {
            CodPago = codPago;
            CodAtencion = codAtencion;
            FechaYHoraPago = fechaYHoraPago;
            SetMonto(monto);
            SetTipoMetodoPago(tipoMetodoPago);
            AportePaciente = aportePaciente;
            AporteObraSocial = aporteObraSocial;
        }

        public void SetMonto(decimal monto)
        {
            if (monto < 0)
                throw new ArgumentException("El monto del pago no puede ser negativo.", nameof(monto));
            Monto = monto;
        }

        public void SetTipoMetodoPago(string tipoMetodoPago)
        {
            if (string.IsNullOrWhiteSpace(tipoMetodoPago))
                throw new ArgumentException("El tipo de método de pago es requerido.", nameof(tipoMetodoPago));
            TipoMetodoPago = tipoMetodoPago.ToUpper().Trim();
        }
    }
}
