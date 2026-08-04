using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using TurnoMolar.Models;

namespace TurnoMolar.Controllers
{
    public class PacienteController : Controller
    {
        private readonly HttpClient _httpClient;

        // Inyectamos el HttpClient que configuramos en Program.cs
        public PacienteController(HttpClient httpClient)
        {
            _httpClient = httpClient;

            // ¡ATENCIÓN ACÁ! Tenés que cambiar este puerto por el que usa tu WebAPI.
            // Lo podés sacar del archivo launchSettings.json del proyecto WebAPI.
            _httpClient.BaseAddress = new Uri("https://localhost:7198/");
        }

        [HttpGet]
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Crear(PacienteViewModel modelo)
        {
            if (ModelState.IsValid)
            {
                // 1. Armamos un objeto anónimo (o DTO) que coincida con lo que espera tu API
                var pacienteDto = new
                {
                    NombrePers = modelo.NombrePers,
                    Apellido = modelo.Apellido,
                    DniPers = modelo.DniPers,
                    TelefonoPers = modelo.TelefonoPers,
                    MailPer = modelo.MailPer,
                    Domicilio = modelo.Domicilio,
                    EstadoHabilitacion = modelo.EstadoHabilitacion,
                    IdObraSocial = modelo.IdObraSocial
                };

                // 2. Convertimos los datos a formato JSON
                var json = JsonSerializer.Serialize(pacienteDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    // 3. Disparamos la petición POST al endpoint de Pacientes[cite: 1]
                    // (Asegurate de que la ruta coincida con la que definiste en PacienteEndpoints.cs)
                    var response = await _httpClient.PostAsync("api/paciente", content);

                    if (response.IsSuccessStatusCode)
                    {
                        // ¡Golazo! Se guardó bien. Redirigimos al inicio.
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        // Si la API tira un código de error (ej. 400 Bad Request o 500 Internal Error)
                        ModelState.AddModelError(string.Empty, $"Error de la API: {response.StatusCode}. Revisá los datos.");
                    }
                }
                catch (Exception ex)
                {
                    // Por si la API está apagada o no hay conexión
                    ModelState.AddModelError(string.Empty, $"No se pudo conectar con el servidor: {ex.Message}");
                }
            }

            // Si falla la validación o hubo un error, devolvemos la vista con los avisos
            return View(modelo);
        }
    }
}