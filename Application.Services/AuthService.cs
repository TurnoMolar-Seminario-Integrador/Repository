using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Data;
using Domain.Model;
using DTOs;
using Microsoft.IdentityModel.Tokens;

namespace Application.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO request);
    }
 
    public class AuthService : IAuthService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        public const string SecretKey = "TurnoMolarSuperSecretSecurityKeyForJWTAuthentication2026";
        public const string Issuer = "TurnoMolarAPI";
        public const string Audience = "TurnoMolarClients";
 
        public AuthService(IUsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }
 
        public async Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.TipoDocumento) || string.IsNullOrWhiteSpace(request.NroDocumento))
                return null;
 
            var candidatos = await _usuarioRepository.BuscarCandidatosAsync(
                request.TipoDocumento.Trim().ToUpper(),
                request.NroDocumento.Trim().ToUpper());
 
            // Si la misma persona tiene cuenta en más de un rol, la contraseña
            // ingresada es lo que desambigua cuál de las cuentas es.
            var usuario = candidatos.FirstOrDefault(u =>
                PasswordHasher.Validar(request.Password, u.Clave, u.SaltClave));
 
            if (usuario == null)
                return null;
 
            return GenerarRespuesta(usuario);
        }
 
        private LoginResponseDTO GenerarRespuesta(Usuario usuario)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(SecretKey);
            var expiration = DateTime.UtcNow.AddDays(7);
 
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, $"{usuario.TipoDocumento}|{usuario.NroDocumento}"),
                    new Claim("TipoDocumento", usuario.TipoDocumento),
                    new Claim("NroDocumento", usuario.NroDocumento),
                    new Claim(ClaimTypes.Role, usuario.Rol),
                    new Claim("Nombre", usuario.Nombre),
                    new Claim("Apellido", usuario.Apellido),
                    new Claim("Email", usuario.Email ?? string.Empty)
                }),
                Expires = expiration,
                Issuer = Issuer,
                Audience = Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
 
            var token = tokenHandler.CreateToken(tokenDescriptor);
 
            return new LoginResponseDTO
            {
                Token = tokenHandler.WriteToken(token),
                TipoDocumento = usuario.TipoDocumento,
                NroDocumento = usuario.NroDocumento,
                NombreCompleto = $"{usuario.Nombre} {usuario.Apellido}",
                Rol = usuario.Rol,
                Expiration = expiration
            };
        }
    }
}