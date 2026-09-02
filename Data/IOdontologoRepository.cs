using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public interface IOdontologoRepository
    {
        Task<Odontologo?> GetAsync(string tipoDocumento, string nroDocumento);
        Task<Odontologo?> GetAsync(string tipoDocumento, int nroDocumento);
        Task<IEnumerable<Odontologo>> GetAllAsync();
        Task<IEnumerable<Odontologo>> GetByEspecialidadAsync(int idEspecialidad);
        Task<Odontologo> AddAsync(Odontologo odontologo);
        Task<bool> UpdateAsync(Odontologo odontologo);
        Task<bool> DeleteAsync(string tipoDocumento, string nroDocumento);
        Task<bool> DeleteAsync(string tipoDocumento, int nroDocumento);
        Task<bool> MatriculaExistsAsync(string matricula);
    }

    public class OdontologoRepository : IOdontologoRepository
    {
        private readonly TurnoMolarDbContext _context;

        public OdontologoRepository(TurnoMolarDbContext context)
        {
            _context = context;
        }

        public async Task<Odontologo?> GetAsync(string tipoDocumento, string nroDocumento)
        {
            return await _context.Odontologos
                .Include(o => o.DisponibilidadesHorarias)
                .ThenInclude(d => d.Especialidad)
                .FirstOrDefaultAsync(o => o.TipoDocumento == tipoDocumento && o.NroDocumento == nroDocumento);
        }

        public async Task<Odontologo?> GetAsync(string tipoDocumento, int nroDocumento)
        {
            return await GetAsync(tipoDocumento, nroDocumento.ToString());
        }

        public async Task<IEnumerable<Odontologo>> GetAllAsync()
        {
            return await _context.Odontologos
                .Include(o => o.DisponibilidadesHorarias)
                .ThenInclude(d => d.Especialidad)
                .OrderBy(o => o.Apellido)
                .ThenBy(o => o.Nombre)
                .ToListAsync();
        }

        public async Task<IEnumerable<Odontologo>> GetByEspecialidadAsync(int idEspecialidad)
        {
            return await _context.Odontologos
                .Include(o => o.DisponibilidadesHorarias)
                .ThenInclude(d => d.Especialidad)
                .Where(o => o.DisponibilidadesHorarias.Any(d => d.IdEspecialidad == idEspecialidad))
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
            var existing = await _context.Odontologos
                .FirstOrDefaultAsync(o => o.TipoDocumento == odontologo.TipoDocumento && o.NroDocumento == odontologo.NroDocumento);
            if (existing == null)
                return false;

            existing.SetNombre(odontologo.Nombre);
            existing.SetApellido(odontologo.Apellido);
            existing.SetTelefono(odontologo.Telefono);
            existing.SetEmail(odontologo.Email);
            existing.SetDomicilio(odontologo.Domicilio);
            existing.SetMatricula(odontologo.Matricula);
            existing.SetEstadoOdontologo(odontologo.EstadoOdontologo);
            existing.SetCredenciales(odontologo.Clave, odontologo.SaltClave, odontologo.Rol);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(string tipoDocumento, string nroDocumento)
        {
            var odontologo = await _context.Odontologos
                .FirstOrDefaultAsync(o => o.TipoDocumento == tipoDocumento && o.NroDocumento == nroDocumento);
            if (odontologo == null)
                return false;

            _context.Odontologos.Remove(odontologo);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(string tipoDocumento, int nroDocumento)
        {
            return await DeleteAsync(tipoDocumento, nroDocumento.ToString());
        }

        public async Task<bool> MatriculaExistsAsync(string matricula)
        {
            return await _context.Odontologos.AnyAsync(o => o.Matricula == matricula);
        }
    }
}
