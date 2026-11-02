using System.Text.RegularExpressions;
using DTOs;

namespace Application.Services
{
    // Validación y normalización de los datos personales del paciente, compartida por CUF01 (paso 1: el
    // paciente ingresa sus datos) y CUF02 (paso 3: "el sistema valida los datos ingresados").
    //
    // Defectos que corrige respecto del alta anterior (PacienteController):
    //  - El número de documento se leía como int: un pasaporte con letras (MU: PAS A9876543) no se podía dar
    //    de alta, y a un DNI con ceros a la izquierda se le perdían, así que después no podía ingresar con el
    //    número que había declarado.
    //  - Si el domicilio no se cargaba se inventaba "Rosario", y sin fecha de nacimiento se inventaba el
    //    1/1/1995. El domicilio y la fecha son datos requeridos del alta (CUF01 paso 1, RN3).
    //  - Los largos no se controlaban: un dato demasiado largo terminaba en un error de base de datos cuyo
    //    texto técnico se le mostraba al paciente.
    public static class DatosPersonalesPacienteValidador
    {
        // Largos de las columnas de Pacientes (ver TurnoMolarDbContext).
        public const int LargoMaxNombre = 100;
        public const int LargoMaxApellido = 100;
        public const int LargoMaxDocumento = 20;
        public const int LargoMaxTelefono = 20;
        public const int LargoMaxEmail = 150;
        public const int LargoMaxDomicilio = 150;

        // RN3: "para registrarse, el usuario ingresa sus datos personales y establece una contraseña". La
        // documentación no fija un largo; el mínimo es una decisión del equipo de desarrollo (ver
        // decisiones-CUF01-03).
        public const int LargoMinClave = 6;
        public const int LargoMaxClave = 100;

        private static readonly Regex PatronSoloDigitos = new(@"^[0-9]+$");
        private static readonly Regex PatronLetrasYNumeros = new(@"^[A-Z0-9]+$");
        private static readonly Regex PatronTelefono = new(@"^[0-9+()\-\s]+$");
        private static readonly Regex PatronEmail = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        // Recorta los textos y deja el tipo y el número de documento en mayúsculas, igual que los guarda el
        // dominio (Persona) y que los busca el login, para que lo declarado y lo almacenado coincidan.
        public static DatosPersonalesPacienteDTO Normalizar(DatosPersonalesPacienteDTO? datos)
        {
            datos ??= new DatosPersonalesPacienteDTO();

            return new DatosPersonalesPacienteDTO
            {
                Nombre = (datos.Nombre ?? string.Empty).Trim(),
                Apellido = (datos.Apellido ?? string.Empty).Trim(),
                TipoDocumento = (datos.TipoDocumento ?? string.Empty).Trim().ToUpperInvariant(),
                NroDocumento = (datos.NroDocumento ?? string.Empty).Trim().ToUpperInvariant(),
                FechaNacimiento = datos.FechaNacimiento?.Date,
                Telefono = (datos.Telefono ?? string.Empty).Trim(),
                Email = (datos.Email ?? string.Empty).Trim(),
                Domicilio = (datos.Domicilio ?? string.Empty).Trim()
            };
        }

