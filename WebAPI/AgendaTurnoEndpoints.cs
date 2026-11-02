using System.Security.Claims;
using Application.Services;
using DTOs;
using Microsoft.AspNetCore.OpenApi;

namespace WebAPI
{
    public static class AgendaTurnoEndpoints
    {
        // Autorización (ver AutorizacionApi). Antes, de /agenda solo pedían token la alta manual, la deuda y el
        // pago de la deuda: consultar el estado de cualquier paciente por su documento y agendar turnos a
        // nombre de cualquiera no pedían nada, y la alta manual (alt 3.a, del Responsable) la podía invocar
        // cualquier usuario autenticado, incluso un paciente.
        //   - Disponibilidad y política de cancelación: cualquier usuario autenticado (no llevan datos personales).
        //   - Estado del paciente, deuda y reserva de un turno: el propio paciente o el personal de la clínica
        //     (la reserva del paciente, solo para sí mismo; el personal usa la alta manual).
        public static void MapAgendaTurnoEndpoints(this WebApplication app)
        {
            app.MapGet("/agenda/estado-paciente", async (string tipoDocumento, string nroDocumento, ClaimsPrincipal user, IAgendaTurnoService agendaService) =>
            {
                if (!AutorizacionApi.EsPersonalDeLaClinica(user) && !AutorizacionApi.EsElPropioPaciente(user, tipoDocumento, nroDocumento))
                {
                    return AutorizacionApi.SinPermiso("Solo podés consultar tu propio estado.");
                }

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
            .RequireAuthorization()
            .WithName("ConsultarEstadoParaAgendar")
            .Produces<EstadoPacienteTurnoDTO>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .WithOpenApi();

            app.MapGet("/agenda/dias-disponibles", async (int idEspecialidad, int? diasHaciaAdelante, IAgendaTurnoService agendaService) =>
            {
                var dias = await agendaService.ObtenerDiasDisponiblesAsync(idEspecialidad, diasHaciaAdelante ?? 30);
                return Results.Ok(dias);
            })
            .RequireAuthorization()
            .WithName("ObtenerDiasDisponibles")
            .Produces<List<DateTime>>(StatusCodes.Status200OK)
            .WithOpenApi();

            app.MapGet("/agenda/horarios-disponibles", async (int idEspecialidad, DateTime dia, IAgendaTurnoService agendaService) =>
            {
                var horarios = await agendaService.ObtenerHorariosDisponiblesAsync(idEspecialidad, dia);
                return Results.Ok(horarios);
            })
            .RequireAuthorization()
            .WithName("ObtenerHorariosDisponibles")
            .Produces<List<HorarioDisponibleDTO>>(StatusCodes.Status200OK)
            .WithOpenApi();

            app.MapGet("/agenda/politica-cancelacion", (IAgendaTurnoService agendaService) =>
            {
                return Results.Ok(new { mensaje = agendaService.ObtenerMensajePoliticaCancelacion() });
            })
            .RequireAuthorization()
            .WithName("ObtenerPoliticaCancelacion")
            .WithOpenApi();

            app.MapPost("/agenda/turnos", async (AgendarTurnoRequestDTO request, ClaimsPrincipal user, IAgendaTurnoService agendaService) =>
            {
                if (!AutorizacionApi.EsElPropioPaciente(user, request.TipoDocumentoPaciente, request.NroDocumentoPaciente))
                {
                    return AutorizacionApi.SinPermiso("Solo el propio paciente puede reservar su turno. El personal de la clínica usa el alta manual (/agenda/turnos/manual).");
                }

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
            .RequireAuthorization()
            .WithName("AgendarTurno")
            .Produces<AgendarTurnoResultDTO>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .WithOpenApi();

            // Alt 3.a: alta manual por el Responsable de la Clínica (o el Admin). Antes alcanzaba con estar
            // autenticado: un paciente podía invocarla y saltearse las reglas de la reserva propia.
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
            .RequireAuthorization(AutorizacionApi.PoliticaPersonal)
            .WithName("AgendarTurnoManual")
            .Produces<AgendarTurnoResultDTO>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .WithOpenApi();

            // CUF10, paso 1: saldo de deuda del paciente y detalle del turno que la originó (nroTurno, fecha
            // y motivo). Sin deuda devuelve el mensaje "No hay saldo de deuda pendiente para regularizar."
            // (alt 1.a). Requiere estar autenticado: la consulta la puede hacer el propio paciente o el
            // personal de la clínica (Responsable / Admin), nadie más.
            app.MapGet("/agenda/deuda", async (string tipoDocumento, string nroDocumento, ClaimsPrincipal user, IRegularizarDeudaService deudaService) =>
            {
                if (!AutorizacionApi.EsPersonalDeLaClinica(user) && !AutorizacionApi.EsElPropioPaciente(user, tipoDocumento, nroDocumento))
                {
                    return AutorizacionApi.SinPermiso("Solo podés consultar tu propia deuda.");
                }

                try
                {
                    var deuda = await deudaService.ConsultarDeudaAsync(tipoDocumento, nroDocumento);
                    return Results.Ok(deuda);
                }
                catch (ArgumentException ex)
                {
                    return Results.NotFound(new { error = ex.Message });
                }
            })
            .RequireAuthorization()
            .WithName("ConsultarDeuda")
            .Produces<DeudaPacienteDTO>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .WithOpenApi();

            // CUF10, pasos 2 y 3 (y CUU01 alt 2.a.1.a): registra el pago del TOTAL de la deuda con el
            // método indicado (tipoMetodoPago: EFECTIVO, TARJETA_DEBITO, TARJETA_CREDITO o
            // TRANSFERENCIA), asociado al turno que la originó, y habilita al paciente. Con
            // presencial = true es el cobro que registra el responsable en la clínica (alt 2.a).
            //
            // Defectos corregidos: no pedía el método de pago; rehabilitaba al paciente sin registrar
            // ningún pago cuando la deuda venía de una cancelación fuera de término; y no exigía
            // autenticación, así que cualquiera podía rehabilitar a cualquier paciente conociendo su
            // documento. Ahora el pago por la plataforma solo lo hace el propio paciente (paso 2) y el
            // cobro presencial solo el personal de la clínica (alt 2.a).
            app.MapPost("/agenda/pagar-deuda", async (string tipoDocumento, string nroDocumento, string tipoMetodoPago, bool? presencial, ClaimsPrincipal user, IRegularizarDeudaService deudaService) =>
            {
                if (presencial == true)
                {
                    if (!AutorizacionApi.EsPersonalDeLaClinica(user))
                    {
                        return AutorizacionApi.SinPermiso("Solo el responsable de la clínica puede registrar un cobro presencial.");
                    }
                }
                else if (!AutorizacionApi.EsElPropioPaciente(user, tipoDocumento, nroDocumento))
                {
                    return AutorizacionApi.SinPermiso("Solo el propio paciente puede abonar su deuda a través de la plataforma.");
                }

                try
                {
                    var resultado = presencial == true
                        ? await deudaService.RegistrarCobroPresencialAsync(tipoDocumento, nroDocumento, tipoMetodoPago)
                        : await deudaService.PagarDeudaAsync(tipoDocumento, nroDocumento, tipoMetodoPago);

                    return resultado.Resultado switch
                    {
                        ResultadoPagarDeuda.MetodoInvalido => Results.BadRequest(new { error = resultado.Mensaje }),
                        ResultadoPagarDeuda.SinTurnoDeOrigen => Results.Conflict(new { error = resultado.Mensaje }),
                        _ => Results.Ok(resultado)
                    };
                }
                catch (ArgumentException ex)
                {
                    return Results.NotFound(new { error = ex.Message });
                }
            })
            .RequireAuthorization()
            .WithName("PagarDeuda")
            .Produces<PagarDeudaResultDTO>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .WithOpenApi();
        }
    }
}
