namespace DTOs
{
    public class ObraSocialDTO
    {
        public string IdentificadorOS { get; set; } = string.Empty;
        public string Id
        {
            get => IdentificadorOS;
            set => IdentificadorOS = value ?? string.Empty;
        }

        public string NombreOS { get; set; } = string.Empty;
        public string Nombre
        {
            get => NombreOS;
            set => NombreOS = value ?? string.Empty;
        }

        public string PlanCobertura { get; set; } = string.Empty;
        public string Plan
        {
            get => PlanCobertura;
            set => PlanCobertura = value ?? string.Empty;
        }

        public string EstadoOS { get; set; } = "ACTIVA";
        public decimal PorcentajeCobertura { get; set; } = 0.50m;
        public List<ConvenioDTO> Convenios { get; set; } = new List<ConvenioDTO>();
    }

    public class ConvenioDTO
    {
        public string IdentificadorOS { get; set; } = string.Empty;
        public int IdEspecialidad { get; set; }
        public string? NombreEspecialidad { get; set; }
        public decimal ArancelConvenio { get; set; }
    }
}
