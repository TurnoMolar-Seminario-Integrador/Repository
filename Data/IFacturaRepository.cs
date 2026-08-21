using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public interface IFacturaRepository
    {
        Task<Pago?> GetAsync(int codPago);
        Task<Pago?> GetByCodAtencionAsync(int codAtencion);
        Task<IEnumerable<Pago>> GetAllAsync();
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
                .Include(p => p.Atencion)
                    .ThenInclude(a => a.Turno)
                        .ThenInclude(t => t.Paciente)
                            .ThenInclude(pac => pac!.ObraSocial);
        }

        public async Task<Pago?> GetAsync(int codPago)
        {
            return await IncludeAll()
                .FirstOrDefaultAsync(p => p.CodPago == codPago);
        }

        public async Task<Pago?> GetByCodAtencionAsync(int codAtencion)
        {
            return await IncludeAll()
                .FirstOrDefaultAsync(p => p.CodAtencion == codAtencion);
        }

        public async Task<IEnumerable<Pago>> GetAllAsync()
        {
            return await IncludeAll()
                .OrderByDescending(p => p.FechaYHoraPago)
                .ToListAsync();
        }

        public async Task<IEnumerable<Pago>> GetByPacienteDocAsync(string tipoDoc, int nroDoc)
        {
            return await IncludeAll()
                .Where(p => p.Atencion != null &&
                            p.Atencion.PacienteTipoDoc == tipoDoc &&
                            p.Atencion.PacienteNroDoc == nroDoc)
                .OrderByDescending(p => p.FechaYHoraPago)
                .ToListAsync();
        }

        public async Task<IEnumerable<Pago>> GetByRangoFechasAsync(DateTime fechaDesde, DateTime fechaHasta)
        {
            var dHasta = fechaHasta.Date.AddDays(1).AddTicks(-1);
            return await IncludeAll()
                .Where(p => p.FechaYHoraPago >= fechaDesde.Date && p.FechaYHoraPago <= dHasta)
                .OrderByDescending(p => p.FechaYHoraPago)
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
            var existing = await _context.Pagos.FindAsync(pago.CodPago);
            if (existing == null)
                return false;

            existing.SetMonto(pago.Monto);
            existing.SetTipoMetodoPago(pago.TipoMetodoPago);

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
