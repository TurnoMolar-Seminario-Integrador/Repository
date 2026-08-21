using Application.Services;
using DTOs;

namespace WebAPI
{
    public static class ConsultaEndpoints
    {
        public static void MapConsultaEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/consultas").WithTags("Consultas Clínicas");

            group.MapGet("/", async (int? pacienteId, IConsultaService service) =>
            {
                if (pacienteId.HasValue && pacienteId.Value > 0)
                {
                    var list = await service.GetByPacienteIdAsync(pacienteId.Value);
                    return Results.Ok(list);
                }
                var all = await service.GetAllAsync();
                return Results.Ok(all);
            });

            group.MapGet("/{id:int}", async (int id, IConsultaService service) =>
            {
                var item = await service.GetAsync(id);
                return item == null ? Results.NotFound() : Results.Ok(item);
            });

            group.MapGet("/turno/{turnoId:int}", async (int turnoId, IConsultaService service) =>
            {
                var item = await service.GetByTurnoIdAsync(turnoId);
                return item == null ? Results.NotFound() : Results.Ok(item);
            });

            group.MapPost("/registrar", async (ConsultaDTO dto, IConsultaService service) =>
            {
                try
                {
                    var created = await service.RegistrarConsultaAsync(dto);
                    return Results.Created($"/api/consultas/{created.Id}", created);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
            });

            group.MapPost("/{id:int}/valorar", async (int id, ValoracionRequest request, IConsultaService service) =>
            {
                var ok = await service.ValorarAtencionAsync(id, request.Estrellas, request.Comentario);
                return ok ? Results.Ok(new { message = "Valoración registrada con éxito." }) : Results.NotFound();
            });
        }
    }

    public record ValoracionRequest(int Estrellas, string Comentario);
}
