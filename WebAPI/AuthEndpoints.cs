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

            group.MapGet("/me", async (ClaimsPrincipal user, IAuthService authService) =>
            {
                var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out var userId))
                    return Results.Unauthorized();

                var usuario = await authService.GetUsuarioActualAsync(userId);
                return usuario == null ? Results.NotFound() : Results.Ok(usuario);
            }).RequireAuthorization();
        }
    }
}
