using System.Security.Claims;
using Application.Services;
using Data;
using Domain.Model;
using DTOs;
using Frontend.MVC.Infrastructure;
using Frontend.MVC.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TurnoMolar.Controllers
{
    public class HomeController : Controller
    {
        private readonly TurnoMolarDbContext _context;
        private readonly IAuthService _authService;
        private readonly IValorarAtencionService _valorarAtencionService;
        private readonly IAgendaTurnoService _agendaTurnoService;
        private readonly IRegularizarDeudaService _regularizarDeudaService;
        private readonly ICoberturaPacienteService _coberturaService;
        private readonly ILogger<HomeController> _logger;

        // Verdadero cuando la sesión es un Admin mirando el portal de "el primer paciente" (vista
        // previa): ver ObtenerPacienteActualAsync y VistaPreviaSoloLectura.
        private bool _vistaPreviaAdmin;

        public HomeController(
            TurnoMolarDbContext context,
            IAuthService authService,
            IValorarAtencionService valorarAtencionService,
            IAgendaTurnoService agendaTurnoService,
            IRegularizarDeudaService regularizarDeudaService,
            ICoberturaPacienteService coberturaService,
            ILogger<HomeController> logger)
        {
            _context = context;
            _authService = authService;
            _valorarAtencionService = valorarAtencionService;
            _agendaTurnoService = agendaTurnoService;
            _regularizarDeudaService = regularizarDeudaService;
            _coberturaService = coberturaService;
            _logger = logger;
        }

        // =========================================================================
        // EC03 - LOGIN: Autenticación por Usuario/DNI y redirección según Rol
        // =========================================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var inicio = InicioDelRol(User.FindFirst(ClaimTypes.Role)?.Value);
                if (inicio != null)
                {
                    return RedirectToAction(inicio.Value.Action, inicio.Value.Controller);
                }
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string tipoDocumento, string nroDocumento, string? password, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(tipoDocumento) || string.IsNullOrWhiteSpace(nroDocumento))
            {
                TempData["ErrorLogin"] = "Por favor, ingresá tu tipo y número de documento.";
                return View();
            }

            try
            {
                var respuesta = await _authService.LoginAsync(new LoginRequestDTO
                {
                    TipoDocumento = tipoDocumento.Trim(),
                    NroDocumento = nroDocumento.Trim(),
                    Password = (password ?? string.Empty).Trim()
                });

                if (respuesta == null)
                {
                    TempData["ErrorLogin"] = "Documento o contraseña incorrectos.";
                    return View();
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, $"{respuesta.TipoDocumento}|{respuesta.NroDocumento}"),
                    new Claim("TipoDocumento", respuesta.TipoDocumento),
                    new Claim("NroDocumento", respuesta.NroDocumento),
                    new Claim(ClaimTypes.Role, respuesta.Rol),
                    new Claim("NombreCompleto", respuesta.NombreCompleto)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("Usuario {TipoDocumento} {NroDocumento} ({Rol}) autenticado con éxito.", respuesta.TipoDocumento, respuesta.NroDocumento, respuesta.Rol);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                var inicioDelRol = InicioDelRol(respuesta.Rol);
                if (inicioDelRol != null)
                {
                    return RedirectToAction(inicioDelRol.Value.Action, inicioDelRol.Value.Controller);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el proceso de autenticación.");
                TempData["ErrorLogin"] = "Ocurrió un error al conectar con la base de datos. Verificá la conexión.";
                return View();
            }
        }

        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Home");
        }

        // Cada rol tiene sus propias secciones. Si alguien con la sesión iniciada entra a una que es de
        // otro rol (por la barra de direcciones o un favorito), se lo devuelve a la consola de su rol
        // con un aviso. Antes se lo mandaba al login, que por tener sesión lo volvía a redirigir y el
        // aviso no se veía.
        [HttpGet]
        public IActionResult AccessDenied()
        {
            var rol = User.FindFirst(ClaimTypes.Role)?.Value;
            var inicio = User.Identity?.IsAuthenticated == true ? InicioDelRol(rol) : null;

            if (inicio != null)
            {
                TempData["MensajeError"] =
                    $"No tenés acceso a esa sección con tu rol actual ({NombreDelRol(rol)}). " +
                    "Si también tenés otro rol, cerrá sesión e ingresá con la contraseña de ese rol.";
                return RedirectToAction(inicio.Value.Action, inicio.Value.Controller);
            }

            TempData["ErrorLogin"] = "No tenés permisos para acceder a la sección solicitada con tu rol actual.";
            return RedirectToAction("Login", "Home");
        }

        // =========================================================================
        // MÉTODOS PRIVADOS DE APOYO
        // =========================================================================

        // Pantalla de inicio de cada rol. El odontólogo entra a su consola; el responsable de la
        // clínica y el admin (un responsable con rango superior) a la del responsable; el paciente a
        // su portal. Devuelve null si el rol no tiene una consola propia.
        private static (string Action, string Controller)? InicioDelRol(string? rol) => rol switch
        {
            "Odontologo" => ("Index", "Odontologo"),
            "ResponsableClinica" or "Admin" => ("Index", "Responsable"),
            "Paciente" => ("Index", "Home"),
            _ => null
        };

        private static string NombreDelRol(string? rol) => rol switch
        {
            "Odontologo" => "Odontólogo",
            "ResponsableClinica" => "Responsable de la Clínica",
            "Admin" => "Administrador",
            "Paciente" => "Paciente",
            _ => rol ?? "sin rol"
        };

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
        // (pagar la deuda, cambiar la obra social, actualizar los datos de contacto) se aplicaban al primer
        // paciente real de la base. El Admin puede mirar todas las pantallas, pero no operar sobre esa
        // cuenta.
        private IActionResult VistaPreviaSoloLectura(string accion)
        {
            TempData["MensajeError"] = VistaPreviaAdmin.MensajeSoloLectura;
            return RedirectToAction(accion);
        }

        // =========================================================================
        // 1. INICIO (DASHBOARD)
        // =========================================================================

        [HttpGet]
        [Authorize(Roles = "Paciente,Admin")]
        public async Task<IActionResult> Index()
        {
            var paciente = await ObtenerPacienteActualAsync();

            // Defecto corregido: "CONFIRMADO" y "PENDIENTE" no son estados reales de Turno (ver
            // Turno.EstadoTurno / ME - Máquina de Estados); el único estado vigente antes de la
            // fecha del turno es "RESERVADO". Esas dos comparaciones nunca eran verdaderas.
            var proximoTurno = await _context.Turnos
                .Include(t => t.Odontologo)
                .Include(t => t.Especialidad)
                .Include(t => t.Comprobante)
                .Where(t => t.NroDocumentoPaciente == paciente.NroDocumento &&
                            t.EstadoTurno == "RESERVADO" &&
                            t.FechaHoraTurno >= DateTime.Today)
                .OrderBy(t => t.FechaHoraTurno)
                .FirstOrDefaultAsync();

            // Defecto corregido: excluía "ATENDIDO", que tampoco es un estado real -- el estado
            // terminal real es "FINALIZADO" (ver Turno.Finalizar()), así que los turnos ya
            // finalizados se seguían contando como pendientes. También se excluye
            // "REPROGRAMADO": ese turno quedó reemplazado por uno nuevo (NroTurnoOriginal) y
            // contarlo además del turno nuevo duplica el conteo.
            var turnosPendientesCount = await _context.Turnos
                .CountAsync(t => t.NroDocumentoPaciente == paciente.NroDocumento &&
                                 t.EstadoTurno != "CANCELADO" &&
                                 t.EstadoTurno != "FINALIZADO" &&
                                 t.EstadoTurno != "REPROGRAMADO");

            var atencionesRealizadasCount = await _context.Atenciones
                .CountAsync(a => a.Turno != null && a.Turno.NroDocumentoPaciente == paciente.NroDocumento);

            var odontologos = await _context.Odontologos
                .Include(o => o.DisponibilidadesHorarias)
                    .ThenInclude(d => d.Especialidad)
                .ToListAsync();

            var especialidades = await _context.Especialidades.ToListAsync();

            var viewModel = new DashboardPacienteViewModel
            {
                Paciente = paciente,
                ProximoTurno = proximoTurno,
                TurnosPendientesCount = turnosPendientesCount,
                AtencionesRealizadasCount = atencionesRealizadasCount,
                Odontologos = odontologos,
                Especialidades = especialidades
            };

            return View(viewModel);
        }

        // =========================================================================
        // 2. MIS TURNOS (LISTADO, CANCELACIÓN, REPROGRAMACIÓN, VALORACIÓN)
        // =========================================================================

        [HttpGet]
        [Authorize(Roles = "Paciente,Admin")]
        public async Task<IActionResult> MisTurnos()
        {
            var paciente = await ObtenerPacienteActualAsync();

            var turnos = await _context.Turnos
                .Include(t => t.Odontologo)
                .Include(t => t.Especialidad)
                .Include(t => t.Comprobante)
                // CUU04 v1.03: Turno.PendienteDeValoracion exige el pago de la atención registrado.
                .Include(t => t.Pago)
                .Include(t => t.Atencion)
                    .ThenInclude(a => a!.Valoracion)
                .Where(t => t.NroDocumentoPaciente == paciente.NroDocumento)
                .OrderByDescending(t => t.FechaHoraTurno)
                .ToListAsync();

            var odontologos = await _context.Odontologos
                .Include(o => o.DisponibilidadesHorarias)
                    .ThenInclude(d => d.Especialidad)
                .ToListAsync();
            var especialidades = await _context.Especialidades.ToListAsync();

            // CUU04 - Valorar Atención Odontológica, camino básico paso 1: listado de
            // atenciones finalizadas pendientes de valoración (pestaña "Para Valorar").
            var resultadoPendientes = await _valorarAtencionService.ObtenerPendientesDeValoracionAsync(
                paciente.TipoDocumento, paciente.NroDocumento);

            // CUU01, mensajePoliticasCancelacion (la misma que se muestra al reservar).
            ViewBag.MensajePoliticaCancelacion = _agendaTurnoService.ObtenerMensajePoliticaCancelacion();

            var viewModel = new MisTurnosViewModel
            {
                Paciente = paciente,
                Turnos = turnos,
                Odontologos = odontologos,
                Especialidades = especialidades,
                PendientesDeValoracion = resultadoPendientes.Pendientes,
                PendientesPorDeuda = resultadoPendientes.PendientesPorDeuda
            };

            return View(viewModel);
        }

        // =========================================================================
        // 3. HISTORIAL CLÍNICO
        // =========================================================================

        [HttpGet]
        [Authorize(Roles = "Paciente,Admin")]
        public async Task<IActionResult> HistorialClinico()
        {
            var paciente = await ObtenerPacienteActualAsync();

            var historiaClinica = await _context.HistoriasClinicas
                .Include(h => h.Paciente)
                .FirstOrDefaultAsync(h => h.NroDocumentoPaciente == paciente.NroDocumento);

            var atenciones = await _context.Atenciones
                .Include(a => a.Turno)
                    .ThenInclude(t => t.Odontologo)
                .Include(a => a.Turno)
                    .ThenInclude(t => t.Especialidad)
                .Include(a => a.DetallesInsumos)
                    .ThenInclude(d => d.Insumo)
                .Include(a => a.Valoracion)
                .Where(a => a.Turno != null && a.Turno.NroDocumentoPaciente == paciente.NroDocumento)
                .OrderByDescending(a => a.FechaHoraAtencionInicio)
                .ToListAsync();

            var viewModel = new HistorialClinicoViewModel
            {
                Paciente = paciente,
                HistoriaClinica = historiaClinica,
                Atenciones = atenciones
            };

            return View(viewModel);
        }

        // =========================================================================
        // 4. MÉTODOS DE PAGO Y FACTURACIÓN
        // =========================================================================

        [HttpGet]
        [Authorize(Roles = "Paciente,Admin")]
        public async Task<IActionResult> MetodosDePago()
        {
            var paciente = await ObtenerPacienteActualAsync();

            var pagos = await _context.Pagos
                .Include(p => p.Turno)
                    .ThenInclude(t => t.Atencion)
                .Include(p => p.Turno)
                    .ThenInclude(t => t.Especialidad)
                .Include(p => p.ObraSocial)
                .Where(p => p.Turno != null && p.Turno.NroDocumentoPaciente == paciente.NroDocumento)
                .OrderByDescending(p => p.FechaHoraPago)
                .ToListAsync();

            var viewModel = new MetodosPagoViewModel
            {
                Paciente = paciente,
                HistorialPagos = pagos,
                // CUF10 paso 1: con saldo pendiente, además del monto, el detalle del turno que lo originó.
                DeudaDetalle = (paciente.MontoAdeudado ?? 0m) > 0m
                    ? await _regularizarDeudaService.ConsultarDeudaAsync(paciente.TipoDocumento, paciente.NroDocumento)
                    : null
            };

            return View(viewModel);
        }

        // CUF10: el paciente abona el TOTAL de la deuda (pasos 2 y 3). Comparte la lógica con la pantalla
        // de Reservar (TurnoController.PagarDeuda, CUU01 alt 2.a.1.a) a través de IRegularizarDeudaService:
        // registra el pago con el método elegido, asociado al turno que originó la deuda, y habilita al
        // paciente.
        //
        // Defecto corregido: la acción recibía el método elegido (y datos de una tarjeta que no existía)
        // y los descartaba: el pago quedaba registrado con un método fijo, sin importar lo que el
        // paciente hubiera elegido. La pantalla ofrecía además dos tarjetas inventadas.
        [HttpPost]
        [Authorize(Roles = "Paciente,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PagarDeuda(string? metodoPago)
        {
            var paciente = await ObtenerPacienteActualAsync();
            if (_vistaPreviaAdmin)
            {
                return VistaPreviaSoloLectura("MetodosDePago");
            }

            var resultado = await _regularizarDeudaService.PagarDeudaAsync(
                paciente.TipoDocumento, paciente.NroDocumento, metodoPago ?? string.Empty);

            switch (resultado.Resultado)
            {
                case ResultadoPagarDeuda.Regularizada:
                    TempData["PagoExitoso"] = resultado.Mensaje;
                    break;
                case ResultadoPagarDeuda.SinDeuda:
                    // CUF10 alt 1.a: mensajeSinDeuda.
                    TempData["MensajeInfo"] = resultado.Mensaje;
                    break;
                default:
                    TempData["MensajeError"] = resultado.Mensaje;
                    break;
            }

            return RedirectToAction("MetodosDePago");
        }

        // =========================================================================
        // 5. SEGUROS Y OBRAS SOCIALES
        // =========================================================================

        [HttpGet]
        [Authorize(Roles = "Paciente,Admin")]
        public async Task<IActionResult> Seguros()
        {
            var paciente = await ObtenerPacienteActualAsync();

            // Solo las obras sociales con acuerdo activo (CUF02 alt 2.b.1.a.1). Antes se ofrecía también la
            // "obra social" PARTICULAR (que dejaba al paciente con una obra social que no cubre nada) y
            // las que figuran activas pero no tienen ningún convenio con la clínica.
            var obrasSociales = (await _coberturaService.ListarObrasSocialesConAcuerdoAsync()).ToList();

            var viewModel = new SegurosViewModel
            {
                Paciente = paciente,
                ObrasSocialesDisponibles = obrasSociales
            };

            return View(viewModel);
        }

        // CUF02: el paciente cambia su obra social o pasa a particular (idObraSocial vacío). Las reglas
        // (acuerdo activo, confirmación para pasar a particular y el resguardo de los turnos de obra
        // social pendientes) están en ICoberturaPacienteService.
        //
        // Defecto corregido: cambiaba la obra social sin ningún control (con una obra social sin
        // convenios, o con un turno de obra social pendiente de atención, que pasaba a calcularse con la
        // obra social nueva) y la pantalla pedía un número de afiliado que se descartaba.
        [HttpPost]
        [Authorize(Roles = "Paciente,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarSeguro(string? idObraSocial, bool confirmaPasarAParticular = false)
        {
            var paciente = await ObtenerPacienteActualAsync();
            if (_vistaPreviaAdmin)
            {
                return VistaPreviaSoloLectura("Seguros");
            }

            var resultado = await _coberturaService.ActualizarObraSocialAsync(
                paciente.TipoDocumento, paciente.NroDocumento, idObraSocial, confirmaPasarAParticular);

            switch (resultado.Resultado)
            {
                case ResultadoActualizarCobertura.Actualizada:
                    // CUF02, mensaje de éxito del diccionario de datos, y la cobertura que quedó.
                    TempData["MensajeExito"] = resultado.NombreObraSocial != null
                        ? $"{resultado.Mensaje} Tu cobertura ahora es {resultado.NombreObraSocial}."
                        : $"{resultado.Mensaje} Ahora abonás tus turnos en la modalidad particular.";
                    break;

                case ResultadoActualizarCobertura.RequiereConfirmacion:
                    // CUF02 alt 2.a.1: se informa que se eliminará el acuerdo de obra social y se pide confirmar.
                    ViewData["Mensaje"] = resultado.Mensaje;
                    ViewData["ActiveTab"] = "Seguros";
                    return View("ConfirmarSeguroParticular");

                default:
                    TempData["MensajeError"] = resultado.Mensaje;
                    break;
            }

            return RedirectToAction("Seguros");
        }

        // =========================================================================
        // 6. CONFIGURACIÓN Y PERFIL DEL PACIENTE
        // =========================================================================

        [HttpGet]
        [Authorize(Roles = "Paciente,Admin")]
        public async Task<IActionResult> Configuracion()
        {
            var paciente = await ObtenerPacienteActualAsync();

            var viewModel = new ConfiguracionViewModel
            {
                TipoDocumento = paciente.TipoDocumento,
                NroDocumento = paciente.NroDocumentoInt,
                Nombre = paciente.Nombre,
                Apellido = paciente.Apellido,
                Telefono = paciente.Telefono,
                Email = paciente.Email,
                Domicilio = paciente.Domicilio,
                EstadoPaciente = paciente.EstadoPaciente,
                ObraSocialNombre = paciente.ObraSocial != null ? $"{paciente.ObraSocial.NombreOS} - {paciente.ObraSocial.PlanCobertura}" : "Particular / Sin Obra Social"
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Paciente,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarPerfil(string telefono, string email, string domicilio)
        {
            var paciente = await ObtenerPacienteActualAsync();
            if (_vistaPreviaAdmin)
            {
                return VistaPreviaSoloLectura("Configuracion");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["MensajeError"] = "El correo electrónico es obligatorio.";
                return RedirectToAction("Configuracion");
            }

            paciente.SetTelefono(telefono);
            paciente.SetEmail(email);
            paciente.SetDomicilio(domicilio);

            await _context.SaveChangesAsync();

            TempData["MensajeExito"] = "Tus datos personales fueron actualizados y guardados correctamente en la base de datos.";
            return RedirectToAction("Configuracion");
        }
    }
}