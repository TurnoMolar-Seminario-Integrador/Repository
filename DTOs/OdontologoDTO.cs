using System;

namespace DTOs
{
    public class OdontologoDTO
    {
        public string TipoDocumento { get; set; } = "DNI";
        public string NroDocumento { get; set; } = string.Empty;

        public int Id
        {
            get => int.TryParse(NroDocumento, out var n) ? n : 0;
            set => NroDocumento = value.ToString();
        }

        public int Dni
        {
            get => Id;
            set => Id = value;
        }

        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;

        public string Matricula { get; set; } = string.Empty;
        public int NumMatricula
        {
            get => int.TryParse(Matricula, out var m) ? m : 0;
            set => Matricula = value.ToString();
        }

        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Mail { get => Email; set => Email = value; }

        public string Domicilio { get; set; } = string.Empty;
        public DateTime FechaNacimiento { get; set; } = new DateTime(1985, 1, 1);
        public string EstadoOdontologo { get; set; } = "ACTIVO";

        public int? CodEspecialidad { get; set; } = 1;
        public int EspecialidadId { get => CodEspecialidad ?? 1; set => CodEspecialidad = value; }

        public string? NombreEspecialidad { get; set; }
        public string? EspecialidadNombre { get => NombreEspecialidad; set => NombreEspecialidad = value; }

        public string NombreCompleto => $"Dr/a. {Apellido}, {Nombre} (Mat. {Matricula})";
    }
}
