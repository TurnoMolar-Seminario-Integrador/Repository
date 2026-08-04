using System.ComponentModel.DataAnnotations;

namespace TurnoMolar.Models
{
    public class PacienteViewModel
    {
        // Datos heredados de Persona
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string NombrePers { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El DNI es obligatorio.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "El DNI debe contener solo números.")]
        public string DniPers { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        public string TelefonoPers { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        public string MailPer { get; set; } = string.Empty;

        public string Domicilio { get; set; } = string.Empty;

        // Datos específicos de Paciente
        // Para el alta (CUF01), generalmente el estado de habilitación arranca en true
        public bool EstadoHabilitacion { get; set; } = true;

        // Relación con Obra Social (usualmente un desplegable en la vista)
        public int? IdObraSocial { get; set; }
    }
}