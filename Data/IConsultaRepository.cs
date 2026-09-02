using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public interface IConsultaRepository
    {
        Task<AtencionOdontologica?> GetAsync(int codAtencion);
        Task<AtencionOdontologica?> GetByCodTurnoAsync(int codTurno);
        Task<IEnumerable<AtencionOdontologica>> GetAllAsync();
        Task<IEnumerable<AtencionOdontologica>> GetByPacienteDocAsync(string tipoDoc, string nroDoc);
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
                .Include(a => a.Turno)
                    .ThenInclude(t => t.Pago)
                .Include(a => a.Valoracion)
                .Include(a => a.DetallesInsumos)
                    .ThenInclude(d => d.Insumo);
        }

        public async Task<AtencionOdontologica?> GetAsync(int codAtencion)
        {
            return await IncludeAll()
                .FirstOrDefaultAsync(a => a.IdAtencion == codAtencion);
        }

        public async Task<AtencionOdontologica?> GetByCodTurnoAsync(int codTurno)
        {
            return await IncludeAll()
                .FirstOrDefaultAsync(a => a.NroTurno == codTurno);
        }

        public async Task<IEnumerable<AtencionOdontologica>> GetAllAsync()
        {
            return await IncludeAll()
                .OrderByDescending(a => a.FechaHoraAtencionInicio)
                .ToListAsync();
        }

        public async Task<IEnumerable<AtencionOdontologica>> GetByPacienteDocAsync(string tipoDoc, string nroDoc)
        {
            return await IncludeAll()
                .Where(a => a.Turno != null && a.Turno.TipoDocumentoPaciente == tipoDoc && a.Turno.NroDocumentoPaciente == nroDoc)
                .OrderByDescending(a => a.FechaHoraAtencionInicio)
                .ToListAsync();
        }

        public async Task<IEnumerable<AtencionOdontologica>> GetByPacienteDocAsync(string tipoDoc, int nroDoc)
        {
            return await GetByPacienteDocAsync(tipoDoc, nroDoc.ToString());
        }

        public async Task<AtencionOdontologica> AddAsync(AtencionOdontologica atencion)
        {
            _context.Atenciones.Add(atencion);
            await _context.SaveChangesAsync();
            return atencion;
        }

        public async Task<bool> UpdateAsync(AtencionOdontologica atencion)
        {
            var existing = await _context.Atenciones.FindAsync(atencion.IdAtencion);
            if (existing == null)
                return false;

            existing.SetHorarioAtencion(atencion.FechaHoraAtencionInicio, atencion.FechaHoraAtencionFin);
            existing.Observaciones = atencion.Observaciones;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
