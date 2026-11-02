using Data;
using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    // =========================================================================
    // Cierre de jornada: turnos "Reservado" vencidos -> "Ausente" con penalización.
    // Decisión del usuario (ambigüedad A3 del análisis): si al terminar el día el responsable no
    // registró la asistencia (CUU02), el turno se trata como una ausencia (CUU02 alt 1.b).
    // ME - Turno: RESERVADO -> AUSENTE. ME - Paciente: HABILITADO -> INHABILITADO (RN9).
    // Penalización (RN11): arancel del convenio si la modalidad es Obra Social; si no, el arancel
    // particular de la especialidad. Mismo criterio que AsistenciaTurnoService.RegistrarAusenteAsync.
    // =========================================================================
    public interface IVencimientoTurnosService
    {
        // Procesa los turnos RESERVADO con fecha anterior a "hoy" y devuelve cuántos pasó a AUSENTE.
        Task<int> ProcesarTurnosVencidosAsync(DateTime hoy);
    }

    public class VencimientoTurnosService : IVencimientoTurnosService
    {
        private readonly TurnoMolarDbContext _contexto;

        public VencimientoTurnosService(TurnoMolarDbContext contexto) => _contexto = contexto;

        public async Task<int> ProcesarTurnosVencidosAsync(DateTime hoy)
        {
            var limite = hoy.Date;
            var vencidos = await _contexto.Turnos
                .Include(t => t.Especialidad)
                .Include(t => t.Paciente)
                .Where(t => t.EstadoTurno == "RESERVADO" && t.FechaHoraTurno < limite)
                .OrderBy(t => t.FechaHoraTurno)
                .ToListAsync();

            foreach (var turno in vencidos)
            {
                decimal monto = turno.Especialidad.ArancelParticular;
                if (turno.ModalidadPagoElegida == ModalidadesPago.ObraSocial && turno.Paciente.IdentificadorOS != null)
                {
                    var convenio = await _contexto.Convenios.AsNoTracking().FirstOrDefaultAsync(c =>
                        c.IdentificadorOS == turno.Paciente.IdentificadorOS && c.IdEspecialidad == turno.IdEspecialidad);
                    // Sin convenio (p. ej. se eliminó después de reservar) se usa el arancel particular.
                    if (convenio != null)
                        monto = convenio.ArancelConvenio;
                }

                turno.MarcarAusente(monto);
                // MD: /montoAdeudado = /montoPenalizacion del turno (asignación directa, no acumulada).
                turno.Paciente.SetMontoAdeudado(monto);
                turno.Paciente.SetEstadoPaciente("INHABILITADO");
            }

            await _contexto.SaveChangesAsync();
            return vencidos.Count;
        }
    }
}
