namespace Domain.Model
{
    public class ObraSocial
    {
        public int IdentificadorOS { get; private set; }
        public string NombreOS { get; private set; } = string.Empty;
        public string PlanCobertura { get; private set; } = string.Empty;
        public decimal ArancelOS { get; private set; }
        public string EstadoOS { get; private set; } = "ACTIVA";

        protected ObraSocial() { }

        public ObraSocial(int identificadorOS, string nombreOS, string planCobertura, decimal arancelOS, string estadoOS = "ACTIVA")
        {
            IdentificadorOS = identificadorOS;
            SetNombreOS(nombreOS);
            SetPlanCobertura(planCobertura);
            SetArancelOS(arancelOS);
            SetEstadoOS(estadoOS);
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

        public void SetArancelOS(decimal arancel)
        {
            if (arancel < 0)
                throw new ArgumentException("El arancel de Obra Social no puede ser negativo.", nameof(arancel));
            ArancelOS = arancel;
        }

        public void SetEstadoOS(string estado)
        {
            EstadoOS = estado?.ToUpper().Trim() ?? "ACTIVA";
        }
    }
}