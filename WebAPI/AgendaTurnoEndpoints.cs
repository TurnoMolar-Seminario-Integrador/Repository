using Application.Services;
using DTOs;
using Microsoft.AspNetCore.OpenApi;

namespace WebAPI
{
    public static class AgendaTurnoEndpoints
    {
        public static void MapAgendaTurnoEndpoints(this WebApplication app)
        {
            app.MapGet("/agenda/estado-paciente", async (string tipoDocumento, string nroDocumento, IAgendaTurnoService agendaService) =>
            {
                try
                {
                    var estado = await agendaService.ConsultarEstadoParaAgendarAsync(tipoDocumento, nroDocumento);
                    return Results.Ok(estado);
                }
                catch (ArgumentException ex)
                {
                    return Results.NotFound(new { error = ex.Message });
                }
            })
            .WithName("ConsultarEstadoParaAgendar")
            .Produces<EstadoPacienteTurnoDTO>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithOpenApi();

            app.MapGet("/agenda/dias-disponibles", async (int idEspecialidad, int? diasHaciaAdelante, IAgendaTurnoService agendaService) =>
            {
                var dias = await agendaService.ObtenerDiasDisponiblesAsync(idEspecialidad, diasHaciaAdelante ?? 30);
                return Results.Ok(dias);
            })
            .WithName("ObtenerDiasDisponibles")
            .Produces<List<DateTime>>(StatusCodes.Status200OK)
            .WithOpenApi();

            app.MapGet("/agenda/horarios-disponibles", async (int idEspecialidad, DateTime dia, IAgendaTurnoService agendaService) =>
            {
                var horarios = await agendaService.ObtenerHorariosDisponiblesAsync(idEspecialidad, dia);
                return Results.Ok(horarios);
            })
            .WithName("ObtenerHorariosDisponibles")
            .Produces<List<HorarioDisponibleDTO>>(StatusCodes.Status200OK)
            .WithOpenApi();

            app.MapGet("/agenda/politica-cancelacion", (IAgendaTurnoService agendaService) =>
            {
                return Results.Ok(new { mensaje = agendaService.ObtenerMensajePoliticaCancelacion() });
            })
            .WithName("ObtenerPoliticaCancelacion")
            .WithOpenApi();

            app.MapPost("/agenda/turnos", async (AgendarTurnoRequestDTO request, IAgendaTurnoService agendaService) =>
            {
                try
                {
                    var resultado = await agendaService.AgendarTurnoAsync(request);
                    return Results.Ok(resultado);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("AgendarTurno")
            .Produces<AgendarTurnoResultDTO>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();

            // Alt 3.a: alta manual por el Responsable de la Clínica. Requiere estar autenticado;
            // el rol específico (Admin / ResponsableClinica) se valida en el frontend que lo invoca.
            app.MapPost("/agenda/turnos/manual", async (AgendarTurnoRequestDTO request, IAgendaTurnoService agendaService) =>
            {
                try
                {
                    var resultado = await agendaService.AgendarTurnoManualAsync(request);
                    return Results.Ok(resultado);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .RequireAuthorization()
            .WithName("AgendarTurnoManual")
            .Produces<AgendarTurnoResultDTO>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();

            app.MapPost("/agenda/pagar-deuda", async (string tipoDocumento, string nroDocumento, IAgendaTurnoService agendaService) =>
            {
                try
                {
                    await agendaService.PagarDeudaAsync(tipoDocumento, nroDocumento);
                    return Results.NoContent();
                }
                catch (ArgumentException ex)
                {
                    return Results.NotFound(new { error = ex.Message });
                }
            })
            .WithName("PagarDeuda")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .WithOpenApi();
        }
    }
}
