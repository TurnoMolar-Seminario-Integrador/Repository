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

        public OdontologoController(IAsistenciaTurnoService asistenciaTurnoService, ITurnoRepository turnoRepository)
        {
            _asistenciaTurnoService = asistenciaTurnoService;
            _turnoRepository = turnoRepository;
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

        // POST: /Odontologo/RegistrarAtencionDelDia -> CUU03, camino básico, paso 4 (parte que
        // le corresponde al Odontólogo), disparado desde la agenda real de hoy (TurnosDelDia).
        // Nota de alcance: por ahora esto solo valida la precondición ("El paciente está
        // registrado como 'Presente' en el sistema", CUU03 §6) y mueve el turno a "Atención
        // Registrada"; todavía no persiste la Historia Clínica, los insumos ni el monto (eso es
        // el resto de CUU03, pendiente de implementar).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarAtencionDelDia(int nroTurno, string tratamiento, string diagnostico, string observaciones, string[]? insumos)
        {
            var tipoDocumento = User.FindFirst("TipoDocumento")?.Value;
            var nroDocumento = User.FindFirst("NroDocumento")?.Value;

            var turno = await _turnoRepository.GetAsync(nroTurno);
            var esDelOdontologoLogueado = turno != null
                && turno.TipoDocumentoOdontologo == tipoDocumento
                && turno.NroDocumentoOdontologo == nroDocumento;

            if (turno == null || !esDelOdontologoLogueado || turno.FechaHoraTurno.Date != DateTime.Today || turno.EstadoTurno != "PRESENTE")
            {
                TempData["MensajeError"] = "No se puede registrar la atención: el turno no está disponible, ya fue procesado, o el paciente todavía no fue marcado como \"Presente\" (eso lo hace el responsable de la clínica en Control de Asistencias).";
                return RedirectToAction("TurnosDelDia");
            }

            turno.RegistrarAtencion();
            await _turnoRepository.UpdateAsync(turno);

            TempData["MensajeExito"] = $"Atención de {turno.Paciente.Nombre} {turno.Paciente.Apellido} registrada en la Historia Clínica. Queda pendiente el cobro por parte del responsable de la clínica para finalizar el turno.";
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