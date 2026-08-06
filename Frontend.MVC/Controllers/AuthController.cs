using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using TurnoMolar.Models;

namespace TurnoMolar.Controllers
{
    public class AuthController : Controller
    {
        private readonly HttpClient _httpClient;

        // Inyectamos el HttpClient configurado en Program.cs
        public AuthController(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Recordá verificar que este puerto coincida con el de tu WebAPI al correr ambos proyectos
            _httpClient.BaseAddress = new Uri("https://localhost:7198/");
        }

        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Auth/Login
        //[HttpPost]
        //public async Task<IActionResult> Login(LoginViewModel modelo)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        // Si dejó campos vacíos, vuelve a la vista para mostrar los errores en rojo
        //        return View(modelo);
        //    }

        //    // 1. Armamos el objeto con las credenciales para mandarlo a la API
        //    var credenciales = new
        //    {
        //        Dni = modelo.Dni,
        //        Contrasena = modelo.Contrasena
        //    };

        //    var json = JsonSerializer.Serialize(credenciales);
        //    var content = new StringContent(json, Encoding.UTF8, "application/json");

        //    try
        //    {
        //        // 2. Le pegamos al endpoint de autenticación de tu WebAPI
        //        // (Nota: Vas a tener que crear este endpoint en tu WebAPI más adelante)
        //        var response = await _httpClient.PostAsync("api/auth/login", content);

        //        if (response.IsSuccessStatusCode)
        //        {
        //            // ¡Login exitoso! 
        //            // Acá a futuro guardaremos la cookie o el token JWT que nos devuelva la API
        //            // y evaluaremos si es Odontólogo o Paciente.
        //            // Por ahora, lo mandamos directo al Portal del Paciente (Inicio).
        //            return RedirectToAction("Index", "Home");
        //        }
        //        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        //        {
        //            // Error 401: Credenciales incorrectas
        //            ModelState.AddModelError(string.Empty, "DNI o contraseña incorrectos.");
        //        }
        //        else
        //        {
        //            // Otro tipo de error del servidor
        //            ModelState.AddModelError(string.Empty, "Ocurrió un error al intentar iniciar sesión. Intentá de nuevo.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Si la WebAPI está apagada
        //        ModelState.AddModelError(string.Empty, $"No se pudo conectar con el servidor: {ex.Message}");
        //    }

        //    // Si llegamos acá es porque falló el login, devolvemos la vista con el error
        //    return View(modelo);
        // Le sacamos el "async Task" porque ya no vamos a esperar a la API por ahora
        [HttpPost]
        public IActionResult Login(LoginViewModel modelo)
        {
            // 1. Validamos que haya escrito ALGO en los campos (que no estén vacíos)
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            // ====================================================================
            // MODO PRUEBA DE INTERFAZ (MOCK)
            // Acá iría la llamada a la WebAPI, pero como estamos probando la UI, 
            // asumimos que la contraseña es correcta y lo dejamos pasar directo.
            // ====================================================================

            // 2. Redirigimos directamente al Dashboard (Portal del Paciente)
            return RedirectToAction("Index", "Home");
        }
    }
}