using Application.Services;
using Data;
using Domain.Model;
using DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TurnoMolar.Controllers
{
    [Authorize(Roles = "Odontologo,ResponsableClinica,Admin")]
    public class OdontologoController : Controller
    {
        private readonly IAsistenciaTurnoService _asistenciaTurnoService;
        private readonly ITurnoRepository _turnoRepository;
        private readonly IFinalizarAtencionService _finalizarAtencionService;
        private readonly IInsumoRepository _insumoRepository;
        private readonly IOdontologoRepository _odontologoRepository;
        private readonly IGestionTurnoService _gestionTurnoService;
        private readonly IAgendaTurnoService _agendaTurnoService;
        private readonly IEspecialidadRepository _especialidadRepository;

        public OdontologoController(
            IAsistenciaTurnoService asistenciaTurnoService,
            ITurnoRepository turnoRepository,
            IFinalizarAtencionService finalizarAtencionService,
            IInsumoRepository insumoRepository,
            IOdontologoRepository odontologoRepository,
            IGestionTurnoService gestionTurnoService,
            IAgendaTurnoService agendaTurnoService,
            IEspecialidadRepository especialidadRepository)
        {
            _asistenciaTurnoService = asistenciaTurnoService;
            _turnoRepository = turnoRepository;
            _finalizarAtencionService = finalizarAtencionService;
            _insumoRepository = insumoRepository;
            _odontologoRepository = odontologoRepository;
            _gestionTurnoService = gestionTurnoService;
            _agendaTurnoService = agendaTurnoService;
            _especialidadRepository = especialidadRepository;
        }

        // Defecto corregido: la matrícula quedaba hardcodeada en "MP 3840" (la de Karina
        // González) sin importar qué profesional estuviera logueado. Ahora se busca la del
        // odontólogo real; si quien inició sesión es un ResponsableClinica/Admin sin fila propia
        // en Odontologos, se deja en blanco en vez de mostrar una matrícula ajena.
        private async Task CargarDatosOdontologoViewDataAsync()
        {
            ViewData["NombreDoctor"] = User.FindFirst("NombreCompleto")?.Value ?? "Profesional";
            ViewData["RolDoctor"] = User.IsInRole("ResponsableClinica") ? "RESPONSABLE CLÍNICO" : "ODONTÓLOGO";

            var tipoDocumento = User.FindFirst("TipoDocumento")?.Value;
            var nroDocumento = User.FindFirst("NroDocumento")?.Value;

            string? matricula = null;
            string? email = null;
            string? telefono = null;
            string? estadoOdontologo = null;

            if (!string.IsNullOrWhiteSpace(tipoDocumento) && !string.IsNullOrWhiteSpace(nroDocumento))
            {
                var odontologo = await _odontologoRepository.GetAsync(tipoDocumento, nroDocumento);
                matricula = odontologo?.Matricula;
                email = odontologo?.Email;
                telefono = odontologo?.Telefono;
                estadoOdontologo = odontologo?.EstadoOdontologo;
            }

            ViewData["Matricula"] = matricula ?? string.Empty;
            ViewData["Email"] = email ?? string.Empty;
            ViewData["Telefono"] = telefono ?? string.Empty;
            ViewData["EstadoOdontologo"] = estadoOdontologo ?? string.Empty;
        }

        // GET: /Odontologo/Index -> Panel Principal del Odontólogo / Responsable de la Clínica
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await CargarDatosOdontologoViewDataAsync();
            return View();
        }

        // GET: /Odontologo/TurnosDelDia -> Agenda de Hoy. Cada odontólogo ve únicamente sus
        // propios turnos de hoy, con el estado real (reflejo de lo que CUU02 haya registrado).
        [HttpGet]
        public async Task<IActionResult> TurnosDelDia()
        {
            await CargarDatosOdontologoViewDataAsync();

            var tipoDocumento = User.FindFirst("TipoDocumento")?.Value;
            var nroDocumento = User.FindFirst("NroDocumento")?.Value;

            var agenda = new List<TurnoAgendaDelDiaDTO>();
            if (!string.IsNullOrWhiteSpace(tipoDocumento) && !string.IsNullOrWhiteSpace(nroDocumento))
            {
                var turnosOdontologo = await _turnoRepository.GetByOdontologoAsync(tipoDocumento, nroDocumento);
                agenda = turnosOdontologo
                    .Where(t => t.FechaHoraTurno.Date == DateTime.Today
                            && t.EstadoTurno != "CANCELADO"
                            && t.EstadoTurno != "REPROGRAMADO")
                    .OrderBy(t => t.FechaHoraTurno)
                    .Select(t => new TurnoAgendaDelDiaDTO
                    {
                        NroTurno = t.NroTurno,
                        FechaHoraTurno = t.FechaHoraTurno,
                        NombrePaciente = t.Paciente?.Nombre ?? string.Empty,
                        ApellidoPaciente = t.Paciente?.Apellido ?? string.Empty,
                        NombreEspecialidad = t.Especialidad?.Nombre ?? string.Empty,
                        ModalidadPago = t.ModalidadPagoElegida == "OBRA_SOCIAL" ? "Obra Social" : "Particular",
                        EstadoTurno = t.EstadoTurno,
                        DescripcionMaterial = t.DescripcionMaterial
                    })
                    .ToList();
            }

            // Paso 3: catálogo de insumos para el formulario de "Iniciar Atención".
            var insumos = await _insumoRepository.GetAllAsync();
            ViewBag.Insumos = insumos
                .Select(i => new InsumoDisponibleDTO
                {
                    IdInsumo = i.IdInsumo,
                    Nombre = i.Nombre,
                    CostoUnitario = i.CostoUnitario,
                    StockDisponible = i.StockDisponible
                })
                .ToList();

            return View(agenda);
        }

        // =========================================================================
        // GESTIÓN DE TURNOS (Responsable de la Clínica)
        // =========================================================================
        // Antes era una maqueta con filas fijas y botones sin acción, visible también para el
        // odontólogo. La documentación (BSQ y GUI) no define una pantalla de "gestión" para el
        // odontólogo: su trabajo es la consola de atención (Turnos del Día). Para el responsable
        // sí hay casos de uso que lo habilitan a actuar en representación del paciente: agendar
        // (CUU01 alt 3.a, Turno/ReservarManual), cancelar (CUF07 alt 2.a) y reprogramar (CUF08
        // alt 2.a). Esta pantalla los reúne sobre datos reales y reutiliza GestionTurnoService, de
        // modo que valen las mismas reglas que para el paciente (RN10, RN11, RN17 y RN19).
        // Por eso se restringe a ResponsableClinica y Admin además del [Authorize] de la clase.
        private const int MaximoFilasGestionTurnos = 200;

        // GET: /Odontologo/GestionTurnos
        [HttpGet]
        [Authorize(Roles = "ResponsableClinica,Admin")]
        public async Task<IActionResult> GestionTurnos(string? busqueda, string? estado)
        {
            await CargarDatosOdontologoViewDataAsync();

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

            var modelo = new Frontend.MVC.Models.GestionTurnosViewModel
            {
                Busqueda = texto,
                Estado = verTodos ? "todos" : "pendientes",
                TotalCoincidencias = coincidencias.Count,
                Filas = coincidencias.Take(MaximoFilasGestionTurnos).Select(AFilaGestionTurno).ToList()
            };

            return View(modelo);
        }

        // POST: /Odontologo/CancelarTurnoEnRepresentacion -> CUF07 alt 2.a: el responsable registra
        // la cancelación en representación del paciente. Con menos de 24 hs de anticipación se
        // aplica la penalización y el paciente queda inhabilitado (CUF07 alt 3.a, RN9 y RN11), igual
        // que si el paciente hubiera cancelado por su cuenta.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ResponsableClinica,Admin")]
        public async Task<IActionResult> CancelarTurnoEnRepresentacion(
            int idTurno, string tipoDocumentoPaciente, string nroDocumentoPaciente,
            string? motivoCancelacion, string? busqueda, string? estado)
        {
            var motivo = string.IsNullOrWhiteSpace(motivoCancelacion)
                ? "Cancelado por el responsable de la clínica a pedido del paciente"
                : motivoCancelacion.Trim();

            var resultado = await _gestionTurnoService.CancelarTurnoAsync(
                idTurno, tipoDocumentoPaciente, nroDocumentoPaciente, motivo);

            if (resultado.Resultado != ResultadoCancelarTurno.Cancelado)
            {
                TempData["MensajeError"] = resultado.Mensaje;
            }
            else if (resultado.Penalizacion.HasValue)
            {
                // El texto del servicio está redactado para el paciente ("en tu cuenta"); acá se
                // informa al responsable.
                TempData["MensajeAdvertencia"] =
                    $"El turno N° {idTurno} fue cancelado con menos de {ReglaAntelacion.HorasMinimas} hs de anticipación. " +
                    $"Se registró una deuda de ${resultado.Penalizacion.Value:N0} y el paciente quedó Inhabilitado hasta regularizarla.";
            }
            else
            {
                TempData["MensajeExito"] = $"El turno N° {idTurno} fue cancelado sin penalización.";
            }

            return RedirectToAction("GestionTurnos", new { busqueda, estado });
        }

        // POST: /Odontologo/ReprogramarTurnoEnRepresentacion -> CUF08 alt 2.a: el responsable
        // registra la reprogramación en representación del paciente. El servicio mantiene
        // odontólogo y especialidad (RN17), exige la antelación mínima (RN19), valida la
        // disponibilidad y el choque de agenda, y no admite reprogramar con menos de 24 hs.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ResponsableClinica,Admin")]
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
        [Authorize(Roles = "ResponsableClinica,Admin")]
        public async Task<IActionResult> NuevoTurno()
        {
            await CargarDatosOdontologoViewDataAsync();
            await CargarListasNuevoTurnoAsync();
            return View(new AgendarTurnoRequestDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ResponsableClinica,Admin")]
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
            await CargarDatosOdontologoViewDataAsync();
            await CargarListasNuevoTurnoAsync();
            return View("NuevoTurno", request);
        }

        private async Task CargarListasNuevoTurnoAsync()
        {
            var especialidades = (await _especialidadRepository.GetAllAsync()).ToList();
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

        private static Frontend.MVC.Models.GestionTurnoFilaViewModel AFilaGestionTurno(Turno t) => new()
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

        // GET: /Odontologo/ControlAsistencias -> CUU02, camino básico, paso 1: el responsable
        // de la clínica busca el turno del paciente por tipo y número de documento.
        // Actor primario de CUU02: Responsable de la Clínica. El Odontólogo es actor
        // secundario (solo recibe la notificación de que el paciente llegó) y no debe poder
        // ejecutar esta acción él mismo.
        [Authorize(Roles = "ResponsableClinica,Admin")]
        [HttpGet]
        public async Task<IActionResult> ControlAsistencias(string? tipoDocumento, string? nroDocumento)
        {
            await CargarDatosOdontologoViewDataAsync();

            if (!string.IsNullOrWhiteSpace(tipoDocumento) && !string.IsNullOrWhiteSpace(nroDocumento))
            {
                ViewBag.TipoDocumentoBuscado = tipoDocumento.Trim();
                ViewBag.NroDocumentoBuscado = nroDocumento.Trim();
                ViewBag.Busqueda = await _asistenciaTurnoService.BuscarTurnoDelDiaAsync(tipoDocumento.Trim(), nroDocumento.Trim());
            }

            return View();
        }

        // POST: /Odontologo/RegistrarPresente -> CUU02, camino básico, paso 2.
        [Authorize(Roles = "ResponsableClinica,Admin")]
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

        // POST: /Odontologo/RegistrarAusente -> CUU02, alt 1.b (registra ausencia + penalización).
        [Authorize(Roles = "ResponsableClinica,Admin")]
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

        // GET: /Odontologo/GestionAtenciones -> Registro de fichas médicas e historias clínicas
        [HttpGet]
        public async Task<IActionResult> GestionAtenciones()
        {
            await CargarDatosOdontologoViewDataAsync();
            return View();
        }

        // GET: /Odontologo/DatosParaAtencion -> CUU03, camino básico, paso 1 (la respuesta del
        // sistema): "muestra el número de historia clínica, la fecha de creación y las
        // atenciones odontológicas previas del paciente, y habilita el formulario de carga".
        // Se consulta ANTES de mostrar el resto del formulario (pasos 2 a 4), no junto con él.
        [HttpGet]
        public async Task<IActionResult> DatosParaAtencion(int nroTurno)
        {
            var tipoDocumento = User.FindFirst("TipoDocumento")?.Value ?? string.Empty;
            var nroDocumento = User.FindFirst("NroDocumento")?.Value ?? string.Empty;

            var resultado = await _finalizarAtencionService.ObtenerDatosParaAtencionAsync(nroTurno, tipoDocumento, nroDocumento);
            return Json(resultado);
        }

        // POST: /Odontologo/RegistrarAtencionDelDia -> CUU03, camino básico, pasos 1 a 4 (parte
        // que le corresponde al Odontólogo), disparado desde la agenda real de hoy (TurnosDelDia).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarAtencionDelDia(
            int nroTurno, DateTime fechaHoraInicio, DateTime fechaHoraFin, string observaciones,
            int[]? insumoIds, int[]? cantidades)
        {
            var tipoDocumento = User.FindFirst("TipoDocumento")?.Value ?? string.Empty;
            var nroDocumento = User.FindFirst("NroDocumento")?.Value ?? string.Empty;

            var detalles = new List<DetalleInsumoInputDTO>();
            if (insumoIds != null && cantidades != null)
            {
                for (int i = 0; i < insumoIds.Length && i < cantidades.Length; i++)
                {
                    detalles.Add(new DetalleInsumoInputDTO { IdInsumo = insumoIds[i], Cantidad = cantidades[i] });
                }
            }

            var resultado = await _finalizarAtencionService.RegistrarAtencionAsync(
                nroTurno, tipoDocumento, nroDocumento, fechaHoraInicio, fechaHoraFin, observaciones, detalles);

            if (resultado.Resultado == ResultadoRegistrarAtencion.Registrada)
            {
                TempData["MensajeExito"] = resultado.Mensaje;
            }
            else
            {
                TempData["MensajeError"] = resultado.Mensaje;
            }

            return RedirectToAction("TurnosDelDia");
        }

        // GET: /Odontologo/DatosParaCobro -> CUU03, camino básico, paso 5 (la respuesta del
        // sistema): "El responsable de la clínica indica al sistema que se procederá al cobro.
        // El sistema muestra el monto total a pagar según la modalidad de pago del paciente."
        [Authorize(Roles = "ResponsableClinica,Admin")]
        [HttpGet]
        public async Task<IActionResult> DatosParaCobro(int nroTurno)
        {
            var resultado = await _finalizarAtencionService.ObtenerDatosParaCobroAsync(nroTurno);
            return Json(resultado);
        }

        // POST: /Odontologo/RegistrarCobroParticular -> CUU03, camino básico, paso 6.
        // Actor: Responsable de la Clínica (el Odontólogo no cobra).
        [Authorize(Roles = "ResponsableClinica,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarCobroParticular(int nroTurno, decimal monto, string tipoMetodoPago)
        {
            var resultado = await _finalizarAtencionService.RegistrarCobroParticularAsync(nroTurno, monto, tipoMetodoPago);

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

        // POST: /Odontologo/RegistrarCobroObraSocial -> CUU03, alt 6.a.
        [Authorize(Roles = "ResponsableClinica,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarCobroObraSocial(int nroTurno, string tipoMetodoPago, decimal aportePaciente, decimal aporteObraSocial)
        {
            var resultado = await _finalizarAtencionService.RegistrarCobroObraSocialAsync(nroTurno, tipoMetodoPago, aportePaciente, aporteObraSocial);

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

        // POST: /Odontologo/RegistrarFaltaDePago -> CUU03, alt 6.b (inhabilita al paciente).
        [Authorize(Roles = "ResponsableClinica,Admin")]
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

        // POST: /Odontologo/GuardarAtencion -> Usado por GestionAtenciones (CUF03, pantalla
        // aparte, todavía mockup). Sin cambios: no se toca hasta abordar esa pantalla.
        [HttpPost]
        public IActionResult GuardarAtencion(string pacienteNombre, string numeroHc, string tratamiento, string diagnostico, string observaciones, string insumos)
        {
            TempData["MensajeExito"] = $"¡Atención de {pacienteNombre} (HC #{numeroHc}) registrada correctamente en la Historia Clínica!";
            return RedirectToAction("TurnosDelDia");
        }
    }
}