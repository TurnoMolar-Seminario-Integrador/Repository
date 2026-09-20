using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public interface IPagoRepository
    {
        Task<Pago?> GetAsync(int idPago);
        Task<Pago?> GetByNroTurnoAsync(int nroTurno);
        Task<Pago> AddAsync(Pago pago);
    }

    public class PagoRepository : IPagoRepository
    {
        private readonly TurnoMolarDbContext _context;

        public PagoRepository(TurnoMolarDbContext context)
        {
            _context = context;
        }

        public async Task<Pago?> GetAsync(int idPago)
        {
            return await _context.Pagos
                .Include(p => p.ObraSocial)
                .FirstOrDefaultAsync(p => p.IdPago == idPago);
        }

        public async Task<Pago?> GetByNroTurnoAsync(int nroTurno)
        {
            return await _context.Pagos
                .Include(p => p.ObraSocial)
                .FirstOrDefaultAsync(p => p.NroTurno == nroTurno);
        }

        public async Task<Pago> AddAsync(Pago pago)
        {
            _context.Pagos.Add(pago);
            await _context.SaveChangesAsync();
            return pago;
        }
    }
}
