namespace Domain.Model
{
    public class ComprobanteDeTurno
    {
        public int NroComprobante { get; private set; }
        public DateTime FechaHoraEmision { get; private set; }
        public int NroTurno { get; private set; }

        public virtual Turno Turno { get; private set; } = null!;

        // Aliases para compatibilidad
        public int CodTurno => NroTurno;
        public DateTime FechaYHoraEmision => FechaHoraEmision;

        protected ComprobanteDeTurno() { }

        public ComprobanteDeTurno(int nroComprobante, int nroTurno, DateTime fechaHoraEmision)
        {
            NroComprobante = nroComprobante;
            NroTurno = nroTurno;
            FechaHoraEmision = fechaHoraEmision;
        }

        public ComprobanteDeTurno(int nroComprobante, int codTurno, DateTime fechaYHoraReserva, DateTime fechaYHoraEmision)
            : this(nroComprobante, codTurno, fechaYHoraEmision)
        {
        }
    }
}
