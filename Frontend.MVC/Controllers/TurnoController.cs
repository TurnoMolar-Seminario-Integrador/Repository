using System.Globalization;
using Data;
using Domain.Model;
using Frontend.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Frontend.MVC.Controllers
{
    [Authorize(Roles = "Paciente,Admin")]
    public class TurnoController : Controller
    {
        private readonly TurnoMolarDbContext _context;
        private readonly ILogger<TurnoController> _logger;

        public TurnoController(TurnoMolarDbContext context, ILogger<TurnoController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private async Task<Paciente> ObtenerPacienteActualAsync()
        {
            var tipoDocumento = User.FindFirst("TipoDocumento")?.Value;
            var nroDocumento = User.FindFirst("NroDocumento")?.Value;

            Paciente? paciente = null;
            if (!string.IsNullOrEmpty(tipoDocumento) && !string.IsNullOrEmpty(nroDocumento))
            {
                paciente = await _context.Pacientes
                    .Include(p => p.ObraSocial)
                    .Include(p => p.HistoriaClinica)
                    .FirstOrDefaultAsync(p => p.TipoDocumento == tipoDocumento && p.NroDocumento == nroDocumento);
            }

            if (paciente == null)
            {
                paciente = await _context.Pacientes
                    .Include(p => p.ObraSocial)
                    .Include(p => p.HistoriaClinica)
                    .FirstOrDefaultAsync();
            }

            if (paciente == null)
            {
                paciente = new Paciente(
                    "DNI",
                    "34567890",
                    "Manuel",
                    "Fernández",
                    new DateTime(1989, 4, 15),
                    "341-3334455",
                    "manuel.fer@email.com",
                    "Córdoba 1540, Rosario",
                    "HABILITADO",
                    "OSDE",
                    0m,
                    "paciente123",
                    "",
                    DateTime.Now,
                    "Paciente"
                );
                _context.Pacientes.Add(paciente);
                await _context.SaveChangesAsync();
            }

            ViewData["NombrePaciente"] = $"{paciente.Nombre} {paciente.Apellido}";
            ViewData["IdPaciente"] = paciente.NroDocumento;
            ViewData["EstadoPaciente"] = paciente.EstadoPaciente;

            return paciente;
        }

        // =========================================================================
        // ALTA DE TURNO (RESERVAR)
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> Reservar(int? odontologoId, int? especialidadId)
        {
            var paciente = await ObtenerPacienteActualAsync();

            var estaInhabilitado = paciente.EstadoPaciente == "INHABILITADO" || (paciente.MontoAdeudado.HasValue && paciente.MontoAdeudado.Value > 0);

            var odontologos = await _context.Odontologos
                .Include(o => o.DisponibilidadesHorarias)
                    .ThenInclude(d => d.Especialidad)
                .ToListAsync();

            var especialidades = await _context.Especialidades.ToListAsync();

            var docIdStr = odontologoId?.ToString();
            var selectedDoc = odontologos.FirstOrDefault(o => o.NroDocumento == docIdStr) ?? odontologos.FirstOrDefault();
            var selectedEsp = especialidades.FirstOrDefault(e => e.IdEspecialidad == (especialidadId ?? selectedDoc?.CodEspecialidad)) ?? especialidades.FirstOrDefault();

            var modelo = new ReservaTurnoViewModel
            {
                OdontologoId = selectedDoc?.NroDocumentoInt ?? 28456789,
                NombreOdontologo = selectedDoc != null ? $"{selectedDoc.Nombre} {selectedDoc.Apellido}" : "Dra. Elena Silva",
                Especialidad = selectedEsp?.Nombre ?? "Odontología General",
                FechaSeleccionada = DateTime.Today.AddDays(2).ToString("yyyy-MM-dd"),
                HorarioSeleccionado = "09:30",
                MetodoPago = paciente.ObraSocial != null ? "ObraSocial" : "Particular",
                ObraSocialNombre = paciente.ObraSocial != null ? $"{paciente.ObraSocial.NombreOS} ({paciente.ObraSocial.PlanCobertura})" : "Particular / Sin Obra Social",
                ArancelConsulta = selectedEsp?.ArancelParticular ?? 15000m,
                CopagoAPagar = paciente.ObraSocial != null ? paciente.ObraSocial.ArancelOS : (selectedEsp?.ArancelParticular ?? 15000m),
                EstaInhabilitado = estaInhabilitado
            };

            ViewBag.Odontologos = odontologos;
            ViewBag.Especialidades = especialidades;

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reservar(ReservaTurnoViewModel modelo)
        {
            var paciente = await ObtenerPacienteActualAsync();

            if (paciente.EstadoPaciente == "INHABILITADO" || (paciente.MontoAdeudado.HasValue && paciente.MontoAdeudado.Value > 0))
            {
                TempData["MensajeError"] = "Tu cuenta se encuentra inhabilitada por registrar saldo pendiente. Regularizá tu deuda en Métodos de Pago.";
                return RedirectToAction("Reservar");
            }

            // Parsear Fecha y Hora seleccionadas
            DateTime fechaHoraTurno = DateTime.Today.AddDays(2).AddHours(9).AddMinutes(30);
            if (!string.IsNullOrEmpty(modelo.FechaSeleccionada))
            {
                if (DateTime.TryParse(modelo.FechaSeleccionada, out DateTime parsedDate))
                {
                    fechaHoraTurno = parsedDate;
                    if (!string.IsNullOrEmpty(modelo.HorarioSeleccionado))
                    {
                        var timeParts = modelo.HorarioSeleccionado.Split(':', ' ');
                        if (timeParts.Length >= 2 && int.TryParse(timeParts[0], out int h) && int.TryParse(timeParts[1], out int m))
                        {
                            fechaHoraTurno = parsedDate.Date.AddHours(h).AddMinutes(m);
                        }
                    }
                }
            }

            var docDniStr = (modelo.OdontologoId ?? 28456789).ToString();
            var odontologo = await _context.Odontologos.FirstOrDefaultAsync(o => o.NroDocumento == docDniStr);
            var especialidadId = odontologo?.CodEspecialidad ?? 1;

            var nuevoTurno = new Turno(
                0,
                fechaHoraTurno,
                modelo.MetodoPago?.ToUpper() == "PARTICULAR" ? "PARTICULAR" : "OBRA_SOCIAL",
                especialidadId,
                "DNI",
                docDniStr,
                paciente.TipoDocumento,
                paciente.NroDocumento,
                "CONFIRMADO"
            );

            _context.Turnos.Add(nuevoTurno);
            await _context.SaveChangesAsync();

            // Emisión de Comprobante de Turno Oficial con persistencia
            var comprobante = new ComprobanteDeTurno(
                0,
                nuevoTurno.NroTurno,
                DateTime.Now
            );
            _context.ComprobantesTurnos.Add(comprobante);
            await _context.SaveChangesAsync();

            TempData["MensajeExito"] = "¡Turno agendado exitosamente!";
            return RedirectToAction("Comprobante", new { idTurno = nuevoTurno.NroTurno });
        }

        // =========================================================================
        // COMPROBANTE DE TURNO
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> Comprobante(int idTurno)
        {
            await ObtenerPacienteActualAsync();

            var turno = await _context.Turnos
                .Include(t => t.Odontologo)
                .Include(t => t.Especialidad)
                .Include(t => t.Paciente)
                    .ThenInclude(p => p.ObraSocial)
                .Include(t => t.Comprobante)
                .FirstOrDefaultAsync(t => t.NroTurno == idTurno);

            if (turno == null)
            {
                TempData["MensajeError"] = "No se encontró el turno solicitado.";
                return RedirectToAction("MisTurnos", "Home");
            }

            ViewData["IdTurno"] = turno.NroTurno;
            ViewData["FechaTurno"] = turno.FechaHoraTurno.ToString("dd 'de' MMMM, yyyy • HH:mm 'hs'", new CultureInfo("es-ES"));
            ViewData["Doctor"] = $"{turno.Odontologo.Nombre} {turno.Odontologo.Apellido} (MP {turno.Odontologo.Matricula})";
            ViewData["Especialidad"] = turno.Especialidad.Nombre;
            ViewData["ModalidadPago"] = turno.ModalidadPagoElegida == "OBRA_SOCIAL" ? "Obra Social / Prepaga" : "Particular";
            ViewData["EstadoTurno"] = turno.EstadoTurno;
            ViewData["FechaEmision"] = turno.Comprobante?.FechaHoraEmision.ToString("dd/MM/yyyy HH:mm") ?? DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            return View(turno);
        }

        // =========================================================================
        // CANCELAR TURNO
        // =========================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int idTurno, string motivoCancelacion)
        {
            var paciente = await ObtenerPacienteActualAsync();

            var turno = await _context.Turnos.FirstOrDefaultAsync(t => t.NroTurno == idTurno && t.NroDocumentoPaciente == paciente.NroDocumento);
            if (turno == null)
            {
                TempData["MensajeError"] = "No se encontró el turno a cancelar.";
                return RedirectToAction("MisTurnos", "Home");
            }

            // Regla de negocio: Si cancela con menos de 24 hs de anticipación, se aplica penalización
            var horasRestantes = (turno.FechaHoraTurno - DateTime.Now).TotalHours;
            decimal? penalizacion = null;

            if (horasRestantes < 24 && horasRestantes > 0)
            {
                penalizacion = 5000m;
                var montoActual = paciente.MontoAdeudado ?? 0m;
                paciente.SetMontoAdeudado(montoActual + penalizacion.Value);
                paciente.SetEstadoPaciente("INHABILITADO");
            }

            turno.Cancelar(motivoCancelacion ?? "Cancelado por el paciente", penalizacion);

            await _context.SaveChangesAsync();

            if (penalizacion.HasValue)
            {
                TempData["MensajeAdvertencia"] = $"El turno fue cancelado con menos de 24 hs de anticipación. Se registró un cargo por penalización de ${penalizacion.Value:N0} en tu cuenta.";
            }
            else
            {
                TempData["MensajeExito"] = "El turno fue cancelado correctamente sin penalizaciones.";
            }

            return RedirectToAction("MisTurnos", "Home");
        }

        // =========================================================================
        // REPROGRAMAR TURNO
        // =========================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reprogramar(int idTurnoOriginal, string nuevaFecha, string nuevoHorario)
        {
            var paciente = await ObtenerPacienteActualAsync();

            var turnoOriginal = await _context.Turnos.FirstOrDefaultAsync(t => t.NroTurno == idTurnoOriginal && t.NroDocumentoPaciente == paciente.NroDocumento);
            if (turnoOriginal == null)
            {
                TempData["MensajeError"] = "No se encontró el turno original para reprogramar.";
                return RedirectToAction("MisTurnos", "Home");
            }

            DateTime nuevaFechaHora = turnoOriginal.FechaHoraTurno.AddDays(7);
            if (!string.IsNullOrEmpty(nuevaFecha) && DateTime.TryParse(nuevaFecha, out DateTime parsedDate))
            {
                nuevaFechaHora = parsedDate;
                if (!string.IsNullOrEmpty(nuevoHorario))
                {
                    var parts = nuevoHorario.Split(':', ' ');
                    if (parts.Length >= 2 && int.TryParse(parts[0], out int h) && int.TryParse(parts[1], out int m))
                    {
                        nuevaFechaHora = parsedDate.Date.AddHours(h).AddMinutes(m);
                    }
                }
            }

            // Marcar turno original como REPROGRAMADO
            turnoOriginal.Reprogramar(nuevaFechaHora);

            // Crear nuevo turno vinculado al original
            var nuevoTurno = new Turno(
                0,
                nuevaFechaHora,
                turnoOriginal.ModalidadPagoElegida,
                turnoOriginal.IdEspecialidad,
                turnoOriginal.TipoDocumentoOdontologo,
                turnoOriginal.NroDocumentoOdontologo,
                turnoOriginal.TipoDocumentoPaciente,
                turnoOriginal.NroDocumentoPaciente,
                "CONFIRMADO",
                null,
                null,
                turnoOriginal.NroTurno
            );

            _context.Turnos.Add(nuevoTurno);
            await _context.SaveChangesAsync();

            // Emitir comprobante para el nuevo turno
            var comprobante = new ComprobanteDeTurno(
                0,
                nuevoTurno.NroTurno,
                DateTime.Now
            );
            _context.ComprobantesTurnos.Add(comprobante);
            await _context.SaveChangesAsync();

            TempData["MensajeExito"] = $"¡Turno reprogramado exitosamente para el {nuevaFechaHora:dd/MM/yyyy HH:mm} hs!";
            return RedirectToAction("Comprobante", new { idTurno = nuevoTurno.NroTurno });
        }

        // =========================================================================
        // VALORAR ATENCIÓN (CALIFICACIÓN Y RESEÑA)
        // =========================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValorarAtencion(int idTurno, int calificacion, string? comentarios)
        {
            var paciente = await ObtenerPacienteActualAsync();

            var turno = await _context.Turnos
                .Include(t => t.Atencion)
                .FirstOrDefaultAsync(t => t.NroTurno == idTurno && t.NroDocumentoPaciente == paciente.NroDocumento);

            if (turno == null)
            {
                TempData["MensajeError"] = "No se encontró el turno para calificar.";
                return RedirectToAction("MisTurnos", "Home");
            }

            // Si aún no tiene atención registrada, creamos la atención completada
            if (turno.Atencion == null)
            {
                var hc = await _context.HistoriasClinicas.FirstOrDefaultAsync(h => h.NroDocumentoPaciente == paciente.NroDocumento);
                var nroHc = hc?.NroHC ?? 1;

                var atencion = new AtencionOdontologica(
                    0,
                    turno.FechaHoraTurno,
                    turno.FechaHoraTurno.AddMinutes(30),
                    "Control odontológico realizado.",
                    15000m,
                    turno.NroTurno,
                    nroHc
                );
                _context.AtencionesOdontologicas.Add(atencion);
                await _context.SaveChangesAsync();

                turno.SetEstado("ATENDIDO");
            }

            var atencionId = turno.Atencion != null ? turno.Atencion.IdAtencion : (await _context.AtencionesOdontologicas.FirstAsync(a => a.NroTurno == turno.NroTurno)).IdAtencion;
            var valoracionExistente = await _context.Valoraciones.FirstOrDefaultAsync(v => v.IdAtencion == atencionId);

            if (valoracionExistente == null)
            {
                var valoracion = new Valoracion(0, calificacion, comentarios, atencionId);
                _context.Valoraciones.Add(valoracion);
            }
            else
            {
                valoracionExistente.SetCalificacion(calificacion);
                valoracionExistente.Observaciones = comentarios;
            }

            await _context.SaveChangesAsync();

            TempData["MensajeExito"] = "¡Muchas gracias por tu valoración! Tu opinión nos ayuda a brindar un mejor servicio.";
            return RedirectToAction("MisTurnos", "Home");
        }
    }
}