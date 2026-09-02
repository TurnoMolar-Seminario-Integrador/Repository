namespace Domain.Model
{
    public class ResponsableClinica : Persona
    {
        protected ResponsableClinica() : base() { }

        public ResponsableClinica(
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
            string rol = "ResponsableClinica")
            : base(tipoDocumento, nroDocumento, nombre, apellido, fechaNacimiento, telefono, email, domicilio, clave, saltClave, fechaAlta, rol)
        {
        }

        public ResponsableClinica(
            string tipoDocumento,
            int nroDocumento,
            string nombre,
            string apellido,
            DateTime fechaNacimiento,
            string telefono,
            string email,
            string domicilio,
            string clave = "",
            string saltClave = "",
            DateTime? fechaAlta = null,
            string rol = "ResponsableClinica")
            : this(tipoDocumento, nroDocumento.ToString(), nombre, apellido, fechaNacimiento, telefono, email, domicilio, clave, saltClave, fechaAlta, rol)
        {
        }
    }
}
