namespace Domain.Model
{
    public abstract class Persona
    {
        public string Nombre { get; private set; }
        public string Apellido { get; private set; }
        public int Dni { get; private set; }
        public string Telefono { get; private set; }
        public string Mail { get; private set; }
        public string Domicilio { get; private set; }


        public Persona(string nombre, string apellido, int dni, string telefono, string mail, string domicilio)
        {
            SetNom(nombre);
            SetApe(apellido);
            SetDni(dni);
            SetTel(telefono);
            SetMail(mail);
            SetDom(domicilio);
        }


        public void SetNom(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre no puede ser nulo o vacío.", nameof(nombre));
            Nombre = nombre;
        }

        public void SetApe(string apellido)
        {
            if (string.IsNullOrWhiteSpace(apellido))
                throw new ArgumentException("El apellido no puede ser nulo o vacío.", nameof(apellido));
            Apellido = apellido;
        }

        public void SetDni(int dni)
        {
            if (dni < 0)
                throw new ArgumentException("El Dni debe ser mayor que 0.", nameof(dni));
            Dni = dni;
        }

        public void SetTel(string telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono))
                throw new ArgumentException("El telefono debe ser mayor que 0.", nameof(telefono));
            Telefono = telefono;
        }


        public void SetMail(string mail)
        {
            if (string.IsNullOrWhiteSpace(mail))
                throw new ArgumentException("El mail no puede ser nulo o vacío.", nameof(mail));
            Mail = mail;
        }

        public void SetDom(string domicilio)
        {
            if (string.IsNullOrWhiteSpace(domicilio))
                throw new ArgumentException("El domicilio no puede ser nulo o vacío.", nameof(domicilio));
            Domicilio = domicilio;
        }


    }
}
