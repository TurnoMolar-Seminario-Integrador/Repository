namespace DTOs
{
    public class EspecialidadDTO
    {
        public int CodEspecialidad { get; set; }
        public int Id { get => CodEspecialidad; set => CodEspecialidad = value; }
        public string Nombre { get; set; } = string.Empty;
        public decimal ArancelParticular { get; set; }
    }
}
