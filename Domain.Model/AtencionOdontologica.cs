namespace Domain.Model
{
    public class AtencionOdontologica
    {
        public int IdAtencion { get; private set; }
        public DateTime FechaHoraAtencionInicio { get; private set; }
        public DateTime FechaHoraAtencionFin { get; private set; }
        public string Observaciones { get; set; } = string.Empty;
        public decimal ArancelAplicado { get; private set; }

        public int NroTurno { get; private set; }
        public virtual Turno Turno { get; private set; } = null!;

        public int NroHC { get; private set; }
        public virtual HistoriaClinica HistoriaClinica { get; private set; } = null!;

        public virtual Valoracion? Valoracion { get; private set; }
        public virtual ICollection<DetalleInsumoUtilizado> DetallesInsumos { get; private set; } = new List<DetalleInsumoUtilizado>();

        // Aliases para retrocompatibilidad
        public int CodAtencion => IdAtencion;
        public DateTime FechaYHoraAtencionInicio => FechaHoraAtencionInicio;
        public DateTime FechaYHoraAtencionFin => FechaHoraAtencionFin;
        public int CodTurno => NroTurno;
        public string PacienteTipoDoc => Turno?.TipoDocumentoPaciente ?? "DNI";
        public string PacienteNroDoc => Turno?.NroDocumentoPaciente ?? "";

        public TimeSpan DuracionReal => FechaHoraAtencionFin - FechaHoraAtencionInicio;

        public decimal MontoTotal
        {
            get
            {
                decimal arancelBase = ArancelAplicado > 0 ? ArancelAplicado : (Turno?.Especialidad?.ArancelParticular ?? 0m);
                decimal insumosTotal = DetallesInsumos?.Sum(d => d.CantidadUtilizada * d.CostoUnitarioAlMomento) ?? 0m;
                return arancelBase + insumosTotal;
            }
        }

        protected AtencionOdontologica() { }

        public AtencionOdontologica(
            int idAtencion,
            DateTime fechaInicio,
            DateTime fechaFin,
            string observaciones,
            decimal arancelAplicado,
            int nroTurno,
            int nroHC)
        {
            IdAtencion = idAtencion;
            SetHorarioAtencion(fechaInicio, fechaFin);
            Observaciones = observaciones ?? string.Empty;
            ArancelAplicado = arancelAplicado;
            NroTurno = nroTurno;
            NroHC = nroHC;
        }

        public AtencionOdontologica(
            int codAtencion,
            DateTime fechaInicio,
            DateTime fechaFin,
            string observaciones,
            int codTurno,
            DateTime fechaYHoraReserva,
            int nroHC,
            string pacienteTipoDoc,
            int pacienteNroDoc,
            decimal arancelAplicado = 0m)
            : this(codAtencion, fechaInicio, fechaFin, observaciones, arancelAplicado, codTurno, nroHC)
        {
        }

        public void SetHorarioAtencion(DateTime inicio, DateTime fin)
        {
            if (fin <= inicio)
                throw new ArgumentException("La fecha/hora de fin de atención debe ser mayor a la de inicio.");
            FechaHoraAtencionInicio = inicio;
            FechaHoraAtencionFin = fin;
        }
    }
}
