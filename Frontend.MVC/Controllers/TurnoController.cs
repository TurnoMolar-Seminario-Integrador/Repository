using System.Globalization;
using Application.Services;
using Data;
using Domain.Model;
using DTOs;
using Frontend.MVC.Infrastructure;
using Frontend.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Frontend.MVC.Controllers
{
    // Portal del paciente (el Admin lo ve en vista previa). El responsable de la clínica ya no entra
    // acá: el alta manual de turnos (CUU01 alt 3.a) está en su propia consola, Responsable/NuevoTurno,
    // y estas acciones resuelven al paciente de la sesión, que un responsable no tiene.
    [Authorize(Roles = "Paciente,Admin")]
    public class TurnoController : Controller
    {
        private readonly TurnoMolarDbContext _context;
        private readonly ILogger<TurnoController> _logger;
        private readonly IAgendaTurnoService _agendaTurnoService;
        private readonly IEspecialidadRepository _especialidadRepository;
        private readonly IOdontologoRepository _odontologoRepository;
        private readonly IObraSocialRepository _obraSocialRepository;
        private readonly IValorarAtencionService _valorarAtencionService;
        private readonly IGestionTurnoService _gestionTurnoService;
        private readonly IRegularizarDeudaService _regularizarDeudaService;

        // Verdadero cuando la sesión es un Admin mirando el portal de "el primer paciente" (vista
        // previa): ver ObtenerPacienteActualAsync y VistaPreviaSoloLectura.
        private bool _vistaPreviaAdmin;

        public TurnoController(
            TurnoMolarDbContext context,
            ILogger<TurnoController> logger,
            IAgendaTurnoService agendaTurnoService,
            IEspecialidadRepository especialidadRepository,
            IOdontologoRepository odontologoRepository,
            IObraSocialRepository obraSocialRepository,
            IValorarAtencionService valorarAtencionService,
            IGestionTurnoService gestionTurnoService,
            IRegularizarDeudaService regularizarDeudaService)
        {
            _context = context;
            _logger = logger;
            _agendaTurnoService = agendaTurnoService;
            _especialidadRepository = especialidadRepository;
            _odontologoRepository = odontologoRepository;
            _obraSocialRepository = obraSocialRepository;
            _valorarAtencionService = valorarAtencionService;
            _gestionTurnoService = gestionTurnoService;
            _regularizarDeudaService = regularizarDeudaService;
        }

        // Defecto corregido: esta resolución caía silenciosamente en "el primer paciente de la
        // base" para CUALQUIER sesión sin claims que matchearan un paciente real -- lo que
        // permitía que una sesión inconsistente terminara operando sobre la cuenta de OTRO
        // paciente -- y si la base estaba vacía, insertaba un paciente hardcodeado nuevo como
        // efecto secundario de un simple GET. Se restringe ese fallback a una vista previa
        // explícita del rol Admin (que a propósito no tiene fila propia en Pacientes, ver
        // DbInitializer); para cualquier otro caso sin coincidencia real, se corta con una
        // excepción en vez de mostrarle a alguien el portal de otro paciente o de fabricar datos.
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

            if (paciente == null && User.IsInRole("Admin"))
            {
                // Vista previa de Admin: el rol "Admin" (ResponsablesClinica, ver DbInitializer)
                // no tiene fila propia en Pacientes a propósito, así que se le muestra el portal
                // con el primer paciente real de la base. Es el único caso en el que se admite
                // este fallback.
                paciente = await _context.Pacientes
                    .Include(p => p.ObraSocial)
                    .Include(p => p.HistoriaClinica)
                    .FirstOrDefaultAsync();
                _vistaPreviaAdmin = paciente != null;
            }

            if (paciente == null)
            {
                throw new InvalidOperationException("No se pudo resolver un paciente para la sesión actual.");
            }

            ViewData["NombrePaciente"] = $"{paciente.Nombre} {paciente.Apellido}";
            ViewData["IdPaciente"] = paciente.NroDocumento;
            ViewData["EstadoPaciente"] = paciente.EstadoPaciente;
            // Lo usa el aviso de deuda del layout del paciente (_LayoutPaciente).
            ViewData["MontoAdeudado"] = paciente.MontoAdeudado ?? 0m;

            return paciente;
        }

        // Defecto corregido: en la vista previa del Admin (sin cuenta de paciente propia) las operaciones
        // (agendar, cancelar, reprogramar, pagar la deuda) se aplicaban al primer paciente real de la
        // base. El Admin puede mirar todas las pantallas, pero no operar sobre esa cuenta.
        private IActionResult VistaPreviaSoloLectura(string accion, string? controlador = null)
        {
            TempData["MensajeError"] = VistaPreviaAdmin.MensajeSoloLectura;
            return RedirectToAction(accion, controlador);
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

            // CUF12 E4.R02 alt 1.d.2: solo se ofrecen las especialidades habilitadas.
            var especialidades = (await _especialidadRepository.GetAllAsync()).Where(e => e.EstaHabilitada).ToList();

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
            // CUF10 paso 1: además del saldo, el detalle del turno que originó la deuda (nro, fecha, motivo).
            ViewBag.DeudaDetalle = estado.Habilitado
                ? null
                : await _regularizarDeudaService.ConsultarDeudaAsync(paciente.TipoDocumento, paciente.NroDocumento);
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
            if (_vistaPreviaAdmin)
            {
                return VistaPreviaSoloLectura("Reservar", "Turno");
            }

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

        // CUU01 alt 2.a.1.a / CUF10 pasos 2 y 3: el paciente abona el TOTAL de la deuda y el sistema
        // registra el pago (con el método elegido, asociado al turno que la originó) y lo rehabilita
        // ("Vuelve al paso 2").
        //
        // Defecto corregido: la acción no recibía el método (la pantalla ni lo preguntaba) y rehabilitaba
        // a cualquier paciente inhabilitado con solo enviar el formulario.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PagarDeuda(string? metodoPago)
        {
            var paciente = await ObtenerPacienteActualAsync();
            if (_vistaPreviaAdmin)
            {
                return VistaPreviaSoloLectura("Reservar", "Turno");
            }

            var resultado = await _regularizarDeudaService.PagarDeudaAsync(
                paciente.TipoDocumento, paciente.NroDocumento, metodoPago ?? string.Empty);

            switch (resultado.Resultado)
            {
                case ResultadoPagarDeuda.Regularizada:
                    TempData["MensajeExito"] = resultado.Mensaje;
                    break;
                case ResultadoPagarDeuda.SinDeuda:
                    // CUF10 alt 1.a: mensajeSinDeuda.
                    TempData["MensajeInfo"] = resultado.Mensaje;
                    break;
                default:
                    TempData["MensajeError"] = resultado.Mensaje;
                    break;
            }

            return RedirectToAction("Reservar");
        }

        // =========================================================================
        // CUU01 ALT 3.a - ALTA MANUAL DE TURNO POR EL RESPONSABLE DE LA CLÍNICA
        // =========================================================================
        // Esta pantalla vivía acá con el layout del portal del paciente, y tras registrar el turno
        // redirigía al comprobante del paciente (que falla para el rol Responsable). Ahora es parte
        // de la consola del responsable: Responsable/NuevoTurno. Se deja esta ruta para no romper
        // enlaces guardados. Es [AllowAnonymous] porque solo redirige (el [Authorize] de la clase ya
        // no incluye al responsable) y la pantalla de destino exige el rol: quien no tenga sesión pasa
        // por el login y quien tenga otro rol recibe el aviso de acceso denegado.
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ReservarManual() => RedirectToActionPermanent("NuevoTurno", "Responsable");

        // =========================================================================
        // COMPROBANTE DE TURNO
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> Comprobante(int idTurno)
        {
            var paciente = await ObtenerPacienteActualAsync();

            var turno = await _context.Turnos
                .Include(t => t.Odontologo)
                .Include(t => t.Especialidad)
                .Include(t => t.Paciente)
                    .ThenInclude(p => p.ObraSocial)
                .Include(t => t.Comprobante)
                .FirstOrDefaultAsync(t => t.NroTurno == idTurno);

            // El comprobante es de su titular. Antes se mostraba el de cualquier número de turno, así
            // que un paciente podía ver el de otro (sus datos y los del odontólogo) cambiando el
            // número en la URL. Mismo mensaje que si el turno no existiera, para no confirmar que existe.
            if (turno == null
                || turno.TipoDocumentoPaciente != paciente.TipoDocumento
                || turno.NroDocumentoPaciente != paciente.NroDocumento)
            {
                TempData["MensajeError"] = "No se encontró el turno solicitado.";
                return RedirectToAction("MisTurnos", "Home");
            }

            // Defecto corregido: la vista leía "MetodoPago" y "CodigoReserva", que este método nunca
            // cargaba (cargaba "ModalidadPago"), así que todos los comprobantes mostraban la cobertura
            // "Obra Social (OSDE - Plan 210)" y el código "TM-2026-8491A" de ejemplo. Ahora se muestran
            // los datos del turno: el número de comprobante (sComprobanteTurno, CUU01) y su cobertura.
            ViewData["IdTurno"] = turno.NroTurno;
            ViewData["FechaTurno"] = turno.FechaHoraTurno.ToString("dd 'de' MMMM, yyyy • HH:mm 'hs'", new CultureInfo("es-ES"));
            ViewData["Doctor"] = $"{turno.Odontologo.Nombre} {turno.Odontologo.Apellido} ({turno.Odontologo.MatriculaVisible})";
            ViewData["Especialidad"] = turno.Especialidad.Nombre;
            ViewData["ModalidadPago"] = ModalidadesPago.Etiqueta(turno.ModalidadPagoElegida);
            ViewData["MetodoPago"] = DescribirCobertura(turno);
            ViewData["EstadoTurno"] = turno.EstadoLegible;
            ViewData["CodigoReserva"] = turno.Comprobante?.NroComprobante.ToString("D6");
            ViewData["FechaEmision"] = turno.Comprobante?.FechaHoraEmision.ToString("dd/MM/yyyy HH:mm");

            return View(turno);
        }

        // El turno guarda solo la modalidad (PARTICULAR u OBRA_SOCIAL), no cuál obra social. Mientras el
        // turno está pendiente la obra social del paciente no se puede cambiar (CoberturaPacienteService),
        // así que la actual es la que corresponde; en un turno ya cerrado no se afirma cuál era.
        private static string DescribirCobertura(Turno turno)
        {
            if (turno.ModalidadPagoElegida != ModalidadesPago.ObraSocial)
            {
                return "Particular";
            }

            var obraSocial = turno.Paciente?.ObraSocial;
            return turno.PendienteDeAtencion && obraSocial != null
                ? $"Obra Social: {obraSocial.NombreOS} ({obraSocial.PlanCobertura})"
                : "Obra Social";
        }

        // =========================================================================
        // CANCELAR TURNO
        // =========================================================================

        // Defecto corregido (ver GestionTurnoService): esta acción manipulaba el DbContext
        // directo con una penalización de $5000 fija, sin relación con la especialidad del
        // turno -- el único lugar del proyecto que no seguía el criterio arquitectónico ya
        // aplicado a CUU01-04 (lógica de negocio en Application.Services, no en el controlador).
        //
        // CUF07 alt 3.a: si la cancelación se pide fuera de término (menos de 24 hs, o el turno ya pasó)
        // no se cancela de inmediato: se muestra el aviso del cobro (3.a.1) y la cancelación se ejecuta
        // recién cuando el paciente la confirma (3.a.2, confirmaFueraDeTermino = true). Antes la
        // penalización se cobraba y el paciente quedaba inhabilitado sin haber visto ninguna advertencia.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int idTurno, string? motivoCancelacion, bool confirmaFueraDeTermino = false)
        {
            var paciente = await ObtenerPacienteActualAsync();
            if (_vistaPreviaAdmin)
            {
                return VistaPreviaSoloLectura("MisTurnos", "Home");
            }

            var resultado = await _gestionTurnoService.CancelarTurnoAsync(
                idTurno, paciente.TipoDocumento, paciente.NroDocumento, motivoCancelacion, confirmaFueraDeTermino);

            if (resultado.Resultado == ResultadoCancelarTurno.RequiereConfirmacion)
            {
                return View("ConfirmarCancelacion", new ConfirmarCancelacionViewModel
                {
                    IdTurno = idTurno,
                    MotivoCancelacion = motivoCancelacion,
                    Mensaje = resultado.Mensaje,
                    Penalizacion = resultado.Penalizacion ?? 0m,
                    FechaHoraTurno = resultado.FechaHoraTurno,
                    NombreEspecialidad = resultado.NombreEspecialidad,
                    AccionConfirmar = "Cancelar",
                    ControladorConfirmar = "Turno",
                    AccionVolver = "MisTurnos",
                    ControladorVolver = "Home"
                });
            }

            if (resultado.Resultado != ResultadoCancelarTurno.Cancelado)
            {
                TempData["MensajeError"] = resultado.Mensaje;
                return RedirectToAction("MisTurnos", "Home");
            }

            if (resultado.Penalizacion.HasValue)
            {
                TempData["MensajeAdvertencia"] = resultado.Mensaje;
            }
            else
            {
                TempData["MensajeExito"] = resultado.Mensaje;
            }

            return RedirectToAction("MisTurnos", "Home");
        }

        // =========================================================================
        // REPROGRAMAR TURNO
        // =========================================================================

        // Defecto corregido (ver GestionTurnoService): esta acción manipulaba el DbContext
        // directo y creaba el turno nuevo sin validar que el horario elegido estuviera dentro
        // de la disponibilidad real del odontólogo ni que no chocara con otro turno ya
        // existente (a diferencia de AgendarTurnoAsync, que sí valida ambas cosas).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reprogramar(int idTurnoOriginal, string nuevaFecha, string nuevoHorario)
        {
            var paciente = await ObtenerPacienteActualAsync();
            if (_vistaPreviaAdmin)
            {
                return VistaPreviaSoloLectura("MisTurnos", "Home");
            }

            // La fecha/hora default (+7 días desde el turno original) y el parseo del horario
            // elegido se resuelven acá porque son datos de entrada del formulario, no lógica de
            // negocio del caso de uso; la validación de disponibilidad y choque de agenda sí es
            // responsabilidad de GestionTurnoService.
            var turnoOriginalActual = await _context.Turnos.AsNoTracking()
                .FirstOrDefaultAsync(t => t.NroTurno == idTurnoOriginal && t.NroDocumentoPaciente == paciente.NroDocumento);
            if (turnoOriginalActual == null)
            {
                TempData["MensajeError"] = "No se encontró el turno original para reprogramar.";
                return RedirectToAction("MisTurnos", "Home");
            }

            DateTime nuevaFechaHora = turnoOriginalActual.FechaHoraTurno.AddDays(7);
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

            var resultado = await _gestionTurnoService.ReprogramarTurnoAsync(
                idTurnoOriginal, paciente.TipoDocumento, paciente.NroDocumento, nuevaFechaHora);

            if (resultado.Resultado != ResultadoReprogramarTurno.Reprogramado)
            {
                TempData["MensajeError"] = resultado.Mensaje;
                return RedirectToAction("MisTurnos", "Home");
            }

            TempData["MensajeExito"] = resultado.Mensaje;
            return RedirectToAction("Comprobante", new { idTurno = resultado.NroTurnoNuevo });
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