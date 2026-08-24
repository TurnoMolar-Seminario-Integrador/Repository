using Microsoft.AspNetCore.Mvc;
using Frontend.MVC.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace Frontend.MVC.Controllers
{
    public class TurnoController : Controller
    {
        private readonly HttpClient _httpClient;

        public TurnoController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // GET: Turno/Reservar
        [HttpGet]
        public IActionResult Reservar(int? odontologoId, string? especialidad, bool inhabilitado = false)
        {
            var modelo = new ReservaTurnoViewModel
            {
                OdontologoId = odontologoId ?? 1,
                NombreOdontologo = odontologoId == 2 ? "Dr. Martín López" : (odontologoId == 3 ? "Dr. Alejandro Pérez" : "Dra. Elena Silva"),
                Especialidad = especialidad ?? (odontologoId == 2 ? "Cirugía Maxilofacial e Implantes" : (odontologoId == 3 ? "Odontopediatría" : "Ortodoncia y Endodoncia")),
                FechaSeleccionada = "12 Noviembre, 2026",
                HorarioSeleccionado = "09:30 AM",
                EstaInhabilitado = inhabilitado
            };

            return View(modelo);
        }

        // POST: Turno/Reservar
        [HttpPost]
        public IActionResult Reservar(ReservaTurnoViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            TempData["MensajeExito"] = "¡Turno agendado exitosamente!";
            
            return RedirectToAction("Comprobante", new { 
                fecha = modelo.FechaSeleccionada ?? "12 Nov, 2026",
                hora = modelo.HorarioSeleccionado ?? "09:30 AM",
                doctor = modelo.NombreOdontologo ?? "Dra. Elena Silva",
                especialidad = modelo.Especialidad ?? "Ortodoncia y Endodoncia",
                metodoPago = modelo.MetodoPago == "Particular" ? "Particular (Pago en Clínica)" : "Obra Social (OSDE - Plan 210)"
            });
        }

        // POST: Turno/Cancelar
        [HttpPost]
        public IActionResult Cancelar(int idTurno)
        {
            TempData["MensajeExito"] = "El turno fue cancelado correctamente.";
            TempData["OcultarTurno"] = true;
            return RedirectToAction("Index", "Home");
        }

        // GET: Turno/Comprobante
        [HttpGet]
        public IActionResult Comprobante(string? fecha, string? hora, string? doctor, string? especialidad, string? metodoPago)
        {
            ViewData["FechaTurno"] = $"{fecha ?? "12 Nov, 2026"} • {hora ?? "09:30 AM"}";
            ViewData["Doctor"] = doctor ?? "Dra. Elena Silva";
            ViewData["Especialidad"] = especialidad ?? "Control General";
            ViewData["MetodoPago"] = metodoPago ?? "Obra Social (OSDE)";
            ViewData["CodigoReserva"] = "TM-2026-" + new Random().Next(1000, 9999) + "A";

            return View();
        }
    }
}