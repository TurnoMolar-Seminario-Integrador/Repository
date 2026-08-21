using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DTOs;
using TurnoMolar.Models;

namespace TurnoMolar.Controllers
{
    public class PacienteController : Controller
    {
        private readonly HttpClient _httpClient;

        // Inyectamos el HttpClient que configuramos en Program.cs
        public PacienteController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;

            // Se obtiene la URL base de la WebAPI desde la configuración o usa la por defecto (puerto 7266/5263 de la WebAPI)
            if (_httpClient.BaseAddress == null)
            {
                var apiBaseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:7266/";
                _httpClient.BaseAddress = new Uri(apiBaseUrl);
            }
        }

        [HttpGet]
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Crear(PacienteViewModel modelo)
        {
            // Validación personalizada: si eligió "Otra" obra social pero no escribió cuál en el campo de texto
            if (modelo.IdObraSocial == "Otra" && string.IsNullOrWhiteSpace(modelo.OtraObraSocial))
            {
                ModelState.AddModelError("OtraObraSocial", "Por favor, especificá el nombre de la obra social.");
            }

            if (ModelState.IsValid)
            {
                // Mapeo correcto al DTO esperado por la WebAPI y Application.Services
                int.TryParse(modelo.DniPers, out int dniParsed);

                var pacienteDto = new PacienteDTO
                {
                    Nombre = modelo.NombrePers,
                    Apellido = modelo.Apellido,
                    Dni = dniParsed,
                    Telefono = modelo.TelefonoPers,
                    Email = modelo.MailPer,
                    Domicilio = modelo.Domicilio,
                    EstadoHabilitado = modelo.EstadoHabilitacion
                };

                try
                {
                    // Disparamos la petición POST al endpoint /pacientes definido en WebAPI
                    var response = await _httpClient.PostAsJsonAsync("pacientes", pacienteDto);

                    if (response.IsSuccessStatusCode)
                    {
                        // Se guardó bien: mostramos mensaje de éxito y limpiamos formulario
                        TempData["MensajeExito"] = "¡Paciente registrado correctamente!";
                        ModelState.Clear();
                        return View(new PacienteViewModel());
                    }
                    else
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        TempData["MensajeError"] = $"Error de la API ({response.StatusCode}): {responseBody}";
                    }
                }
                catch (Exception ex)
                {
                    // Por si la API está apagada o no hay conexión
                    TempData["MensajeError"] = $"No se pudo conectar con el servidor: {ex.Message}";
                }
            }
            else
            {
                // Si el ModelState es inválido (faltan datos requeridos)
                TempData["MensajeError"] = "Por favor, revisá los campos marcados en rojo.";
            }

            // Si falla la validación o hubo un error, devolvemos la vista con los avisos y los datos que ya había escrito
            return View(modelo);
        }
    }
}