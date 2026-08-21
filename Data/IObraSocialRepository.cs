using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public interface IObraSocialRepository
    {
        Task<ObraSocial?> GetAsync(int id);
        Task<IEnumerable<ObraSocial>> GetAllAsync();
        Task<ObraSocial> AddAsync(ObraSocial obraSocial);
        Task<bool> UpdateAsync(ObraSocial obraSocial);
    }

    public class ObraSocialRepository : IObraSocialRepository
    {
        private readonly TurnoMolarDbContext _context;

        public ObraSocialRepository(TurnoMolarDbContext context)
        {
            _context = context;
        }

        public async Task<ObraSocial?> GetAsync(int id)
        {
            return await _context.ObrasSociales.FindAsync(id);
        }

        public async Task<IEnumerable<ObraSocial>> GetAllAsync()
        {
            return await _context.ObrasSociales.OrderBy(o => o.Nombre).ToListAsync();
        }

        public async Task<ObraSocial> AddAsync(ObraSocial obraSocial)
        {
            _context.ObrasSociales.Add(obraSocial);
            await _context.SaveChangesAsync();
            return obraSocial;
        }

        public async Task<bool> UpdateAsync(ObraSocial obraSocial)
        {
            var existing = await _context.ObrasSociales.FindAsync(obraSocial.Id);
            if (existing == null)
                return false;

            existing.Nombre = obraSocial.Nombre;
            existing.Plan = obraSocial.Plan;
            existing.PorcentajeCobertura = obraSocial.PorcentajeCobertura;

            await _context.SaveChangesAsync();
            return true;
        }
    }

    public interface IHistoriaClinicaRepository
    {
        Task<HistoriaClinica?> GetByPacienteIdAsync(int pacienteId);
        Task<HistoriaClinica> AddAsync(HistoriaClinica historiaClinica);
        Task<bool> UpdateAsync(HistoriaClinica historiaClinica);
    }

    public class HistoriaClinicaRepository : IHistoriaClinicaRepository
    {
        private readonly TurnoMolarDbContext _context;

        public HistoriaClinicaRepository(TurnoMolarDbContext context)
        {
            _context = context;
        }

        public async Task<HistoriaClinica?> GetByPacienteIdAsync(int pacienteId)
        {
            return await _context.HistoriasClinicas
                .Include(h => h.Paciente)
                .FirstOrDefaultAsync(h => h.PacienteId == pacienteId);
        }

        public async Task<HistoriaClinica> AddAsync(HistoriaClinica historiaClinica)
        {
            _context.HistoriasClinicas.Add(historiaClinica);
            await _context.SaveChangesAsync();
            return historiaClinica;
        }

        public async Task<bool> UpdateAsync(HistoriaClinica historiaClinica)
        {
            var existing = await _context.HistoriasClinicas.FindAsync(historiaClinica.Id);
            if (existing == null)
                return false;

            existing.AntecedentesMedicos = historiaClinica.AntecedentesMedicos;
            existing.Alergias = historiaClinica.Alergias;
            existing.ObservacionesGenerales = historiaClinica.ObservacionesGenerales;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
