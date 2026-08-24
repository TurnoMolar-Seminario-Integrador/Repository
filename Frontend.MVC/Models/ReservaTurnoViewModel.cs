using System.ComponentModel.DataAnnotations;

namespace Frontend.MVC.Models
{
    public class ReservaTurnoViewModel
    {
        public int? OdontologoId { get; set; }

        public string? NombreOdontologo { get; set; }

        public string? Especialidad { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una fecha.")]
        public string? FechaSeleccionada { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un horario.")]
        public string? HorarioSeleccionado { get; set; }

        [Required(ErrorMessage = "Debe elegir un método de pago.")]
        public string MetodoPago { get; set; } = "ObraSocial"; // "ObraSocial" o "Particular"

        public string ObraSocialNombre { get; set; } = "OSDE (Plan 210)";

        public decimal ArancelConsulta { get; set; } = 15000m;

        public decimal CopagoAPagar { get; set; } = 0m;

        [Range(typeof(bool), "true", "true", ErrorMessage = "Debe aceptar las políticas de cancelación para continuar.")]
        public bool AceptaPoliticas { get; set; } = true;

        public bool EstaInhabilitado { get; set; } = false;
    }
}