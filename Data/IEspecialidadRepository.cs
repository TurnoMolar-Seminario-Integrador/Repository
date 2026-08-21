using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public interface IEspecialidadRepository
    {
        Task<Especialidad?> GetAsync(int codEspecialidad);
        Task<IEnumerable<Especialidad>> GetAllAsync();
        Task<Especialidad> AddAsync(Especialidad especialidad);
        Task<bool> UpdateAsync(Especialidad especialidad);
        Task<bool> DeleteAsync(int codEspecialidad);
    }

    public class EspecialidadRepository : IEspecialidadRepository
    {
        private readonly TurnoMolarDbContext _context;

        public EspecialidadRepository(TurnoMolarDbContext context)
        {
            _context = context;
        }

        public async Task<Especialidad?> GetAsync(int codEspecialidad)
        {
            return await _context.Especialidades.FindAsync(codEspecialidad);
        }

        public async Task<IEnumerable<Especialidad>> GetAllAsync()
        {
            return await _context.Especialidades.OrderBy(e => e.Nombre).ToListAsync();
        }

        public async Task<Especialidad> AddAsync(Especialidad especialidad)
        {
            _context.Especialidades.Add(especialidad);
            await _context.SaveChangesAsync();
            return especialidad;
        }

        public async Task<bool> UpdateAsync(Especialidad especialidad)
        {
            var existing = await _context.Especialidades.FindAsync(especialidad.CodEspecialidad);
            if (existing == null)
                return false;

            existing.SetNombre(especialidad.Nombre);
            existing.SetArancelParticular(especialidad.ArancelParticular);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int codEspecialidad)
        {
            var especialidad = await _context.Especialidades.FindAsync(codEspecialidad);
            if (especialidad == null)
                return false;

            _context.Especialidades.Remove(especialidad);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
