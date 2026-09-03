namespace DTOs
{
    // Representa la identidad autenticada, sin importar si salió de Pacientes,
    // Odontologos o ResponsablesClinica. No mapea a ninguna tabla propia.
    public class UsuarioDTO
    {
        public string TipoDocumento { get; set; } = string.Empty;
        public string NroDocumento { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty; // "Paciente" | "Odontologo" | "Responsable de la Clínica" | "Admin"
        public string? Email { get; set; }
    }
 
    public class LoginRequestDTO
    {
        public string TipoDocumento { get; set; } = string.Empty;
        public string NroDocumento { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
 
    public class LoginResponseDTO
    {
        // Lo sigue completando la WebAPI para clientes JWT (ej. Blazor.WebAssembly).
        // El Frontend.MVC lo ignora porque autentica con cookie, no con token.
        public string Token { get; set; } = string.Empty;
        public string TipoDocumento { get; set; } = string.Empty;
        public string NroDocumento { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
    }
}