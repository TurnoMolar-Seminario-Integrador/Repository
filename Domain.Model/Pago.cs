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
        // MD - Modelo del Dominio: "/responsablePago = modalidadPagoElegida del Turno que da
        // origen a la atención pagada."
        //
        // Defecto corregido: esto se calculaba a partir de AporteObraSocial (>0 => "Obra
        // Social"), que no siempre coincide con la modalidad realmente elegida en el turno.
        public string ResponsablePago => Turno.ModalidadPagoElegida;

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
            SetAportes(aportePaciente, aporteObraSocial);
        }

        public void SetMonto(decimal monto)
        {
            if (monto < 0)
                throw new ArgumentException("El monto del pago no puede ser negativo.", nameof(monto));
            Monto = monto;
        }

        // MD: aportePaciente [0..1] y aporteObraSocial [0..1]. Un aporte negativo no tiene sentido
        // (antes pasaba si la suma daba el total: -5.000 + 45.000).
        private void SetAportes(decimal? aportePaciente, decimal? aporteObraSocial)
        {
            if (aportePaciente < 0)
                throw new ArgumentException("El aporte del paciente no puede ser negativo.", nameof(aportePaciente));
            if (aporteObraSocial < 0)
                throw new ArgumentException("El aporte de la obra social no puede ser negativo.", nameof(aporteObraSocial));

            AportePaciente = aportePaciente;
            AporteObraSocial = aporteObraSocial;
        }

        public void SetMetodoPago(string metodoPago)
        {
            if (string.IsNullOrWhiteSpace(metodoPago))
                throw new ArgumentException("El método de pago es requerido.", nameof(metodoPago));

            // Lista cerrada (ver MetodosPago): antes se guardaba cualquier texto, en mayúsculas.
            MetodoPago = MetodosPago.Normalizar(metodoPago)
                ?? throw new ArgumentException(
                    "El método de pago indicado no es válido. Las opciones son Efectivo, Tarjeta de débito, Tarjeta de crédito o Transferencia.",
                    nameof(metodoPago));
        }

        public void SetTipoMetodoPago(string tipoMetodoPago) => SetMetodoPago(tipoMetodoPago);
    }
}
