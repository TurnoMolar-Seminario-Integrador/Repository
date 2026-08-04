using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Model
{
    public class Multa
    {
        public int Id { get; private set; }
        public float Monto { get; private set; }
        public bool EstadoPago { get; private set; }
        public DateTime FechaPago { get; private set; }


        public Multa(int id, float monto, bool estadoPago, DateTime fechaPago)
        {
            Id = id;
            SetMonto(monto);
            SetEstadoPago(estadoPago);
            SetFechaPago(fechaPago);
        }

        public void SetMonto(float monto)
        {
            if (monto <= 0)
                throw new ArgumentException("El monto debe ser mayor que 0.", nameof(monto));
            Monto = monto;
        }

        public void SetEstadoPago(bool estadoPago)
        {
            EstadoPago = estadoPago;
        }

        public void SetFechaPago(DateTime fechaPago)
        {
            if (fechaPago > DateTime.Now)
                throw new ArgumentException("La fecha de pago no puede ser en el futuro.", nameof(fechaPago));
            FechaPago = fechaPago;
        }

        public void MarcarComoPagada()
        {
            if (EstadoPago)
                throw new InvalidOperationException("La multa ya fue pagada.");
            EstadoPago = true;
            FechaPago = DateTime.Now;
        }
    }
}

