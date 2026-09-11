namespace DTOs
{
    // CUU02 - Gestionar Asistencia a Turno Odontológico.
    // Camino básico, paso 1 / Alt 1.a: resultado de buscar el turno del día por documento.
    public enum ResultadoBusquedaTurnoAsistencia
    {
        Encontrado,     // Camino básico, paso 1: hay un turno "Reservado" para hoy con esos datos.
        NoEncontrado    // Alt 1.a: no existe un turno registrado con los datos ingresados. FCU.
    }

    // Diccionario de datos, paso 1: sDatosTurnoEncontrado / 1.a.1: sMensajeTurnoNoEncontrado.
    public class BuscarTurnoAsistenciaResultDTO
    {
        public ResultadoBusquedaTurnoAsistencia Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;

        public int? NroTurno { get; set; }
        public string? NombrePaciente { get; set; }
        public string? ApellidoPaciente { get; set; }
        public DateTime? FechaHoraTurno { get; set; }
        public string? NombreEspecialidad { get; set; }
        public string? NombreOdontologo { get; set; }
        public string? ApellidoOdontologo { get; set; }
        public string? DescripcionMaterial { get; set; }
    }

    // Camino básico, paso 2 / Alt 1.b: resultado de registrar la asistencia (Presente o Ausente).
    public enum ResultadoRegistrarAsistencia
    {
        Presente,       // Camino básico, paso 2: éxito. Turno -> "Presente".
        Ausente,        // Alt 1.b.1: éxito alternativo. Turno -> "Ausente", paciente -> "Inhabilitado".
        TurnoInvalido   // El turno no existe o ya no está en condiciones de registrar asistencia.
    }

    // Diccionario de datos, paso 2: sNotificacionPacientePresente.
    // Diccionario de datos, 1.b.1: sAvisoAusenciaRegistrada = estadoTurno(d) + montoAdeudado + estadoPaciente(d).
    public class RegistrarAsistenciaResultDTO
    {
        public ResultadoRegistrarAsistencia Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string EstadoTurno { get; set; } = string.Empty;

        // Solo se completan en el camino de Ausente (Alt 1.b.1 / RN11).
        public decimal? MontoPenalizacion { get; set; }
        public decimal? MontoAdeudadoTotal { get; set; }
        public string? EstadoPaciente { get; set; }
    }

    // Agenda real del odontólogo para hoy (TurnosDelDia). Refleja lo que efectivamente dejó
    // registrado CUU02 (Presente/Ausente) — no es parte del diccionario de datos de CUU02, es
    // la vista de consulta que el odontólogo necesita para saber a quién puede atender.
    public class TurnoAgendaDelDiaDTO
    {
        public int NroTurno { get; set; }
        public DateTime FechaHoraTurno { get; set; }
        public string NombrePaciente { get; set; } = string.Empty;
        public string ApellidoPaciente { get; set; } = string.Empty;
        public string NombreEspecialidad { get; set; } = string.Empty;
        public string ModalidadPago { get; set; } = string.Empty;
        public string EstadoTurno { get; set; } = string.Empty;
        public string? DescripcionMaterial { get; set; }
    }
}
