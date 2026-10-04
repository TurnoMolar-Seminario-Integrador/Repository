using System.Security.Claims;
using Application.Services;
using Data;
using Domain.Model;
using DTOs;
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
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            TurnoMolarDbContext context,
            IAuthService authService,
            IValorarAtencionService valorarAtencionService,
            IAgendaTurnoService agendaTurnoService,
            ILogger<HomeController> logger)
        {
            _context = context;
            _authService = authService;
            _valorarAtencionService = valorarAtencionService;
            _agendaTurnoService = agendaTurnoService;
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
                if (User.IsInRole("ResponsableClinica") || User.IsInRole("Responsable de la Clínica") || User.IsInRole("Odontologo") || User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Odontologo");
                }
                else if (User.IsInRole("Paciente"))
                {
                    return RedirectToAction("Index", "Home");
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

                if (respuesta.Rol == "ResponsableClinica" || respuesta.Rol == "Responsable de la Clínica" || respuesta.Rol == "Odontologo" || respuesta.Rol == "Admin")
                {
                    return RedirectToAction("Index", "Odontologo");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
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

        [HttpGet]
        public IActionResult AccessDenied()
        {
            TempData["ErrorLogin"] = "No tenés permisos para acceder a la sección solicitada con tu rol actual.";
            return RedirectToAction("Login", "Home");
        }

        // =========================================================================
        // MÉTODOS PRIVADOS DE APOYO
        // =========================================================================

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
                .Include(p => p.ObraSocial)
                .Where(p => p.Turno != null && p.Turno.NroDocumentoPaciente == paciente.NroDocumento)
                .OrderByDescending(p => p.FechaHoraPago)
                .ToListAsync();

            var viewModel = new MetodosPagoViewModel
            {
                Paciente = paciente,
                HistorialPagos = pagos
            };

            return View(viewModel);
        }

        // Defecto corregido: esta acción tenía su propia implementación de "pagar la deuda",
        // duplicada e independiente de AgendaTurnoService.PagarDeudaAsync (la que usa
        // TurnoController para la alt 2.a.1.a de CUU01). Si el paciente no tenía ningún turno,
        // esta versión inventaba uno nuevo en un estado ("ATENDIDO") que ni siquiera existe en
        // la máquina de estados de Turno, y ligaba el Pago a "el primer turno que aparezca" en
        // vez de al turno que realmente originó la deuda. Se unifica en un solo lugar
        // (AgendaTurnoService.PagarDeudaAsync) para que ambas pantallas se comporten igual.
        [HttpPost]
        [Authorize(Roles = "Paciente,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PagarDeuda(string? numeroTarjeta, string? titular, string metodoPago = "TARJETA_CREDITO")
        {
            var paciente = await ObtenerPacienteActualAsync();

            var resultado = await _agendaTurnoService.PagarDeudaAsync(paciente.TipoDocumento, paciente.NroDocumento);

            if (resultado.Resultado == ResultadoPagarDeuda.Regularizada)
            {
                TempData["PagoExitoso"] = $"¡Pago de ${resultado.MontoPagado:N0} procesado correctamente! Tu cuenta ha sido habilitada para reservar nuevos turnos.";
            }
            else
            {
                TempData["MensajeInfo"] = resultado.Mensaje;
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

            var obrasSociales = await _context.ObrasSociales
                .Include(o => o.Convenios)
                .Where(o => o.EstadoOS == "ACTIVA")
                .ToListAsync();

            var viewModel = new SegurosViewModel
            {
                Paciente = paciente,
                ObrasSocialesDisponibles = obrasSociales
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Paciente,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarSeguro(string idObraSocial, string? nroAfiliado)
        {
            var paciente = await ObtenerPacienteActualAsync();

            var obraSocial = await _context.ObrasSociales.FindAsync(idObraSocial);
            if (obraSocial != null)
            {
                paciente.AsignarObraSocial(obraSocial);
                await _context.SaveChangesAsync();
                TempData["MensajeExito"] = $"¡Tu cobertura médica se actualizó a {obraSocial.NombreOS} ({obraSocial.PlanCobertura}) exitosamente!";
            }
            else
            {
                TempData["MensajeError"] = "La obra social seleccionada no es válida.";
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