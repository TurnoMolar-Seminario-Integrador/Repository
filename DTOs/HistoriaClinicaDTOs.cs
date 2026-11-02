namespace DTOs
{
    // =========================================================================
    // CUF03 - ADMINISTRAR HISTORIA CLÍNICA (actor primario: Odontólogo)
    // =========================================================================

    public enum ResultadoBusquedaHistoriaClinica
    {
        Encontrada,             // Paso 1: el paciente existe; se muestran sus datos y su historial (puede estar vacío, alt 2.a).
        BusquedaIncompleta,     // Faltan el tipo o el número de documento (eBusquedaPaciente).
        PacienteNoEncontrado,   // Paso 1: "El sistema valida la existencia del paciente".
        SinHistoriaClinica      // Precondición de sistema incumplida: el paciente no tiene su historia clínica creada.
    }

    // Diccionario, paso 2: detalleInsumoUtilizado(e) = nombreInsumo + cantidadUtilizada + costoUnitarioAlMomento.
    public class InsumoUtilizadoDTO
    {
        public string NombreInsumo { get; set; } = string.Empty;
        public int CantidadUtilizada { get; set; }
        public decimal CostoUnitarioAlMomento { get; set; }
    }

    // Diccionario, paso 2: sHistorialAtenciones = 1{fechaYHoraAtencionInicio + nombreEspecialidad +
    // nombreOdontologo + observaciones + 0{detalleInsumoUtilizado(e)}N}N. NroTurno identifica la atención
    // (eSeleccionAtencion = nroTurno).
    public class AtencionHistorialDTO
    {
        public int NroTurno { get; set; }
        public DateTime FechaHoraInicio { get; set; }
        public string NombreEspecialidad { get; set; } = string.Empty;
        public string NombreOdontologo { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public List<InsumoUtilizadoDTO> Insumos { get; set; } = new();
    }

    // Diccionario, paso 3: sDetalleAtencion = atencionOdontologica(e) + nombreEspecialidad +
    // nombreOdontologo + 0{detalleInsumoUtilizado(e)}N, con atencionOdontologica(e) =
    // fechaYHoraAtencionInicio + fechaYHoraAtencionFin + /duracionReal + /montoTotal + observaciones.
    public class DetalleAtencionHistorialDTO
    {
        public int NroTurno { get; set; }
        public DateTime FechaHoraInicio { get; set; }
        public DateTime FechaHoraFin { get; set; }
        public TimeSpan DuracionReal { get; set; }
        public decimal MontoTotal { get; set; }
        public string Observaciones { get; set; } = string.Empty;

        // Huella de las observaciones tal como están registradas; el formulario de corrección la devuelve al
        // guardar para detectar que otro odontólogo las modificó mientras se editaban (ver VersionDeDatos).
        public string VersionObservaciones { get; set; } = string.Empty;

        public string NombreEspecialidad { get; set; } = string.Empty;
        public string NombreOdontologo { get; set; } = string.Empty;
        public List<InsumoUtilizadoDTO> Insumos { get; set; } = new();
    }

    // Pasos 1 y 2: sDatosPersonalesPaciente + el historial cronológico de atenciones.
    public class HistoriaClinicaPacienteDTO
    {
        public ResultadoBusquedaHistoriaClinica Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;

        // Solo con Encontrada.
        public int? NroHC { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public string TipoDocumento { get; set; } = string.Empty;
        public string NroDocumento { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public DateTime FechaNacimiento { get; set; }
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Domicilio { get; set; } = string.Empty;

        // En orden cronológico (de la más antigua a la más reciente).
        public List<AtencionHistorialDTO> Atenciones { get; set; } = new();

        // Alt 2.a.1: la historia clínica se encuentra vacía (lleva mensajeHistoriaVacia en Mensaje).
        public bool Vacia => Resultado == ResultadoBusquedaHistoriaClinica.Encontrada && Atenciones.Count == 0;
    }

    public enum ResultadoCorreccionObservaciones
    {
        Corregida,              // Alt 3.a.1: "Las observaciones de la atención fueron actualizadas correctamente."
        AtencionNoEncontrada,   // La atención no pertenece a la historia clínica indicada.
        ObservacionesInvalidas, // Observaciones vacías o más largas de lo que admite el registro.
        ConflictoConcurrencia   // Otro odontólogo modificó las observaciones mientras se editaban: no se guardó nada.
    }

    // Diccionario, alt 3.a.1: eObservacionesCorregidas = nroTurno + observaciones; sConfirmacionCorreccion.
    public class CorreccionObservacionesResultDTO
    {
        public ResultadoCorreccionObservaciones Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;

        // Solo con ConflictoConcurrencia: el texto vigente y su versión, para que el odontólogo los compare con
        // lo que había escrito y vuelva a corregir sobre lo último.
        public string? ObservacionesVigentes { get; set; }
        public string? VersionObservaciones { get; set; }
    }
}
