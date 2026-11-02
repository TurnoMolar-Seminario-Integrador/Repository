using System;
using System.Text.Json.Serialization;

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

        // Pacientes.IdentificadorOS (NVARCHAR(20) NULL, MDF): nulo = paciente particular (RN6 / MU).
        // En POST: nulo o vacío = particular. En PUT: nulo o vacío = no se modifica la obra social (un cliente
        // que solo corrige un teléfono no debe dejar al paciente sin cobertura); "PARTICULAR" = pasar a particular.
        public string? IdentificadorOS { get; set; }
        public string? NombreObraSocial { get; set; }

        // Solo se envía en POST (RN3: el paciente establece su contraseña). Nunca se devuelve.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Password { get; set; }

        // GET lo devuelve y PUT lo exige: huella de los datos personales que el cliente vio (concurrencia
        // optimista). Si otro usuario los modificó, PUT responde 409.
        public string? VersionDatos { get; set; }
    }
}