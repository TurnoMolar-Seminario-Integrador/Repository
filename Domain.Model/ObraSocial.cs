using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Model
{
    public class ObraSocial
    {
        public int Id { get; private set; }
        public string Nombre { get; private set; }
        public string Plan { get; private set; }

        public ObraSocial(int id, string nombre, string plan)
        {
            Id = id;
            SetNombre(nombre);
            SetPlan(plan);
        }

        public void SetNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre no puede ser nulo o vacío.", nameof(nombre));
            Nombre = nombre;
        }

        public void SetPlan(string plan)
        {
            if (string.IsNullOrWhiteSpace(plan))
                throw new ArgumentException("El plan no puede ser nulo o vacío.", nameof(plan));
            Plan = plan;
        }
    }
}