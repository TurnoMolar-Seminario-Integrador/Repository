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

        public OdontologoController(
            IAsistenciaTurnoService asistenciaTurnoService,
            ITurnoRepository turnoRepository,
            IFinalizarAtencionService finalizarAtencionService,
            IInsumoRepository insumoRepository)
        {
            _asistenciaTurnoService = asistenciaTurnoService;
            _turnoRepository = turnoRepository;
            _finalizarAtencionService = finalizarAtencionService;
            _insumoRepository = insumoRepository;
        }

        private void CargarDatosOdontologoViewData()
        {
            ViewData["NombreDoctor"] = User.FindFirst("NombreCompleto")?.Value ?? "Dra. Karina González";
            ViewData["RolDoctor"] = User.IsInRole("ResponsableClinica") ? "RESPONSABLE CLÍNICO" : "ODONTÓLOGO";
            ViewData["Matricula"] = "MP 3840";
        }

        // GET: /Odontologo/Index -> Panel Principal del Odontólogo / Responsable de la Clínica
        [HttpGet]
        public IActionResult Index()
        {
            CargarDatosOdontologoViewData();
            return View();
        }

        // GET: /Odontologo/TurnosDelDia -> Agenda de Hoy. Cada odontólogo ve únicamente sus
        // propios turnos de hoy, con el estado real (reflejo de lo que CUU02 haya registrado).
        [HttpGet]
        public async Task<IActionResult> TurnosDelDia()
        {
            CargarDatosOdontologoViewData();

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

        // GET: /Odontologo/GestionTurnos -> Calendario completo, cancelaciones y reprogramaciones
        [HttpGet]
        public IActionResult GestionTurnos()
        {
            CargarDatosOdontologoViewData();
            return View();
        }

        // GET: /Odontologo/ControlAsistencias -> CUU02, camino básico, paso 1: el responsable
        // de la clínica busca el turno del paciente por tipo y número de documento.
        // Actor primario de CUU02: Responsable de la Clínica. El Odontólogo es actor
        // secundario (solo recibe la notificación de que el paciente llegó) y no debe poder
        // ejecutar esta acción él mismo.
        [Authorize(Roles = "ResponsableClinica,Admin")]
        [HttpGet]
        public async Task<IActionResult> ControlAsistencias(string? tipoDocumento, string? nroDocumento)
        {
            CargarDatosOdontologoViewData();

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
        public IActionResult GestionAtenciones()
        {
            CargarDatosOdontologoViewData();
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