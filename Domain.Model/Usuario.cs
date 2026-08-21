namespace Domain.Model
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty; // "Admin", "Recepcionista", "Odontologo", "Paciente"
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
        public int? EntidadId { get; set; } // Id del Paciente u Odontólogo vinculado, si aplica

        public Usuario() { }

        public Usuario(int id, string username, string passwordHash, string rol, string nombreCompleto, string email, bool activo = true, int? entidadId = null)
        {
            Id = id;
            Username = username;
            PasswordHash = passwordHash;
            Rol = rol;
            NombreCompleto = nombreCompleto;
            Email = email;
            Activo = activo;
            EntidadId = entidadId;
        }
    }
}
