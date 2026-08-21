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
                .Include(t => t.Atencion);
        }

        public async Task<IEnumerable<Turno>> GetAllAsync()
        {
            return await IncludeAll()
                .OrderBy(t => t.FechaYHoraReserva)
                .ToListAsync();
        }

        public async Task<Turno?> GetAsync(int codTurno)
        {
            return await IncludeAll()
                .FirstOrDefaultAsync(t => t.CodTurno == codTurno);
        }

        public async Task<Turno> AddAsync(Turno turno)
        {
            _context.Turnos.Add(turno);
            await _context.SaveChangesAsync();
            return turno;
        }

        public async Task<bool> UpdateAsync(Turno turno)
        {
            var existing = await _context.Turnos.FindAsync(turno.CodTurno);
            if (existing == null)
                return false;

            _context.Entry(existing).CurrentValues.SetValues(turno);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int codTurno)
        {
            var turno = await _context.Turnos.FindAsync(codTurno);
            if (turno == null)
                return false;

            _context.Turnos.Remove(turno);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TurnoExistsAsync(DateTime fechaReserva, string odontologoTipoDoc, int odontologoNroDoc)
        {
            return await _context.Turnos.AnyAsync(t =>
                t.FechaYHoraReserva == fechaReserva &&
                t.OdontologoTipoDoc == odontologoTipoDoc &&
                t.OdontologoNroDoc == odontologoNroDoc &&
                t.Estado != "CANCELADO");
        }

        public async Task<IEnumerable<Turno>> GetByFechaAsync(DateTime fecha)
        {
            return await IncludeAll()
                .Where(t => t.FechaYHoraReserva.Date == fecha.Date)
                .OrderBy(t => t.FechaYHoraReserva)
                .ToListAsync();
        }

        public async Task<IEnumerable<Turno>> GetByOdontologoAsync(string tipoDoc, int nroDoc)
        {
            return await IncludeAll()
                .Where(t => t.OdontologoTipoDoc == tipoDoc && t.OdontologoNroDoc == nroDoc)
                .OrderBy(t => t.FechaYHoraReserva)
                .ToListAsync();
        }

        public async Task<IEnumerable<Turno>> GetByPacienteAsync(string tipoDoc, int nroDoc)
        {
            return await IncludeAll()
                .Where(t => t.PacienteTipoDoc == tipoDoc && t.PacienteNroDoc == nroDoc)
                .OrderByDescending(t => t.FechaYHoraReserva)
                .ToListAsync();
        }
    }
}
