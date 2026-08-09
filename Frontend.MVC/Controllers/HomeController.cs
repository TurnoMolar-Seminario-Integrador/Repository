using Microsoft.AspNetCore.Mvc;

namespace TurnoMolar.Controllers
{
    public class HomeController : Controller
    {
        // GET: /Home/Login (Acceso principal / Selector de pruebas)
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Home/Login (Diferenciación piloto: 1 = Odontólogo, 2 = Paciente)
        [HttpPost]
        public IActionResult Login(string dni)
        {
            if (string.IsNullOrWhiteSpace(dni))
            {
                TempData["ErrorLogin"] = "Por favor, ingresá un número de documento o seleccioná un perfil.";
                return View();
            }

            var valor = dni.Trim();

            if (valor == "1")
            {
                // 1 -> Odontólogo (Dra. Karina González)
                return RedirectToAction("Index", "Odontologo");
            }
            else if (valor == "2")
            {
                // 2 -> Paciente (Manuel Fernández)
                return RedirectToAction("Index", "Home");
            }

            TempData["ErrorLogin"] = "DNI no reconocido para el modo piloto. Ingresá '1' (Odontólogo) o '2' (Paciente).";
            return View();
        }

        // GET: /Home/Index (Portal del Paciente)
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Home/MisTurnos
        [HttpGet]
        public IActionResult MisTurnos()
        {
            return View();
        }

        // GET: /Home/HistorialClinico
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

        // POST: /Home/PagarDeuda
        [HttpPost]
        public IActionResult PagarDeuda()
        {
            TempData["PagoExitoso"] = "¡Pago procesado correctamente! Tu cuenta ha sido habilitada para reservar turnos.";
            return RedirectToAction("MetodosDePago");
        }

        // GET: /Home/Seguros
        [HttpGet]
        public IActionResult Seguros()
        {
            return View();
        }

        // GET: /Home/Configuracion
        [HttpGet]
        public IActionResult Configuracion()
        {
            return View();
        }
    }
}
