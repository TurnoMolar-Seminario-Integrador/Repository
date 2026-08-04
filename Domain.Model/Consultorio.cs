using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Model
{
    public class Consultorio
    {
        public int NumConsultorio { get; private set; }
        public string Direccion { get; private set; }

        public Consultorio(int numConsultorio, string direccion)
        {
            NumConsultorio = numConsultorio;
            SetDireccion(direccion);
        }

        public void SetDireccion(string direccion)
        {
            if (string.IsNullOrWhiteSpace(direccion))
                throw new ArgumentException("La dirección no puede ser nula o vacía.", nameof(direccion));
            Direccion = direccion;
        }
    }
}