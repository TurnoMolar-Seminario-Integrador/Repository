namespace DTOs
{
    public class ConsultaDTO
    {
        public int Id { get; set; }
        public int TurnoId { get; set; }
        public int PacienteId { get; set; }
        public string? PacienteNombre { get; set; }
        public string? OdontologoNombre { get; set; }
        public string Diagnostico { get; set; } = string.Empty;
        public string Tratamiento { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public bool AnestesiaLocal { get; set; }
        public bool Radiografias { get; set; }
        public string? Valoracion { get; set; }
        public int? CalificacionEstrellas { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;

        public List<ItemFacturaDTO> InsumosUtilizados { get; set; } = new();
    }
}
