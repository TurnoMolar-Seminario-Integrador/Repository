namespace Domain.Model
{
    public class Paciente : Persona
    {
        public string EstadoPaciente { get; private set; } = "HABILITADO";
        public string? IdentificadorOS { get; private set; }
        public decimal? MontoAdeudado { get; private set; } = 0m;

        public virtual ObraSocial? ObraSocial { get; private set; }
        public virtual HistoriaClinica? HistoriaClinica { get; private set; }
        public virtual ICollection<Turno> Turnos { get; private set; } = new List<Turno>();

        protected Paciente() : base() { }

        public Paciente(
            string tipoDocumento,
            string nroDocumento,
            string nombre,
            string apellido,
            DateTime fechaNacimiento,
            string telefono,
            string email,
            string domicilio,
            string estadoPaciente = "HABILITADO",
            string? identificadorOS = null,
            decimal? montoAdeudado = 0m,
            string clave = "",
            string saltClave = "",
            DateTime? fechaAlta = null,
            string rol = "Paciente")
            : base(tipoDocumento, nroDocumento, nombre, apellido, fechaNacimiento, telefono, email, domicilio, clave, saltClave, fechaAlta, rol)
        {
            SetEstadoPaciente(estadoPaciente);
            SetIdentificadorOS(identificadorOS);
            SetMontoAdeudado(montoAdeudado);
        }

        public Paciente(
            string tipoDocumento,
            int nroDocumento,
            string nombre,
            string apellido,
            DateTime fechaNacimiento,
            string telefono,
            string email,
            string domicilio,
            string estadoPaciente = "HABILITADO",
            string? identificadorOS = null,
            decimal? montoAdeudado = 0m,
            string clave = "",
            string saltClave = "",
            DateTime? fechaAlta = null,
            string rol = "Paciente")
            : this(tipoDocumento, nroDocumento.ToString(), nombre, apellido, fechaNacimiento, telefono, email, domicilio, estadoPaciente, identificadorOS, montoAdeudado, clave, saltClave, fechaAlta, rol)
        {
        }

        public void SetEstadoPaciente(string estado)
        {
            if (string.IsNullOrWhiteSpace(estado))
                throw new ArgumentException("El estado del paciente es requerido.", nameof(estado));
            EstadoPaciente = estado.ToUpper().Trim();
        }

        public void SetIdentificadorOS(string? identificadorOS)
        {
            IdentificadorOS = string.IsNullOrWhiteSpace(identificadorOS) ? null : identificadorOS.Trim();
        }

        public void SetMontoAdeudado(decimal? monto)
        {
            if (monto.HasValue && monto.Value < 0)
                throw new ArgumentException("El monto adeudado no puede ser negativo.", nameof(monto));
            MontoAdeudado = monto;
        }

        public void AsignarObraSocial(ObraSocial? obraSocial)
        {
            ObraSocial = obraSocial;
            IdentificadorOS = obraSocial?.IdentificadorOS;
        }
    }
}