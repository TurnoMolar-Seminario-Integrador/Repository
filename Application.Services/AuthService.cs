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
        Task<UsuarioDTO?> GetUsuarioActualAsync(int userId);
        Task<UsuarioDTO> RegistrarUsuarioAsync(UsuarioDTO dto, string password);
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
            var user = await _usuarioRepository.GetByUsernameAsync(request.Username);
            if (user == null || !user.Activo)
                return null;

            // Validación de contraseña (plana o hash)
            if (user.PasswordHash != request.Password)
            {
                return null;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(SecretKey);
            var expiration = DateTime.UtcNow.AddDays(7);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Rol),
                    new Claim("NombreCompleto", user.NombreCompleto),
                    new Claim("Email", user.Email),
                    new Claim("EntidadId", user.EntidadId?.ToString() ?? "")
                }),
                Expires = expiration,
                Issuer = Issuer,
                Audience = Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return new LoginResponseDTO
            {
                Token = tokenString,
                Username = user.Username,
                Rol = user.Rol,
                NombreCompleto = user.NombreCompleto,
                UserId = user.Id,
                EntidadId = user.EntidadId,
                Expiration = expiration
            };
        }

        public async Task<UsuarioDTO?> GetUsuarioActualAsync(int userId)
        {
            var user = await _usuarioRepository.GetByIdAsync(userId);
            if (user == null) return null;

            return new UsuarioDTO
            {
                Id = user.Id,
                Username = user.Username,
                Rol = user.Rol,
                NombreCompleto = user.NombreCompleto,
                Email = user.Email,
                Activo = user.Activo,
                EntidadId = user.EntidadId
            };
        }

        public async Task<UsuarioDTO> RegistrarUsuarioAsync(UsuarioDTO dto, string password)
        {
            var user = new Usuario(
                0,
                dto.Username,
                password,
                dto.Rol,
                dto.NombreCompleto,
                dto.Email,
                true,
                dto.EntidadId
            );

            await _usuarioRepository.AddAsync(user);
            dto.Id = user.Id;
            return dto;
        }
    }
}
