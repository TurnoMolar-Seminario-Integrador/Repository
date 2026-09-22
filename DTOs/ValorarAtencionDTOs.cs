namespace DTOs
{
    // CUU04 - Valorar Atención Odontológica.
    // Camino básico, paso 1 / Alt 1.a: resultado de consultar las atenciones finalizadas
    // pendientes de valoración.
    public enum ResultadoPendientesDeValoracion
    {
        Ok,             // Hay al menos una atención pendiente de valorar.
        SinPendientes   // Alt 1.a <durante>: no hay ninguna. FCU.
    }

    // Diccionario de datos, paso 1: sListadoAtencionesPendientesValoracion =
    // 1{nroTurno + fechaYHoraAtencionInicio + nombreEspecialidad + nombreOdontologo}N.
    // Alt 1.a.1: sMensajeSinAtencionesPendientes (mensaje literal).
    public class PendientesDeValoracionResultDTO
    {
        public ResultadoPendientesDeValoracion Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public List<AtencionPendienteValoracionDTO> Pendientes { get; set; } = new();
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
                                // "Finalizado", el paciente no está "Habilitado", o la
                                // atención ya tiene una Valoracion registrada (RN14).
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
