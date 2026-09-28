namespace DTOs
{
    // =========================================================================
    // CUF07 - CANCELAR TURNO
    // =========================================================================
    public enum ResultadoCancelarTurno
    {
        Cancelado,
        TurnoInvalido
    }

    public class CancelarTurnoResultDTO
    {
        public ResultadoCancelarTurno Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;

        // Null cuando la cancelación no generó penalización (>= 24 hs de anticipación).
        public decimal? Penalizacion { get; set; }
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

    // =========================================================================
    // CUU01 alt 2.a.1.a / CUF10 - PAGAR DEUDA
    // =========================================================================
    public enum ResultadoPagarDeuda
    {
        Regularizada,
        SinDeuda
    }

    public class PagarDeudaResultDTO
    {
        public ResultadoPagarDeuda Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public decimal MontoPagado { get; set; }
    }
}
