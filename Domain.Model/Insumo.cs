using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Model
{
    public class Insumo
    {
        public int Id { get; private set; }
        public string Nombre { get; private set; }
        public string Descripcion { get; private set; }

        public Insumo(int id, string nombre, string descripcion)
        {
            Id = id;
            SetNombre(nombre);
            SetDescripcion(descripcion);
        }
        public void SetNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre no puede ser nulo o vacío.", nameof(nombre));
            Nombre = nombre;
        }

        public void SetDescripcion(string descripcion)
        {
            if (string.IsNullOrWhiteSpace(descripcion))
                throw new ArgumentException("La descripción no puede ser nula o vacía.", nameof(descripcion));
            Descripcion = descripcion;
        }
    }
}