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
            var idString = User.FindFirst("EntidadId")?.Value;
            int.TryParse(idString, out int dni);

            Paciente? paciente = null;
            if (dni > 0)
            {
                paciente = await _context.Pacientes
                    .Include(p => p.ObraSocial)
                    .Include(p => p.HistoriaClinica)
                    .FirstOrDefaultAsync(p => p.NroDocumento == dni);
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
                    34567890,
                    "Manuel",
                    "Fernández",
                    new DateTime(1989, 4, 15),
                    "341-3334455",
                    "manuel.fer@email.com",
                    "Córdoba 1540, Rosario",
                    "HABILITADO",
                    0m,
                    1
                );
                _context.Pacientes.Add(paciente);
                await _context.SaveChangesAsync();
            }

            ViewData["NombrePaciente"] = $"{paciente.Nombre} {paciente.Apellido}";
            ViewData["IdPaciente"] = paciente.NroDocumento.ToString();
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
                .Include(o => o.Especialidad)
                .ToListAsync();

            var especialidades = await _context.Especialidades.ToListAsync();

            var selectedDoc = odontologos.FirstOrDefault(o => o.NroDocumento == odontologoId) ?? odontologos.FirstOrDefault();
            var selectedEsp = especialidades.FirstOrDefault(e => e.CodEspecialidad == (especialidadId ?? selectedDoc?.CodEspecialidad)) ?? especialidades.FirstOrDefault();

            var modelo = new ReservaTurnoViewModel
            {
                OdontologoId = selectedDoc?.NroDocumento ?? 28456789,
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

            var docDni = modelo.OdontologoId ?? 28456789;
            var odontologo = await _context.Odontologos.FirstOrDefaultAsync(o => o.NroDocumento == docDni);
            var especialidadId = odontologo?.CodEspecialidad ?? 1;

            var nuevoTurno = new Turno(
                0,
                fechaHoraTurno,
                modelo.MetodoPago?.ToUpper() == "PARTICULAR" ? "PARTICULAR" : "OBRA_SOCIAL",
                especialidadId,
                "DNI",
                docDni,
                paciente.TipoDocumento,
                paciente.NroDocumento,
                "CONFIRMADO"
            );

            _context.Turnos.Add(nuevoTurno);
            await _context.SaveChangesAsync();

            // Emisión de Comprobante de Turno Oficial con persistencia
            var comprobante = new ComprobanteDeTurno(
                0,
                nuevoTurno.CodTurno,
                nuevoTurno.FechaYHoraReserva,
                DateTime.Now
            );
            _context.Comprobantes.Add(comprobante);
            await _context.SaveChangesAsync();

            TempData["MensajeExito"] = "¡Turno agendado exitosamente!";
            return RedirectToAction("Comprobante", new { idTurno = nuevoTurno.CodTurno });
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
                .FirstOrDefaultAsync(t => t.CodTurno == idTurno);

            if (turno == null)
            {
                TempData["MensajeError"] = "No se encontró el turno solicitado.";
                return RedirectToAction("MisTurnos", "Home");
            }

            ViewData["IdTurno"] = turno.CodTurno;
            ViewData["FechaTurno"] = turno.FechaYHoraReserva.ToString("dd 'de' MMMM, yyyy • HH:mm 'hs'", new CultureInfo("es-ES"));
            ViewData["Doctor"] = $"{turno.Odontologo.Nombre} {turno.Odontologo.Apellido} (MP {turno.Odontologo.Matricula})";
            ViewData["Especialidad"] = turno.Especialidad.Nombre;
            ViewData["MetodoPago"] = turno.ModalidadPagoElegida == "OBRA_SOCIAL" && turno.Paciente.ObraSocial != null
                ? $"{turno.Paciente.ObraSocial.NombreOS} ({turno.Paciente.ObraSocial.PlanCobertura})"
                : "Particular (Pago en Clínica)";
            ViewData["CodigoReserva"] = $"TM-{turno.FechaYHoraReserva.Year}-{turno.CodTurno:D4}A";
            ViewData["NroComprobante"] = turno.Comprobante?.NroComprobante ?? turno.CodTurno;

            return View();
        }

        // =========================================================================
        // BAJA / CANCELACIÓN DE TURNO
        // =========================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int idTurno, string? motivo)
        {
            var turno = await _context.Turnos.FindAsync(idTurno);
            if (turno != null)
            {
                turno.Cancelar(motivo ?? "Cancelación solicitada por el paciente");
                await _context.SaveChangesAsync();
                TempData["MensajeExito"] = $"El turno #{idTurno} fue cancelado correctamente.";
            }
            else
            {
                TempData["MensajeError"] = "No se encontró el turno a cancelar.";
            }

            return RedirectToAction("MisTurnos", "Home");
        }

        // =========================================================================
        // MODIFICACIÓN / REPROGRAMACIÓN DE TURNO
        // =========================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reprogramar(int idTurnoOriginal, string nuevaFecha, string nuevoHorario)
        {
            var turnoOriginal = await _context.Turnos.FindAsync(idTurnoOriginal);
            if (turnoOriginal == null)
            {
                TempData["MensajeError"] = "No se encontró el turno original para reprogramar.";
                return RedirectToAction("MisTurnos", "Home");
            }

            DateTime nuevaFechaHora = DateTime.Today.AddDays(7).AddHours(10);
            if (!string.IsNullOrEmpty(nuevaFecha) && DateTime.TryParse(nuevaFecha, out DateTime pDate))
            {
                nuevaFechaHora = pDate;
                if (!string.IsNullOrEmpty(nuevoHorario))
                {
                    var parts = nuevoHorario.Split(':', ' ');
                    if (parts.Length >= 2 && int.TryParse(parts[0], out int h) && int.TryParse(parts[1], out int m))
                    {
                        nuevaFechaHora = pDate.Date.AddHours(h).AddMinutes(m);
                    }
                }
            }

            var nuevoTurno = new Turno(
                0,
                nuevaFechaHora,
                turnoOriginal.ModalidadPagoElegida,
                turnoOriginal.CodEspecialidad,
                turnoOriginal.OdontologoTipoDoc,
                turnoOriginal.OdontologoNroDoc,
                turnoOriginal.PacienteTipoDoc,
                turnoOriginal.PacienteNroDoc,
                "CONFIRMADO"
            );

            _context.Turnos.Add(nuevoTurno);
            await _context.SaveChangesAsync();

            turnoOriginal.Reprogramar(nuevaFechaHora, nuevoTurno.CodTurno);

            var comprobante = new ComprobanteDeTurno(0, nuevoTurno.CodTurno, nuevoTurno.FechaYHoraReserva, DateTime.Now);
            _context.Comprobantes.Add(comprobante);

            await _context.SaveChangesAsync();

            TempData["MensajeExito"] = $"¡Turno reprogramado con éxito para el {nuevaFechaHora:dd/MM/yyyy HH:mm} hs!";
            return RedirectToAction("MisTurnos", "Home");
        }

        // =========================================================================
        // VALORACIÓN / CALIFICACIÓN DE ATENCIÓN
        // =========================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Calificar(int idTurno, int calificacion, string? comentarios)
        {
            if (calificacion < 1 || calificacion > 5)
            {
                calificacion = 5;
            }

            var turno = await _context.Turnos
                .Include(t => t.Atencion)
                    .ThenInclude(a => a!.Valoracion)
                .Include(t => t.Paciente)
                    .ThenInclude(p => p.HistoriaClinica)
                .FirstOrDefaultAsync(t => t.CodTurno == idTurno);

            if (turno == null)
            {
                TempData["MensajeError"] = "No se encontró el turno a calificar.";
                return RedirectToAction("MisTurnos", "Home");
            }

            // Si el turno no tiene atención creada, la creamos para asociar la valoración
            if (turno.Atencion == null)
            {
                var hc = turno.Paciente?.HistoriaClinica ?? await _context.HistoriasClinicas.FirstOrDefaultAsync(h => h.PacienteNroDoc == turno.PacienteNroDoc);
                int nroHc = hc?.NroHC ?? 1;

                var atencion = new AtencionOdontologica(
                    0,
                    turno.FechaYHoraReserva,
                    turno.FechaYHoraReserva.AddMinutes(30),
                    "Control odontológico realizado.",
                    turno.CodTurno,
                    turno.FechaYHoraReserva,
                    nroHc,
                    "DNI",
                    turno.PacienteNroDoc,
                    15000m
                );
                _context.Atenciones.Add(atencion);
                await _context.SaveChangesAsync();

                turno.SetEstado("ATENDIDO");
            }

            var atencionId = turno.Atencion != null ? turno.Atencion.CodAtencion : (await _context.Atenciones.FirstAsync(a => a.CodTurno == turno.CodTurno)).CodAtencion;
            var valoracionExistente = await _context.Valoraciones.FirstOrDefaultAsync(v => v.CodAtencion == atencionId);

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