namespace DTOs
{
    public class ObraSocialDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Plan { get; set; } = string.Empty;
        public decimal PorcentajeCobertura { get; set; } = 0.50m;
    }
}
