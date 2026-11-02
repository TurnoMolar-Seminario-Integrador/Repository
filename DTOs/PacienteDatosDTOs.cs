namespace DTOs
{
    // =========================================================================
    // CUF01 - INCORPORAR PACIENTE / CUF02 - RECTIFICAR DATOS DEL PACIENTE
    // =========================================================================

    // Diccionario de datos: datosPersonales(e) = nombre + apellido + identificadorPersona(e) +
    // fechaNacimiento + telefono + email + domicilio(e), con identificadorPersona(e) = tipoDocumento +
    // nroDocumento. Es eDatosPersonalesPaciente en CUF01 y sDatosPersonalesActuales / eDatoAModificar en
    // CUF02. Todo es nulable a propósito: la validación (y sus mensajes) la hace el servicio, no el binder
    // del formulario.
    public class DatosPersonalesPacienteDTO
    {
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? TipoDocumento { get; set; } = "DNI";
        public string? NroDocumento { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Domicilio { get; set; }
    }

    // CUF02, paso 1: los datos registrados del paciente, más lo necesario para mostrar su cobertura.
    public class DatosActualesPacienteDTO : DatosPersonalesPacienteDTO
    {
        public string EstadoPaciente { get; set; } = string.Empty;

        // Huella de los datos personales tal como están registrados. El formulario la devuelve al guardar: si
        // no coincide con lo que hay en la base, otra persona los modificó mientras se editaban (concurrencia
        // optimista; ver VersionDeDatos).
        public string VersionDatos { get; set; } = string.Empty;

        // Nulo = paciente particular (RN6 / MU).
        public string? IdentificadorOS { get; set; }
        public string? NombreObraSocial { get; set; }
    }

    // ---------------------------------------------------------------------
    // CUF01 - Incorporar Paciente
    // ---------------------------------------------------------------------
    public enum ResultadoIncorporarPaciente
    {
        Incorporado,            // Paso 3: paciente Habilitado, con número de historia clínica y su HC creada.
        PacienteYaRegistrado,   // Alt 1.a.1: ya existe un paciente con ese tipo y número de documento.
        DatosInvalidos,         // Paso 1: los datos personales no pasan la validación (ver Errores).
        ObraSocialInvalida      // Alt 2.a.1.a.1: la obra social no posee acuerdo activo (o la selección está incompleta).
    }

    public class IncorporarPacienteRequestDTO
    {
        // Paso 1: eDatosPersonalesPaciente.
        public DatosPersonalesPacienteDTO DatosPersonales { get; set; } = new();

        // Paso 2: eCoberturaPaciente. "Particular", el identificador de una obra social o "Otra" (con el
        // nombre en OtraObraSocial; alt 2.a.1: eDatosObraSocialPaciente).
        public string? ObraSocial { get; set; }
        public string? OtraObraSocial { get; set; }

        // RN3: el paciente establece su propia contraseña al registrarse (no hay una provisoria fija).
        public string? Password { get; set; }
    }

    // Diccionario, paso 3: sConfirmacionAltaPaciente = estadoPaciente(d) + nroHC.
    public class IncorporarPacienteResultDTO
    {
        public ResultadoIncorporarPaciente Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;

        // Con DatosInvalidos: campo (Nombre, Apellido, TipoDocumento, NroDocumento, FechaNacimiento,
        // Telefono, Email, Domicilio, Password) -> mensaje.
        public Dictionary<string, string> Errores { get; set; } = new();

        // Solo con Incorporado.
        public string? TipoDocumento { get; set; }
        public string? NroDocumento { get; set; }
        public string? EstadoPaciente { get; set; }
        public int? NroHC { get; set; }
    }

    // ---------------------------------------------------------------------
    // CUF02 - Rectificar Datos del Paciente
    // ---------------------------------------------------------------------
    public enum ResultadoRectificarDatos
    {
        Actualizada,            // Paso 3: "Sus datos fueron actualizados correctamente."
        SinCambios,             // Los valores ingresados son los que ya estaban registrados.
        DatosInvalidos,         // Paso 3: el sistema valida los datos ingresados (ver Errores).
        DocumentoYaRegistrado,  // El tipo y número de documento nuevos ya pertenecen a otro paciente.
        PacienteNoEncontrado,   // Precondición: el paciente está registrado.
        ConflictoConcurrencia   // Otro usuario modificó los datos del paciente mientras se editaban: no se guardó nada.
    }

    public class RectificarDatosResultDTO
    {
        public ResultadoRectificarDatos Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public Dictionary<string, string> Errores { get; set; } = new();

        // Identificación vigente del paciente tras la operación (cambia si se rectificó el documento).
        public string? TipoDocumento { get; set; }
        public string? NroDocumento { get; set; }
        public string? NombreCompleto { get; set; }
        public bool CambioElDocumento { get; set; }
    }
}
