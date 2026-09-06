namespace DTOs
{
    // Resultado posible de intentar agendar un turno (CUU01, sección 8 - Flujo de sucesos).
    // Cada valor corresponde a uno de los caminos (básico o alternativo) descriptos en
    // G25-CUU-AgendarTurno-E2.R02.pdf.
    public enum ResultadoAgendarTurno
    {
        Reservado,                 // Camino básico, paso 6: éxito.
        Inhabilitado,               // Alt 2.a: paciente con deuda pendiente.
        TurnoPendienteExistente,    // Alt 2.b: ya tiene un turno reservado pendiente de atención (RN8).
        ObraSocialSinConvenio,      // Alt 3.a.2.a / 5.b: la obra social no cubre la especialidad.
        HorarioNoDisponible         // El horario elegido ya no está libre (choque de agenda).
    }

    // Resultado de la validación de estado del paciente al consultar turnos disponibles
    // (CUU01, camino básico, paso 2).
    public class EstadoPacienteTurnoDTO
    {
        public bool Habilitado { get; set; }
        public decimal MontoAdeudado { get; set; }
        public bool TieneTurnoPendiente { get; set; }
        public TurnoOdontologicoDTO? TurnoPendiente { get; set; }
    }

    // Un horario puntual disponible para agendar (CUU01, diccionario de datos:
    // sHorariosDisponiblesDia = 0{fechaYHoraTurno + nombreEspecialidad + nombreOdontologo}N).
    public class HorarioDisponibleDTO
    {
        public DateTime FechaHoraTurno { get; set; }
        public int IdEspecialidad { get; set; }
        public string NombreEspecialidad { get; set; } = string.Empty;
        public string OdontologoTipoDocumento { get; set; } = "DNI";
        public string OdontologoNroDocumento { get; set; } = string.Empty;
        public string NombreOdontologo { get; set; } = string.Empty;
    }

    // Comprobante de turno (CUU01, diccionario de datos: sComprobanteTurno).
    public class ComprobanteTurnoDTO
    {
        public int NroComprobante { get; set; }
        public DateTime FechaHoraEmision { get; set; }
        public DateTime FechaHoraTurno { get; set; }
        public string NombrePaciente { get; set; } = string.Empty;
        public string ApellidoPaciente { get; set; } = string.Empty;
        public string NombreEspecialidad { get; set; } = string.Empty;
        public string NombreOdontologo { get; set; } = string.Empty;
        public string ApellidoOdontologo { get; set; } = string.Empty;
    }

    // Datos necesarios para agendar un turno (camino básico, pasos 4 a 6).
    public class AgendarTurnoRequestDTO
    {
        public string TipoDocumentoPaciente { get; set; } = "DNI";
        public string NroDocumentoPaciente { get; set; } = string.Empty;
        public int IdEspecialidad { get; set; }
        public string OdontologoTipoDocumento { get; set; } = "DNI";
        public string OdontologoNroDocumento { get; set; } = string.Empty;
        public DateTime FechaHoraTurno { get; set; }
        public string ModalidadPago { get; set; } = "PARTICULAR"; // "PARTICULAR" | "OBRA_SOCIAL"
    }

    public class AgendarTurnoResultDTO
    {
        public ResultadoAgendarTurno Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int? NroTurno { get; set; }
        public decimal? MontoAdeudado { get; set; }
        public TurnoOdontologicoDTO? TurnoPendiente { get; set; }
        public ComprobanteTurnoDTO? Comprobante { get; set; }
    }
}