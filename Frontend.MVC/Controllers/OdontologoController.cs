using Microsoft.AspNetCore.Mvc;

namespace TuProyecto.Controllers
{
    public class OdontologoController : Controller
    {
        // GET: /Odontologo/Index -> Equivale al "Menu Responsable clinica"
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult TurnosDelDia()
        {
            return View();
        }
    }
}