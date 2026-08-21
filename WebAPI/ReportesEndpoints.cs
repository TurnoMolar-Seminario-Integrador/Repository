using Application.Services;

namespace WebAPI
{
    public static class ReportesEndpoints
    {
        public static void MapReportesEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/reportes").WithTags("Reportes y Estadísticas");

            group.MapGet("/turnos-dia", async (DateTime? fecha, IReportesService service) =>
            {
                var targetFecha = fecha ?? DateTime.Today;
                var rep = await service.GetReporteTurnosDiaAsync(targetFecha);
                return Results.Ok(rep);
            });

            group.MapGet("/ausentismo", async (DateTime? fechaDesde, DateTime? fechaHasta, IReportesService service) =>
            {
                var fDesde = fechaDesde ?? DateTime.Today.AddDays(-30);
                var fHasta = fechaHasta ?? DateTime.Today;
                var rep = await service.GetReporteAusentismoAsync(fDesde, fHasta);
                return Results.Ok(rep);
            });

            group.MapGet("/facturacion", async (DateTime? fechaDesde, DateTime? fechaHasta, IReportesService service) =>
            {
                var fDesde = fechaDesde ?? DateTime.Today.AddDays(-30);
                var fHasta = fechaHasta ?? DateTime.Today;
                var rep = await service.GetReporteFacturacionAsync(fDesde, fHasta);
                return Results.Ok(rep);
            });

            group.MapGet("/historia-clinica/{pacienteId:int}", async (int pacienteId, IReportesService service) =>
            {
                var rep = await service.GetHistoriaClinicaPacienteAsync("DNI", pacienteId);
                return rep == null ? Results.NotFound() : Results.Ok(rep);
            });
        }
    }
}
