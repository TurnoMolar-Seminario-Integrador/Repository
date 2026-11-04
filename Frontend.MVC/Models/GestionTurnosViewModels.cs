namespace Frontend.MVC.Models
{
    // Gestión de Turnos del Responsable de la Clínica: listado real de turnos con las acciones
    // que los casos de uso documentan para el responsable en representación del paciente
    // (CUU01 alt 3.a agendar, CUF07 alt 2.a cancelar y CUF08 alt 2.a reprogramar).
    public class GestionTurnosViewModel
    {
        public string Busqueda { get; set; } = string.Empty;

        // "pendientes" (Reservado, Presente o Atención Registrada) o "todos".
        public string Estado { get; set; } = "pendientes";

        public List<GestionTurnoFilaViewModel> Filas { get; set; } = new();

        // Cantidad de turnos que cumplen el filtro (puede ser mayor que Filas.Count si se truncó).
        public int TotalCoincidencias { get; set; }

        public bool Truncado => TotalCoincidencias > Filas.Count;
    }

    public class GestionTurnoFilaViewModel
    {
        public int NroTurno { get; set; }
        public DateTime FechaHoraTurno { get; set; }

        // Valor persistido (RESERVADO, PRESENTE, ATENCION_REGISTRADA, ...) y texto para mostrar.
        public string EstadoTurno { get; set; } = string.Empty;
        public string EstadoLegible { get; set; } = string.Empty;

        public string TipoDocumentoPaciente { get; set; } = string.Empty;
        public string NroDocumentoPaciente { get; set; } = string.Empty;
        public string NombrePaciente { get; set; } = string.Empty;

        public string NombreEspecialidad { get; set; } = string.Empty;
        public string NombreOdontologo { get; set; } = string.Empty;
        public string ModalidadPagoLegible { get; set; } = string.Empty;

        // Solo los turnos "Reservado" admiten cancelar o reprogramar (ME - Turno).
        public bool PuedeModificar { get; set; }
    }
}
