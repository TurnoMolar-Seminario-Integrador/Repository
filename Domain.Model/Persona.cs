namespace Domain.Model
{
    public abstract class Persona
    {
        public string TipoDocumento { get; protected set; } = "DNI";
        public string NroDocumento { get; protected set; } = string.Empty;
        public string Nombre { get; protected set; } = string.Empty;
        public string Apellido { get; protected set; } = string.Empty;
        public DateTime FechaNacimiento { get; protected set; }
        public string Telefono { get; protected set; } = string.Empty;
        public string Email { get; protected set; } = string.Empty;
        public string Domicilio { get; protected set; } = string.Empty;
        public string Clave { get; protected set; } = string.Empty;
        public string SaltClave { get; protected set; } = string.Empty;
        public DateTime FechaAlta { get; protected set; } = DateTime.Now;
        public string Rol { get; protected set; } = string.Empty;

        // Helper para compatibilidad numérica si se requiere
        public int NroDocumentoInt
        {
            get => int.TryParse(NroDocumento, out var n) ? n : 0;
        }

        protected Persona() { }

        protected Persona(
            string tipoDocumento,
            string nroDocumento,
            string nombre,
            string apellido,
            DateTime fechaNacimiento,
            string telefono,
            string email,
            string domicilio,
            string clave = "",
            string saltClave = "",
            DateTime? fechaAlta = null,
            string rol = "")
        {
            SetTipoDocumento(tipoDocumento);
            SetNroDocumento(nroDocumento);
            SetNombre(nombre);
            SetApellido(apellido);
            SetFechaNacimiento(fechaNacimiento);
            SetTelefono(telefono);
            SetEmail(email);
            SetDomicilio(domicilio);
            SetCredenciales(clave, saltClave, rol);
            FechaAlta = fechaAlta ?? DateTime.Now;
        }

        public void SetTipoDocumento(string tipoDocumento)
        {
            if (string.IsNullOrWhiteSpace(tipoDocumento))
                throw new ArgumentException("El tipo de documento es requerido.", nameof(tipoDocumento));
            TipoDocumento = tipoDocumento.ToUpper().Trim();
        }

        public void SetNroDocumento(string nroDocumento)
        {
            if (string.IsNullOrWhiteSpace(nroDocumento))
                throw new ArgumentException("El número de documento no puede ser vacío.", nameof(nroDocumento));
            NroDocumento = nroDocumento.ToUpper().Trim();
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

        public void SetCredenciales(string clave, string saltClave, string rol)
        {
            Clave = clave ?? string.Empty;
            SaltClave = saltClave ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(rol))
            {
                Rol = rol.Trim();
            }
        }

        public void SetRol(string rol)
        {
            if (string.IsNullOrWhiteSpace(rol))
                throw new ArgumentException("El rol es requerido.", nameof(rol));
            Rol = rol.Trim();
        }
    }
}