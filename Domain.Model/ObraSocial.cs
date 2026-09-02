namespace Domain.Model
{
    public class ObraSocial
    {
        public string IdentificadorOS { get; private set; } = string.Empty;
        public string NombreOS { get; private set; } = string.Empty;
        public string PlanCobertura { get; private set; } = string.Empty;
        public string EstadoOS { get; private set; } = "ACTIVA";

        // Helper para compatibilidad de vistas
        public decimal ArancelOS => Convenios.FirstOrDefault()?.ArancelConvenio ?? 0m;

        public virtual ICollection<Convenio> Convenios { get; private set; } = new List<Convenio>();
        public virtual ICollection<Paciente> Pacientes { get; private set; } = new List<Paciente>();
        public virtual ICollection<Pago> Pagos { get; private set; } = new List<Pago>();

        protected ObraSocial() { }

        public ObraSocial(string identificadorOS, string nombreOS, string planCobertura = "", string estadoOS = "ACTIVA")
        {
            SetIdentificadorOS(identificadorOS);
            SetNombreOS(nombreOS);
            SetPlanCobertura(planCobertura);
            SetEstadoOS(estadoOS);
        }

        public ObraSocial(string identificadorOS, string nombreOS, string planCobertura, decimal arancelOS, string estadoOS = "ACTIVA")
            : this(identificadorOS, nombreOS, planCobertura, estadoOS)
        {
        }

        public void SetIdentificadorOS(string identificadorOS)
        {
            if (string.IsNullOrWhiteSpace(identificadorOS))
                throw new ArgumentException("El identificador de la Obra Social es requerido.", nameof(identificadorOS));
            IdentificadorOS = identificadorOS.Trim();
        }

        public void SetNombreOS(string nombreOS)
        {
            if (string.IsNullOrWhiteSpace(nombreOS))
                throw new ArgumentException("El nombre de la Obra Social es requerido.", nameof(nombreOS));
            NombreOS = nombreOS.Trim();
        }

        public void SetPlanCobertura(string planCobertura)
        {
            PlanCobertura = planCobertura?.Trim() ?? string.Empty;
        }

        public void SetEstadoOS(string estado)
        {
            EstadoOS = estado?.ToUpper().Trim() ?? "ACTIVA";
        }

        public void SetArancelOS(decimal arancel)
        {
            // Helper de compatibilidad
        }
    }
}