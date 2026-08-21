namespace DTOs
{
    public class OdontologoDTO
    {
        public int Id { get; set; }
        public int NumMatricula { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public int Dni { get; set; }
        public string Telefono { get; set; } = string.Empty;
        public string Mail { get; set; } = string.Empty;
        public string Domicilio { get; set; } = string.Empty;
        public int EspecialidadId { get; set; }
        public string? EspecialidadNombre { get; set; }

        public string NombreCompleto => $"Dr/a. {Apellido}, {Nombre} (Mat. {NumMatricula})";
    }
}
