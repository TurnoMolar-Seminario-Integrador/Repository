namespace Domain.Model
{
    public class Pago
    {
        public int IdPago { get; private set; }
        public DateTime FechaHoraPago { get; private set; }
        public decimal Monto { get; private set; }
        public string MetodoPago { get; private set; } = "EFECTIVO";
        public decimal? AportePaciente { get; private set; }
        public decimal? AporteObraSocial { get; private set; }

        public int NroTurno { get; private set; }
        public virtual Turno Turno { get; private set; } = null!;

        public string? IdentificadorOS { get; private set; }
        public virtual ObraSocial? ObraSocial { get; private set; }

        // Aliases para retrocompatibilidad
        public int CodPago => IdPago;
        public DateTime FechaYHoraPago => FechaHoraPago;
        public string TipoMetodoPago => MetodoPago;
        public int? CodAtencion => Turno?.Atencion?.IdAtencion;
        public string ResponsablePago => AporteObraSocial.HasValue && AporteObraSocial.Value > 0 ? "Obra Social" : "Particular";

        protected Pago() { }

        public Pago(
            int idPago,
            int nroTurno,
            DateTime fechaHoraPago,
            decimal monto,
            string metodoPago,
            string? identificadorOS = null,
            decimal? aportePaciente = null,
            decimal? aporteObraSocial = null)
        {
            IdPago = idPago;
            NroTurno = nroTurno;
            FechaHoraPago = fechaHoraPago;
            SetMonto(monto);
            SetMetodoPago(metodoPago);
            IdentificadorOS = string.IsNullOrWhiteSpace(identificadorOS) ? null : identificadorOS.Trim();
            AportePaciente = aportePaciente;
            AporteObraSocial = aporteObraSocial;
        }

        public void SetMonto(decimal monto)
        {
            if (monto < 0)
                throw new ArgumentException("El monto del pago no puede ser negativo.", nameof(monto));
            Monto = monto;
        }

        public void SetMetodoPago(string metodoPago)
        {
            if (string.IsNullOrWhiteSpace(metodoPago))
                throw new ArgumentException("El método de pago es requerido.", nameof(metodoPago));
            MetodoPago = metodoPago.ToUpper().Trim();
        }

        public void SetTipoMetodoPago(string tipoMetodoPago) => SetMetodoPago(tipoMetodoPago);
    }
}
