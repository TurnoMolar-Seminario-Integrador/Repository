using Application.Services;
using Data;
using Domain.Model;
using DTOs;
using Frontend.MVC.Models;
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
        private readonly IAdministrarHistoriaClinicaService _historiaClinicaService;
        private readonly IConsultarValoracionesService _valoracionesService;
        private readonly ILogger<OdontologoController> _logger;

        public OdontologoController(
            ITurnoRepository turnoRepository,
            IFinalizarAtencionService finalizarAtencionService,
            IInsumoRepository insumoRepository,
            IOdontologoRepository odontologoRepository,
            IAdministrarHistoriaClinicaService historiaClinicaService,
            IConsultarValoracionesService valoracionesService,
            ILogger<OdontologoController> logger)
        {
            _turnoRepository = turnoRepository;
            _finalizarAtencionService = finalizarAtencionService;
            _insumoRepository = insumoRepository;
            _odontologoRepository = odontologoRepository;
            _historiaClinicaService = historiaClinicaService;
            _valoracionesService = valoracionesService;
            _logger = logger;
        }

        // Defecto corregido: la matrícula quedaba hardcodeada en "MP 3840" (la de Karina
        // González) sin importar qué profesional estuviera logueado. Ahora se busca la del
        // odontólogo real.
        // CUF05 - Consultar Valoraciones (pasos 1 y 2, alt 1.a): el odontólogo ve solo las valoraciones
        // de sus propias atenciones y el detalle de una de ellas.
        [HttpGet]
        public async Task<IActionResult> MisValoraciones(int? nroTurno)
        {
            await CargarDatosOdontologoViewDataAsync();
            var tipoDocumento = User.FindFirst("TipoDocumento")?.Value ?? string.Empty;
            var nroDocumento = User.FindFirst("NroDocumento")?.Value ?? string.Empty;
            if (nroTurno.HasValue)
                ViewBag.Detalle = await _valoracionesService.ObtenerDetalleAsync(nroTurno.Value, tipoDocumento, nroDocumento);
            return View(await _valoracionesService.ConsultarPorOdontologoAsync(tipoDocumento, nroDocumento));
        }

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
                .Where(i => i.EstaHabilitado) // CUF09 E4.R02 alt 3.c
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

        // =========================================================================
        // CUF03 - ADMINISTRAR HISTORIA CLÍNICA
        // =========================================================================
        // Antes esta pantalla era un mockup: mostraba dos atenciones inventadas y su botón "Guardar
        // Evolución" respondía "registrada correctamente en la Historia Clínica" sin guardar nada. Ahora
        // consulta las historias clínicas reales (IAdministrarHistoriaClinicaService) y permite corregir las
        // observaciones de una atención ya registrada. Cargar una atención nueva no es de este caso de uso:
        // se hace desde "Turnos del día" (CUU03).

        // GET: /Odontologo/GestionAtenciones -> pasos 1 a 3: busca al paciente (por documento o por N° de
        // historia clínica), muestra sus datos y su historial cronológico y, con nroTurno, el detalle de la
        // atención seleccionada.
        [HttpGet]
        public async Task<IActionResult> GestionAtenciones(string? tipoDocumento, string? nroDocumento, string? nroHC, int? nroTurno)
        {
            await CargarDatosOdontologoViewDataAsync();
            return View(await ArmarGestionHistoriaClinicaAsync(tipoDocumento, nroDocumento, nroHC, nroTurno, null));
        }

        // POST: /Odontologo/CorregirObservaciones -> alt 3.a.1: corrige el campo de observaciones de la
        // atención seleccionada; el sistema registra el cambio y lo confirma.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CorregirObservaciones(int nroHC, int nroTurno, string? observaciones, string? versionObservaciones)
        {
            CorreccionObservacionesResultDTO resultado;
            try
            {
                resultado = await _historiaClinicaService.CorregirObservacionesAsync(nroHC, nroTurno, observaciones, versionObservaciones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al corregir las observaciones de la atención del turno {NroTurno} (HC {NroHC}).", nroTurno, nroHC);
                TempData["MensajeError"] = "No pudimos guardar la corrección por un problema del sistema. Intentá nuevamente en unos minutos.";
                return RedirectToAction("GestionAtenciones", new { nroHC = nroHC.ToString(), nroTurno });
            }

            switch (resultado.Resultado)
            {
                case ResultadoCorreccionObservaciones.Corregida:
                    TempData["MensajeExito"] = resultado.Mensaje;
                    return RedirectToAction("GestionAtenciones", new { nroHC = nroHC.ToString(), nroTurno });

                case ResultadoCorreccionObservaciones.ObservacionesInvalidas:
                    // Se vuelve a mostrar la atención con el texto que el odontólogo había escrito, para que lo
                    // corrija sin perderlo.
                    TempData["MensajeError"] = resultado.Mensaje;
                    await CargarDatosOdontologoViewDataAsync();
                    return View("GestionAtenciones",
                        await ArmarGestionHistoriaClinicaAsync(null, null, nroHC.ToString(), nroTurno, observaciones));

                case ResultadoCorreccionObservaciones.ConflictoConcurrencia:
                {
                    // Otro odontólogo corrigió la misma atención mientras se editaba: no se guardó nada. Se
                    // muestra el texto que había escrito el odontólogo junto al vigente, con la versión
                    // vigente en el formulario, para que decida qué texto queda.
                    TempData["MensajeError"] = resultado.Mensaje;
                    await CargarDatosOdontologoViewDataAsync();
                    var modelo = await ArmarGestionHistoriaClinicaAsync(null, null, nroHC.ToString(), nroTurno, observaciones);
                    modelo.ObservacionesVigentesEnConflicto = resultado.ObservacionesVigentes;
                    return View("GestionAtenciones", modelo);
                }

                default:
                    TempData["MensajeError"] = resultado.Mensaje;
                    return RedirectToAction("GestionAtenciones", new { nroHC = nroHC.ToString() });
            }
        }

        private async Task<GestionHistoriaClinicaViewModel> ArmarGestionHistoriaClinicaAsync(
            string? tipoDocumento, string? nroDocumento, string? nroHC, int? nroTurno, string? observacionesEnEdicion)
        {
            var modelo = new GestionHistoriaClinicaViewModel
            {
                TipoDocumento = string.IsNullOrWhiteSpace(tipoDocumento) ? "DNI" : tipoDocumento.Trim().ToUpperInvariant(),
                NroDocumento = (nroDocumento ?? string.Empty).Trim(),
                NroHCBuscada = (nroHC ?? string.Empty).Trim()
            };

            int? nroHCBuscado = null;
            if (modelo.NroHCBuscada.Length > 0)
            {
                if (!int.TryParse(modelo.NroHCBuscada, out var numero) || numero <= 0)
                {
                    modelo.Busco = true;
                    modelo.MensajeBusqueda = "El número de historia clínica debe ser un número entero positivo.";
                    return modelo;
                }

                nroHCBuscado = numero;
            }

            if (!nroHCBuscado.HasValue && modelo.NroDocumento.Length == 0)
            {
                return modelo;
            }

            // Paso 1: busca al paciente (el N° de historia clínica, si viene, prevalece sobre el documento).
            modelo.Busco = true;
            modelo.Historia = await _historiaClinicaService.BuscarHistoriaClinicaAsync(modelo.TipoDocumento, modelo.NroDocumento, nroHCBuscado);

            if (modelo.Historia.Resultado == ResultadoBusquedaHistoriaClinica.Encontrada)
            {
                // El formulario de búsqueda por documento queda con el paciente encontrado.
                modelo.TipoDocumento = modelo.Historia.TipoDocumento;
                modelo.NroDocumento = modelo.Historia.NroDocumento;

                // Paso 3: la atención seleccionada.
                if (nroTurno.HasValue)
                {
                    modelo.NroTurnoSolicitado = nroTurno;
                    modelo.Detalle = await _historiaClinicaService.ObtenerDetalleAtencionAsync(modelo.Historia.NroHC!.Value, nroTurno.Value);
                    modelo.ObservacionesEnEdicion = observacionesEnEdicion ?? modelo.Detalle?.Observaciones;
                    modelo.VersionObservaciones = modelo.Detalle?.VersionObservaciones;
                }
            }

            return modelo;
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
    }
}
