using System.Globalization;
using Application.Services;
using Data;
using Domain.Model;
using DTOs;
using Frontend.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Frontend.MVC.Controllers
{
    // Responsable de la Clínica también puede acceder (alta manual de turnos, CUU01 alt 3.a).
    [Authorize(Roles = "Paciente,Admin,ResponsableClinica")]
    public class TurnoController : Controller
    {
        private readonly TurnoMolarDbContext _context;
        private readonly ILogger<TurnoController> _logger;
        private readonly IAgendaTurnoService _agendaTurnoService;
        private readonly IEspecialidadRepository _especialidadRepository;
        private readonly IOdontologoRepository _odontologoRepository;
        private readonly IObraSocialRepository _obraSocialRepository;
        private readonly IValorarAtencionService _valorarAtencionService;

        public TurnoController(
            TurnoMolarDbContext context,
            ILogger<TurnoController> logger,
            IAgendaTurnoService agendaTurnoService,
            IEspecialidadRepository especialidadRepository,
            IOdontologoRepository odontologoRepository,
            IObraSocialRepository obraSocialRepository,
            IValorarAtencionService valorarAtencionService)
        {
            _context = context;
            _logger = logger;
            _agendaTurnoService = agendaTurnoService;
            _especialidadRepository = especialidadRepository;
            _odontologoRepository = odontologoRepository;
            _obraSocialRepository = obraSocialRepository;
            _valorarAtencionService = valorarAtencionService;
        }

        private async Task<Paciente> ObtenerPacienteActualAsync()
        {
            var tipoDocumento = User.FindFirst("TipoDocumento")?.Value;
            var nroDocumento = User.FindFirst("NroDocumento")?.Value;

            Paciente? paciente = null;
            if (!string.IsNullOrEmpty(tipoDocumento) && !string.IsNullOrEmpty(nroDocumento))
            {
                paciente = await _context.Pacientes
                    .Include(p => p.ObraSocial)
                    .Include(p => p.HistoriaClinica)
                    .FirstOrDefaultAsync(p => p.TipoDocumento == tipoDocumento && p.NroDocumento == nroDocumento);
            }

            if (paciente == null)
            {
                paciente = await _context.Pacientes
                    .Include(p => p.ObraSocial)
                    .Include(p => p.HistoriaClinica)
                    .FirstOrDefaultAsync();
            }

            if (paciente == null)
            {
                paciente = new Paciente(
                    "DNI",
                    "34567890",
                    "Manuel",
                    "Fernández",
                    new DateTime(1989, 4, 15),
                    "341-3334455",
                    "manuel.fer@email.com",
                    "Córdoba 1540, Rosario",
                    "HABILITADO",
                    "OSDE",
                    0m,
                    "paciente123",
                    "",
                    DateTime.Now,
                    "Paciente"
                );
                _context.Pacientes.Add(paciente);
                await _context.SaveChangesAsync();
            }

            ViewData["NombrePaciente"] = $"{paciente.Nombre} {paciente.Apellido}";
            ViewData["IdPaciente"] = paciente.NroDocumento;
            ViewData["EstadoPaciente"] = paciente.EstadoPaciente;

            return paciente;
        }

        // =========================================================================
        // CUU01 - AGENDAR TURNO ODONTOLÓGICO (camino básico, pasos 2 a 6)
        // La lógica de negocio (habilitación, turno pendiente, disponibilidad horaria,
        // convenio de obra social, emisión de comprobante) vive en IAgendaTurnoService
        // (Application.Services), no en este controlador — mismo criterio arquitectónico
        // ya aplicado al login (ver AuthService / TurnoMolar_Login_Resumen).
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> Reservar(int? idEspecialidad)
        {
            var paciente = await ObtenerPacienteActualAsync();

            var especialidades = (await _especialidadRepository.GetAllAsync()).ToList();

            Especialidad? especialidadSeleccionada;
            if (idEspecialidad.HasValue)
            {
                // El paciente (o un link "volver") ya eligió una especialidad puntual: la respetamos
                // aunque no tenga turnos disponibles, para que vea el mensaje correspondiente.
                especialidadSeleccionada = especialidades.FirstOrDefault(e => e.IdEspecialidad == idEspecialidad)
                    ?? especialidades.FirstOrDefault();
            }
            else
            {
                // Primera visita sin especialidad elegida: en vez de tomar la primera en orden
                // alfabético (que puede no tener ningún odontólogo asignado todavía), buscamos la
                // primera que realmente tenga turnos disponibles en los próximos 30 días.
                especialidadSeleccionada = null;
                foreach (var esp in especialidades)
                {
                    var dias = await _agendaTurnoService.ObtenerDiasDisponiblesAsync(esp.IdEspecialidad, 30);
                    if (dias.Any())
                    {
                        especialidadSeleccionada = esp;
                        break;
                    }
                }
                especialidadSeleccionada ??= especialidades.FirstOrDefault();
            }

            // Camino básico, paso 2: validar estado del paciente antes de mostrarle el calendario.
            var estado = await _agendaTurnoService.ConsultarEstadoParaAgendarAsync(paciente.TipoDocumento, paciente.NroDocumento);

            var obraSocial = string.IsNullOrWhiteSpace(paciente.IdentificadorOS)
                ? null
                : await _obraSocialRepository.GetAsync(paciente.IdentificadorOS);

            ViewBag.Especialidades = especialidades;
            ViewBag.EspecialidadSeleccionada = especialidadSeleccionada?.IdEspecialidad ?? 0;
            ViewBag.MensajePoliticaCancelacion = _agendaTurnoService.ObtenerMensajePoliticaCancelacion();

            ViewBag.Habilitado = estado.Habilitado;
            ViewBag.MontoAdeudado = estado.MontoAdeudado;
            ViewBag.TieneTurnoPendiente = estado.TieneTurnoPendiente;
            ViewBag.TurnoPendiente = estado.TurnoPendiente;

            ViewBag.TieneObraSocial = obraSocial != null;
            ViewBag.NombreObraSocial = obraSocial != null ? $"{obraSocial.NombreOS} ({obraSocial.PlanCobertura})" : null;

            var modelo = new ReservaTurnoViewModel
            {
                IdEspecialidad = especialidadSeleccionada?.IdEspecialidad ?? 0,
                ModalidadPago = obraSocial != null ? "OBRA_SOCIAL" : "PARTICULAR"
            };

            return View(modelo);
        }

        // AJAX (paso 1 del wizard): odontólogos, aranceles, cobertura y días disponibles
        // para la especialidad elegida (CUU01, diccionario de datos: sCalendarioTurnosDisponibles).
        [HttpGet]
        public async Task<IActionResult> DatosEspecialidad(int idEspecialidad)
        {
            var paciente = await ObtenerPacienteActualAsync();

            var especialidad = await _especialidadRepository.GetAsync(idEspecialidad);
            var odontologos = await _odontologoRepository.GetByEspecialidadAsync(idEspecialidad);
            var diasDisponibles = await _agendaTurnoService.ObtenerDiasDisponiblesAsync(idEspecialidad);

            var obraSocial = string.IsNullOrWhiteSpace(paciente.IdentificadorOS)
                ? null
                : await _obraSocialRepository.GetAsync(paciente.IdentificadorOS);

            var obraSocialCubre = obraSocial != null
                && await _agendaTurnoService.ObraSocialCubreEspecialidadAsync(obraSocial.IdentificadorOS, idEspecialidad);
            var convenio = obraSocial?.Convenios.FirstOrDefault(c => c.IdEspecialidad == idEspecialidad);

            return Json(new
            {
                nombreEspecialidad = especialidad?.Nombre ?? string.Empty,
                arancelParticular = especialidad?.ArancelParticular ?? 0m,
                tieneObraSocial = obraSocial != null,
                nombreObraSocial = obraSocial?.NombreOS,
                obraSocialCubre,
                arancelObraSocial = convenio?.ArancelConvenio,
                diasDisponibles = diasDisponibles.Select(d => d.ToString("yyyy-MM-dd")),
                odontologos = odontologos.Select(o => new
                {
                    tipoDocumento = o.TipoDocumento,
                    nroDocumento = o.NroDocumento,
                    nombreCompleto = $"{o.Nombre} {o.Apellido}",
                    matricula = o.Matricula
                })
            });
        }

        // AJAX (paso 1 del wizard): horarios disponibles de un día puntual
        // (CUU01, diccionario de datos: sHorariosDisponiblesDia).
        [HttpGet]
        public async Task<IActionResult> Horarios(int idEspecialidad, string dia)
        {
            if (!DateTime.TryParse(dia, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            {
                return BadRequest(new { error = "Fecha inválida." });
            }

            var horarios = await _agendaTurnoService.ObtenerHorariosDisponiblesAsync(idEspecialidad, fecha);

            return Json(horarios.Select(h => new
            {
                fechaHoraIso = h.FechaHoraTurno.ToString("yyyy-MM-ddTHH:mm:ss"),
                odontologoTipoDocumento = h.OdontologoTipoDocumento,
                odontologoNroDocumento = h.OdontologoNroDocumento,
                nombreOdontologo = h.NombreOdontologo
            }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reservar(ReservaTurnoViewModel modelo)
        {
            var paciente = await ObtenerPacienteActualAsync();

            // Alt 5.a: el paciente no acepta la política de cancelación -> se descarta la selección.
            if (!modelo.AceptaPoliticas)
            {
                TempData["MensajeError"] = "Para confirmar el turno primero tenés que aceptar la política de cancelación.";
                return RedirectToAction("Reservar", new { idEspecialidad = modelo.IdEspecialidad });
            }

            if (!DateTime.TryParse(modelo.FechaHoraTurnoIso, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaHoraTurno))
            {
                TempData["MensajeError"] = "Elegí un horario válido antes de confirmar.";
                return RedirectToAction("Reservar", new { idEspecialidad = modelo.IdEspecialidad });
            }

            var request = new AgendarTurnoRequestDTO
            {
                TipoDocumentoPaciente = paciente.TipoDocumento,
                NroDocumentoPaciente = paciente.NroDocumento,
                IdEspecialidad = modelo.IdEspecialidad,
                OdontologoTipoDocumento = modelo.OdontologoTipoDocumento,
                OdontologoNroDocumento = modelo.OdontologoNroDocumento,
                FechaHoraTurno = fechaHoraTurno,
                ModalidadPago = modelo.ModalidadPago
            };

            AgendarTurnoResultDTO resultado;
            try
            {
                resultado = await _agendaTurnoService.AgendarTurnoAsync(request);
            }
            catch (ArgumentException ex)
            {
                TempData["MensajeError"] = ex.Message;
                return RedirectToAction("Reservar", new { idEspecialidad = modelo.IdEspecialidad });
            }

            switch (resultado.Resultado)
            {
                case ResultadoAgendarTurno.Reservado:
                    TempData["MensajeExito"] = resultado.Mensaje;
                    return RedirectToAction("Comprobante", new { idTurno = resultado.NroTurno });

                case ResultadoAgendarTurno.Inhabilitado:
                    // Alt 2.a: se vuelve a mostrar el paso 2 con el aviso de deuda pendiente.
                    TempData["MensajeError"] = resultado.Mensaje;
                    return RedirectToAction("Reservar", new { idEspecialidad = modelo.IdEspecialidad });

                case ResultadoAgendarTurno.TurnoPendienteExistente:
                    // Alt 2.b: FCU mostrando los datos del turno ya reservado.
                    TempData["MensajeInfo"] = resultado.Mensaje;
                    return RedirectToAction("MisTurnos", "Home");

                case ResultadoAgendarTurno.ObraSocialSinConvenio:
                case ResultadoAgendarTurno.HorarioNoDisponible:
                default:
                    TempData["MensajeError"] = resultado.Mensaje;
                    return RedirectToAction("Reservar", new { idEspecialidad = modelo.IdEspecialidad });
            }
        }

        // Alt 2.a.1.a: el paciente abona la deuda y el sistema lo rehabilita ("Vuelve al paso 2").
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PagarDeuda()
        {
            var paciente = await ObtenerPacienteActualAsync();
            await _agendaTurnoService.PagarDeudaAsync(paciente.TipoDocumento, paciente.NroDocumento);

            TempData["MensajeExito"] = "¡Deuda regularizada! Ya podés agendar un nuevo turno.";
            return RedirectToAction("Reservar");
        }

        // =========================================================================
        // CUU01 ALT 3.a - ALTA MANUAL DE TURNO POR EL RESPONSABLE DE LA CLÍNICA
        // Cuando el paciente no encuentra turnos disponibles/convenientes y acuerda
        // directamente con la clínica una fecha y hora. Formulario simple orientado al
        // personal administrativo (no reutiliza el wizard de autoservicio del paciente).
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> ReservarManual()
        {
            if (!User.IsInRole("Admin") && !User.IsInRole("ResponsableClinica"))
            {
                return Forbid();
            }

            ViewBag.Especialidades = await _especialidadRepository.GetAllAsync();
            ViewBag.Odontologos = await _odontologoRepository.GetAllAsync();

            return View(new AgendarTurnoRequestDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReservarManual(AgendarTurnoRequestDTO request)
        {
            if (!User.IsInRole("Admin") && !User.IsInRole("ResponsableClinica"))
            {
                return Forbid();
            }

            AgendarTurnoResultDTO resultado;
            try
            {
                resultado = await _agendaTurnoService.AgendarTurnoManualAsync(request);
            }
            catch (ArgumentException ex)
            {
                TempData["MensajeError"] = ex.Message;
                return RedirectToAction("ReservarManual");
            }

            if (resultado.Resultado == ResultadoAgendarTurno.Reservado)
            {
                TempData["MensajeExito"] = resultado.Mensaje;
                return RedirectToAction("Comprobante", new { idTurno = resultado.NroTurno });
            }

            TempData["MensajeError"] = resultado.Mensaje;
            return RedirectToAction("ReservarManual");
        }

        // =========================================================================
        // COMPROBANTE DE TURNO
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> Comprobante(int idTurno)
        {
            await ObtenerPacienteActualAsync();

            var turno = await _context.Turnos
                .Include(t => t.Odontologo)
                .Include(t => t.Especialidad)
                .Include(t => t.Paciente)
                    .ThenInclude(p => p.ObraSocial)
                .Include(t => t.Comprobante)
                .FirstOrDefaultAsync(t => t.NroTurno == idTurno);

            if (turno == null)
            {
                TempData["MensajeError"] = "No se encontró el turno solicitado.";
                return RedirectToAction("MisTurnos", "Home");
            }

            ViewData["IdTurno"] = turno.NroTurno;
            ViewData["FechaTurno"] = turno.FechaHoraTurno.ToString("dd 'de' MMMM, yyyy • HH:mm 'hs'", new CultureInfo("es-ES"));
            ViewData["Doctor"] = $"{turno.Odontologo.Nombre} {turno.Odontologo.Apellido} (MP {turno.Odontologo.Matricula})";
            ViewData["Especialidad"] = turno.Especialidad.Nombre;
            ViewData["ModalidadPago"] = turno.ModalidadPagoElegida == "OBRA_SOCIAL" ? "Obra Social / Prepaga" : "Particular";
            ViewData["EstadoTurno"] = turno.EstadoTurno;
            ViewData["FechaEmision"] = turno.Comprobante?.FechaHoraEmision.ToString("dd/MM/yyyy HH:mm") ?? DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            return View(turno);
        }

        // =========================================================================
        // CANCELAR TURNO
        // =========================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int idTurno, string motivoCancelacion)
        {
            var paciente = await ObtenerPacienteActualAsync();

            var turno = await _context.Turnos.FirstOrDefaultAsync(t => t.NroTurno == idTurno && t.NroDocumentoPaciente == paciente.NroDocumento);
            if (turno == null)
            {
                TempData["MensajeError"] = "No se encontró el turno a cancelar.";
                return RedirectToAction("MisTurnos", "Home");
            }

            // Solo un turno "Reservado" puede cancelarse. Desde que el responsable registra la
            // asistencia (Presente / Ausente) el paciente ya no puede modificarlo (ME - Turno).
            if (!turno.PermiteCancelarOReprogramar)
            {
                TempData["MensajeError"] = "Este turno ya no admite cancelación: solo pueden cancelarse los turnos en estado Reservado.";
                return RedirectToAction("MisTurnos", "Home");
            }

            // Regla de negocio: Si cancela con menos de 24 hs de anticipación, se aplica penalización
            var horasRestantes = (turno.FechaHoraTurno - DateTime.Now).TotalHours;
            decimal? penalizacion = null;

            if (horasRestantes < 24 && horasRestantes > 0)
            {
                penalizacion = 5000m;
                var montoActual = paciente.MontoAdeudado ?? 0m;
                paciente.SetMontoAdeudado(montoActual + penalizacion.Value);
                paciente.SetEstadoPaciente("INHABILITADO");
            }

            turno.Cancelar(motivoCancelacion ?? "Cancelado por el paciente", penalizacion);

            await _context.SaveChangesAsync();

            if (penalizacion.HasValue)
            {
                TempData["MensajeAdvertencia"] = $"El turno fue cancelado con menos de 24 hs de anticipación. Se registró un cargo por penalización de ${penalizacion.Value:N0} en tu cuenta.";
            }
            else
            {
                TempData["MensajeExito"] = "El turno fue cancelado correctamente sin penalizaciones.";
            }

            return RedirectToAction("MisTurnos", "Home");
        }

        // =========================================================================
        // REPROGRAMAR TURNO
        // =========================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reprogramar(int idTurnoOriginal, string nuevaFecha, string nuevoHorario)
        {
            var paciente = await ObtenerPacienteActualAsync();

            var turnoOriginal = await _context.Turnos.FirstOrDefaultAsync(t => t.NroTurno == idTurnoOriginal && t.NroDocumentoPaciente == paciente.NroDocumento);
            if (turnoOriginal == null)
            {
                TempData["MensajeError"] = "No se encontró el turno original para reprogramar.";
                return RedirectToAction("MisTurnos", "Home");
            }

            // Solo un turno "Reservado" puede reprogramarse (ver comentario en Cancelar).
            if (!turnoOriginal.PermiteCancelarOReprogramar)
            {
                TempData["MensajeError"] = "Este turno ya no admite reprogramación: solo pueden reprogramarse los turnos en estado Reservado.";
                return RedirectToAction("MisTurnos", "Home");
            }

            DateTime nuevaFechaHora = turnoOriginal.FechaHoraTurno.AddDays(7);
            if (!string.IsNullOrEmpty(nuevaFecha) && DateTime.TryParse(nuevaFecha, out DateTime parsedDate))
            {
                nuevaFechaHora = parsedDate;
                if (!string.IsNullOrEmpty(nuevoHorario))
                {
                    var parts = nuevoHorario.Split(':', ' ');
                    if (parts.Length >= 2 && int.TryParse(parts[0], out int h) && int.TryParse(parts[1], out int m))
                    {
                        nuevaFechaHora = parsedDate.Date.AddHours(h).AddMinutes(m);
                    }
                }
            }

            // Marcar turno original como REPROGRAMADO
            turnoOriginal.Reprogramar(nuevaFechaHora);

            // Crear nuevo turno vinculado al original
            var nuevoTurno = new Turno(
                0,
                nuevaFechaHora,
                turnoOriginal.ModalidadPagoElegida,
                turnoOriginal.IdEspecialidad,
                turnoOriginal.TipoDocumentoOdontologo,
                turnoOriginal.NroDocumentoOdontologo,
                turnoOriginal.TipoDocumentoPaciente,
                turnoOriginal.NroDocumentoPaciente,
                "RESERVADO",
                null,
                null,
                turnoOriginal.NroTurno
            );

            _context.Turnos.Add(nuevoTurno);
            await _context.SaveChangesAsync();

            // Emitir comprobante para el nuevo turno
            var comprobante = new ComprobanteDeTurno(
                0,
                nuevoTurno.NroTurno,
                DateTime.Now
            );
            _context.ComprobantesTurnos.Add(comprobante);
            await _context.SaveChangesAsync();

            TempData["MensajeExito"] = $"¡Turno reprogramado exitosamente para el {nuevaFechaHora:dd/MM/yyyy HH:mm} hs!";
            return RedirectToAction("Comprobante", new { idTurno = nuevoTurno.NroTurno });
        }

        // =========================================================================
        // CUU04 - VALORAR ATENCIÓN ODONTOLÓGICA
        // =========================================================================

        // Actor primario: Paciente de la clínica. Otros: <vacío> -- a diferencia de
        // Cancelar/Reprogramar/etc., acá se restringe el rol a nivel de acción (además del
        // [Authorize] de la clase) porque CUU04 no admite que Admin ni ResponsableClinica
        // valoren en nombre de un paciente.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Paciente")]
        public async Task<IActionResult> ValorarAtencion(int idTurno, int calificacion, string? observaciones)
        {
            var paciente = await ObtenerPacienteActualAsync();

            var resultado = await _valorarAtencionService.RegistrarValoracionAsync(
                paciente.TipoDocumento, paciente.NroDocumento, idTurno, calificacion, observaciones);

            if (resultado.Resultado == ResultadoRegistrarValoracion.Registrada)
            {
                TempData["MensajeExito"] = resultado.Mensaje;
            }
            else
            {
                TempData["MensajeError"] = resultado.Mensaje;
            }

            return RedirectToAction("MisTurnos", "Home");
        }
    }
}