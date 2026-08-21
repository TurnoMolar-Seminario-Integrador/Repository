using Application.Services;
using DTOs;

namespace WebAPI
{
    public static class FacturaEndpoints
    {
        public static void MapFacturaEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/facturas").WithTags("Facturación y Caja");

            group.MapGet("/", async (int? pacienteId, IFacturaService service) =>
            {
                if (pacienteId.HasValue && pacienteId.Value > 0)
                {
                    var list = await service.GetByPacienteDocAsync("DNI", pacienteId.Value);
                    return Results.Ok(list);
                }
                var all = await service.GetAllAsync();
                return Results.Ok(all);
            });

            group.MapGet("/{id:int}", async (int id, IFacturaService service) =>
            {
                var f = await service.GetAsync(id);
                return f == null ? Results.NotFound() : Results.Ok(f);
            });

            group.MapGet("/turno/{turnoId:int}", async (int turnoId, IFacturaService service) =>
            {
                var f = await service.GetByCodAtencionAsync(turnoId);
                return f == null ? Results.NotFound() : Results.Ok(f);
            });

            group.MapPost("/crear", async (FacturaDTO dto, IFacturaService service) =>
            {
                var created = await service.CrearFacturaAsync(dto);
                return Results.Created($"/api/facturas/{created.CodPago}", created);
            });

            group.MapPost("/{id:int}/pagar", async (int id, RegistrarPagoRequest request, IFacturaService service) =>
            {
                var ok = await service.RegistrarPagoAsync(id, request.MetodoPago);
                return ok ? Results.Ok(new { message = "Pago registrado y comprobante emitido." }) : Results.NotFound();
            });
        }
    }

    public record RegistrarPagoRequest(string MetodoPago);
}
