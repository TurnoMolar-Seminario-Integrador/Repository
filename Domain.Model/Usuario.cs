namespace Domain.Model
{
    // Representa la identidad resuelta durante el login. NO es una entidad persistida:
    // no tiene DbSet ni tabla propia. Se arma en memoria con los datos de la fila real
    // encontrada en Pacientes, Odontologos o ResponsablesClinica (el MDF v1.01 no tiene
    // una tabla "Usuarios" — Clave/SaltClave/Rol viven en cada una de esas tres tablas).
    public class Usuario
    {
        public string TipoDocumento { get; set; } = string.Empty;
        public string NroDocumento { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Rol { get; set; } = string.Empty;
 
        // Solo para validar la contraseña dentro de AuthService. No debe salir
        // de la capa de aplicación (UsuarioDTO no tiene estos dos campos).
        public string Clave { get; set; } = string.Empty;
        public string SaltClave { get; set; } = string.Empty;
    }
}
