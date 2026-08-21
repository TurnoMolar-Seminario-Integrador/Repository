namespace Domain.Model
{
    public class Paciente : Persona
    {
        public string EstadoPaciente { get; private set; } = "HABILITADO";
        public decimal? MontoAdeudado { get; private set; }
        public int? IdentificadorOS { get; private set; }

        public virtual ObraSocial? ObraSocial { get; private set; }
        public virtual HistoriaClinica? HistoriaClinica { get; private set; }

        protected Paciente() : base() { }

        public Paciente(string tipoDocumento, int nroDocumento, string nombre, string apellido, DateTime fechaNacimiento, string telefono, string email, string domicilio, string estadoPaciente = "HABILITADO", decimal? montoAdeudado = null, int? identificadorOS = null)
            : base(tipoDocumento, nroDocumento, nombre, apellido, fechaNacimiento, telefono, email, domicilio)
        {
            SetEstadoPaciente(estadoPaciente);
            SetMontoAdeudado(montoAdeudado);
            SetIdentificadorOS(identificadorOS);
        }

        public void SetEstadoPaciente(string estado)
        {
            if (string.IsNullOrWhiteSpace(estado))
                throw new ArgumentException("El estado del paciente es requerido.", nameof(estado));
            EstadoPaciente = estado.ToUpper().Trim();
        }

        public void SetMontoAdeudado(decimal? monto)
        {
            if (monto.HasValue && monto.Value < 0)
                throw new ArgumentException("El monto adeudado no puede ser negativo.", nameof(monto));
            MontoAdeudado = monto;
        }

        public void SetIdentificadorOS(int? identificadorOS)
        {
            IdentificadorOS = identificadorOS;
        }

        public void AsignarObraSocial(ObraSocial? obraSocial)
        {
            ObraSocial = obraSocial;
            IdentificadorOS = obraSocial?.IdentificadorOS;
        }
    }
}