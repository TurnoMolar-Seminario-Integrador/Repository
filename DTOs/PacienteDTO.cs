using System;

namespace DTOs
{
    public class PacienteDTO
    {
        public string TipoDocumento { get; set; } = "DNI";
        public int NroDocumento { get; set; }
        public int Id { get => NroDocumento; set => NroDocumento = value; }
        public int Dni { get => NroDocumento; set => NroDocumento = value; }

        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string Mail { get => Email; set => Email = value; }

        public string Domicilio { get; set; } = string.Empty;
        public DateTime FechaNacimiento { get; set; } = new DateTime(1990, 1, 1);
        public string EstadoPaciente { get; set; } = "ACTIVO";
        public bool EstadoHabilitado
        {
            get => EstadoPaciente == "ACTIVO";
            set => EstadoPaciente = value ? "ACTIVO" : "INACTIVO";
        }

        public decimal? MontoAdeudado { get; set; }
        public int? IdentificadorOS { get; set; }
    }
}