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
            // Validación personalizada: si eligió "Otra" obra social pero no escribió cuál en el campo de texto
            if (modelo.IdObraSocial == "Otra" && string.IsNullOrWhiteSpace(modelo.OtraObraSocial))
            {
                ModelState.AddModelError("OtraObraSocial", "Por favor, especificá el nombre de la obra social.");
            }

            if (ModelState.IsValid)
            {
                // Determinamos qué valor de obra social enviar al backend
                string obraSocialFinal = modelo.IdObraSocial == "Otra" ? modelo.OtraObraSocial : modelo.IdObraSocial;

                // 1. Armamos el objeto anónimo (DTO) agregando los campos nuevos
                var pacienteDto = new
                {
                    NombrePers = modelo.NombrePers,
                    Apellido = modelo.Apellido,
                    TipoDocumento = modelo.TipoDocumento, // Nuevo campo
                    DniPers = modelo.DniPers,
                    FechaNacimiento = modelo.FechaNacimiento, // Nuevo campo
                    TelefonoPers = modelo.TelefonoPers,
                    MailPer = modelo.MailPer,
                    Domicilio = modelo.Domicilio,
                    EstadoHabilitacion = modelo.EstadoHabilitacion,
                    ObraSocial = obraSocialFinal // Mandamos el nombre final de la obra social
                };

                // 2. Convertimos los datos a formato JSON
                var json = JsonSerializer.Serialize(pacienteDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    // 3. Disparamos la petición POST al endpoint de Pacientes
                    // (Asegurate de que la ruta coincida con la que definiste en PacienteEndpoints.cs)
                    var response = await _httpClient.PostAsync("api/paciente", content);

                    if (response.IsSuccessStatusCode)
                    {
                        // ¡Golazo! Se guardó bien. 
                        // En vez de redirigir, mostramos el cartel de éxito que agregamos en la vista.
                        TempData["MensajeExito"] = "¡Paciente registrado correctamente!";

                        // Limpiamos el formulario para que quede en blanco para cargar un próximo paciente
                        ModelState.Clear();
                        return View(new PacienteViewModel());
                    }
                    else
                    {
                        // Si la API tira un código de error (ej. 400 Bad Request o 500 Internal Error)
                        TempData["MensajeError"] = $"Error de la API: {response.StatusCode}. Revisá los datos.";
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