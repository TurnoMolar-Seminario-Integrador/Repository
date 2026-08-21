using Application.Services;
using DTOs;

namespace WebAPI
{
    public static class InsumoEndpoints
    {
        public static void MapInsumoEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/insumos").WithTags("Insumos");

            group.MapGet("/", async (IInsumoService service) =>
            {
                var list = await service.GetAllAsync();
                return Results.Ok(list);
            });

            group.MapGet("/{id:int}", async (int id, IInsumoService service) =>
            {
                var item = await service.GetAsync(id);
                return item == null ? Results.NotFound() : Results.Ok(item);
            });

            group.MapPost("/", async (InsumoDTO dto, IInsumoService service) =>
            {
                var created = await service.AddAsync(dto);
                return Results.Created($"/api/insumos/{created.Id}", created);
            });

            group.MapPut("/{id:int}", async (int id, InsumoDTO dto, IInsumoService service) =>
            {
                dto.Id = id;
                var updated = await service.UpdateAsync(dto);
                return updated ? Results.Ok(dto) : Results.NotFound();
            });

            group.MapDelete("/{id:int}", async (int id, IInsumoService service) =>
            {
                var deleted = await service.DeleteAsync(id);
                return deleted ? Results.NoContent() : Results.NotFound();
            });
        }
    }
}
