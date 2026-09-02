using System;

namespace DTOs
{
    public class PacienteDTO
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
        public string Telefono { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string Mail { get => Email; set => Email = value; }

        public string Domicilio { get; set; } = string.Empty;
        public DateTime FechaNacimiento { get; set; } = new DateTime(1990, 1, 1);
        public string EstadoPaciente { get; set; } = "HABILITADO";
        public bool EstadoHabilitado
        {
            get => EstadoPaciente == "HABILITADO" || EstadoPaciente == "ACTIVO";
            set => EstadoPaciente = value ? "HABILITADO" : "INHABILITADO";
        }

        public decimal? MontoAdeudado { get; set; }
        public string? IdentificadorOS { get; set; }
        public string? NombreObraSocial { get; set; }
    }
}