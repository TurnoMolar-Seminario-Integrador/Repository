using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public interface IComprobanteTurnoRepository
    {
        Task<ComprobanteDeTurno> AddAsync(ComprobanteDeTurno comprobante);
        Task<ComprobanteDeTurno?> GetByTurnoAsync(int nroTurno);
    }

    public class ComprobanteTurnoRepository : IComprobanteTurnoRepository
    {
        private readonly TurnoMolarDbContext _context;

        public ComprobanteTurnoRepository(TurnoMolarDbContext context)
        {
            _context = context;
        }

        public async Task<ComprobanteDeTurno> AddAsync(ComprobanteDeTurno comprobante)
        {
            _context.ComprobantesTurnos.Add(comprobante);
            await _context.SaveChangesAsync();
            return comprobante;
        }

        public async Task<ComprobanteDeTurno?> GetByTurnoAsync(int nroTurno)
        {
            return await _context.ComprobantesTurnos
                .FirstOrDefaultAsync(c => c.NroTurno == nroTurno);
        }
    }
}