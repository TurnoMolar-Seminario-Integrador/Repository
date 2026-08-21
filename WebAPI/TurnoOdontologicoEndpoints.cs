using Application.Services;
using DTOs;
using Microsoft.AspNetCore.OpenApi;

namespace WebAPI
{
    public static class TurnoOdontologicoEndpoints
    {
        public static void MapTurnoOdontologicoEndpoints(this WebApplication app)
        {
            app.MapGet("/turnos/{id}", async (int id, ITurnoOdontologicoService turnoService) =>
            {
                var dto = await turnoService.GetAsync(id);
                if (dto == null)
                {
                    return Results.NotFound();
                }
                return Results.Ok(dto);
            })
            .WithName("GetTurno")
            .Produces<TurnoOdontologicoDTO>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithOpenApi();

            app.MapGet("/turnos", async (ITurnoOdontologicoService turnoService) =>
            {
                var dtos = await turnoService.GetAllAsync();
                return Results.Ok(dtos);
            })
            .WithName("GetAllTurnos")
            .Produces<List<TurnoOdontologicoDTO>>(StatusCodes.Status200OK)
            .WithOpenApi();

            app.MapPost("/turnos", async (TurnoOdontologicoDTO dto, ITurnoOdontologicoService turnoService) =>
            {
                try
                {
                    var turnoDTO = await turnoService.AddAsync(dto);
                    return Results.Created($"/turnos/{turnoDTO.Id}", turnoDTO);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("AddTurno")
            .Produces<TurnoOdontologicoDTO>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();

            app.MapPut("/turnos", async (TurnoOdontologicoDTO dto, ITurnoOdontologicoService turnoService) =>
            {
                try
                {
                    var found = await turnoService.UpdateAsync(dto);
                    if (!found)
                    {
                        return Results.NotFound();
                    }
                    return Results.NoContent();
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("UpdateTurno")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .WithOpenApi();

            app.MapDelete("/turnos/{id}", async (int id, ITurnoOdontologicoService turnoService) =>
            {
                var deleted = await turnoService.DeleteAsync(id);
                if (!deleted)
                {
                    return Results.NotFound();
                }
                return Results.NoContent();
            })
            .WithName("DeleteTurno")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .WithOpenApi();

            app.MapGet("/turnos/criteria", async (DateTime? fecha, string? estadoTurno, ITurnoOdontologicoService turnoService) =>
            {
                try
                {
                    var criteria = new TurnoCriteriaDTO { Fecha = fecha, EstadoTurno = estadoTurno };
                    var turnos = await turnoService.GetByCriteriaAsync(criteria);
                    return Results.Ok(turnos);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
                 .WithName("GetTurnosByCriteria")
                 .Produces<List<TurnoOdontologicoDTO>>(StatusCodes.Status200OK)
                 .Produces(StatusCodes.Status400BadRequest)
                 .WithOpenApi();
        }
    }
}