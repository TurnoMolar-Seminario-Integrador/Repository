using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Model
{
    public class ItemFactura
    {
        public int CantidadInsumo { get; private set; }

        public ItemFactura(int cantidadInsumo)
        {
            SetCantidadInsumo(cantidadInsumo);
        }

        public void SetCantidadInsumo(int cantidadInsumo)
        {
            if (cantidadInsumo <= 0)
                throw new ArgumentException("La cantidad de insumo debe ser mayor que 0.", nameof(cantidadInsumo));
            CantidadInsumo = cantidadInsumo;
        }
    }
}