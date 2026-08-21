using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public interface IObraSocialRepository
    {
        Task<ObraSocial?> GetAsync(int identificadorOS);
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

        public async Task<ObraSocial?> GetAsync(int identificadorOS)
        {
            return await _context.ObrasSociales.FindAsync(identificadorOS);
        }

        public async Task<IEnumerable<ObraSocial>> GetAllAsync()
        {
            return await _context.ObrasSociales.OrderBy(o => o.NombreOS).ToListAsync();
        }

        public async Task<ObraSocial> AddAsync(ObraSocial obraSocial)
        {
            _context.ObrasSociales.Add(obraSocial);
            await _context.SaveChangesAsync();
            return obraSocial;
        }

        public async Task<bool> UpdateAsync(ObraSocial obraSocial)
        {
            var existing = await _context.ObrasSociales.FindAsync(obraSocial.IdentificadorOS);
            if (existing == null)
                return false;

            existing.SetNombreOS(obraSocial.NombreOS);
            existing.SetPlanCobertura(obraSocial.PlanCobertura);
            existing.SetArancelOS(obraSocial.ArancelOS);
            existing.SetEstadoOS(obraSocial.EstadoOS);

            await _context.SaveChangesAsync();
            return true;
        }
    }

    public interface IHistoriaClinicaRepository
    {
        Task<HistoriaClinica?> GetByPacienteDocAsync(string tipoDocumento, int nroDocumento);
        Task<HistoriaClinica?> GetByNroHCAsync(int nroHC);
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

        public async Task<HistoriaClinica?> GetByPacienteDocAsync(string tipoDocumento, int nroDocumento)
        {
            return await _context.HistoriasClinicas
                .Include(h => h.Paciente)
                .FirstOrDefaultAsync(h => h.PacienteTipoDoc == tipoDocumento && h.PacienteNroDoc == nroDocumento);
        }

        public async Task<HistoriaClinica?> GetByNroHCAsync(int nroHC)
        {
            return await _context.HistoriasClinicas
                .Include(h => h.Paciente)
                .FirstOrDefaultAsync(h => h.NroHC == nroHC);
        }

        public async Task<HistoriaClinica> AddAsync(HistoriaClinica historiaClinica)
        {
            _context.HistoriasClinicas.Add(historiaClinica);
            await _context.SaveChangesAsync();
            return historiaClinica;
        }

        public async Task<bool> UpdateAsync(HistoriaClinica historiaClinica)
        {
            var existing = await _context.HistoriasClinicas.FindAsync(historiaClinica.NroHC);
            if (existing == null)
                return false;

            existing.AntecedentesMedicos = historiaClinica.AntecedentesMedicos;
            existing.Alergias = historiaClinica.Alergias;
            existing.ObservacionesGeneral = historiaClinica.ObservacionesGeneral;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
