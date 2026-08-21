namespace Domain.Model
{
    public class Especialidad
    {
        public int CodEspecialidad { get; private set; }
        public string Nombre { get; private set; } = string.Empty;
        public decimal ArancelParticular { get; private set; }

        protected Especialidad() { }

        public Especialidad(int codEspecialidad, string nombre, decimal arancelParticular)
        {
            CodEspecialidad = codEspecialidad;
            SetNombre(nombre);
            SetArancelParticular(arancelParticular);
        }

        public void SetNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre de la especialidad no puede ser nulo o vacío.", nameof(nombre));
            Nombre = nombre.Trim();
        }

        public void SetArancelParticular(decimal arancel)
        {
            if (arancel < 0)
                throw new ArgumentException("El arancel particular no puede ser negativo.", nameof(arancel));
            ArancelParticular = arancel;
        }
    }
}
