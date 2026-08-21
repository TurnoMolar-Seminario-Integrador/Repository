using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public interface IInsumoRepository
    {
        Task<Insumo?> GetAsync(int codInsumo);
        Task<IEnumerable<Insumo>> GetAllAsync();
        Task<Insumo> AddAsync(Insumo insumo);
        Task<bool> UpdateAsync(Insumo insumo);
        Task<bool> DeleteAsync(int codInsumo);
        Task<bool> DescontarStockAsync(int codInsumo, int cantidad);
    }

    public class InsumoRepository : IInsumoRepository
    {
        private readonly TurnoMolarDbContext _context;

        public InsumoRepository(TurnoMolarDbContext context)
        {
            _context = context;
        }

        public async Task<Insumo?> GetAsync(int codInsumo)
        {
            return await _context.Insumos.FindAsync(codInsumo);
        }

        public async Task<IEnumerable<Insumo>> GetAllAsync()
        {
            return await _context.Insumos.OrderBy(i => i.Nombre).ToListAsync();
        }

        public async Task<Insumo> AddAsync(Insumo insumo)
        {
            _context.Insumos.Add(insumo);
            await _context.SaveChangesAsync();
            return insumo;
        }

        public async Task<bool> UpdateAsync(Insumo insumo)
        {
            var existing = await _context.Insumos.FindAsync(insumo.CodInsumo);
            if (existing == null)
                return false;

            existing.SetNombre(insumo.Nombre);
            existing.SetCostoUnitario(insumo.CostoUnitario);
            existing.SetStockDisponible(insumo.StockDisponible);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int codInsumo)
        {
            var insumo = await _context.Insumos.FindAsync(codInsumo);
            if (insumo == null)
                return false;

            _context.Insumos.Remove(insumo);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DescontarStockAsync(int codInsumo, int cantidad)
        {
            var insumo = await _context.Insumos.FindAsync(codInsumo);
            if (insumo == null || insumo.StockDisponible < cantidad)
                return false;

            insumo.DescontarStock(cantidad);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
