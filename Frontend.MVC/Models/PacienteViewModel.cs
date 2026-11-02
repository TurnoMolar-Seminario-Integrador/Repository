using System.ComponentModel.DataAnnotations;

namespace TurnoMolar.Models
{
    public class PacienteViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string NombrePers { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "Seleccioná un tipo de documento.")]
        public string TipoDocumento { get; set; } = "DNI";

        // Alfanumérico: un pasaporte lleva letras. Que un DNI, LC o LE sea solo numérico lo controla el servicio
        // del alta (DatosPersonalesPacienteValidador), que conoce el tipo elegido. Se guarda como texto: leerlo
        // como int perdía los ceros a la izquierda y rechazaba los documentos con letras.
        [Required(ErrorMessage = "El número de documento es obligatorio.")]
        [StringLength(20, ErrorMessage = "El número de documento no puede superar los 20 caracteres.")]
        [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "Solo se permiten letras y números, sin puntos ni espacios.")]
        public string DniPers { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        [DataType(DataType.Date)]
        public DateTime? FechaNacimiento { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        public string TelefonoPers { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        public string MailPer { get; set; } = string.Empty;

        // Dato requerido del alta (CUF01 paso 1; antes se inventaba "Rosario" si no se cargaba). Es "string?"
        // para que el mensaje lo dé este [Required] y no el requerido implícito de <Nullable>enable</Nullable>.
        [Required(ErrorMessage = "El domicilio es obligatorio.")]
        public string? Domicilio { get; set; }

        public bool EstadoHabilitacion { get; set; } = true;

        // RN3: el paciente establece su contraseña al registrarse. El largo mínimo y el resto de las reglas
        // los controla el servicio del alta (DatosPersonalesPacienteValidador.ValidarClave); acá solo se pide
        // y se confirma. Nunca se vuelve a mostrar en el formulario (ver PacienteController).
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        public string Clave { get; set; } = string.Empty;

        [Required(ErrorMessage = "Repetí la contraseña.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Clave), ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarClave { get; set; } = string.Empty;

        [Required(ErrorMessage = "Seleccioná una obra social.")]
        public string IdObraSocial { get; set; } = string.Empty;

        // Propiedad extra para cuando eligen "Otra". Solo se completa en ese caso: sin el "?" el alta se
        // rechazaba con cualquier otra opción ("requerido" implícito de los strings no nulos).
        public string? OtraObraSocial { get; set; }
    }
}