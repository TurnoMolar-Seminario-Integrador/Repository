using Microsoft.AspNetCore.Mvc;
using TurnoMolar.Models;

namespace TurnoMolar.Controllers
{
    public class AuthController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel modelo)
        {
            if (ModelState.IsValid)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(modelo);
        }
    }
}