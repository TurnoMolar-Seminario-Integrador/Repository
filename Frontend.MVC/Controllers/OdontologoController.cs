using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TurnoMolar.Controllers
{
    [Authorize(Roles = "Odontologo,ResponsableClinica,Admin")]
    public class OdontologoController : Controller
    {
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

        // GET: /Odontologo/TurnosDelDia -> Agenda de Hoy y atenciones en tiempo real
        [HttpGet]
        public IActionResult TurnosDelDia()
        {
            CargarDatosOdontologoViewData();
            return View();
        }

        // GET: /Odontologo/GestionTurnos -> Calendario completo, cancelaciones y reprogramaciones
        [HttpGet]
        public IActionResult GestionTurnos()
        {
            CargarDatosOdontologoViewData();
            return View();
        }

        // GET: /Odontologo/ControlAsistencias -> Control de llegadas, presencia en sala de espera y ausencias
        [HttpGet]
        public IActionResult ControlAsistencias()
        {
            CargarDatosOdontologoViewData();
            return View();
        }

        // GET: /Odontologo/GestionAtenciones -> Registro de fichas médicas e historias clínicas
        [HttpGet]
        public IActionResult GestionAtenciones()
        {
            CargarDatosOdontologoViewData();
            return View();
        }

        // POST: /Odontologo/GuardarAtencion -> Registra la atención realizada e impacta en la Historia Clínica
        [HttpPost]
        public IActionResult GuardarAtencion(string pacienteNombre, string numeroHc, string tratamiento, string diagnostico, string observaciones, string insumos)
        {
            TempData["MensajeExito"] = $"¡Atención de {pacienteNombre} (HC #{numeroHc}) registrada correctamente en la Historia Clínica!";
            return RedirectToAction("TurnosDelDia");
        }
    }
}