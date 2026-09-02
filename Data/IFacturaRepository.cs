using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public interface IFacturaRepository
    {
        Task<Pago?> GetAsync(int codPago);
        Task<Pago?> GetByCodAtencionAsync(int codAtencion);
        Task<IEnumerable<Pago>> GetAllAsync();
        Task<IEnumerable<Pago>> GetByPacienteDocAsync(string tipoDoc, string nroDoc);
        Task<IEnumerable<Pago>> GetByPacienteDocAsync(string tipoDoc, int nroDoc);
        Task<IEnumerable<Pago>> GetByRangoFechasAsync(DateTime fechaDesde, DateTime fechaHasta);
        Task<Pago> AddAsync(Pago pago);
        Task<bool> UpdateAsync(Pago pago);
    }

    public class FacturaRepository : IFacturaRepository
    {
        private readonly TurnoMolarDbContext _context;

        public FacturaRepository(TurnoMolarDbContext context)
        {
            _context = context;
        }

        private IQueryable<Pago> IncludeAll()
        {
            return _context.Pagos
                .Include(p => p.Turno)
                    .ThenInclude(t => t.Atencion)
                .Include(p => p.Turno)
                    .ThenInclude(t => t.Paciente)
                        .ThenInclude(pac => pac!.ObraSocial)
                .Include(p => p.ObraSocial);
        }

        public async Task<Pago?> GetAsync(int codPago)
        {
            return await IncludeAll()
                .FirstOrDefaultAsync(p => p.IdPago == codPago);
        }

        public async Task<Pago?> GetByCodAtencionAsync(int codAtencion)
        {
            return await IncludeAll()
                .FirstOrDefaultAsync(p => p.Turno.Atencion != null && p.Turno.Atencion.IdAtencion == codAtencion);
        }

        public async Task<IEnumerable<Pago>> GetAllAsync()
        {
            return await IncludeAll()
                .OrderByDescending(p => p.FechaHoraPago)
                .ToListAsync();
        }

        public async Task<IEnumerable<Pago>> GetByPacienteDocAsync(string tipoDoc, string nroDoc)
        {
            return await IncludeAll()
                .Where(p => p.Turno != null &&
                            p.Turno.TipoDocumentoPaciente == tipoDoc &&
                            p.Turno.NroDocumentoPaciente == nroDoc)
                .OrderByDescending(p => p.FechaHoraPago)
                .ToListAsync();
        }

        public async Task<IEnumerable<Pago>> GetByPacienteDocAsync(string tipoDoc, int nroDoc)
        {
            return await GetByPacienteDocAsync(tipoDoc, nroDoc.ToString());
        }

        public async Task<IEnumerable<Pago>> GetByRangoFechasAsync(DateTime fechaDesde, DateTime fechaHasta)
        {
            var dHasta = fechaHasta.Date.AddDays(1).AddTicks(-1);
            return await IncludeAll()
                .Where(p => p.FechaHoraPago >= fechaDesde.Date && p.FechaHoraPago <= dHasta)
                .OrderByDescending(p => p.FechaHoraPago)
                .ToListAsync();
        }

        public async Task<Pago> AddAsync(Pago pago)
        {
            _context.Pagos.Add(pago);
            await _context.SaveChangesAsync();
            return pago;
        }

        public async Task<bool> UpdateAsync(Pago pago)
        {
            var existing = await _context.Pagos.FindAsync(pago.IdPago);
            if (existing == null)
                return false;

            existing.SetMonto(pago.Monto);
            existing.SetMetodoPago(pago.MetodoPago);

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
