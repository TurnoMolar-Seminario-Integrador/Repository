using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public class PacienteRepository : IPacienteRepository
    {
        private readonly TurnoMolarDbContext _context;

        public PacienteRepository(TurnoMolarDbContext context)
        {
            _context = context;
        }

        public async Task<Paciente> AddAsync(Paciente paciente)
        {
            _context.Pacientes.Add(paciente);
            await _context.SaveChangesAsync();
            return paciente;
        }

        public async Task<bool> DeleteAsync(string tipoDocumento, string nroDocumento)
        {
            var paciente = await _context.Pacientes
                .FirstOrDefaultAsync(p => p.TipoDocumento == tipoDocumento && p.NroDocumento == nroDocumento);
            if (paciente != null)
            {
                _context.Pacientes.Remove(paciente);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> DeleteAsync(int nroDocumento)
        {
            return await DeleteAsync("DNI", nroDocumento.ToString());
        }

        public async Task<Paciente?> GetAsync(string tipoDocumento, string nroDocumento)
        {
            return await _context.Pacientes
                .Include(p => p.ObraSocial)
                .Include(p => p.HistoriaClinica)
                .FirstOrDefaultAsync(p => p.TipoDocumento == tipoDocumento && p.NroDocumento == nroDocumento);
        }

        public async Task<Paciente?> GetAsync(int nroDocumento)
        {
            var strDoc = nroDocumento.ToString();
            return await _context.Pacientes
                .Include(p => p.ObraSocial)
                .Include(p => p.HistoriaClinica)
                .FirstOrDefaultAsync(p => p.NroDocumento == strDoc);
        }

        public async Task<IEnumerable<Paciente>> GetAllAsync()
        {
            return await _context.Pacientes
                .Include(p => p.ObraSocial)
                .Include(p => p.HistoriaClinica)
                .OrderBy(p => p.Apellido)
                .ThenBy(p => p.Nombre)
                .ToListAsync();
        }

        public async Task<bool> UpdateAsync(Paciente paciente)
        {
            var existing = await _context.Pacientes
                .FirstOrDefaultAsync(p => p.TipoDocumento == paciente.TipoDocumento && p.NroDocumento == paciente.NroDocumento);
            if (existing == null)
                return false;

            existing.SetNombre(paciente.Nombre);
            existing.SetApellido(paciente.Apellido);
            existing.SetTelefono(paciente.Telefono);
            existing.SetEmail(paciente.Email);
            existing.SetDomicilio(paciente.Domicilio);
            existing.SetEstadoPaciente(paciente.EstadoPaciente);
            existing.SetIdentificadorOS(paciente.IdentificadorOS);
            existing.SetCredenciales(paciente.Clave, paciente.SaltClave, paciente.Rol);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EmailExistsAsync(string email, string? excludeNroDoc = null)
        {
            return await _context.Pacientes.AnyAsync(p =>
                p.Email.ToLower() == email.ToLower() &&
                (excludeNroDoc == null || p.NroDocumento != excludeNroDoc));
        }

        public async Task<bool> EmailExistsAsync(string email, int excludeNroDoc)
        {
            return await EmailExistsAsync(email, excludeNroDoc.ToString());
        }

        public async Task<IEnumerable<Paciente>> GetByCriteriaAsync(PacienteCriteria criteria)
        {
            var texto = criteria.Texto?.ToLower() ?? string.Empty;
            return await _context.Pacientes
                .Include(p => p.ObraSocial)
                .Where(p =>
                    p.Nombre.ToLower().Contains(texto) ||
                    p.Apellido.ToLower().Contains(texto) ||
                    p.Email.ToLower().Contains(texto) ||
                    p.NroDocumento.Contains(texto))
                .ToListAsync();
        }
    }
}