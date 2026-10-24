namespace DTOs
{
    // =========================================================================
    // CUF07 - CANCELAR TURNO
    // =========================================================================
    public enum ResultadoCancelarTurno
    {
        Cancelado,
        TurnoInvalido,

        // CUF07 alt 3.a.1: la cancelación se pide fuera de término (menos de 24 hs, o el turno ya pasó).
        // No se cancela nada: el sistema informa y advierte sobre el cobro (Mensaje y Penalizacion) y
        // espera la confirmación (alt 3.a.2: volver a llamar con confirmaFueraDeTermino = true).
        RequiereConfirmacion
    }

    public class CancelarTurnoResultDTO
    {
        public ResultadoCancelarTurno Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;

        // Null cuando la cancelación no genera penalización (>= 24 hs de anticipación). Con
        // RequiereConfirmacion es el monto que se cobraría si se confirma.
        public decimal? Penalizacion { get; set; }

        // Diccionario, alt 3.a.2: sConfirmacionCancelacionFueraDeTermino = estadoTurno + montoAdeudado +
        // estadoPaciente. Solo se completan cuando la cancelación fuera de término se confirmó.
        public string? EstadoTurno { get; set; }
        public decimal? MontoAdeudado { get; set; }
        public string? EstadoPaciente { get; set; }

        // Datos del turno para armar la advertencia (RequiereConfirmacion).
        public DateTime? FechaHoraTurno { get; set; }
        public string? NombreEspecialidad { get; set; }
    }

    // =========================================================================
    // CUF08 - REPROGRAMAR TURNO
    // =========================================================================
    public enum ResultadoReprogramarTurno
    {
        Reprogramado,
        TurnoInvalido,
        HorarioNoDisponible,
        FueraDeTermino
    }

    public class ReprogramarTurnoResultDTO
    {
        public ResultadoReprogramarTurno Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int? NroTurnoNuevo { get; set; }
    }
}
