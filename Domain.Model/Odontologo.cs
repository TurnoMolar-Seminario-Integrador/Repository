namespace Domain.Model
{
    public class Odontologo : Persona
    {
        public string Matricula { get; private set; } = string.Empty;

        // Matrícula para mostrar en pantalla. El dato guardado ya incluye el prefijo (en el seed,
        // "MP 3840"), así que se antepone "MP" solo si falta; antes las pantallas lo agregaban siempre
        // y mostraban "MP MP 3840". Propiedad de solo lectura: EF no la mapea.
        public string MatriculaVisible =>
            Matricula.StartsWith("MP", StringComparison.OrdinalIgnoreCase) ? Matricula : $"MP {Matricula}";
        public string EstadoOdontologo { get; private set; } = "ACTIVO";

        public virtual ICollection<DisponibilidadHoraria> DisponibilidadesHorarias { get; private set; } = new List<DisponibilidadHoraria>();
        public virtual ICollection<Turno> Turnos { get; private set; } = new List<Turno>();

        // Helpers para compatibilidad con código anterior y vistas
        public int? CodEspecialidad => DisponibilidadesHorarias.FirstOrDefault()?.IdEspecialidad ?? 1;
        public virtual Especialidad? Especialidad => DisponibilidadesHorarias.FirstOrDefault()?.Especialidad;
        public virtual DisponibilidadHoraria? Disponibilidad => DisponibilidadesHorarias.FirstOrDefault();

        protected Odontologo() : base() { }

        public Odontologo(
            string tipoDocumento,
            string nroDocumento,
            string matricula,
            string nombre,
            string apellido,
            DateTime fechaNacimiento,
            string telefono,
            string email,
            string domicilio,
            string estadoOdontologo = "ACTIVO",
            string clave = "",
            string saltClave = "",
            DateTime? fechaAlta = null,
            string rol = "Odontologo")
            : base(tipoDocumento, nroDocumento, nombre, apellido, fechaNacimiento, telefono, email, domicilio, clave, saltClave, fechaAlta, rol)
        {
            SetMatricula(matricula);
            SetEstadoOdontologo(estadoOdontologo);
        }

        public Odontologo(
            string tipoDocumento,
            int nroDocumento,
            string matricula,
            string nombre,
            string apellido,
            DateTime fechaNacimiento,
            string telefono,
            string email,
            string domicilio,
            string estadoOdontologo = "ACTIVO",
            int? codDisponibilidad = null,
            int? codEspecialidad = null,
            string clave = "",
            string saltClave = "",
            DateTime? fechaAlta = null,
            string rol = "Odontologo")
            : this(tipoDocumento, nroDocumento.ToString(), matricula, nombre, apellido, fechaNacimiento, telefono, email, domicilio, estadoOdontologo, clave, saltClave, fechaAlta, rol)
        {
        }

        public void SetMatricula(string matricula)
        {
            if (string.IsNullOrWhiteSpace(matricula))
                throw new ArgumentException("La matrícula es requerida.", nameof(matricula));
            Matricula = matricula.Trim();
        }

        public void SetEstadoOdontologo(string estado)
        {
            EstadoOdontologo = estado?.ToUpper().Trim() ?? "ACTIVO";
        }

        public void AsignarEspecialidad(Especialidad especialidad)
        {
            // Helper de asignación
        }
    }
}