using Application.Services;
using Data;
using Domain.Model;
using DTOs;
using Frontend.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TurnoMolar.Controllers
{
    // Consola del Responsable de la Clínica. El Admin es un responsable con rango superior (en la
    // documentación solo se distingue por el valor de Rol en ResponsablesClinica), así que comparte
    // esta consola.
    //
    // Una misma persona puede tener varios roles (una contraseña por rol; el login elige el rol
    // según la contraseña con la que ingresa). Cada rol tiene sus propias secciones: las del
    // odontólogo están en OdontologoController y estas no se comparten con él. Por eso el
    // responsable ve y cobra los turnos de TODOS los odontólogos, mientras que el odontólogo solo ve
    // los propios; ya no hace falta ser odontólogo y responsable a la vez para llevar un turno a
    // "Finalizado".
    [Authorize(Roles = "ResponsableClinica,Admin")]
    public class ResponsableController : Controller
    {
        private readonly IAsistenciaTurnoService _asistenciaTurnoService;
        private readonly ITurnoRepository _turnoRepository;
        private readonly IFinalizarAtencionService _finalizarAtencionService;
        private readonly IOdontologoRepository _odontologoRepository;
        private readonly IGestionTurnoService _gestionTurnoService;
        private readonly IAgendaTurnoService _agendaTurnoService;
        private readonly IEspecialidadRepository _especialidadRepository;
        private readonly IRegularizarDeudaService _regularizarDeudaService;
        private readonly IRectificarDatosPacienteService _rectificarDatosService;
        private readonly ICoberturaPacienteService _coberturaService;
        private readonly ILogger<ResponsableController> _logger;

        public ResponsableController(
            IAsistenciaTurnoService asistenciaTurnoService,
            ITurnoRepository turnoRepository,
            IFinalizarAtencionService finalizarAtencionService,
            IOdontologoRepository odontologoRepository,
            IGestionTurnoService gestionTurnoService,
            IAgendaTurnoService agendaTurnoService,
            IEspecialidadRepository especialidadRepository,
            IRegularizarDeudaService regularizarDeudaService,
            IRectificarDatosPacienteService rectificarDatosService,
            ICoberturaPacienteService coberturaService,
            ILogger<ResponsableController> logger)
        {
            _asistenciaTurnoService = asistenciaTurnoService;
            _turnoRepository = turnoRepository;
            _finalizarAtencionService = finalizarAtencionService;
            _odontologoRepository = odontologoRepository;
            _gestionTurnoService = gestionTurnoService;
            _agendaTurnoService = agendaTurnoService;
            _especialidadRepository = especialidadRepository;
            _regularizarDeudaService = regularizarDeudaService;
            _rectificarDatosService = rectificarDatosService;
            _coberturaService = coberturaService;
            _logger = logger;
        }

        // GET: /Responsable/Index -> Panel Principal del Responsable de la Clínica
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var agenda = await ConstruirAgendaAsync();

            return View(new PanelResponsableViewModel
            {
                TurnosHoy = agenda.TurnosDeHoy.Count,
                PendientesDeCobro = agenda.PendientesDeCobro
            });
        }

        // =========================================================================
        // TURNOS DEL DÍA (todos los odontólogos)
        // =========================================================================
        // GET: /Responsable/TurnosDelDia -> agenda de hoy de la clínica completa. Es la pantalla
        // desde la que se cobra (CUU03, pasos 5 y 6). Antes el único botón "Cobrar" estaba en la
        // agenda del odontólogo, que solo lista los turnos de la persona con la sesión iniciada:
        // un turno solo se podía cobrar si quien cobraba era, además, el odontólogo de ese turno.
        [HttpGet]
        public async Task<IActionResult> TurnosDelDia()
        {
            return View(await ConstruirAgendaAsync());
        }

        private async Task<AgendaResponsableViewModel> ConstruirAgendaAsync()
        {
            var hoy = DateTime.Today;

            var deHoy = (await _turnoRepository.GetByFechaAsync(hoy))
                .Where(t => t.EstadoTurno != "CANCELADO" && t.EstadoTurno != "REPROGRAMADO")
                .OrderBy(t => t.FechaHoraTurno)
                .Select(AFilaGestionTurno)
                .ToList();

            var anteriores = (await _turnoRepository.GetByEstadoAsync("ATENCION_REGISTRADA"))
                .Where(t => t.FechaHoraTurno.Date < hoy)
                .OrderBy(t => t.FechaHoraTurno)
                .Select(AFilaGestionTurno)
                .ToList();

            return new AgendaResponsableViewModel
            {
                Fecha = hoy,
                TurnosDeHoy = deHoy,
                CobrosPendientesAnteriores = anteriores
            };
        }

        // =========================================================================
        // GESTIÓN DE TURNOS
        // =========================================================================
        // Antes era una maqueta con filas fijas y botones sin acción, visible también para el
        // odontólogo. La documentación (BSQ y GUI) no define una pantalla de "gestión" para el
        // odontólogo: su trabajo es la consola de atención (Turnos del Día). Para el responsable
        // sí hay casos de uso que lo habilitan a actuar en representación del paciente: agendar
        // (CUU01 alt 3.a, Responsable/NuevoTurno), cancelar (CUF07 alt 2.a) y reprogramar (CUF08
        // alt 2.a). Esta pantalla los reúne sobre datos reales y reutiliza GestionTurnoService, de
        // modo que valen las mismas reglas que para el paciente (RN10, RN11, RN17 y RN19).
        private const int MaximoFilasGestionTurnos = 200;

        // GET: /Responsable/GestionTurnos
        [HttpGet]
        public async Task<IActionResult> GestionTurnos(string? busqueda, string? estado)
        {
            var verTodos = string.Equals(estado, "todos", StringComparison.OrdinalIgnoreCase);
            var texto = (busqueda ?? string.Empty).Trim();

            IEnumerable<Turno> turnos = await _turnoRepository.GetAllAsync();

            // Por defecto, los turnos que todavía no concluyeron (RN8): Reservado, Presente o
            // Atención Registrada.
            if (!verTodos)
            {
                turnos = turnos.Where(t => t.PendienteDeAtencion);
            }

            if (texto.Length > 0)
            {
                turnos = turnos.Where(t =>
                    t.NroDocumentoPaciente.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                    $"{t.Paciente?.Nombre} {t.Paciente?.Apellido}".Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                    $"{t.Paciente?.Apellido} {t.Paciente?.Nombre}".Contains(texto, StringComparison.OrdinalIgnoreCase));
            }

            // Pendientes: del más próximo al más lejano. Todos: del más reciente al más antiguo.
            var coincidencias = (verTodos
                ? turnos.OrderByDescending(t => t.FechaHoraTurno)
                : turnos.OrderBy(t => t.FechaHoraTurno)).ToList();

            var modelo = new GestionTurnosViewModel
            {
                Busqueda = texto,
                Estado = verTodos ? "todos" : "pendientes",
                TotalCoincidencias = coincidencias.Count,
                Filas = coincidencias.Take(MaximoFilasGestionTurnos).Select(AFilaGestionTurno).ToList()
            };

            return View(modelo);
        }

        // POST: /Responsable/CancelarTurnoEnRepresentacion -> CUF07 alt 2.a: el responsable registra
        // la cancelación en representación del paciente. Con menos de 24 hs de anticipación (o con el
        // turno ya vencido) corresponde la penalización y el paciente queda inhabilitado (CUF07 alt
        // 3.a, RN9 y RN11), igual que si el paciente hubiera cancelado por su cuenta: primero se
        // muestra el aviso del cobro (3.a.1) y la cancelación se ejecuta recién al confirmarla (3.a.2,
        // confirmaFueraDeTermino = true).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarTurnoEnRepresentacion(
            int idTurno, string tipoDocumentoPaciente, string nroDocumentoPaciente,
            string? motivoCancelacion, string? busqueda, string? estado, bool confirmaFueraDeTermino = false)
        {
            var motivo = string.IsNullOrWhiteSpace(motivoCancelacion)
                ? "Cancelado por el responsable de la clínica a pedido del paciente"
                : motivoCancelacion.Trim();

            var resultado = await _gestionTurnoService.CancelarTurnoAsync(
                idTurno, tipoDocumentoPaciente, nroDocumentoPaciente, motivo, confirmaFueraDeTermino);

            if (resultado.Resultado == ResultadoCancelarTurno.RequiereConfirmacion)
            {
                var turno = await _turnoRepository.GetAsync(idTurno);
                return View("ConfirmarCancelacion", new ConfirmarCancelacionViewModel
                {
                    IdTurno = idTurno,
                    MotivoCancelacion = motivoCancelacion,
                    Mensaje = resultado.Mensaje,
                    Penalizacion = resultado.Penalizacion ?? 0m,
                    FechaHoraTurno = resultado.FechaHoraTurno,
                    NombreEspecialidad = resultado.NombreEspecialidad,
                    TipoDocumentoPaciente = tipoDocumentoPaciente,
                    NroDocumentoPaciente = nroDocumentoPaciente,
                    NombrePaciente = turno?.Paciente != null
                        ? $"{turno.Paciente.Apellido}, {turno.Paciente.Nombre}"
                        : $"{tipoDocumentoPaciente} {nroDocumentoPaciente}",
                    Busqueda = busqueda,
                    Estado = estado,
                    AccionConfirmar = "CancelarTurnoEnRepresentacion",
                    ControladorConfirmar = "Responsable",
                    AccionVolver = "GestionTurnos",
                    ControladorVolver = "Responsable"
                });
            }

            if (resultado.Resultado != ResultadoCancelarTurno.Cancelado)
            {
                TempData["MensajeError"] = resultado.Mensaje;
            }
            else if (resultado.Penalizacion.HasValue)
            {
                // El texto del servicio está redactado para el paciente ("quedaste Inhabilitado"); acá se
                // informa al responsable (sConfirmacionCancelacionFueraDeTermino = estadoTurno +
                // montoAdeudado + estadoPaciente).
                TempData["MensajeAdvertencia"] =
                    $"El turno N° {idTurno} fue cancelado fuera de término. " +
                    $"Se registró una deuda de ${resultado.Penalizacion.Value:N0} y el paciente quedó Inhabilitado hasta regularizarla.";
            }
            else
            {
                TempData["MensajeExito"] = $"El turno N° {idTurno} fue cancelado sin penalización.";
            }

            return RedirectToAction("GestionTurnos", new { busqueda, estado });
        }

        // POST: /Responsable/ReprogramarTurnoEnRepresentacion -> CUF08 alt 2.a: el responsable
        // registra la reprogramación en representación del paciente. El servicio mantiene
        // odontólogo y especialidad (RN17), exige la antelación mínima (RN19), valida la
        // disponibilidad y el choque de agenda, y no admite reprogramar con menos de 24 hs.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReprogramarTurnoEnRepresentacion(
            int idTurno, string tipoDocumentoPaciente, string nroDocumentoPaciente,
            string? nuevaFecha, string? nuevoHorario, string? busqueda, string? estado)
        {
            if (!DateTime.TryParse(nuevaFecha, out var fecha) || !TimeOnly.TryParse(nuevoHorario, out var hora))
            {
                TempData["MensajeError"] = "Indique una fecha y un horario válidos para el nuevo turno.";
                return RedirectToAction("GestionTurnos", new { busqueda, estado });
            }

            var nuevaFechaHora = fecha.Date.Add(hora.ToTimeSpan());

            var resultado = await _gestionTurnoService.ReprogramarTurnoAsync(
                idTurno, tipoDocumentoPaciente, nroDocumentoPaciente, nuevaFechaHora);

            if (resultado.Resultado != ResultadoReprogramarTurno.Reprogramado)
            {
                TempData["MensajeError"] = resultado.Mensaje;
            }
            else
            {
                TempData["MensajeExito"] =
                    $"El turno N° {idTurno} fue reprogramado para el {nuevaFechaHora:dd/MM/yyyy HH:mm} hs. " +
                    $"Nuevo turno N° {resultado.NroTurnoNuevo}.";
            }

            return RedirectToAction("GestionTurnos", new { busqueda, estado });
        }

        // =========================================================================
        // NUEVO TURNO (CUU01 alt 3.a): alta manual por el Responsable de la Clínica
        // =========================================================================
        // Antes vivía en TurnoController/ReservarManual con el layout del portal del paciente (menú
        // y perfil del paciente para el responsable) y, al terminar, redirigía al comprobante del
        // paciente, que falla para este rol. Ahora forma parte de la consola del responsable y
        // vuelve a Gestión de Turnos, donde se ve el turno registrado.
        [HttpGet]
        public async Task<IActionResult> NuevoTurno()
        {
            await CargarListasNuevoTurnoAsync();
            return View(new AgendarTurnoRequestDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NuevoTurno(AgendarTurnoRequestDTO request)
        {
            // Sin fecha y hora no se puede evaluar la antelación mínima (RN19): se pide completarla en
            // lugar de mostrar "no respeta las 24 horas" para el año 1.
            if (request.FechaHoraTurno == default)
            {
                return await VolverANuevoTurnoAsync(request, "Indique la fecha y hora acordadas para el turno.");
            }

            AgendarTurnoResultDTO resultado;
            try
            {
                resultado = await _agendaTurnoService.AgendarTurnoManualAsync(request);
            }
            catch (ArgumentException ex)
            {
                return await VolverANuevoTurnoAsync(request, ex.Message);
            }

            if (resultado.Resultado != ResultadoAgendarTurno.Reservado)
            {
                return await VolverANuevoTurnoAsync(request, DescribirRechazoReservaManual(resultado));
            }

            var turno = resultado.NroTurno.HasValue ? await _turnoRepository.GetAsync(resultado.NroTurno.Value) : null;
            var paciente = turno?.Paciente != null
                ? $"{turno.Paciente.Apellido}, {turno.Paciente.Nombre} ({request.TipoDocumentoPaciente} {request.NroDocumentoPaciente})"
                : $"{request.TipoDocumentoPaciente} {request.NroDocumentoPaciente}";

            TempData["MensajeExito"] =
                $"Turno N° {resultado.NroTurno} registrado para {paciente} el {request.FechaHoraTurno:dd/MM/yyyy HH:mm} hs.";

            // Se muestra el listado filtrado por ese paciente para que el turno nuevo quede a la vista.
            return RedirectToAction("GestionTurnos", new { busqueda = request.NroDocumentoPaciente, estado = "pendientes" });
        }

        // Vuelve a mostrar el formulario con lo que se había cargado y el motivo del rechazo (antes se
        // redirigía y el formulario aparecía vacío, con la fecha "01/01/0001").
        private async Task<IActionResult> VolverANuevoTurnoAsync(AgendarTurnoRequestDTO request, string mensaje)
        {
            ViewData["MensajeError"] = mensaje;
            await CargarListasNuevoTurnoAsync();
            return View("NuevoTurno", request);
        }

        private async Task CargarListasNuevoTurnoAsync()
        {
            // CUF12 E4.R02 alt 1.d.2: solo se ofrecen las especialidades habilitadas.
            var especialidades = (await _especialidadRepository.GetAllAsync()).Where(e => e.EstaHabilitada).ToList();
            ViewBag.Especialidades = especialidades;
            ViewBag.Odontologos = (await _odontologoRepository.GetAllAsync()).OrderBy(o => o.Apellido).ThenBy(o => o.Nombre).ToList();

            // "tipo|nro" del odontólogo -> especialidades que atiende (según sus franjas de
            // disponibilidad; DisponibilidadHoraria es la clase asociativa Odontólogo-Especialidad). La
            // vista lo usa para ofrecer solo los odontólogos de la especialidad elegida.
            var porOdontologo = new Dictionary<string, List<int>>();
            foreach (var esp in especialidades)
            {
                foreach (var odontologo in await _odontologoRepository.GetByEspecialidadAsync(esp.IdEspecialidad))
                {
                    var clave = $"{odontologo.TipoDocumento}|{odontologo.NroDocumento}";
                    if (!porOdontologo.TryGetValue(clave, out var ids))
                    {
                        porOdontologo[clave] = ids = new List<int>();
                    }
                    ids.Add(esp.IdEspecialidad);
                }
            }
            ViewBag.EspecialidadesPorOdontologo = porOdontologo;
        }

        private static string DescribirRechazoReservaManual(AgendarTurnoResultDTO r)
        {
            if (r.Resultado == ResultadoAgendarTurno.TurnoPendienteExistente && r.TurnoPendiente != null)
            {
                var t = r.TurnoPendiente;
                return $"{r.Mensaje} Turno N° {t.Id} del {t.Fecha:dd/MM/yyyy} {t.HorarioTurno.ToString("HH:mm")} hs ({TextoEstado(t.EstadoTurno)}).";
            }

            if (r.Resultado == ResultadoAgendarTurno.Inhabilitado && r.MontoAdeudado.GetValueOrDefault() > 0m)
            {
                return $"{r.Mensaje} Monto adeudado: ${r.MontoAdeudado!.Value:N0}.";
            }

            return r.Mensaje;
        }

        private static string TextoEstado(string estadoTurno) => estadoTurno switch
        {
            "RESERVADO" => "Reservado",
            "PRESENTE" => "Presente",
            "AUSENTE" => "Ausente",
            "ATENCION_REGISTRADA" => "Atención Registrada",
            "FINALIZADO" => "Finalizado",
            "CANCELADO" => "Cancelado",
            "REPROGRAMADO" => "Reprogramado",
            _ => estadoTurno
        };

        private static GestionTurnoFilaViewModel AFilaGestionTurno(Turno t) => new()
        {
            NroTurno = t.NroTurno,
            FechaHoraTurno = t.FechaHoraTurno,
            EstadoTurno = t.EstadoTurno,
            EstadoLegible = TextoEstado(t.EstadoTurno),
            TipoDocumentoPaciente = t.TipoDocumentoPaciente,
            NroDocumentoPaciente = t.NroDocumentoPaciente,
            NombrePaciente = t.Paciente != null ? $"{t.Paciente.Apellido}, {t.Paciente.Nombre}" : t.NroDocumentoPaciente,
            NombreEspecialidad = t.Especialidad?.Nombre ?? string.Empty,
            NombreOdontologo = t.Odontologo != null ? $"{t.Odontologo.Nombre} {t.Odontologo.Apellido}" : string.Empty,
            ModalidadPagoLegible = t.ModalidadPagoElegida == "OBRA_SOCIAL" ? "Obra social" : "Particular",
            PuedeModificar = t.PermiteCancelarOReprogramar
        };

        // =========================================================================
        // CONTROL DE ASISTENCIAS (CUU02)
        // =========================================================================
        // Actor primario de CUU02: Responsable de la Clínica. El Odontólogo es actor secundario
        // (solo recibe la notificación de que el paciente llegó) y no ejecuta esta acción.

        // GET: /Responsable/ControlAsistencias -> CUU02, camino básico, paso 1: el responsable
        // busca el turno del paciente por tipo y número de documento.
        [HttpGet]
        public async Task<IActionResult> ControlAsistencias(string? tipoDocumento, string? nroDocumento)
        {
            if (!string.IsNullOrWhiteSpace(tipoDocumento) && !string.IsNullOrWhiteSpace(nroDocumento))
            {
                ViewBag.TipoDocumentoBuscado = tipoDocumento.Trim();
                ViewBag.NroDocumentoBuscado = nroDocumento.Trim();
                ViewBag.Busqueda = await _asistenciaTurnoService.BuscarTurnoDelDiaAsync(tipoDocumento.Trim(), nroDocumento.Trim());
            }

            return View();
        }

        // POST: /Responsable/RegistrarPresente -> CUU02, camino básico, paso 2.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarPresente(int nroTurno)
        {
            var resultado = await _asistenciaTurnoService.RegistrarPresenteAsync(nroTurno);

            if (resultado.Resultado == ResultadoRegistrarAsistencia.Presente)
            {
                TempData["MensajeExito"] = resultado.Mensaje;
            }
            else
            {
                TempData["MensajeError"] = resultado.Mensaje;
            }

            return RedirectToAction("ControlAsistencias");
        }

        // POST: /Responsable/RegistrarAusente -> CUU02, alt 1.b (registra ausencia + penalización).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarAusente(int nroTurno)
        {
            var resultado = await _asistenciaTurnoService.RegistrarAusenteAsync(nroTurno);

            if (resultado.Resultado == ResultadoRegistrarAsistencia.Ausente)
            {
                TempData["MensajeAdvertencia"] = resultado.Mensaje;
            }
            else
            {
                TempData["MensajeError"] = resultado.Mensaje;
            }

            return RedirectToAction("ControlAsistencias");
        }

        // =========================================================================
        // COBRO DE LA ATENCIÓN (CUU03, pasos 5 y 6)
        // =========================================================================
        // El odontólogo registra la atención (OdontologoController, pasos 1 a 4); el cobro lo hace el
        // responsable de la clínica, sea cual sea el odontólogo que atendió.

        // GET: /Responsable/DatosParaCobro -> CUU03, camino básico, paso 5 (la respuesta del
        // sistema): "El responsable de la clínica indica al sistema que se procederá al cobro.
        // El sistema muestra el monto total a pagar según la modalidad de pago del paciente."
        [HttpGet]
        public async Task<IActionResult> DatosParaCobro(int nroTurno)
        {
            var resultado = await _finalizarAtencionService.ObtenerDatosParaCobroAsync(nroTurno);
            return Json(resultado);
        }

        private const string MensajeDatosCobroInvalidos =
            "Los datos del cobro no son válidos. Revisá los importes (solo números) y el método de pago.";

        // POST: /Responsable/RegistrarCobroParticular -> CUU03, camino básico, paso 6.
        // Actor: Responsable de la Clínica (el Odontólogo no cobra).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarCobroParticular(int nroTurno, decimal monto, string? tipoMetodoPago)
        {
            // Un importe que no se pudo leer (el binder deja el error en ModelState y el valor en 0) no se
            // puede tratar como "monto 0": el mensaje del servicio sería engañoso.
            if (!ModelState.IsValid)
            {
                TempData["MensajeError"] = MensajeDatosCobroInvalidos;
                return RedirectToAction("TurnosDelDia");
            }

            var resultado = await _finalizarAtencionService.RegistrarCobroParticularAsync(nroTurno, monto, tipoMetodoPago ?? string.Empty);

            if (resultado.Resultado == ResultadoRegistrarCobro.Finalizado)
            {
                TempData["MensajeExito"] = resultado.Mensaje;
            }
            else
            {
                TempData["MensajeError"] = resultado.Mensaje;
            }

            return RedirectToAction("TurnosDelDia");
        }

        // POST: /Responsable/RegistrarCobroObraSocial -> CUU03, alt 6.a.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarCobroObraSocial(int nroTurno, string? tipoMetodoPago, decimal aportePaciente, decimal aporteObraSocial)
        {
            if (!ModelState.IsValid)
            {
                TempData["MensajeError"] = MensajeDatosCobroInvalidos;
                return RedirectToAction("TurnosDelDia");
            }

            var resultado = await _finalizarAtencionService.RegistrarCobroObraSocialAsync(nroTurno, tipoMetodoPago ?? string.Empty, aportePaciente, aporteObraSocial);

            if (resultado.Resultado == ResultadoRegistrarCobro.Finalizado)
            {
                TempData["MensajeExito"] = resultado.Mensaje;
            }
            else
            {
                TempData["MensajeError"] = resultado.Mensaje;
            }

            return RedirectToAction("TurnosDelDia");
        }

        // =========================================================================
        // RECTIFICAR DATOS DEL PACIENTE (CUF02, el responsable de la clínica como otro actor)
        // =========================================================================
        // El responsable identifica al paciente por tipo y número de documento, ve sus datos registrados y
        // los rectifica (incluidos el tipo y el número de documento y la obra social). Es el mismo caso de
        // uso que el paciente ejecuta desde su Configuración (IRectificarDatosPacienteService): se valida,
        // se actualiza sin modificar el estado de habilitación y se confirma.

        // GET: /Responsable/RectificarDatosPaciente
        [HttpGet]
        public async Task<IActionResult> RectificarDatosPaciente(string? tipoDocumento, string? nroDocumento)
        {
            var modelo = new RectificarDatosPacienteViewModel
            {
                TipoDocumento = string.IsNullOrWhiteSpace(tipoDocumento) ? "DNI" : tipoDocumento.Trim(),
                NroDocumento = (nroDocumento ?? string.Empty).Trim()
            };

            if (!string.IsNullOrWhiteSpace(tipoDocumento) && modelo.NroDocumento.Length > 0)
            {
                modelo.Busco = true;
                await CargarPacienteAsync(modelo, null);
            }

            return View(modelo);
        }

        // POST: /Responsable/GuardarDatosPaciente -> CUF02, pasos 2 y 3.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarDatosPaciente(FormDatosPersonalesViewModel form)
        {
            var tipoActual = (form.TipoDocumentoActual ?? string.Empty).Trim();
            var nroActual = (form.NroDocumentoActual ?? string.Empty).Trim();

            RectificarDatosResultDTO resultado;
            try
            {
                resultado = await _rectificarDatosService.RectificarDatosAsync(tipoActual, nroActual, form.ADatos(), form.VersionDatos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al rectificar los datos del paciente {TipoDocumento} {NroDocumento}.", tipoActual, nroActual);
                TempData["MensajeError"] = "No pudimos guardar los datos por un problema del sistema. Intentá nuevamente en unos minutos.";
                return RedirectToAction("RectificarDatosPaciente", new { tipoDocumento = tipoActual, nroDocumento = nroActual });
            }

            switch (resultado.Resultado)
            {
                case ResultadoRectificarDatos.Actualizada:
                    TempData["MensajeExito"] = $"{resultado.Mensaje} Paciente: {resultado.NombreCompleto}.";
                    // Si cambió el documento, se vuelve a mostrar al paciente con el documento nuevo.
                    return RedirectToAction("RectificarDatosPaciente",
                        new { tipoDocumento = resultado.TipoDocumento, nroDocumento = resultado.NroDocumento });

                case ResultadoRectificarDatos.SinCambios:
                    TempData["MensajeInfo"] = resultado.Mensaje;
                    return RedirectToAction("RectificarDatosPaciente", new { tipoDocumento = tipoActual, nroDocumento = nroActual });

                case ResultadoRectificarDatos.PacienteNoEncontrado:
                    TempData["MensajeError"] = resultado.Mensaje;
                    return RedirectToAction("RectificarDatosPaciente");

                case ResultadoRectificarDatos.ConflictoConcurrencia:
                    // Otro usuario (p. ej. el propio paciente desde su portal) modificó la ficha mientras se
                    // editaba: no se guardó nada y se vuelven a mostrar los datos vigentes, con su versión nueva.
                    TempData["MensajeError"] = resultado.Mensaje;
                    return RedirectToAction("RectificarDatosPaciente", new { tipoDocumento = tipoActual, nroDocumento = nroActual });

                default:
                    // Datos inválidos o documento ya registrado: se muestra el formulario con lo cargado y el
                    // error junto a cada campo.
                    form.Controlador = "Responsable";
                    form.Accion = "GuardarDatosPaciente";
                    form.Errores = resultado.Errores;
                    TempData["MensajeError"] = resultado.Mensaje;

                    var modelo = new RectificarDatosPacienteViewModel
                    {
                        TipoDocumento = tipoActual,
                        NroDocumento = nroActual,
                        Busco = true
                    };
                    await CargarPacienteAsync(modelo, form);
                    return View("RectificarDatosPaciente", modelo);
            }
        }

        // POST: /Responsable/ActualizarObraSocialPaciente -> CUF02, alt 2.a (baja) y 2.b (alta / cambio).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarObraSocialPaciente(
            string tipoDocumento, string nroDocumento, string? idObraSocial, bool confirmaPasarAParticular = false)
        {
            ActualizarCoberturaResultDTO resultado;
            try
            {
                resultado = await _coberturaService.ActualizarObraSocialAsync(
                    tipoDocumento, nroDocumento, idObraSocial, confirmaPasarAParticular);
            }
            catch (ArgumentException)
            {
                TempData["MensajeError"] = "No se encontró un paciente con ese tipo y número de documento.";
                return RedirectToAction("RectificarDatosPaciente");
            }

            switch (resultado.Resultado)
            {
                case ResultadoActualizarCobertura.Actualizada:
                    TempData["MensajeExito"] = resultado.NombreObraSocial != null
                        ? $"{resultado.Mensaje} La cobertura del paciente ahora es {resultado.NombreObraSocial}."
                        : $"{resultado.Mensaje} El paciente ahora abona en la modalidad particular.";
                    break;

                case ResultadoActualizarCobertura.RequiereConfirmacion:
                    // Alt 2.a.1: pasar a particular elimina el acuerdo de obra social; hay que confirmar.
                    TempData["MensajeInfo"] = $"{resultado.Mensaje} Tildá la confirmación y volvé a guardar.";
                    break;

                default:
                    TempData["MensajeError"] = resultado.Mensaje;
                    break;
            }

            return RedirectToAction("RectificarDatosPaciente", new { tipoDocumento, nroDocumento });
        }

        // Carga en el modelo los datos registrados del paciente (CUF02, paso 1). formEnviado: formulario que el
        // responsable acaba de enviar con errores (conserva lo que cargó) en lugar de los datos registrados.
        private async Task CargarPacienteAsync(RectificarDatosPacienteViewModel modelo, FormDatosPersonalesViewModel? formEnviado)
        {
            try
            {
                var datos = await _rectificarDatosService.ConsultarDatosAsync(modelo.TipoDocumento, modelo.NroDocumento);

                modelo.Form = formEnviado ?? FormDatosPersonalesViewModel.Desde(datos, "Responsable", "GuardarDatosPaciente");
                modelo.Form.TipoDocumentoActual = datos.TipoDocumento;
                modelo.Form.NroDocumentoActual = datos.NroDocumento;
                modelo.TipoDocumento = datos.TipoDocumento ?? modelo.TipoDocumento;
                modelo.NroDocumento = datos.NroDocumento ?? modelo.NroDocumento;
                modelo.EstadoPaciente = datos.EstadoPaciente;
                modelo.IdentificadorOS = datos.IdentificadorOS;
                modelo.ObraSocialNombre = datos.NombreObraSocial;
                modelo.ObrasSociales = await _coberturaService.ListarObrasSocialesConAcuerdoAsync();
            }
            catch (ArgumentException)
            {
                modelo.MensajeBusqueda = "No se encontró un paciente con ese tipo y número de documento.";
            }
        }

        // =========================================================================
        // REGULARIZAR DEUDA DEL PACIENTE (CUF10, alt 2.a: pago presencial)
        // =========================================================================
        // CUF10 alt 2.a <reemplaza> "pago a través de la plataforma": el paciente abona en la clínica; el
        // responsable lo identifica por tipo y número de documento (eRegistroCobroManual) y registra el
        // cobro en el sistema, que continúa en el paso 3: registra el pago del total de la deuda, asociado
        // al turno que la originó, y pasa al paciente a Habilitado. Antes esta alternativa no existía: un
        // paciente inhabilitado solo podía regularizarse desde su portal.

        // GET: /Responsable/RegularizarDeuda
        [HttpGet]
        public async Task<IActionResult> RegularizarDeuda(string? tipoDocumento, string? nroDocumento)
        {
            var modelo = new RegularizarDeudaViewModel
            {
                TipoDocumento = string.IsNullOrWhiteSpace(tipoDocumento) ? "DNI" : tipoDocumento.Trim(),
                NroDocumento = (nroDocumento ?? string.Empty).Trim()
            };

            if (!string.IsNullOrWhiteSpace(tipoDocumento) && modelo.NroDocumento.Length > 0)
            {
                modelo.Busco = true;
                try
                {
                    modelo.Consulta = await _regularizarDeudaService.ConsultarDeudaAsync(modelo.TipoDocumento, modelo.NroDocumento);
                }
                catch (ArgumentException)
                {
                    modelo.MensajeBusqueda = "No se encontró un paciente con ese tipo y número de documento.";
                }
            }

            modelo.PacientesConDeuda = await _regularizarDeudaService.ListarPacientesConDeudaAsync();
            return View(modelo);
        }

        // POST: /Responsable/RegistrarCobroDeuda -> CUF10 alt 2.a.1 y paso 3.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarCobroDeuda(string tipoDocumento, string nroDocumento, string? metodoPago)
        {
            PagarDeudaResultDTO resultado;
            try
            {
                resultado = await _regularizarDeudaService.RegistrarCobroPresencialAsync(tipoDocumento, nroDocumento, metodoPago ?? string.Empty);
            }
            catch (ArgumentException)
            {
                TempData["MensajeError"] = "No se encontró un paciente con ese tipo y número de documento.";
                return RedirectToAction("RegularizarDeuda");
            }

            switch (resultado.Resultado)
            {
                case ResultadoPagarDeuda.Regularizada:
                    TempData["MensajeExito"] = resultado.Mensaje;
                    return RedirectToAction("RegularizarDeuda");

                case ResultadoPagarDeuda.SinDeuda:
                    // Alt 1.a: mensajeSinDeuda.
                    TempData["MensajeInfo"] = resultado.Mensaje;
                    break;

                default:
                    TempData["MensajeError"] = resultado.Mensaje;
                    break;
            }

            // Vuelve con el paciente a la vista para corregir el dato (método) sin tipear de nuevo el documento.
            return RedirectToAction("RegularizarDeuda", new { tipoDocumento, nroDocumento });
        }

        // POST: /Responsable/RegistrarFaltaDePago -> CUU03, alt 6.b (inhabilita al paciente).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarFaltaDePago(int nroTurno)
        {
            var resultado = await _finalizarAtencionService.RegistrarFaltaDePagoAsync(nroTurno);

            if (resultado.Resultado == ResultadoRegistrarCobro.FaltaDePago)
            {
                TempData["MensajeAdvertencia"] = resultado.Mensaje;
            }
            else
            {
                TempData["MensajeError"] = resultado.Mensaje;
            }

            return RedirectToAction("TurnosDelDia");
        }
    }
}
