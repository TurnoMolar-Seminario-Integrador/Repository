namespace Domain.Model
{
    public class HistoriaClinica
    {
        public int NroHC { get; private set; }
        public DateTime FechaCreacion { get; private set; }

        public string PacienteTipoDoc { get; private set; } = "DNI";
        public int PacienteNroDoc { get; private set; }
        public virtual Paciente Paciente { get; private set; }

        public string? AntecedentesMedicos { get; set; }
        public string? Alergias { get; set; }
        public string? ObservacionesGeneral { get; set; }

        protected HistoriaClinica() { }

        public HistoriaClinica(int nroHC, string pacienteTipoDoc, int pacienteNroDoc, DateTime fechaCreacion, string? antecedentes = null, string? alergias = null, string? observaciones = null)
        {
            SetNroHC(nroHC);
            PacienteTipoDoc = pacienteTipoDoc?.ToUpper().Trim() ?? "DNI";
            PacienteNroDoc = pacienteNroDoc;
            SetFechaCreacion(fechaCreacion);
            AntecedentesMedicos = antecedentes;
            Alergias = alergias;
            ObservacionesGeneral = observaciones;
        }

        public void SetNroHC(int nroHC)
        {
            if (nroHC <= 0)
                throw new ArgumentException("El número de Historia Clínica debe ser mayor que 0.", nameof(nroHC));
            NroHC = nroHC;
        }

        public void SetFechaCreacion(DateTime fecha)
        {
            if (fecha > DateTime.Now)
                throw new ArgumentException("La fecha de creación no puede ser futura.", nameof(fecha));
            FechaCreacion = fecha;
        }
    }
}