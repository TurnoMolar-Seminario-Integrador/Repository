using Microsoft.AspNetCore.Mvc;
using Frontend.MVC.Models; // Asegurate de que este namespace coincida con el tuyo
using System.Net.Http;
using System.Threading.Tasks;

namespace Frontend.MVC.Controllers
{
    public class TurnoController : Controller
    {
        private readonly HttpClient _httpClient;

        // Inyectamos el HttpClient tal como lo configuraste en Program.cs
        public TurnoController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // GET: Turno/Reservar
        [HttpGet]
        public async Task<IActionResult> Reservar(int pacienteId)
        {
            // 1. ACÁ IRÍA LA LÓGICA DE VALIDACIÓN DE DEUDA
            // var respuesta = await _httpClient.GetAsync($"/api/pacientes/{pacienteId}/deuda");
            // si tiene deuda -> TempData["Error"] = "Posee deudas..." y redirigir.

            // Si está todo OK, devolvemos la vista para empezar el Paso 1
            return View(new ReservaTurnoViewModel());
        }

        // POST: Turno/Reservar
        [HttpPost]
        public async Task<IActionResult> Reservar(ReservaTurnoViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                // Si faltan datos (ej: no tildó aceptar políticas), devolvemos el form con los errores
                return View(modelo);
            }

            // 2. ACÁ LLAMAMOS A LA WEBAPI PARA GUARDAR EL TURNO
            // var response = await _httpClient.PostAsJsonAsync("/api/turnos", modelo);
            // if (response.IsSuccessStatusCode) ...

            // 3. PASO 5: Emisión de comprobante (Transición)
            return RedirectToAction("Comprobante", new { fecha = modelo.FechaSeleccionada });
        }

        // POST: Turno/Cancelar
        // Este es el nuevo método que recibe el clic del botón rojo
        [HttpPost]
        public IActionResult Cancelar(int idTurno)
        {
            // ACÁ IRÍA LA LÓGICA DE LA WEBAPI PARA ELIMINAR EL TURNO
            // Ej: await _httpClient.DeleteAsync($"/api/turnos/{idTurno}");

            // Guardamos el mensaje de éxito para que la vista Index lo muestre arriba de todo
            TempData["MensajeExito"] = "El turno fue cancelado correctamente.";

            // AGREGAR ESTA LÍNEA: Avisamos a la vista que debe ocultar la tarjeta
            TempData["OcultarTurno"] = true;

            // Redirigimos al paciente de vuelta al dashboard (Pestaña Inicio)
            return RedirectToAction("Index", "Home");
        }
    }
}