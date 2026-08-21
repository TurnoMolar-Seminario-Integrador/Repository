using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public interface IConsultaRepository
    {
        Task<Consulta?> GetAsync(int id);
        Task<Consulta?> GetByTurnoIdAsync(int turnoId);
        Task<IEnumerable<Consulta>> GetAllAsync();
        Task<IEnumerable<Consulta>> GetByPacienteIdAsync(int pacienteId);
        Task<Consulta> AddAsync(Consulta consulta);
        Task<bool> UpdateAsync(Consulta consulta);
    }

    public class ConsultaRepository : IConsultaRepository
    {
        private readonly TurnoMolarDbContext _context;

        public ConsultaRepository(TurnoMolarDbContext context)
        {
            _context = context;
        }

        public async Task<Consulta?> GetAsync(int id)
        {
            return await _context.Consultas
                .Include(c => c.Turno)
                    .ThenInclude(t => t!.Paciente)
                .Include(c => c.Turno)
                    .ThenInclude(t => t!.Odontologo)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Consulta?> GetByTurnoIdAsync(int turnoId)
        {
            return await _context.Consultas
                .Include(c => c.Turno)
                    .ThenInclude(t => t!.Paciente)
                .Include(c => c.Turno)
                    .ThenInclude(t => t!.Odontologo)
                .FirstOrDefaultAsync(c => c.TurnoId == turnoId);
        }

        public async Task<IEnumerable<Consulta>> GetAllAsync()
        {
            return await _context.Consultas
                .Include(c => c.Turno)
                    .ThenInclude(t => t!.Paciente)
                .Include(c => c.Turno)
                    .ThenInclude(t => t!.Odontologo)
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();
        }

        public async Task<IEnumerable<Consulta>> GetByPacienteIdAsync(int pacienteId)
        {
            return await _context.Consultas
                .Include(c => c.Turno)
                    .ThenInclude(t => t!.Paciente)
                .Include(c => c.Turno)
                    .ThenInclude(t => t!.Odontologo)
                .Where(c => c.Turno != null && c.Turno.PacienteId == pacienteId)
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();
        }

        public async Task<Consulta> AddAsync(Consulta consulta)
        {
            _context.Consultas.Add(consulta);
            await _context.SaveChangesAsync();
            return consulta;
        }

        public async Task<bool> UpdateAsync(Consulta consulta)
        {
            var existing = await _context.Consultas.FindAsync(consulta.Id);
            if (existing == null)
                return false;

            existing.Diagnostico = consulta.Diagnostico;
            existing.Tratamiento = consulta.Tratamiento;
            existing.Observaciones = consulta.Observaciones;
            existing.AnestesiaLocal = consulta.AnestesiaLocal;
            existing.Radiografias = consulta.Radiografias;
            existing.Valoracion = consulta.Valoracion;
            existing.CalificacionEstrellas = consulta.CalificacionEstrellas;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