        // Devuelve campo -> mensaje; vacío si los datos son válidos. Espera datos ya normalizados.
        public static Dictionary<string, string> Validar(DatosPersonalesPacienteDTO datos)
        {
            var errores = new Dictionary<string, string>();

            var nombre = datos.Nombre ?? string.Empty;
            if (nombre.Length == 0)
                errores["Nombre"] = "El nombre es obligatorio.";
            else if (nombre.Length > LargoMaxNombre)
                errores["Nombre"] = $"El nombre no puede superar los {LargoMaxNombre} caracteres.";

            var apellido = datos.Apellido ?? string.Empty;
            if (apellido.Length == 0)
                errores["Apellido"] = "El apellido es obligatorio.";
            else if (apellido.Length > LargoMaxApellido)
                errores["Apellido"] = $"El apellido no puede superar los {LargoMaxApellido} caracteres.";

            var tipoDocumento = datos.TipoDocumento ?? string.Empty;
            if (tipoDocumento.Length == 0)
                errores["TipoDocumento"] = "Seleccioná un tipo de documento.";
            else if (!TiposDocumento.EsValido(tipoDocumento))
                errores["TipoDocumento"] = "El tipo de documento ingresado no es válido: elegí DNI, LC, LE o Pasaporte.";

            var nroDocumento = datos.NroDocumento ?? string.Empty;
            if (nroDocumento.Length == 0)
                errores["NroDocumento"] = "El número de documento es obligatorio.";
            else if (nroDocumento.Length > LargoMaxDocumento)
                errores["NroDocumento"] = $"El número de documento no puede superar los {LargoMaxDocumento} caracteres.";
            else if (TiposDocumento.EsNumerico(tipoDocumento))
            {
                if (!PatronSoloDigitos.IsMatch(nroDocumento))
                    errores["NroDocumento"] = "Ingresá un número de documento válido: solo dígitos, sin puntos ni espacios.";
            }
            else if (!PatronLetrasYNumeros.IsMatch(nroDocumento))
            {
                errores["NroDocumento"] = "Ingresá un número de documento válido: solo letras y números, sin puntos ni espacios.";
            }

            if (!datos.FechaNacimiento.HasValue)
                errores["FechaNacimiento"] = "La fecha de nacimiento es obligatoria.";
            else if (datos.FechaNacimiento.Value.Date > DateTime.Today)
                errores["FechaNacimiento"] = "La fecha de nacimiento no puede ser futura.";
            else if (datos.FechaNacimiento.Value.Year < 1900)
                errores["FechaNacimiento"] = "Ingresá una fecha de nacimiento válida.";

            var telefono = datos.Telefono ?? string.Empty;
            if (telefono.Length == 0)
                errores["Telefono"] = "El teléfono es obligatorio.";
            else if (telefono.Length > LargoMaxTelefono)
                errores["Telefono"] = $"El teléfono no puede superar los {LargoMaxTelefono} caracteres.";
            else if (!PatronTelefono.IsMatch(telefono))
                errores["Telefono"] = "Ingresá un teléfono válido: solo números, espacios, +, - y paréntesis.";

            var email = datos.Email ?? string.Empty;
            if (email.Length == 0)
                errores["Email"] = "El correo electrónico es obligatorio.";
            else if (email.Length > LargoMaxEmail)
                errores["Email"] = $"El correo electrónico no puede superar los {LargoMaxEmail} caracteres.";
            else if (!PatronEmail.IsMatch(email))
                errores["Email"] = "Formato de correo inválido.";

            var domicilio = datos.Domicilio ?? string.Empty;
            if (domicilio.Length == 0)
                errores["Domicilio"] = "El domicilio es obligatorio.";
            else if (domicilio.Length > LargoMaxDomicilio)
                errores["Domicilio"] = $"El domicilio no puede superar los {LargoMaxDomicilio} caracteres.";

            return errores;
        }

        // RN3: la contraseña la establece el propio paciente al registrarse. Devuelve el mensaje de error o
        // nulo si es válida. El login recorta los espacios de los extremos de lo que se escribe (ver
        // HomeController.Login), así que una contraseña que empiece o termine con espacios nunca coincidiría.
        public static string? ValidarClave(string? clave)
        {
            if (string.IsNullOrEmpty(clave))
                return "La contraseña es obligatoria.";
            if (clave != clave.Trim())
                return "La contraseña no puede empezar ni terminar con espacios.";
            if (clave.Length < LargoMinClave)
                return $"La contraseña debe tener al menos {LargoMinClave} caracteres.";
            if (clave.Length > LargoMaxClave)
                return $"La contraseña no puede superar los {LargoMaxClave} caracteres.";
            return null;
        }
    }
}
