using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public interface IConsultaRepository
    {
        Task<AtencionOdontologica?> GetAsync(int codAtencion);
        Task<AtencionOdontologica?> GetByCodTurnoAsync(int codTurno);
        Task<IEnumerable<AtencionOdontologica>> GetAllAsync();
        Task<IEnumerable<AtencionOdontologica>> GetByPacienteDocAsync(string tipoDoc, int nroDoc);
        Task<AtencionOdontologica> AddAsync(AtencionOdontologica atencion);
        Task<bool> UpdateAsync(AtencionOdontologica atencion);
    }

    public class ConsultaRepository : IConsultaRepository
    {
        private readonly TurnoMolarDbContext _context;

        public ConsultaRepository(TurnoMolarDbContext context)
        {
            _context = context;
        }

        private IQueryable<AtencionOdontologica> IncludeAll()
        {
            return _context.Atenciones
                .Include(a => a.Turno)
                    .ThenInclude(t => t.Paciente)
                .Include(a => a.Turno)
                    .ThenInclude(t => t.Odontologo)
                .Include(a => a.Turno)
                    .ThenInclude(t => t.Especialidad)
                .Include(a => a.Valoracion)
                .Include(a => a.Pago)
                .Include(a => a.DetallesInsumos)
                    .ThenInclude(d => d.Insumo);
        }

        public async Task<AtencionOdontologica?> GetAsync(int codAtencion)
        {
            return await IncludeAll()
                .FirstOrDefaultAsync(a => a.CodAtencion == codAtencion);
        }

        public async Task<AtencionOdontologica?> GetByCodTurnoAsync(int codTurno)
        {
            return await IncludeAll()
                .FirstOrDefaultAsync(a => a.CodTurno == codTurno);
        }

        public async Task<IEnumerable<AtencionOdontologica>> GetAllAsync()
        {
            return await IncludeAll()
                .OrderByDescending(a => a.FechaYHoraAtencionInicio)
                .ToListAsync();
        }

        public async Task<IEnumerable<AtencionOdontologica>> GetByPacienteDocAsync(string tipoDoc, int nroDoc)
        {
            return await IncludeAll()
                .Where(a => a.PacienteTipoDoc == tipoDoc && a.PacienteNroDoc == nroDoc)
                .OrderByDescending(a => a.FechaYHoraAtencionInicio)
                .ToListAsync();
        }

        public async Task<AtencionOdontologica> AddAsync(AtencionOdontologica atencion)
        {
            _context.Atenciones.Add(atencion);
            await _context.SaveChangesAsync();
            return atencion;
        }

        public async Task<bool> UpdateAsync(AtencionOdontologica atencion)
        {
            var existing = await _context.Atenciones.FindAsync(atencion.CodAtencion);
            if (existing == null)
                return false;

            existing.SetHorarioAtencion(atencion.FechaYHoraAtencionInicio, atencion.FechaYHoraAtencionFin);
            existing.Observaciones = atencion.Observaciones;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
