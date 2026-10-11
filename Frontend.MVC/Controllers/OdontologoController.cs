using Application.Services;
using Data;
using Domain.Model;
using DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TurnoMolar.Controllers
{
    // Consola del Odontólogo: solo sus propios turnos y su parte de la atención (CUU03, pasos 1 a
    // 4). Todo lo que es del responsable de la clínica (control de asistencia, gestión de turnos,
    // alta manual y cobro) está en ResponsableController. Una persona que es odontólogo y
    // responsable entra a cada consola con la contraseña del rol correspondiente.
    [Authorize(Roles = "Odontologo")]
    public class OdontologoController : Controller
    {
        private readonly ITurnoRepository _turnoRepository;
        private readonly IFinalizarAtencionService _finalizarAtencionService;
        private readonly IInsumoRepository _insumoRepository;
        private readonly IOdontologoRepository _odontologoRepository;

        public OdontologoController(
            ITurnoRepository turnoRepository,
            IFinalizarAtencionService finalizarAtencionService,
            IInsumoRepository insumoRepository,
            IOdontologoRepository odontologoRepository)
        {
            _turnoRepository = turnoRepository;
            _finalizarAtencionService = finalizarAtencionService;
            _insumoRepository = insumoRepository;
            _odontologoRepository = odontologoRepository;
        }

        // Defecto corregido: la matrícula quedaba hardcodeada en "MP 3840" (la de Karina
        // González) sin importar qué profesional estuviera logueado. Ahora se busca la del
        // odontólogo real.
        private async Task CargarDatosOdontologoViewDataAsync()
        {
            ViewData["NombreDoctor"] = User.FindFirst("NombreCompleto")?.Value ?? "Profesional";
            ViewData["RolDoctor"] = "ODONTÓLOGO";

            var tipoDocumento = User.FindFirst("TipoDocumento")?.Value;
            var nroDocumento = User.FindFirst("NroDocumento")?.Value;

            string? matricula = null;
            string? email = null;
            string? telefono = null;
            string? estadoOdontologo = null;

            if (!string.IsNullOrWhiteSpace(tipoDocumento) && !string.IsNullOrWhiteSpace(nroDocumento))
            {
                var odontologo = await _odontologoRepository.GetAsync(tipoDocumento, nroDocumento);
                matricula = odontologo?.Matricula;
                email = odontologo?.Email;
                telefono = odontologo?.Telefono;
                estadoOdontologo = odontologo?.EstadoOdontologo;
            }

            ViewData["Matricula"] = matricula ?? string.Empty;
            ViewData["Email"] = email ?? string.Empty;
            ViewData["Telefono"] = telefono ?? string.Empty;
            ViewData["EstadoOdontologo"] = estadoOdontologo ?? string.Empty;
        }

        // Turnos de hoy del odontólogo con la sesión iniciada (y solo los suyos), sin los
        // cancelados ni los reprogramados.
        private async Task<List<Turno>> ObtenerTurnosDeHoyAsync()
        {
            var tipoDocumento = User.FindFirst("TipoDocumento")?.Value;
            var nroDocumento = User.FindFirst("NroDocumento")?.Value;

            if (string.IsNullOrWhiteSpace(tipoDocumento) || string.IsNullOrWhiteSpace(nroDocumento))
            {
                return new List<Turno>();
            }

            var turnosOdontologo = await _turnoRepository.GetByOdontologoAsync(tipoDocumento, nroDocumento);
            return turnosOdontologo
                .Where(t => t.FechaHoraTurno.Date == DateTime.Today
                        && t.EstadoTurno != "CANCELADO"
                        && t.EstadoTurno != "REPROGRAMADO")
                .OrderBy(t => t.FechaHoraTurno)
                .ToList();
        }

        // GET: /Odontologo/Index -> Panel Principal del Odontólogo
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await CargarDatosOdontologoViewDataAsync();
            ViewData["TurnosHoy"] = (await ObtenerTurnosDeHoyAsync()).Count;
            return View();
        }

        // GET: /Odontologo/TurnosDelDia -> Agenda de Hoy. Cada odontólogo ve únicamente sus
        // propios turnos de hoy, con el estado real (reflejo de lo que CUU02 haya registrado).
        // El cobro no se hace desde acá: lo registra el responsable de la clínica en su propia
        // agenda (Responsable/TurnosDelDia), que reúne los turnos de todos los odontólogos.
        [HttpGet]
        public async Task<IActionResult> TurnosDelDia()
        {
            await CargarDatosOdontologoViewDataAsync();

            var agenda = (await ObtenerTurnosDeHoyAsync())
                .Select(t => new TurnoAgendaDelDiaDTO
                {
                    NroTurno = t.NroTurno,
                    FechaHoraTurno = t.FechaHoraTurno,
                    NombrePaciente = t.Paciente?.Nombre ?? string.Empty,
                    ApellidoPaciente = t.Paciente?.Apellido ?? string.Empty,
                    NombreEspecialidad = t.Especialidad?.Nombre ?? string.Empty,
                    ModalidadPago = t.ModalidadPagoElegida == "OBRA_SOCIAL" ? "Obra Social" : "Particular",
                    EstadoTurno = t.EstadoTurno,
                    DescripcionMaterial = t.DescripcionMaterial
                })
                .ToList();

            // Paso 3: catálogo de insumos para el formulario de "Iniciar Atención".
            var insumos = await _insumoRepository.GetAllAsync();
            ViewBag.Insumos = insumos
                .Select(i => new InsumoDisponibleDTO
                {
                    IdInsumo = i.IdInsumo,
                    Nombre = i.Nombre,
                    CostoUnitario = i.CostoUnitario,
                    StockDisponible = i.StockDisponible
                })
                .ToList();

            return View(agenda);
        }

        // GET: /Odontologo/GestionAtenciones -> Registro de fichas médicas e historias clínicas
        [HttpGet]
        public async Task<IActionResult> GestionAtenciones()
        {
            await CargarDatosOdontologoViewDataAsync();
            return View();
        }

        // GET: /Odontologo/DatosParaAtencion -> CUU03, camino básico, paso 1 (la respuesta del
        // sistema): "muestra el número de historia clínica, la fecha de creación y las
        // atenciones odontológicas previas del paciente, y habilita el formulario de carga".
        // Se consulta ANTES de mostrar el resto del formulario (pasos 2 a 4), no junto con él.
        [HttpGet]
        public async Task<IActionResult> DatosParaAtencion(int nroTurno)
        {
            var tipoDocumento = User.FindFirst("TipoDocumento")?.Value ?? string.Empty;
            var nroDocumento = User.FindFirst("NroDocumento")?.Value ?? string.Empty;

            var resultado = await _finalizarAtencionService.ObtenerDatosParaAtencionAsync(nroTurno, tipoDocumento, nroDocumento);
            return Json(resultado);
        }

        // POST: /Odontologo/RegistrarAtencionDelDia -> CUU03, camino básico, pasos 1 a 4 (parte
        // que le corresponde al Odontólogo), disparado desde la agenda real de hoy (TurnosDelDia).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarAtencionDelDia(
            int nroTurno, DateTime fechaHoraInicio, DateTime fechaHoraFin, string observaciones,
            int[]? insumoIds, int[]? cantidades)
        {
            var tipoDocumento = User.FindFirst("TipoDocumento")?.Value ?? string.Empty;
            var nroDocumento = User.FindFirst("NroDocumento")?.Value ?? string.Empty;

            var detalles = new List<DetalleInsumoInputDTO>();
            if (insumoIds != null && cantidades != null)
            {
                for (int i = 0; i < insumoIds.Length && i < cantidades.Length; i++)
                {
                    detalles.Add(new DetalleInsumoInputDTO { IdInsumo = insumoIds[i], Cantidad = cantidades[i] });
                }
            }

            var resultado = await _finalizarAtencionService.RegistrarAtencionAsync(
                nroTurno, tipoDocumento, nroDocumento, fechaHoraInicio, fechaHoraFin, observaciones, detalles);

            if (resultado.Resultado == ResultadoRegistrarAtencion.Registrada)
            {
                TempData["MensajeExito"] = resultado.Mensaje;
            }
            else
            {
                TempData["MensajeError"] = resultado.Mensaje;
            }

            return RedirectToAction("TurnosDelDia");
        }

        // POST: /Odontologo/GuardarAtencion -> Usado por GestionAtenciones (CUF03, pantalla
        // aparte, todavía mockup). Sin cambios: no se toca hasta abordar esa pantalla.
        [HttpPost]
        public IActionResult GuardarAtencion(string pacienteNombre, string numeroHc, string tratamiento, string diagnostico, string observaciones, string insumos)
        {
            TempData["MensajeExito"] = $"¡Atención de {pacienteNombre} (HC #{numeroHc}) registrada correctamente en la Historia Clínica!";
            return RedirectToAction("TurnosDelDia");
        }
    }
}
