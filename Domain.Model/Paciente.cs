using System;
using System.Net;

namespace Domain.Model
{
    public class Paciente : Persona
    {
        public int Id { get; private set; }
        public bool EstadoHabilitado { get; private set; }

        public Paciente(int id, string nombre, string apellido, int dni, string telefono, string mail, string domicilio, bool estadoHabilitado = true)
            : base(nombre, apellido, dni, telefono, mail, domicilio)
        {
            Id = id;
            SetEstadoHabilitado(estadoHabilitado);
        }
        public void SetId(int id)
        {
            Id = id;
        }

        public void SetEstadoHabilitado(bool habilitado)
        {
            EstadoHabilitado = habilitado;
        }
    }
}