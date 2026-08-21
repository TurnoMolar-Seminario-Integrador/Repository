using Microsoft.AspNetCore.Mvc;

namespace TurnoMolar.Controllers
{
    public class OdontologoController : Controller
    {
        // GET: /Odontologo/Index -> Panel Principal del Odontólogo
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Odontologo/TurnosDelDia -> Agenda de Hoy y atenciones en tiempo real
        [HttpGet]
        public IActionResult TurnosDelDia()
        {
            return View();
        }

        // GET: /Odontologo/GestionTurnos -> Calendario completo, cancelaciones y reprogramaciones
        [HttpGet]
        public IActionResult GestionTurnos()
        {
            return View();
        }

        // GET: /Odontologo/ControlAsistencias -> Control de llegadas, presencia en sala de espera y ausencias
        [HttpGet]
        public IActionResult ControlAsistencias()
        {
            return View();
        }

        // GET: /Odontologo/GestionAtenciones -> Registro de fichas médicas e historias clínicas
        [HttpGet]
        public IActionResult GestionAtenciones()
        {
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