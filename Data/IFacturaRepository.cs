using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public interface IFacturaRepository
    {
        Task<Factura?> GetAsync(int id);
        Task<Factura?> GetByTurnoIdAsync(int turnoId);
        Task<IEnumerable<Factura>> GetAllAsync();
        Task<IEnumerable<Factura>> GetByPacienteIdAsync(int pacienteId);
        Task<IEnumerable<Factura>> GetByRangoFechasAsync(DateTime fechaDesde, DateTime fechaHasta);
        Task<Factura> AddAsync(Factura factura);
        Task<bool> UpdateAsync(Factura factura);
    }

    public class FacturaRepository : IFacturaRepository
    {
        private readonly TurnoMolarDbContext _context;

        public FacturaRepository(TurnoMolarDbContext context)
        {
            _context = context;
        }

        public async Task<Factura?> GetAsync(int id)
        {
            return await _context.Facturas
                .Include(f => f.Paciente)
                    .ThenInclude(p => p!.ObraSocial)
                .Include(f => f.Turno)
                .Include(f => f.Items)
                    .ThenInclude(i => i.Insumo)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<Factura?> GetByTurnoIdAsync(int turnoId)
        {
            return await _context.Facturas
                .Include(f => f.Paciente)
                    .ThenInclude(p => p!.ObraSocial)
                .Include(f => f.Turno)
                .Include(f => f.Items)
                    .ThenInclude(i => i.Insumo)
                .FirstOrDefaultAsync(f => f.TurnoId == turnoId);
        }

        public async Task<IEnumerable<Factura>> GetAllAsync()
        {
            return await _context.Facturas
                .Include(f => f.Paciente)
                    .ThenInclude(p => p!.ObraSocial)
                .Include(f => f.Turno)
                .Include(f => f.Items)
                    .ThenInclude(i => i.Insumo)
                .OrderByDescending(f => f.FechaEmision)
                .ToListAsync();
        }

        public async Task<IEnumerable<Factura>> GetByPacienteIdAsync(int pacienteId)
        {
            return await _context.Facturas
                .Include(f => f.Paciente)
                    .ThenInclude(p => p!.ObraSocial)
                .Include(f => f.Items)
                    .ThenInclude(i => i.Insumo)
                .Where(f => f.PacienteId == pacienteId)
                .OrderByDescending(f => f.FechaEmision)
                .ToListAsync();
        }

        public async Task<IEnumerable<Factura>> GetByRangoFechasAsync(DateTime fechaDesde, DateTime fechaHasta)
        {
            var dDesde = fechaDesde.Date;
            var dHasta = fechaHasta.Date.AddDays(1).AddTicks(-1);

            return await _context.Facturas
                .Include(f => f.Paciente)
                    .ThenInclude(p => p!.ObraSocial)
                .Include(f => f.Items)
                    .ThenInclude(i => i.Insumo)
                .Where(f => f.FechaEmision >= dDesde && f.FechaEmision <= dHasta)
                .OrderByDescending(f => f.FechaEmision)
                .ToListAsync();
        }

        public async Task<Factura> AddAsync(Factura factura)
        {
            _context.Facturas.Add(factura);
            await _context.SaveChangesAsync();
            return factura;
        }

        public async Task<bool> UpdateAsync(Factura factura)
        {
            var existing = await _context.Facturas.FindAsync(factura.Id);
            if (existing == null)
                return false;

            existing.Descripcion = factura.Descripcion;
            existing.Subtotal = factura.Subtotal;
            existing.DescuentoObraSocial = factura.DescuentoObraSocial;
            existing.Total = factura.Total;
            existing.MontoAPagarPaciente = factura.MontoAPagarPaciente;
            existing.EstadoPago = factura.EstadoPago;
            existing.MetodoPago = factura.MetodoPago;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
