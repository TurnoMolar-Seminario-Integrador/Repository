using System.Security.Claims;
using Application.Services;
using Data;
using DTOs;
using Microsoft.AspNetCore.OpenApi;

namespace WebAPI
{
  public static class PacienteEndpoints
  {
    // Autorización (ver AutorizacionApi): el alta es anónima (CUF01: el paciente se registra antes de tener
    // cuenta); consultar y rectificar sus datos (CUF02) lo puede hacer el propio paciente o el personal de
    // la clínica; listar y buscar pacientes, solo el personal; borrar un paciente, solo el Admin (ningún
    // caso de uso documentado da de baja a un paciente y el borrado arrastra su historia clínica).
    public static void MapPacienteEndpoints(this WebApplication app)
    {
      app.MapGet("/pacientes/{id}", async (int id, ClaimsPrincipal user, IPacienteService pacienteService) =>
      {
        PacienteDTO? dto = await pacienteService.GetAsync(id);

        if (dto == null)
        {
          // A quien no es personal no se le revela si el documento existe.
          return AutorizacionApi.EsPersonalDeLaClinica(user)
            ? Results.NotFound()
            : AutorizacionApi.SinPermiso("Solo podés consultar tus propios datos.");
        }

        if (!AutorizacionApi.EsPersonalDeLaClinica(user) && !AutorizacionApi.EsElPropioPaciente(user, dto.TipoDocumento, dto.NroDocumento))
        {
          return AutorizacionApi.SinPermiso("Solo podés consultar tus propios datos.");
        }

        return Results.Ok(dto);
      })
      .RequireAuthorization()
      .WithName("GetPaciente")
      .Produces<PacienteDTO>(StatusCodes.Status200OK)
      .Produces(StatusCodes.Status401Unauthorized)
      .Produces(StatusCodes.Status403Forbidden)
      .Produces(StatusCodes.Status404NotFound)
      .WithOpenApi();

      app.MapGet("/pacientes", async (IPacienteService pacienteService) =>
      {
        var dtos = await pacienteService.GetAllAsync();

        return Results.Ok(dtos);
      })
      .RequireAuthorization(AutorizacionApi.PoliticaPersonal)
      .WithName("GetAllPacientes")
      .Produces<List<PacienteDTO>>(StatusCodes.Status200OK)
      .Produces(StatusCodes.Status401Unauthorized)
      .Produces(StatusCodes.Status403Forbidden)
      .WithOpenApi();

      // CUF01. Anónimo a propósito. El PacienteDTO debe traer Password (RN3: la establece quien se registra).
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
      .AllowAnonymous()
      .WithName("AddPaciente")
      .Produces<PacienteDTO>(StatusCodes.Status201Created)
      .Produces(StatusCodes.Status400BadRequest)
      .WithOpenApi();

      // CUF02. El PacienteDTO debe traer la VersionDatos que devolvió el GET: si otro usuario modificó los
      // datos personales desde entonces, responde 409 y no cambia nada. Sin IdentificadorOS no se modifica la
      // obra social; "PARTICULAR" pasa al paciente a particular.
      app.MapPut("/pacientes", async (PacienteDTO dto, ClaimsPrincipal user, IPacienteService pacienteService) =>
      {
        if (!AutorizacionApi.EsPersonalDeLaClinica(user) && !AutorizacionApi.EsElPropioPaciente(user, dto.TipoDocumento, dto.NroDocumento))
        {
          return AutorizacionApi.SinPermiso("Solo podés modificar tus propios datos.");
        }

        try
        {
          var found = await pacienteService.UpdateAsync(dto);

          if (!found)
          {
            return Results.NotFound();
          }

          return Results.NoContent();
        }
        catch (ConflictoConcurrenciaException ex)
        {
          return Results.Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
          return Results.BadRequest(new { error = ex.Message });
        }
      })
      .RequireAuthorization()
      .WithName("UpdatePaciente")
      .Produces(StatusCodes.Status204NoContent)
      .Produces(StatusCodes.Status400BadRequest)
      .Produces(StatusCodes.Status401Unauthorized)
      .Produces(StatusCodes.Status403Forbidden)
      .Produces(StatusCodes.Status404NotFound)
      .Produces(StatusCodes.Status409Conflict)
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
      .RequireAuthorization(AutorizacionApi.PoliticaAdmin)
      .WithName("DeletePaciente")
      .Produces(StatusCodes.Status204NoContent)
      .Produces(StatusCodes.Status401Unauthorized)
      .Produces(StatusCodes.Status403Forbidden)
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
      .RequireAuthorization(AutorizacionApi.PoliticaPersonal)
      .WithName("GetPacientesByCriteria")
      .Produces<List<PacienteDTO>>(StatusCodes.Status200OK)
      .Produces(StatusCodes.Status400BadRequest)
      .Produces(StatusCodes.Status401Unauthorized)
      .Produces(StatusCodes.Status403Forbidden)
      .WithOpenApi();
    }
  }
}
