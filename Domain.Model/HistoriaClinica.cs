namespace Domain.Model
{
    public class HistoriaClinica
    {
        public int NroHC { get; private set; }
        public DateTime FechaCreacion { get; private set; }

        public string TipoDocumentoPaciente { get; private set; } = "DNI";
        public string NroDocumentoPaciente { get; private set; } = string.Empty;
        public virtual Paciente Paciente { get; private set; } = null!;

        public string? AntecedentesMedicos { get; set; }
        public string? Alergias { get; set; }
        public string? ObservacionesGeneral { get; set; }

        public virtual ICollection<AtencionOdontologica> Atenciones { get; private set; } = new List<AtencionOdontologica>();

        // Helpers para compatibilidad
        public string PacienteTipoDoc => TipoDocumentoPaciente;
        public string PacienteNroDoc => NroDocumentoPaciente;

        protected HistoriaClinica() { }

        public HistoriaClinica(
            int nroHC,
            string tipoDocumentoPaciente,
            string nroDocumentoPaciente,
            DateTime fechaCreacion,
            string? antecedentes = null,
            string? alergias = null,
            string? observaciones = null)
        {
            SetNroHC(nroHC);
            TipoDocumentoPaciente = tipoDocumentoPaciente?.ToUpper().Trim() ?? "DNI";
            NroDocumentoPaciente = nroDocumentoPaciente?.Trim() ?? string.Empty;
            SetFechaCreacion(fechaCreacion);
            AntecedentesMedicos = antecedentes;
            Alergias = alergias;
            ObservacionesGeneral = observaciones;
        }

        public HistoriaClinica(
            int nroHC,
            string pacienteTipoDoc,
            int pacienteNroDoc,
            DateTime fechaCreacion,
            string? antecedentes = null,
            string? alergias = null,
            string? observaciones = null)
            : this(nroHC, pacienteTipoDoc, pacienteNroDoc.ToString(), fechaCreacion, antecedentes, alergias, observaciones)
        {
        }

        public void SetNroHC(int nroHC)
        {
            if (nroHC < 0)
                throw new ArgumentException("El número de Historia Clínica no puede ser negativo.", nameof(nroHC));
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