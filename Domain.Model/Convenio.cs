namespace Domain.Model
{
    public class Convenio
    {
        public string IdentificadorOS { get; private set; } = string.Empty;
        public int IdEspecialidad { get; private set; }
        public decimal ArancelConvenio { get; private set; }

        public virtual ObraSocial ObraSocial { get; private set; } = null!;
        public virtual Especialidad Especialidad { get; private set; } = null!;

        protected Convenio() { }

        public Convenio(string identificadorOS, int idEspecialidad, decimal arancelConvenio)
        {
            SetIdentificadorOS(identificadorOS);
            IdEspecialidad = idEspecialidad;
            SetArancelConvenio(arancelConvenio);
        }

        public void SetIdentificadorOS(string identificadorOS)
        {
            if (string.IsNullOrWhiteSpace(identificadorOS))
                throw new ArgumentException("El identificador de Obra Social es requerido.", nameof(identificadorOS));
            IdentificadorOS = identificadorOS.Trim();
        }

        public void SetArancelConvenio(decimal arancelConvenio)
        {
            if (arancelConvenio <= 0)
                throw new ArgumentException("El arancel del convenio debe ser mayor a 0.", nameof(arancelConvenio));
            ArancelConvenio = arancelConvenio;
        }
    }
}
