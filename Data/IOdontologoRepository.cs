using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public interface IOdontologoRepository
    {
        Task<Odontologo?> GetAsync(int id);
        Task<IEnumerable<Odontologo>> GetAllAsync();
        Task<IEnumerable<Odontologo>> GetByEspecialidadAsync(int especialidadId);
        Task<Odontologo> AddAsync(Odontologo odontologo);
        Task<bool> UpdateAsync(Odontologo odontologo);
        Task<bool> DeleteAsync(int id);
        Task<bool> MatriculaExistsAsync(int matricula, int? excludeId = null);
    }

    public class OdontologoRepository : IOdontologoRepository
    {
        private readonly TurnoMolarDbContext _context;

        public OdontologoRepository(TurnoMolarDbContext context)
        {
            _context = context;
        }

        public async Task<Odontologo?> GetAsync(int id)
        {
            return await _context.Odontologos
                .Include(o => o.Especialidad)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<IEnumerable<Odontologo>> GetAllAsync()
        {
            return await _context.Odontologos
                .Include(o => o.Especialidad)
                .OrderBy(o => o.Apellido)
                .ThenBy(o => o.Nombre)
                .ToListAsync();
        }

        public async Task<IEnumerable<Odontologo>> GetByEspecialidadAsync(int especialidadId)
        {
            return await _context.Odontologos
                .Include(o => o.Especialidad)
                .Where(o => o.EspecialidadId == especialidadId)
                .OrderBy(o => o.Apellido)
                .ToListAsync();
        }

        public async Task<Odontologo> AddAsync(Odontologo odontologo)
        {
            _context.Odontologos.Add(odontologo);
            await _context.SaveChangesAsync();
            return odontologo;
        }

        public async Task<bool> UpdateAsync(Odontologo odontologo)
        {
            var existing = await _context.Odontologos.FindAsync(odontologo.Id);
            if (existing == null)
                return false;

            existing.Nombre = odontologo.Nombre;
            existing.Apellido = odontologo.Apellido;
            existing.Dni = odontologo.Dni;
            existing.Telefono = odontologo.Telefono;
            existing.Mail = odontologo.Mail;
            existing.Domicilio = odontologo.Domicilio;
            existing.NumMatricula = odontologo.NumMatricula;
            existing.EspecialidadId = odontologo.EspecialidadId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var odontologo = await _context.Odontologos.FindAsync(id);
            if (odontologo == null)
                return false;

            _context.Odontologos.Remove(odontologo);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MatriculaExistsAsync(int matricula, int? excludeId = null)
        {
            return await _context.Odontologos.AnyAsync(o =>
                o.NumMatricula == matricula &&
                (!excludeId.HasValue || o.Id != excludeId.Value));
        }
    }
}
