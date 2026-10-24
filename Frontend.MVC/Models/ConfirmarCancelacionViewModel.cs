namespace Frontend.MVC.Models
{
    // CUF07 alt 3.a.1 / 3.a.2: pantalla de advertencia y confirmación cuando la cancelación se solicita
    // fuera de término (menos de 24 hs de anticipación, o el turno ya pasó). La usan el paciente
    // (Turno/Cancelar) y el responsable (Responsable/CancelarTurnoEnRepresentacion).
    public class ConfirmarCancelacionViewModel
    {
        public int IdTurno { get; set; }
        public string? MotivoCancelacion { get; set; }

        // sAvisoCancelacionFueraDeTermino (diccionario de datos de CUF07).
        public string Mensaje { get; set; } = string.Empty;

        // Penalización que se cobraría al confirmar.
        public decimal Penalizacion { get; set; }
        public DateTime? FechaHoraTurno { get; set; }
        public string? NombreEspecialidad { get; set; }

        // Solo en la cancelación en representación del paciente (responsable).
        public string? TipoDocumentoPaciente { get; set; }
        public string? NroDocumentoPaciente { get; set; }
        public string? NombrePaciente { get; set; }
        public string? Busqueda { get; set; }
        public string? Estado { get; set; }

        // Destino del formulario de confirmación y del botón "Volver".
        public string AccionConfirmar { get; set; } = "Cancelar";
        public string ControladorConfirmar { get; set; } = "Turno";
        public string AccionVolver { get; set; } = "MisTurnos";
        public string ControladorVolver { get; set; } = "Home";
    }
}
