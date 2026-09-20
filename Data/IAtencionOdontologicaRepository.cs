using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public interface IAtencionOdontologicaRepository
    {
        Task<AtencionOdontologica?> GetAsync(int idAtencion);
        Task<AtencionOdontologica?> GetByNroTurnoAsync(int nroTurno);
        Task<AtencionOdontologica> AddAsync(AtencionOdontologica atencion);
        Task<DetalleInsumoUtilizado> AgregarDetalleInsumoAsync(DetalleInsumoUtilizado detalle);
    }

    public class AtencionOdontologicaRepository : IAtencionOdontologicaRepository
    {
        private readonly TurnoMolarDbContext _context;

        public AtencionOdontologicaRepository(TurnoMolarDbContext context)
        {
            _context = context;
        }

        private IQueryable<AtencionOdontologica> IncludeAll()
        {
            return _context.AtencionesOdontologicas
                .Include(a => a.DetallesInsumos)
                    .ThenInclude(d => d.Insumo)
                .Include(a => a.Valoracion);
        }

        public async Task<AtencionOdontologica?> GetAsync(int idAtencion)
        {
            return await IncludeAll().FirstOrDefaultAsync(a => a.IdAtencion == idAtencion);
        }

        public async Task<AtencionOdontologica?> GetByNroTurnoAsync(int nroTurno)
        {
            return await IncludeAll().FirstOrDefaultAsync(a => a.NroTurno == nroTurno);
        }

        public async Task<AtencionOdontologica> AddAsync(AtencionOdontologica atencion)
        {
            _context.AtencionesOdontologicas.Add(atencion);
            await _context.SaveChangesAsync();
            return atencion;
        }

        public async Task<DetalleInsumoUtilizado> AgregarDetalleInsumoAsync(DetalleInsumoUtilizado detalle)
        {
            _context.DetallesInsumosUtilizados.Add(detalle);
            await _context.SaveChangesAsync();
            return detalle;
        }
    }
}
