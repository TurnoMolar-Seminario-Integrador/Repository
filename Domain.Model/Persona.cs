namespace Domain.Model
{
    public abstract class Persona
    {
        public string TipoDocumento { get; protected set; } = "DNI";
        public int NroDocumento { get; protected set; }
        public string Nombre { get; protected set; } = string.Empty;
        public string Apellido { get; protected set; } = string.Empty;
        public DateTime FechaNacimiento { get; protected set; }
        public string Telefono { get; protected set; } = string.Empty;
        public string Email { get; protected set; } = string.Empty;
        public string Domicilio { get; protected set; } = string.Empty;

        protected Persona() { }

        protected Persona(string tipoDocumento, int nroDocumento, string nombre, string apellido, DateTime fechaNacimiento, string telefono, string email, string domicilio)
        {
            SetTipoDocumento(tipoDocumento);
            SetNroDocumento(nroDocumento);
            SetNombre(nombre);
            SetApellido(apellido);
            SetFechaNacimiento(fechaNacimiento);
            SetTelefono(telefono);
            SetEmail(email);
            SetDomicilio(domicilio);
        }

        public void SetTipoDocumento(string tipoDocumento)
        {
            if (string.IsNullOrWhiteSpace(tipoDocumento))
                throw new ArgumentException("El tipo de documento es requerido.", nameof(tipoDocumento));
            TipoDocumento = tipoDocumento.ToUpper().Trim();
        }

        public void SetNroDocumento(int nroDocumento)
        {
            if (nroDocumento <= 0)
                throw new ArgumentException("El número de documento debe ser mayor a 0.", nameof(nroDocumento));
            NroDocumento = nroDocumento;
        }

        public void SetNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre no puede ser nulo o vacío.", nameof(nombre));
            Nombre = nombre.Trim();
        }

        public void SetApellido(string apellido)
        {
            if (string.IsNullOrWhiteSpace(apellido))
                throw new ArgumentException("El apellido no puede ser nulo o vacío.", nameof(apellido));
            Apellido = apellido.Trim();
        }

        public void SetFechaNacimiento(DateTime fechaNacimiento)
        {
            if (fechaNacimiento > DateTime.Now)
                throw new ArgumentException("La fecha de nacimiento no puede ser futura.", nameof(fechaNacimiento));
            FechaNacimiento = fechaNacimiento;
        }

        public void SetTelefono(string telefono)
        {
            Telefono = telefono?.Trim() ?? string.Empty;
        }

        public void SetEmail(string email)
        {
            Email = email?.Trim() ?? string.Empty;
        }

        public void SetDomicilio(string domicilio)
        {
            Domicilio = domicilio?.Trim() ?? string.Empty;
        }
    }
}
