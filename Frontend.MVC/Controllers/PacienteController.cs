using Application.Services;
using Data;
using Domain.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurnoMolar.Models;

namespace TurnoMolar.Controllers
{
    public class PacienteController : Controller
    {
        private readonly TurnoMolarDbContext _context;
        private readonly ICoberturaPacienteService _coberturaService;
        private readonly ILogger<PacienteController> _logger;

        public PacienteController(
            TurnoMolarDbContext context,
            ICoberturaPacienteService coberturaService,
            ILogger<PacienteController> logger)
        {
            _context = context;
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

            if (!int.TryParse(modelo.DniPers, out int dniParsed) || dniParsed <= 0)
            {
                ModelState.AddModelError("DniPers", "Ingresá un número de documento válido.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var dniStr = dniParsed.ToString();
                    var tipoDoc = modelo.TipoDocumento ?? "DNI";

                    // CUF01 / RN6: obra social con acuerdo activo o modalidad particular (sin obra social).
                    // Antes una obra social que no estaba en la base dejaba al paciente como particular,
                    // con mensaje de éxito y sin avisarle; y "Otra" buscaba con Contains (escribir "s"
                    // asignaba OSDE).
                    var cobertura = await _coberturaService.ResolverObraSocialParaAltaAsync(modelo.IdObraSocial, modelo.OtraObraSocial);
                    if (!cobertura.Valida)
                    {
                        // CUF01 alt 2.a.1.a.1: se informa y el paciente puede elegir la modalidad particular.
                        // El formulario conserva lo que ya cargó.
                        TempData["MensajeError"] = cobertura.Mensaje;
                        return View(modelo);
                    }

                    // Validar si el paciente ya existe en BD
                    var existe = await _context.Pacientes
                        .AnyAsync(p => p.NroDocumento == dniStr && p.TipoDocumento == tipoDoc.ToUpper());

                    if (existe)
                    {
                        TempData["MensajeError"] = $"El paciente con documento {dniParsed} ya se encuentra registrado en el sistema.";
                        return View(modelo);
                    }

                    // MU: el paciente particular se registra con IdentificadorOS nulo.
                    var idOs = cobertura.IdentificadorOS;

                    // 1. Crear Entidad Paciente (MDF v1.01)
                    // El propio Paciente es la cuenta de login (Clave/SaltClave/Rol viven acá,
                    // no hay una tabla Usuarios aparte — ver AuthService/IUsuarioRepository).
                    var (clave, salt) = PasswordHasher.Generar("paciente123");
                    var nuevoPaciente = new Paciente(
                        tipoDoc,
                        dniStr,
                        modelo.NombrePers,
                        modelo.Apellido,
                        modelo.FechaNacimiento ?? new DateTime(1995, 1, 1),
                        modelo.TelefonoPers,
                        modelo.MailPer,
                        modelo.Domicilio ?? "Rosario",
                        "HABILITADO",
                        idOs,
                        0m,
                        clave,
                        salt,
                        DateTime.Now,
                        "Paciente"
                    );
                    _context.Pacientes.Add(nuevoPaciente);

                    // 2. Crear Historia Clínica de base (MDF v1.01)
                    var hc = new HistoriaClinica(
                        0,
                        nuevoPaciente.TipoDocumento,
                        nuevoPaciente.NroDocumento,
                        DateTime.Now,
                        "Sin antecedentes registrados en el alta.",
                        "Ninguna declarada.",
                        "Ficha clínica de registro inicial."
                    );
                    _context.HistoriasClinicas.Add(hc);

                    await _context.SaveChangesAsync();

                    TempData["MensajeExito"] = $"¡Paciente registrado con éxito! Podés iniciar sesión con tu DNI ({dniParsed}) y contraseña provisoria: 'paciente123'.";
                    ModelState.Clear();
                    return View(new PacienteViewModel());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al registrar el paciente.");
                    TempData["MensajeError"] = $"Ocurrió un error al guardar en la base de datos: {ex.Message}";
                }
            }
            else
            {
                TempData["MensajeError"] = "Por favor, revisá los campos marcados en rojo.";
            }

            return View(modelo);
        }

        // Obras sociales con acuerdo activo para el desplegable del alta (las demás no se pueden elegir).
        private async Task CargarObrasSocialesAsync()
        {
            ViewBag.ObrasSociales = await _coberturaService.ListarObrasSocialesConAcuerdoAsync();
        }
    }
}