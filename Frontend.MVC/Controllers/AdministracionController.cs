using Application.Services;
using DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TurnoMolar.Controllers
{
    // Casos de uso de administración de la clínica (actor: Responsable de la clínica):
    // CUF04 Conformar cuerpo de odontólogos, CUF05 alt 1.b Consultar valoraciones de la clínica,
    // CUF06 Administrar convenios con obras sociales, CUF09 Abastecer insumos y materiales,
    // CUF11 Coordinar atención extraordinaria y CUF12 Administrar especialidad.
    // La lógica vive en los servicios; el controlador solo traduce el formulario y muestra el mensaje.
    [Authorize(Roles = "ResponsableClinica,Admin")]
    public class AdministracionController : Controller
    {
        private readonly IConformarCuerpoOdontologosService _odontologos;
        private readonly IConsultarValoracionesService _valoraciones;
        private readonly IAdministrarConveniosService _convenios;
        private readonly IAbastecerInsumosService _insumos;
        private readonly ICoordinarAtencionExtraordinariaService _extraordinaria;
        private readonly IAdministrarEspecialidadService _especialidades;

        public AdministracionController(
            IConformarCuerpoOdontologosService odontologos,
            IConsultarValoracionesService valoraciones,
            IAdministrarConveniosService convenios,
            IAbastecerInsumosService insumos,
            ICoordinarAtencionExtraordinariaService extraordinaria,
            IAdministrarEspecialidadService especialidades)
        {
            _odontologos = odontologos;
            _valoraciones = valoraciones;
            _convenios = convenios;
            _insumos = insumos;
            _extraordinaria = extraordinaria;
            _especialidades = especialidades;
        }

        private IActionResult Volver(ResultadoOperacionDTO r, string accion, object? ruta = null)
        {
            TempData[r.Exito ? "MensajeExito" : "MensajeError"] = r.Mensaje;
            return RedirectToAction(accion, ruta);
        }

        // ===================== CUF12 - Especialidades =====================

        [HttpGet]
        public async Task<IActionResult> Especialidades() => View(await _especialidades.ListarAsync());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarEspecialidad(string? nombre, decimal? arancelParticular) =>
            Volver(await _especialidades.RegistrarAsync(nombre, arancelParticular), nameof(Especialidades));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ModificarArancel(int idEspecialidad, decimal? arancelParticular) =>
            Volver(await _especialidades.ModificarArancelAsync(idEspecialidad, arancelParticular), nameof(Especialidades));

        // CUF12 E4.R02, alt 1.d / 1.e / 1.f.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeshabilitarEspecialidad(int idEspecialidad) =>
            Volver(await _especialidades.DeshabilitarAsync(idEspecialidad), nameof(Especialidades));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RehabilitarEspecialidad(int idEspecialidad) =>
            Volver(await _especialidades.RehabilitarAsync(idEspecialidad), nameof(Especialidades));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarEspecialidad(int idEspecialidad) =>
            Volver(await _especialidades.EliminarAsync(idEspecialidad), nameof(Especialidades));

        // ===================== CUF09 - Insumos =====================

        [HttpGet]
        public async Task<IActionResult> Insumos() => View(await _insumos.ListarAsync());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarInsumo(string? nombre, decimal? costoUnitario, int? stockDisponible) =>
            Volver(await _insumos.RegistrarAsync(nombre, costoUnitario, stockDisponible), nameof(Insumos));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ReponerStock(int idInsumo, int? cantidadIncorporada) =>
            Volver(await _insumos.ReponerStockAsync(idInsumo, cantidadIncorporada), nameof(Insumos));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ModificarCostoInsumo(int idInsumo, decimal? costoUnitario) =>
            Volver(await _insumos.ModificarCostoAsync(idInsumo, costoUnitario), nameof(Insumos));

        // CUF09 E4.R02, alt 3.c / 3.d / 3.e.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeshabilitarInsumo(int idInsumo) =>
            Volver(await _insumos.DeshabilitarAsync(idInsumo), nameof(Insumos));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RehabilitarInsumo(int idInsumo) =>
            Volver(await _insumos.RehabilitarAsync(idInsumo), nameof(Insumos));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarInsumo(int idInsumo) =>
            Volver(await _insumos.EliminarAsync(idInsumo), nameof(Insumos));

        // ===================== CUF06 - Obras sociales y convenios =====================

        [HttpGet]
        public async Task<IActionResult> ObrasSociales()
        {
            ViewBag.Especialidades = await _especialidades.ListarHabilitadasAsync();
            return View(await _convenios.ListarAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarObraSocial(string? identificadorOS, string? nombreOS, string? planCobertura,
            int[]? convenioEspecialidad, decimal?[]? convenioArancel)
        {
            var datos = new NuevaObraSocialDTO
            {
                IdentificadorOS = identificadorOS ?? string.Empty,
                NombreOS = nombreOS ?? string.Empty,
                PlanCobertura = planCobertura ?? string.Empty
            };
            convenioEspecialidad ??= Array.Empty<int>();
            convenioArancel ??= Array.Empty<decimal?>();
            for (var i = 0; i < convenioEspecialidad.Length && i < convenioArancel.Length; i++)
            {
                if (convenioEspecialidad[i] <= 0 && convenioArancel[i] == null) continue; // fila vacía
                datos.Convenios.Add(new ConvenioDTO { IdEspecialidad = convenioEspecialidad[i], ArancelConvenio = convenioArancel[i] ?? 0m });
            }
            return Volver(await _convenios.RegistrarAsync(datos), nameof(ObrasSociales));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ModificarPlan(string identificadorOS, string? planCobertura) =>
            Volver(await _convenios.ModificarPlanAsync(identificadorOS, planCobertura), nameof(ObrasSociales));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BajaObraSocial(string identificadorOS) =>
            Volver(await _convenios.DarDeBajaAsync(identificadorOS), nameof(ObrasSociales));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RehabilitarObraSocial(string identificadorOS) =>
            Volver(await _convenios.RehabilitarAsync(identificadorOS), nameof(ObrasSociales));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarConvenio(string identificadorOS, int idEspecialidad, decimal? arancelConvenio) =>
            Volver(await _convenios.GuardarConvenioAsync(identificadorOS, idEspecialidad, arancelConvenio), nameof(ObrasSociales));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarConvenio(string identificadorOS, int idEspecialidad) =>
            Volver(await _convenios.EliminarConvenioAsync(identificadorOS, idEspecialidad), nameof(ObrasSociales));

        // ===================== CUF04 - Cuerpo de odontólogos =====================

        [HttpGet]
        public async Task<IActionResult> Odontologos()
        {
            ViewBag.Especialidades = await _especialidades.ListarHabilitadasAsync();
            return View(await _odontologos.ListarAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarOdontologo(NuevoOdontologoDTO datos,
            int[]? franjaEspecialidad, string[]? franjaDia, string[]? franjaInicio, string[]? franjaFin)
        {
            datos.Especialidades = ArmarFranjas(franjaEspecialidad, franjaDia, franjaInicio, franjaFin)
                .GroupBy(x => x.idEspecialidad)
                .Select(g => new EspecialidadDisponibilidadDTO { IdEspecialidad = g.Key, Franjas = g.Select(x => x.franja).ToList() })
                .ToList();
            return Volver(await _odontologos.RegistrarAsync(datos), nameof(Odontologos));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AjustarDisponibilidad(string tipoDocumento, string nroDocumento, int idEspecialidad,
            string[]? franjaDia, string[]? franjaInicio, string[]? franjaFin)
        {
            var franjas = ArmarFranjas(null, franjaDia, franjaInicio, franjaFin).Select(x => x.franja).ToList();
            return Volver(await _odontologos.AjustarDisponibilidadAsync(tipoDocumento, nroDocumento, idEspecialidad, franjas), nameof(Odontologos));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarEspecialidad(string tipoDocumento, string nroDocumento, int idEspecialidad,
            string[]? franjaDia, string[]? franjaInicio, string[]? franjaFin)
        {
            var franjas = ArmarFranjas(null, franjaDia, franjaInicio, franjaFin).Select(x => x.franja).ToList();
            return Volver(await _odontologos.AgregarEspecialidadAsync(tipoDocumento, nroDocumento, idEspecialidad, franjas), nameof(Odontologos));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> QuitarEspecialidad(string tipoDocumento, string nroDocumento, int idEspecialidad) =>
            Volver(await _odontologos.QuitarEspecialidadAsync(tipoDocumento, nroDocumento, idEspecialidad), nameof(Odontologos));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarContactoOdontologo(string tipoDocumento, string nroDocumento, string? telefono, string? email, string? domicilio) =>
            Volver(await _odontologos.ActualizarContactoAsync(tipoDocumento, nroDocumento, telefono, email, domicilio), nameof(Odontologos));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> InhabilitarOdontologo(string tipoDocumento, string nroDocumento) =>
            Volver(await _odontologos.InhabilitarAsync(tipoDocumento, nroDocumento), nameof(Odontologos));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RehabilitarOdontologo(string tipoDocumento, string nroDocumento) =>
            Volver(await _odontologos.RehabilitarAsync(tipoDocumento, nroDocumento), nameof(Odontologos));

        // Filas del formulario -> franjas. Las filas vacías se ignoran; un horario ilegible queda en 00:00
        // y el servicio lo rechaza con su mensaje.
        private static List<(int idEspecialidad, FranjaHorariaDTO franja)> ArmarFranjas(
            int[]? especialidades, string[]? dias, string[]? inicios, string[]? fines)
        {
            var lista = new List<(int, FranjaHorariaDTO)>();
            dias ??= Array.Empty<string>();
            for (var i = 0; i < dias.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(dias[i])) continue;
                TimeOnly.TryParse(inicios?.ElementAtOrDefault(i), out var inicio);
                TimeOnly.TryParse(fines?.ElementAtOrDefault(i), out var fin);
                lista.Add((especialidades?.ElementAtOrDefault(i) ?? 0,
                    new FranjaHorariaDTO { DiaSemana = dias[i], HoraInicio = inicio, HoraFin = fin }));
            }
            return lista;
        }

        // ===================== CUF05 alt 1.b - Valoraciones de la clínica =====================

        [HttpGet]
        public async Task<IActionResult> Valoraciones(int? nroTurno)
        {
            if (nroTurno.HasValue)
                ViewBag.Detalle = await _valoraciones.ObtenerDetalleAsync(nroTurno.Value);
            return View(await _valoraciones.ConsultarClinicaAsync());
        }

        // ===================== CUF11 - Atención extraordinaria =====================

        [HttpGet]
        public async Task<IActionResult> AtencionExtraordinaria(string? tipoDocumento, string? nroDocumento)
        {
            ViewBag.Especialidades = await _especialidades.ListarHabilitadasAsync();
            ViewBag.Odontologos = (await _odontologos.ListarAsync()).Where(o => o.Habilitado).ToList();
            ViewBag.TipoDocumento = tipoDocumento ?? "DNI";
            ViewBag.NroDocumento = nroDocumento;
            if (!string.IsNullOrWhiteSpace(nroDocumento))
                ViewBag.Validacion = await _extraordinaria.ValidarPacienteAsync(tipoDocumento ?? "DNI", nroDocumento.Trim());
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarAtencionExtraordinaria(AtencionExtraordinariaRequestDTO solicitud, string? odontologo)
        {
            // El select envía "TIPO|NRO" del odontólogo.
            var partes = (odontologo ?? string.Empty).Split('|');
            if (partes.Length == 2)
            {
                solicitud.OdontologoTipoDocumento = partes[0];
                solicitud.OdontologoNroDocumento = partes[1];
            }
            var r = await _extraordinaria.RegistrarAsync(solicitud);
            if (!r.Exito)
            {
                TempData["MensajeError"] = r.Mensaje;
                return RedirectToAction(nameof(AtencionExtraordinaria),
                    new { tipoDocumento = solicitud.TipoDocumentoPaciente, nroDocumento = solicitud.NroDocumentoPaciente });
            }
            TempData["MensajeExito"] = r.Mensaje;
            return View("ComprobanteExtraordinario", r);
        }
    }
}
