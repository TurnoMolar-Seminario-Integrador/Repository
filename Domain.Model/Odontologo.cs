namespace Domain.Model
{
    public class Odontologo : Persona
    {
        public string Matricula { get; private set; } = string.Empty;
        public string EstadoOdontologo { get; private set; } = "ACTIVO";
        public int? CodDisponibilidad { get; private set; }
        public virtual DisponibilidadHoraria? Disponibilidad { get; private set; }

        public int? CodEspecialidad { get; private set; }
        public virtual Especialidad? Especialidad { get; private set; }

        protected Odontologo() : base() { }

        public Odontologo(string tipoDocumento, int nroDocumento, string matricula, string nombre, string apellido, DateTime fechaNacimiento, string telefono, string email, string domicilio, string estadoOdontologo = "ACTIVO", int? codDisponibilidad = null, int? codEspecialidad = null)
            : base(tipoDocumento, nroDocumento, nombre, apellido, fechaNacimiento, telefono, email, domicilio)
        {
            SetMatricula(matricula);
            SetEstadoOdontologo(estadoOdontologo);
            CodDisponibilidad = codDisponibilidad;
            CodEspecialidad = codEspecialidad;
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
            Especialidad = especialidad;
            CodEspecialidad = especialidad?.CodEspecialidad;
        }

        public void AsignarDisponibilidad(DisponibilidadHoraria disponibilidad)
        {
            Disponibilidad = disponibilidad;
            CodDisponibilidad = disponibilidad?.CodDisponibilidad;
        }
    }
}