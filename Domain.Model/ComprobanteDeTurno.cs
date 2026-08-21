namespace Domain.Model
{
    public class ComprobanteDeTurno
    {
        public int NroComprobante { get; private set; }
        public int CodTurno { get; private set; }
        public DateTime FechaYHoraReserva { get; private set; }
        public DateTime FechaYHoraEmision { get; private set; }

        public virtual Turno Turno { get; private set; }

        protected ComprobanteDeTurno() { }

        public ComprobanteDeTurno(int nroComprobante, int codTurno, DateTime fechaYHoraReserva, DateTime fechaYHoraEmision)
        {
            NroComprobante = nroComprobante;
            CodTurno = codTurno;
            FechaYHoraReserva = fechaYHoraReserva;
            FechaYHoraEmision = fechaYHoraEmision;
        }
    }
}
