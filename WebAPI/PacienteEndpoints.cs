using DentalClinic.Application.Services;
using DTOs;
using Microsoft.AspNetCore.OpenApi;

namespace WebAPI
{
  public static class PacienteEndpoints
  {
    public static void MapPacienteEndpoints(this WebApplication app)
    {
      app.MapGet("/pacientes/{id}", async (int id, IPacienteService pacienteService) =>
      {
        PacienteDTO? dto = await pacienteService.GetAsync(id);

        if (dto == null)
        {
          return Results.NotFound();
        }

        return Results.Ok(dto);
      })
      .WithName("GetPaciente")
      .Produces<PacienteDTO>(StatusCodes.Status200OK)
      .Produces(StatusCodes.Status404NotFound)
      .WithOpenApi();

      app.MapGet("/pacientes", async (IPacienteService pacienteService) =>
      {
        var dtos = await pacienteService.GetAllAsync();

        return Results.Ok(dtos);
      })  
      .WithName("GetAllPacientes")
      .Produces<List<PacienteDTO>>(StatusCodes.Status200OK)
      .WithOpenApi();

      app.MapPost("/pacientes", async (PacienteDTO dto, IPacienteService pacienteService) =>
      {
        try
        {
          PacienteDTO pacienteDTO = await pacienteService.AddAsync(dto);

          return Results.Created($"/pacientes/{pacienteDTO.Id}", pacienteDTO);
        }
        catch (ArgumentException ex)
        {
          return Results.BadRequest(new { error = ex.Message });
        }
      })
      .WithName("AddPaciente")
      .Produces<PacienteDTO>(StatusCodes.Status201Created)
      .Produces(StatusCodes.Status400BadRequest)
      .WithOpenApi();

      app.MapPut("/pacientes", async (PacienteDTO dto, IPacienteService pacienteService) =>
      {
        try
        {
          var found = await pacienteService.UpdateAsync(dto);

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
      .WithName("UpdatePaciente")
      .Produces(StatusCodes.Status204NoContent)
      .Produces(StatusCodes.Status404NotFound)
      .Produces(StatusCodes.Status400BadRequest)
      .WithOpenApi();

      app.MapDelete("/pacientes/{id}", async (int id, IPacienteService pacienteService) =>
      {
        var deleted = await pacienteService.DeleteAsync(id);

        if (!deleted)
        {
          return Results.NotFound();
        }

        return Results.NoContent();
      })
      .WithName("DeletePaciente")
      .Produces(StatusCodes.Status204NoContent)
      .Produces(StatusCodes.Status404NotFound)
      .WithOpenApi();

      app.MapGet("/pacientes/criteria", async (string texto, IPacienteService pacienteService) =>
      {
        try
        {
          var criteria = new PacienteCriteriaDTO { Texto = texto };
          var pacientes = await pacienteService.GetByCriteriaAsync(criteria);
          return Results.Ok(pacientes);
        }
        catch (Exception ex)
        {
          return Results.BadRequest(new { error = ex.Message });
        }
      })
      .WithName("GetPacientesByCriteria")
      .Produces<List<PacienteDTO>>(StatusCodes.Status200OK)
      .Produces(StatusCodes.Status400BadRequest)
      .WithOpenApi();
    }
  }
}
