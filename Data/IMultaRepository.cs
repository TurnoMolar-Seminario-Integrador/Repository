using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public interface IMultaRepository
    {
        Task<Multa?> GetAsync(int id);
        Task<IEnumerable<Multa>> GetAllAsync();
        Task<IEnumerable<Multa>> GetByPacienteDocAsync(string tipoDoc, int nroDoc);
        Task<IEnumerable<Multa>> GetImpagasByPacienteDocAsync(string tipoDoc, int nroDoc);
        Task<Multa> AddAsync(Multa multa);
        Task<bool> PagarMultaAsync(int id);
    }

    public class MultaRepository : IMultaRepository
    {
        private readonly TurnoMolarDbContext _context;

        public MultaRepository(TurnoMolarDbContext context)
        {
            _context = context;
        }

        public async Task<Multa?> GetAsync(int id)
        {
            return await _context.Set<Multa>()
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<IEnumerable<Multa>> GetAllAsync()
        {
            return await _context.Set<Multa>()
                .OrderByDescending(m => m.FechaPago)
                .ToListAsync();
        }

        public async Task<IEnumerable<Multa>> GetByPacienteDocAsync(string tipoDoc, int nroDoc)
        {
            return await _context.Set<Multa>()
                .ToListAsync();
        }

        public async Task<IEnumerable<Multa>> GetImpagasByPacienteDocAsync(string tipoDoc, int nroDoc)
        {
            return await _context.Set<Multa>()
                .Where(m => !m.EstadoPago)
                .ToListAsync();
        }

        public async Task<Multa> AddAsync(Multa multa)
        {
            _context.Set<Multa>().Add(multa);
            await _context.SaveChangesAsync();
            return multa;
        }

        public async Task<bool> PagarMultaAsync(int id)
        {
            var multa = await _context.Set<Multa>().FindAsync(id);
            if (multa == null)
                return false;

            multa.MarcarComoPagada();
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
