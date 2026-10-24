namespace DTOs
{
    // =========================================================================
    // CUU01 alt 2.a.1.a / CUF10 - REGULARIZAR DEUDA DEL PACIENTE
    // =========================================================================
    public enum ResultadoPagarDeuda
    {
        Regularizada,       // Paso 3: pago registrado para el turno que originó la deuda; paciente Habilitado.
        SinDeuda,           // Alt 1.a: "No hay saldo de deuda pendiente para regularizar."
        MetodoInvalido,     // eDatosPagoDeuda: el tipoMetodoPago no es uno de los admitidos.
        SinTurnoDeOrigen    // Hay saldo pero no se pudo identificar el turno que lo originó: no hay dónde registrar el pago.
    }

    // Diccionario, paso 1: sDeudaPendienteConMotivos = montoAdeudado + nroTurno + fechaYHoraTurno +
    // motivoDeuda(d). Sin deuda, Mensaje lleva mensajeSinDeuda (alt 1.a).
    public class DeudaPacienteDTO
    {
        public string TipoDocumento { get; set; } = "DNI";
        public string NroDocumento { get; set; } = string.Empty;
        public string NombrePaciente { get; set; } = string.Empty;
        public string EstadoPaciente { get; set; } = string.Empty;

        public bool TieneDeuda { get; set; }
        public decimal MontoAdeudado { get; set; }

        // Turno que originó la deuda. Null si no se pudo identificar (inconsistencia de datos).
        public int? NroTurno { get; set; }
        public DateTime? FechaHoraTurno { get; set; }

        // "Inasistencia" | "Cancelación fuera de término" | "Falta de pago de la atención".
        public string? MotivoDeuda { get; set; }

        public string Mensaje { get; set; } = string.Empty;
    }

    // Diccionario, paso 3: sConfirmacionPagoDeuda = monto + estadoPaciente(d).
    public class PagarDeudaResultDTO
    {
        public ResultadoPagarDeuda Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public decimal MontoPagado { get; set; }

        // Solo con Regularizada.
        public int? NroTurno { get; set; }
        public string? MetodoPago { get; set; }
        public string? EstadoPaciente { get; set; }
    }
}
