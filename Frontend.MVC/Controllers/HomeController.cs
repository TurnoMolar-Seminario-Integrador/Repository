using Microsoft.AspNetCore.Mvc;

namespace TurnoMolar.Controllers
{
    public class HomeController : Controller
    {
        // GET: /Home/Index (Pestaña Inicio)
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Home/HistorialClinico (Pestaña Historial Clínico)
        [HttpGet]
        public IActionResult HistorialClinico()
        {
            return View();
        }
    }
}