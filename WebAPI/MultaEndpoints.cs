using Application.Services;
using DTOs;

namespace WebAPI
{
    public static class MultaEndpoints
    {
        public static void MapMultaEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/multas").WithTags("Multas y Sanciones");

            group.MapGet("/", async (int? pacienteId, bool? soloImpagas, IMultaService service) =>
            {
                if (pacienteId.HasValue && pacienteId.Value > 0)
                {
                    if (soloImpagas.HasValue && soloImpagas.Value)
                    {
                        var impagas = await service.GetImpagasByPacienteIdAsync(pacienteId.Value);
                        return Results.Ok(impagas);
                    }
                    var list = await service.GetByPacienteIdAsync(pacienteId.Value);
                    return Results.Ok(list);
                }

                var all = await service.GetAllAsync();
                return Results.Ok(all);
            });

            group.MapGet("/{id:int}", async (int id, IMultaService service) =>
            {
                var multa = await service.GetAsync(id);
                return multa == null ? Results.NotFound() : Results.Ok(multa);
            });

            group.MapPost("/", async (MultaDTO dto, IMultaService service) =>
            {
                var created = await service.CrearMultaAsync(dto);
                return Results.Created($"/api/multas/{created.Id}", created);
            });

            group.MapPost("/{id:int}/pagar", async (int id, IMultaService service) =>
            {
                var ok = await service.PagarMultaAsync(id);
                return ok ? Results.Ok(new { message = "Multa saldada y paciente rehabilitado si no registra más deudas." }) : Results.NotFound();
            });
        }
    }
}
