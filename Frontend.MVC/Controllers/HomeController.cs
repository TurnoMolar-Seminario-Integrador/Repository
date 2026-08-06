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

        // GET: /Home/MetodosDePago
        [HttpGet]
        public IActionResult MetodosDePago()
        {
            return View();
        }

        // POST: /Home/PagarDeuda (Simula el procesamiento del pago)
        [HttpPost]
        public IActionResult PagarDeuda()
        {
            // ACÁ IRÍA LA LÓGICA DE LA API PARA REGISTRAR EL PAGO Y QUITAR LA DEUDA
            // ej: await _httpClient.PostAsync("/api/pagos", ...);

            // Guardamos el mensaje de éxito
            TempData["PagoExitoso"] = "¡Pago procesado correctamente! Tu cuenta ha sido habilitada para reservar turnos.";

            return RedirectToAction("MetodosDePago");
        }
    }
}