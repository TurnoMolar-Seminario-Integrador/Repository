using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Model
{
    public class Factura
    {
        public int Id { get; private set; }
        public string Descripcion { get; private set; }
        public float Subtotal { get; private set; }
        public float Total { get; private set; }

        public Factura(int id, string descripcion, float subtotal, float total)
        {
            Id = id;
            SetDescripcion(descripcion);
            SetSubtotal(subtotal);
            SetTotal(total);
        }

        public void SetDescripcion(string descripcion)
        {
            if (string.IsNullOrWhiteSpace(descripcion))
                throw new ArgumentException("La descripción no puede ser nula o vacía.", nameof(descripcion));
            Descripcion = descripcion;
        }

        public void SetSubtotal(float subtotal)
        {
            if (subtotal < 0)
                throw new ArgumentException("El subtotal debe ser mayor o igual a 0.", nameof(subtotal));
            Subtotal = subtotal;
        }

        public void SetTotal(float total)
        {
            if (total < 0)
                throw new ArgumentException("El total debe ser mayor o igual a 0.", nameof(total));
            Total = total;
        }
    }
}