using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public class TurnoRepository : ITurnoRepository
    {
        private readonly TurnoMolarDbContext _context;

        public TurnoRepository(TurnoMolarDbContext context)
        {
            _context = context;
        }

        private IQueryable<Turno> IncludeAll()
        {
            return _context.Turnos
                .Include(t => t.Especialidad)
                .Include(t => t.Paciente)
                .Include(t => t.Odontologo)
                .Include(t => t.Comprobante)
                .Include(t => t.Atencion)
                .Include(t => t.Pago);
        }

        public async Task<IEnumerable<Turno>> GetAllAsync()
        {
            return await IncludeAll()
                .OrderBy(t => t.FechaHoraTurno)
                .ToListAsync();
        }

        public async Task<Turno?> GetAsync(int nroTurno)
        {
            return await IncludeAll()
                .FirstOrDefaultAsync(t => t.NroTurno == nroTurno);
        }

        public async Task<Turno> AddAsync(Turno turno)
        {
            _context.Turnos.Add(turno);
            await _context.SaveChangesAsync();
            return turno;
        }

        public async Task<bool> UpdateAsync(Turno turno)
        {
            var existing = await _context.Turnos.FindAsync(turno.NroTurno);
            if (existing == null)
                return false;

            _context.Entry(existing).CurrentValues.SetValues(turno);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int nroTurno)
        {
            var turno = await _context.Turnos.FindAsync(nroTurno);
            if (turno == null)
                return false;

            _context.Turnos.Remove(turno);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TurnoExistsAsync(DateTime fechaReserva, string odontologoTipoDoc, string odontologoNroDoc)
        {
            return await _context.Turnos.AnyAsync(t =>
                t.FechaHoraTurno == fechaReserva &&
                t.TipoDocumentoOdontologo == odontologoTipoDoc &&
                t.NroDocumentoOdontologo == odontologoNroDoc &&
                t.EstadoTurno != "CANCELADO");
        }

        public async Task<bool> TurnoExistsAsync(DateTime fechaReserva, string odontologoTipoDoc, int odontologoNroDoc)
        {
            return await TurnoExistsAsync(fechaReserva, odontologoTipoDoc, odontologoNroDoc.ToString());
        }

        public async Task<IEnumerable<Turno>> GetByFechaAsync(DateTime fecha)
        {
            return await IncludeAll()
                .Where(t => t.FechaHoraTurno.Date == fecha.Date)
                .OrderBy(t => t.FechaHoraTurno)
                .ToListAsync();
        }

        public async Task<IEnumerable<Turno>> GetByOdontologoAsync(string tipoDoc, string nroDoc)
        {
            return await IncludeAll()
                .Where(t => t.TipoDocumentoOdontologo == tipoDoc && t.NroDocumentoOdontologo == nroDoc)
                .OrderBy(t => t.FechaHoraTurno)
                .ToListAsync();
        }

        public async Task<IEnumerable<Turno>> GetByOdontologoAsync(string tipoDoc, int nroDoc)
        {
            return await GetByOdontologoAsync(tipoDoc, nroDoc.ToString());
        }

        public async Task<IEnumerable<Turno>> GetByPacienteAsync(string tipoDoc, string nroDoc)
        {
            return await IncludeAll()
                .Where(t => t.TipoDocumentoPaciente == tipoDoc && t.NroDocumentoPaciente == nroDoc)
                .OrderByDescending(t => t.FechaHoraTurno)
                .ToListAsync();
        }

        public async Task<IEnumerable<Turno>> GetByPacienteAsync(string tipoDoc, int nroDoc)
        {
            return await GetByPacienteAsync(tipoDoc, nroDoc.ToString());
        }
    }
}
