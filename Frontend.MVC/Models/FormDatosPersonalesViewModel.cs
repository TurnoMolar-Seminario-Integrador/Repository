using DTOs;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Frontend.MVC.Models
{
    // CUF02 - Rectificar Datos del Paciente: formulario de los ocho datos personales (nombre, apellido,
    // tipo y número de documento, fecha de nacimiento, teléfono, email y domicilio). Lo usan el paciente
    // (Configuración) y el responsable de la clínica (Rectificar datos del paciente) a través del parcial
    // _FormDatosPersonales.
    //
    // Todo es nulable a propósito: con <Nullable>enable</Nullable> un string no nulo es "requerido" para el
    // binder y el mensaje saldría del framework. Los mensajes de validación los da el servicio
    // (DatosPersonalesPacienteValidador), compartidos con el alta (CUF01).
    public class FormDatosPersonalesViewModel
    {
        // Documento con el que el paciente figura HOY. Lo usa solo el responsable para identificar al
        // paciente; en el portal del paciente se ignora y se usa el de la sesión.
        public string? TipoDocumentoActual { get; set; }
        public string? NroDocumentoActual { get; set; }

        // Concurrencia: huella de los datos que se mostraron (DatosActualesPacienteDTO.VersionDatos). Viaja en
        // un campo oculto y el servicio la compara con la base al guardar.
        public string? VersionDatos { get; set; }

        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? TipoDocumento { get; set; }
        public string? NroDocumento { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Domicilio { get; set; }

        // A dónde envía el parcial el formulario. No vienen del formulario.
        [BindNever]
        public string Controlador { get; set; } = "Home";

        [BindNever]
        public string Accion { get; set; } = "ActualizarPerfil";

        // Campo del servicio (Nombre, Apellido, TipoDocumento, NroDocumento, FechaNacimiento, Telefono,
        // Email, Domicilio) -> mensaje. Vacío si no hay errores.
        [BindNever]
        public Dictionary<string, string> Errores { get; set; } = new();

        public DatosPersonalesPacienteDTO ADatos() => new()
        {
            Nombre = Nombre,
            Apellido = Apellido,
            TipoDocumento = TipoDocumento,
            NroDocumento = NroDocumento,
            FechaNacimiento = FechaNacimiento,
            Telefono = Telefono,
            Email = Email,
            Domicilio = Domicilio
        };

        // Formulario cargado con los datos registrados del paciente (CUF02, paso 1).
        public static FormDatosPersonalesViewModel Desde(DatosActualesPacienteDTO datos, string controlador, string accion) => new()
        {
            TipoDocumentoActual = datos.TipoDocumento,
            NroDocumentoActual = datos.NroDocumento,
            VersionDatos = datos.VersionDatos,
            Nombre = datos.Nombre,
            Apellido = datos.Apellido,
            TipoDocumento = datos.TipoDocumento,
            NroDocumento = datos.NroDocumento,
            FechaNacimiento = datos.FechaNacimiento,
            Telefono = datos.Telefono,
            Email = datos.Email,
            Domicilio = datos.Domicilio,
            Controlador = controlador,
            Accion = accion
        };
    }
}
