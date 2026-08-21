namespace Domain.Model
{
    public class AtencionOdontologica
    {
        public int CodAtencion { get; private set; }
        public DateTime FechaYHoraAtencionInicio { get; private set; }
        public DateTime FechaYHoraAtencionFin { get; private set; }
        public string Observaciones { get; set; } = string.Empty;

        // Foreign keys según DER
        public int CodTurno { get; private set; }
        public DateTime FechaYHoraReserva { get; private set; }
        public virtual Turno Turno { get; private set; }

        public int NroHC { get; private set; }
        public string PacienteTipoDoc { get; private set; } = "DNI";
        public int PacienteNroDoc { get; private set; }
        public virtual HistoriaClinica HistoriaClinica { get; private set; }

        // Navigations
        public virtual Valoracion? Valoracion { get; private set; }
        public virtual Pago? Pago { get; private set; }
        public virtual ICollection<DetalleInsumoUtilizado> DetallesInsumos { get; private set; } = new List<DetalleInsumoUtilizado>();

        // Derived Attributes
        public TimeSpan DuracionReal => FechaYHoraAtencionFin - FechaYHoraAtencionInicio;

        public decimal MontoTotal
        {
            get
            {
                decimal arancelBase = Turno?.Especialidad?.ArancelParticular ?? 0m;
                decimal insumosTotal = DetallesInsumos?.Sum(d => d.CantidadUtilizada * d.CostoUnitarioAlMomento) ?? 0m;
                return arancelBase + insumosTotal;
            }
        }

        protected AtencionOdontologica() { }

        public AtencionOdontologica(int codAtencion, DateTime fechaInicio, DateTime fechaFin, string observaciones, int codTurno, DateTime fechaYHoraReserva, int nroHC, string pacienteTipoDoc, int pacienteNroDoc)
        {
            CodAtencion = codAtencion;
            SetHorarioAtencion(fechaInicio, fechaFin);
            Observaciones = observaciones ?? string.Empty;
            CodTurno = codTurno;
            FechaYHoraReserva = fechaYHoraReserva;
            NroHC = nroHC;
            PacienteTipoDoc = pacienteTipoDoc;
            PacienteNroDoc = pacienteNroDoc;
        }

        public void SetHorarioAtencion(DateTime inicio, DateTime fin)
        {
            if (fin <= inicio)
                throw new ArgumentException("La fecha/hora de fin de atención debe ser mayor a la de inicio.");
            FechaYHoraAtencionInicio = inicio;
            FechaYHoraAtencionFin = fin;
        }
    }
}
