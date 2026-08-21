using Application.Services;
using DTOs;

namespace WebAPI
{
    public static class OdontologoEndpoints
    {
        public static void MapOdontologoEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/odontologos").WithTags("Odontólogos");

            group.MapGet("/", async (int? especialidadId, IOdontologoService service) =>
            {
                if (especialidadId.HasValue && especialidadId.Value > 0)
                {
                    var result = await service.GetByEspecialidadAsync(especialidadId.Value);
                    return Results.Ok(result);
                }
                var list = await service.GetAllAsync();
                return Results.Ok(list);
            });

            group.MapGet("/{id:int}", async (int id, IOdontologoService service) =>
            {
                var o = await service.GetAsync(id);
                return o == null ? Results.NotFound() : Results.Ok(o);
            });

            group.MapPost("/", async (OdontologoDTO dto, IOdontologoService service) =>
            {
                try
                {
                    var created = await service.AddAsync(dto);
                    return Results.Created($"/api/odontologos/{created.Id}", created);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
            });

            group.MapPut("/{id:int}", async (int id, OdontologoDTO dto, IOdontologoService service) =>
            {
                dto.Id = id;
                try
                {
                    var updated = await service.UpdateAsync(dto);
                    return updated ? Results.Ok(dto) : Results.NotFound();
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
            });

            group.MapDelete("/{id:int}", async (int id, IOdontologoService service) =>
            {
                var deleted = await service.DeleteAsync(id);
                return deleted ? Results.NoContent() : Results.NotFound();
            });
        }
    }
}
