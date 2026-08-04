using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Model
{
    public class Odontologo : Persona
    {
        public int NumMatricula { get; set; } 
      
        public Odontologo(int numMatricula, string nombre, string apellido, int dni, string telefono, string mail, string domicilio)
            : base(nombre, apellido, dni, telefono, mail, domicilio)
        {
            NumMatricula = numMatricula;
        }
    }
}