using System.ComponentModel.DataAnnotations;

namespace Frontend.MVC.Models
{
    public class ReservaTurnoViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un profesional.")]
        public int OdontologoId { get; set; }

        public string? NombreOdontologo { get; set; } // Para mostrar en la vista de confirmación

        [Required(ErrorMessage = "Debe seleccionar una fecha.")]
        [DataType(DataType.Date)]
        public DateTime FechaSeleccionada { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un horario.")]
        [DataType(DataType.Time)]
        public TimeSpan HorarioSeleccionado { get; set; }

        [Required(ErrorMessage = "Debe elegir un método de pago.")]
        public string MetodoPago { get; set; } // Podría ser "ObraSocial" o "Particular"

        [Required]
        [Range(typeof(bool), "true", "true", ErrorMessage = "Debe aceptar las políticas de cancelación para continuar.")]
        public bool AceptaPoliticas { get; set; }
    }
}