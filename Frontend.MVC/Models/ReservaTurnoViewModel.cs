using System.ComponentModel.DataAnnotations;

namespace Frontend.MVC.Models
{
    // Datos que viajan desde el wizard de "Agendar Turno" (CUU01) hacia el controlador.
    // El calendario, los horarios disponibles y los odontólogos se completan de forma
    // dinámica en el navegador (fetch a los endpoints JSON de TurnoController), por lo
    // que este modelo sólo contiene lo estrictamente necesario para registrar la reserva.
    public class ReservaTurnoViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar una especialidad.")]
        public int IdEspecialidad { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un odontólogo.")]
        public string OdontologoTipoDocumento { get; set; } = "DNI";

        [Required(ErrorMessage = "Debe seleccionar un odontólogo.")]
        public string OdontologoNroDocumento { get; set; } = string.Empty;

        // Fecha y hora del turno en formato ISO 8601 ("2026-11-12T09:30:00"),
        // tal como la arma el JS a partir del horario elegido en el paso 1.
        [Required(ErrorMessage = "Debe seleccionar fecha y horario.")]
        public string FechaHoraTurnoIso { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe elegir una forma de abonar.")]
        public string ModalidadPago { get; set; } = "PARTICULAR"; // "PARTICULAR" | "OBRA_SOCIAL"

        [Range(typeof(bool), "true", "true", ErrorMessage = "Debe aceptar las políticas de cancelación para continuar.")]
        public bool AceptaPoliticas { get; set; }
    }
}
