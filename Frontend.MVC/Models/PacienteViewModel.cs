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

        [Required(ErrorMessage = "El número de documento es obligatorio.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Solo se permiten números.")]
        public string DniPers { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        [DataType(DataType.Date)]
        public DateTime? FechaNacimiento { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        public string TelefonoPers { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        public string MailPer { get; set; } = string.Empty;

        public string Domicilio { get; set; } = string.Empty;

        public bool EstadoHabilitacion { get; set; } = true;

        [Required(ErrorMessage = "Seleccioná una obra social.")]
        public string IdObraSocial { get; set; } = string.Empty;

        // Propiedad extra para cuando eligen "Otra"
        public string OtraObraSocial { get; set; } = string.Empty;
    }
}