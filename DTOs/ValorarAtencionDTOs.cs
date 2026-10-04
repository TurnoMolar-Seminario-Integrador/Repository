namespace DTOs
{
    // CUU04 - Valorar Atención Odontológica.
    // Camino básico, paso 1 / Alt 1.a: resultado de consultar las atenciones finalizadas y
    // cobradas pendientes de valoración.
    public enum ResultadoPendientesDeValoracion
    {
        Ok,                 // Hay al menos una atención pendiente de valorar.
        SinPendientes,      // Alt 1.a <durante>: no hay ninguna. FCU.
        PendientesPorDeuda  // No hay ninguna valorable, pero sí atenciones finalizadas con el cobro
                            // pendiente (CUU03 alt 6.b): se habilitan al regularizar la deuda
                            // (CUF10). Agregado a CUU04 v1.03: pendiente de reflejar como alt 1.b.
    }

    // Diccionario de datos, paso 1: sListadoAtencionesPendientesValoracion =
    // 1{nroTurno + fechaYHoraAtencionInicio + nombreEspecialidad + nombreOdontologo}N.
    // Alt 1.a.1: sMensajeSinAtencionesPendientes (mensaje literal).
    public class PendientesDeValoracionResultDTO
    {
        public ResultadoPendientesDeValoracion Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public List<AtencionPendienteValoracionDTO> Pendientes { get; set; } = new();
        // Atenciones finalizadas y sin valoración cuyo cobro está pendiente. No se pueden
        // valorar todavía; se informan para que el paciente sepa que las tiene pendientes.
        public List<AtencionPendienteValoracionDTO> PendientesPorDeuda { get; set; } = new();
    }

    public class AtencionPendienteValoracionDTO
    {
        public int NroTurno { get; set; }
        public DateTime FechaHoraAtencionInicio { get; set; }
        public string NombreEspecialidad { get; set; } = string.Empty;
        public string NombreOdontologo { get; set; } = string.Empty;
    }

    // Camino básico, paso 2 / Alt 2.a: resultado de registrar la valoración.
    public enum ResultadoRegistrarValoracion
    {
        Registrada,             // Éxito (con o sin observaciones -- Alt 2.a).
        NoDisponible,           // El turno no existe, no es del paciente logueado, no está
                                // "Finalizado", no tiene el pago de la atención registrado, o
                                // la atención ya tiene una Valoracion registrada (RN14).
        CalificacionInvalida,   // La calificación no está entre 1 y 5 (Valoracion.SetCalificacion).
        ObservacionesInvalidas  // Observaciones de más de 500 caracteres (límite del MDF).
    }

    // Diccionario de datos, paso 2: eDatosValoracion = nroTurno + valoracion(e), donde
    // valoracion(e) = calificacion + (observaciones).
    public class RegistrarValoracionResultDTO
    {
        public ResultadoRegistrarValoracion Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }
}
