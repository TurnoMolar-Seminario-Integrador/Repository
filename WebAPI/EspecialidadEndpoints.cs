using Application.Services;
using DTOs;

namespace WebAPI
{
    public static class EspecialidadEndpoints
    {
        public static void MapEspecialidadEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/especialidades").WithTags("Especialidades");

            group.MapGet("/", async (IEspecialidadService service) =>
            {
                var list = await service.GetAllAsync();
                return Results.Ok(list);
            });

            group.MapGet("/{id:int}", async (int id, IEspecialidadService service) =>
            {
                var item = await service.GetAsync(id);
                return item == null ? Results.NotFound() : Results.Ok(item);
            });

            group.MapPost("/", async (EspecialidadDTO dto, IEspecialidadService service) =>
            {
                var created = await service.AddAsync(dto);
                return Results.Created($"/api/especialidades/{created.Id}", created);
            });

            group.MapPut("/{id:int}", async (int id, EspecialidadDTO dto, IEspecialidadService service) =>
            {
                dto.Id = id;
                var updated = await service.UpdateAsync(dto);
                return updated ? Results.Ok(dto) : Results.NotFound();
            });

            group.MapDelete("/{id:int}", async (int id, IEspecialidadService service) =>
            {
                var deleted = await service.DeleteAsync(id);
                return deleted ? Results.NoContent() : Results.NotFound();
            });
        }
    }
}
