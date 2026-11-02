namespace Domain.Model
{
    public class ObraSocial
    {
        public string IdentificadorOS { get; private set; } = string.Empty;
        public string NombreOS { get; private set; } = string.Empty;
        public string PlanCobertura { get; private set; } = string.Empty;
        public string EstadoOS { get; private set; } = EstadosObraSocial.Disponible;

        // MU: el paciente particular tiene IdentificadorOS nulo. "PARTICULAR" era además el identificador
        // de una "obra social" sembrada ("Particular / Sin cobertura") que algunas pantallas ofrecían
        // como si fuera una OS más: con ese valor el sistema creía que el paciente tenía obra social
        // que no cubre nada. Se trata como "sin obra social" (ver Paciente.SetIdentificadorOS).
        public const string IdentificadorParticular = "PARTICULAR";

        public static bool EsParticular(string? identificadorOS) =>
            string.Equals(identificadorOS?.Trim(), IdentificadorParticular, StringComparison.OrdinalIgnoreCase);

        // CUF01 2.a.1.a.1 / CUF02 2.b.1.a.1: "La obra social ingresada no posee un acuerdo activo".
        // Acuerdo activo = la obra social figura como ACTIVA y tiene al menos un convenio con la clínica
        // (requiere Convenios cargados). La "obra social" particular nunca tiene acuerdo.
        public bool TieneAcuerdoActivo =>
            !EsParticular(IdentificadorOS) && EstaDisponible && Convenios.Any();

        public bool EstaDisponible => EstadosObraSocial.EsDisponible(EstadoOS);

        // Helper para compatibilidad de vistas
        public decimal ArancelOS => Convenios.FirstOrDefault()?.ArancelConvenio ?? 0m;

        public virtual ICollection<Convenio> Convenios { get; private set; } = new List<Convenio>();
        public virtual ICollection<Paciente> Pacientes { get; private set; } = new List<Paciente>();
        public virtual ICollection<Pago> Pagos { get; private set; } = new List<Pago>();

        protected ObraSocial() { }

        public ObraSocial(string identificadorOS, string nombreOS, string planCobertura = "", string estadoOS = EstadosObraSocial.Disponible)
        {
            SetIdentificadorOS(identificadorOS);
            SetNombreOS(nombreOS);
            SetPlanCobertura(planCobertura);
            SetEstadoOS(estadoOS);
        }

        public ObraSocial(string identificadorOS, string nombreOS, string planCobertura, decimal arancelOS, string estadoOS = EstadosObraSocial.Disponible)
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
            EstadoOS = string.IsNullOrWhiteSpace(estado) ? EstadosObraSocial.Disponible : estado.ToUpper().Trim();
        }

        public void SetArancelOS(decimal arancel)
        {
            // Helper de compatibilidad
        }

        // CUF06 alt 3.b.1: deja de ofrecerse a nuevos pacientes, conservando atenciones y pagos (RN15).
        public void DarDeBaja() => EstadoOS = EstadosObraSocial.NoDisponible;

        // CUF06 alt 3.c.1: vuelve a ofrecerse, conservando su historial.
        public void Rehabilitar() => EstadoOS = EstadosObraSocial.Disponible;
    }
}