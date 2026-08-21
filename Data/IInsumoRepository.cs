using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public interface IInsumoRepository
    {
        Task<Insumo?> GetAsync(int id);
        Task<IEnumerable<Insumo>> GetAllAsync();
        Task<Insumo> AddAsync(Insumo insumo);
        Task<bool> UpdateAsync(Insumo insumo);
        Task<bool> DeleteAsync(int id);
        Task<bool> DescontarStockAsync(int insumoId, int cantidad);
    }

    public class InsumoRepository : IInsumoRepository
    {
        private readonly TurnoMolarDbContext _context;

        public InsumoRepository(TurnoMolarDbContext context)
        {
            _context = context;
        }

        public async Task<Insumo?> GetAsync(int id)
        {
            return await _context.Insumos.FindAsync(id);
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
            var existing = await _context.Insumos.FindAsync(insumo.Id);
            if (existing == null)
                return false;

            existing.Nombre = insumo.Nombre;
            existing.Descripcion = insumo.Descripcion;
            existing.Precio = insumo.Precio;
            existing.Stock = insumo.Stock;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var insumo = await _context.Insumos.FindAsync(id);
            if (insumo == null)
                return false;

            _context.Insumos.Remove(insumo);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DescontarStockAsync(int insumoId, int cantidad)
        {
            var insumo = await _context.Insumos.FindAsync(insumoId);
            if (insumo == null || insumo.Stock < cantidad)
                return false;

            insumo.Stock -= cantidad;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
