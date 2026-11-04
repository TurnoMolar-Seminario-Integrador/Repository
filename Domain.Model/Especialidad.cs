namespace Domain.Model
{
    public class Especialidad
    {
        public int IdEspecialidad { get; private set; }
        public int CodEspecialidad
        {
            get => IdEspecialidad;
            private set => IdEspecialidad = value;
        }

        public string Nombre { get; private set; } = string.Empty;
        public decimal ArancelParticular { get; private set; }

        // CUF12 alt 1.d / 1.e: estadoEspecialidad(d) = [ "Habilitada" | "Deshabilitada" ].
        public string EstadoEspecialidad { get; private set; } = EstadosEspecialidad.Habilitada;
        public bool EstaHabilitada => EstadosEspecialidad.EsHabilitada(EstadoEspecialidad);

        public virtual ICollection<Convenio> Convenios { get; private set; } = new List<Convenio>();
        public virtual ICollection<DisponibilidadHoraria> DisponibilidadesHorarias { get; private set; } = new List<DisponibilidadHoraria>();
        public virtual ICollection<Turno> Turnos { get; private set; } = new List<Turno>();

        protected Especialidad() { }

        // Constructor con ID explícito (para compatibilidad interna)
        public Especialidad(int idEspecialidad, string nombre, decimal arancelParticular)
        {
            IdEspecialidad = idEspecialidad;
            SetNombre(nombre);
            SetArancelParticular(arancelParticular);
        }

        // Constructor sin ID — para seeder (EF IDENTITY lo asigna automáticamente)
        public Especialidad(string nombre, decimal arancelParticular)
        {
            SetNombre(nombre);
            SetArancelParticular(arancelParticular);
        }

        public void SetNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre de la especialidad no puede ser nulo o vacío.", nameof(nombre));
            Nombre = nombre.Trim();
        }

        public void SetArancelParticular(decimal arancel)
        {
            if (arancel < 0)
                throw new ArgumentException("El arancel particular no puede ser negativo.", nameof(arancel));
            ArancelParticular = arancel;
        }

        // CUF12 alt 1.d.2: deja de ofrecerse, conservando turnos, atenciones, convenios y pagos previos.
        public void Deshabilitar() => EstadoEspecialidad = EstadosEspecialidad.Deshabilitada;

        // CUF12 alt 1.e.2: vuelve a ofrecerse.
        public void Rehabilitar() => EstadoEspecialidad = EstadosEspecialidad.Habilitada;
    }
}
