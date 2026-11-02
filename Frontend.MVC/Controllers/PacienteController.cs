using Application.Services;
using DTOs;
using Microsoft.AspNetCore.Mvc;
using TurnoMolar.Models;

namespace TurnoMolar.Controllers
{
    // CUF01 - Incorporar Paciente. El controlador solo traduce el formulario al caso de uso
    // (IIncorporarPacienteService) y el resultado a mensajes: la validación, el control de paciente ya
    // registrado, la obra social y el alta (paciente + historia clínica) viven en el servicio.
    //
    // Antes este controlador accedía directo al DbContext y armaba el alta acá: la historia clínica salía
    // con textos inventados en lugar de vacía, el número de documento se leía como int, el domicilio y la
    // fecha de nacimiento ausentes se completaban con "Rosario" y 1/1/1995, la confirmación no informaba
    // el número de historia clínica y ante un error se le mostraba al paciente el texto de la excepción.
    public class PacienteController : Controller
    {
        // Campo del servicio -> campo del formulario.
        private static readonly Dictionary<string, string> CamposDelFormulario = new()
        {
            ["Nombre"] = nameof(PacienteViewModel.NombrePers),
            ["Apellido"] = nameof(PacienteViewModel.Apellido),
            ["TipoDocumento"] = nameof(PacienteViewModel.TipoDocumento),
            ["NroDocumento"] = nameof(PacienteViewModel.DniPers),
            ["FechaNacimiento"] = nameof(PacienteViewModel.FechaNacimiento),
            ["Telefono"] = nameof(PacienteViewModel.TelefonoPers),
            ["Email"] = nameof(PacienteViewModel.MailPer),
            ["Domicilio"] = nameof(PacienteViewModel.Domicilio),
            ["Password"] = nameof(PacienteViewModel.Clave)
        };

        private readonly IIncorporarPacienteService _incorporarPacienteService;
        private readonly ICoberturaPacienteService _coberturaService;
        private readonly ILogger<PacienteController> _logger;

        public PacienteController(
            IIncorporarPacienteService incorporarPacienteService,
            ICoberturaPacienteService coberturaService,
            ILogger<PacienteController> logger)
        {
            _incorporarPacienteService = incorporarPacienteService;
            _coberturaService = coberturaService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            await CargarObrasSocialesAsync();
            return View(new PacienteViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(PacienteViewModel modelo)
        {
            await CargarObrasSocialesAsync();

            if (modelo.IdObraSocial == "Otra" && string.IsNullOrWhiteSpace(modelo.OtraObraSocial))
            {
                ModelState.AddModelError("OtraObraSocial", "Por favor, especificá el nombre de la obra social.");
            }

            if (!ModelState.IsValid)
            {
                TempData["MensajeError"] = IncorporarPacienteService.MensajeDatosInvalidos;
                return VistaConClavesVacias(modelo);
            }

            try
            {
                var resultado = await _incorporarPacienteService.IncorporarAsync(new IncorporarPacienteRequestDTO
                {
                    DatosPersonales = new DatosPersonalesPacienteDTO
                    {
                        Nombre = modelo.NombrePers,
                        Apellido = modelo.Apellido,
                        TipoDocumento = modelo.TipoDocumento,
                        NroDocumento = modelo.DniPers,
                        FechaNacimiento = modelo.FechaNacimiento,
                        Telefono = modelo.TelefonoPers,
                        Email = modelo.MailPer,
                        Domicilio = modelo.Domicilio
                    },
                    ObraSocial = modelo.IdObraSocial,
                    OtraObraSocial = modelo.OtraObraSocial,
                    Password = modelo.Clave
                });

                switch (resultado.Resultado)
                {
                    case ResultadoIncorporarPaciente.Incorporado:
                        // Paso 3: confirmación con el estado y el número de historia clínica asignado.
                        TempData["MensajeExito"] =
                            $"¡Paciente registrado con éxito! {resultado.Mensaje} " +
                            $"Podés iniciar sesión con tu documento ({resultado.TipoDocumento} {resultado.NroDocumento}) " +
                            "y la contraseña que elegiste.";
                        // Redirección (PRG): recargar la página no vuelve a enviar el alta.
                        return RedirectToAction(nameof(Crear));

                    case ResultadoIncorporarPaciente.DatosInvalidos:
                        foreach (var error in resultado.Errores)
                        {
                            var campo = CamposDelFormulario.TryGetValue(error.Key, out var nombreCampo) ? nombreCampo : string.Empty;
                            ModelState.AddModelError(campo, error.Value);
                        }
                        TempData["MensajeError"] = resultado.Mensaje;
                        break;

                    default:
                        // Alt 1.a.1 (paciente ya registrado) y alt 2.a.1.a.1 (obra social sin acuerdo activo):
                        // se informa y el formulario conserva lo que ya cargó, para poder corregirlo (por
                        // ejemplo, elegir la modalidad particular).
                        TempData["MensajeError"] = resultado.Mensaje;
                        break;
                }
            }
            catch (Exception ex)
            {
                // El detalle técnico va al log, no a la pantalla del paciente.
                _logger.LogError(ex, "Error al registrar el paciente.");
                TempData["MensajeError"] = "No pudimos registrar tus datos por un problema del sistema. Intentá nuevamente en unos minutos.";
            }

            return VistaConClavesVacias(modelo);
        }

        // La contraseña no se devuelve en el modelo ante un error (el campo type="password" tampoco la escribe
        // en el HTML): se la pide de nuevo. Los mensajes de error del ModelState se conservan.
        private ViewResult VistaConClavesVacias(PacienteViewModel modelo)
        {
            modelo.Clave = string.Empty;
            modelo.ConfirmarClave = string.Empty;
            return View(modelo);
        }

        // Obras sociales con acuerdo activo para el desplegable del alta (las demás no se pueden elegir).
        private async Task CargarObrasSocialesAsync()
        {
            ViewBag.ObrasSociales = await _coberturaService.ListarObrasSocialesConAcuerdoAsync();
        }
    }
}
