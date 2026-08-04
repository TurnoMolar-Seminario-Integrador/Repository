using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Model
{
    public class HistoriaClinica
    {
        public int NumeroHistoriaClinica { get; private set; }
        public DateTime FechaAlta { get; private set; }

        public HistoriaClinica(int numeroHistoriaClinica, DateTime fechaAlta)
        {
            SetNumeroHistoriaClinica(numeroHistoriaClinica);
            SetFechaAlta(fechaAlta);
        }

        public void SetNumeroHistoriaClinica(int numeroHistoriaClinica)
        {
            if (numeroHistoriaClinica <= 0)
                throw new ArgumentException("El número de historia clínica debe ser mayor que 0.", nameof(numeroHistoriaClinica));
            NumeroHistoriaClinica = numeroHistoriaClinica;
        }

        public void SetFechaAlta(DateTime fechaAlta)
        {
            if (fechaAlta == DateTime.MinValue)
                throw new ArgumentException("La fecha de alta es requerida.", nameof(fechaAlta));
            if (fechaAlta > DateTime.Now)
                throw new ArgumentException("La fecha de alta no puede ser en el futuro.", nameof(fechaAlta));
            FechaAlta = fechaAlta;
        }
    }
}