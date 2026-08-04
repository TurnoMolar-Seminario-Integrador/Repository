using System.ComponentModel.DataAnnotations;

namespace TurnoMolar.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El DNI es obligatorio.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Ingresá tu DNI sin puntos ni letras.")]
        public string Dni { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        public string Contrasena { get; set; } = string.Empty;
    }
}