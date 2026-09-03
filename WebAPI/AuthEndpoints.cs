using Application.Services;
using DTOs;
using System.Security.Claims;

namespace WebAPI
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/auth").WithTags("Autenticación");
 
            group.MapPost("/login", async (LoginRequestDTO request, IAuthService authService) =>
            {
                var response = await authService.LoginAsync(request);
                if (response == null)
                    return Results.Unauthorized();
 
                return Results.Ok(response);
            }).AllowAnonymous();
 
            // El token JWT ya trae todo lo necesario (ver claims en AuthService),
            // no hace falta volver a consultar la base para responder "quién soy".
            group.MapGet("/me", (ClaimsPrincipal user) =>
            {
                var tipoDocumento = user.FindFirst("TipoDocumento")?.Value;
                var nroDocumento = user.FindFirst("NroDocumento")?.Value;
                if (string.IsNullOrEmpty(tipoDocumento) || string.IsNullOrEmpty(nroDocumento))
                    return Results.Unauthorized();
 
                var dto = new UsuarioDTO
                {
                    TipoDocumento = tipoDocumento,
                    NroDocumento = nroDocumento,
                    Nombre = user.FindFirst("Nombre")?.Value ?? string.Empty,
                    Apellido = user.FindFirst("Apellido")?.Value ?? string.Empty,
                    Rol = user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty,
                    Email = user.FindFirst("Email")?.Value
                };
 
                return Results.Ok(dto);
            }).RequireAuthorization();
        }
    }
}
