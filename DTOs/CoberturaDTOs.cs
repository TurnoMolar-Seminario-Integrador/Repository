namespace DTOs
{
    // =========================================================================
    // CUF01 (alta) / CUF02 (modificación) - OBRA SOCIAL DEL PACIENTE
    // =========================================================================

    // CUF01, RN6: resultado de resolver la obra social declarada en el alta. IdentificadorOS nulo =
    // paciente particular (MU).
    public class ObraSocialDeclaradaResultDTO
    {
        public bool Valida { get; set; }
        public string? IdentificadorOS { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }

    public enum ResultadoActualizarCobertura
    {
        Actualizada,                // CUF02: "Sus datos fueron actualizados correctamente."
        RequiereConfirmacion,       // CUF02 alt 2.a.1: pasar a particular elimina el acuerdo de la cuenta; falta confirmar.
        ObraSocialSinAcuerdo,       // CUF02 alt 2.b.1.a.1: la obra social no posee un acuerdo activo.
        TurnoObraSocialPendiente,   // Hay un turno con modalidad Obra Social pendiente de atención o de cobro.
        ConflictoConcurrencia       // Otro usuario modificó al paciente mientras se guardaba: no se guardó nada.
    }

    public class ActualizarCoberturaResultDTO
    {
        public ResultadoActualizarCobertura Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;

        // Nulo cuando el paciente pasó a particular.
        public string? NombreObraSocial { get; set; }
    }
}
