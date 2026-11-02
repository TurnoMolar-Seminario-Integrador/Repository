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

        // CUU04 - Valorar Atención Odontológica, camino básico paso 2: registra la Valoracion
        // de una Atencion (relación 1:1, RN14). Mismo criterio que AgregarDetalleInsumoAsync:
        // el repositorio de AtencionOdontologica es dueño de sus entidades relacionadas.
        Task<Valoracion> AgregarValoracionAsync(Valoracion valoracion);

        // CUF03 - Administrar Historia Clínica, pasos 2 y 3: las atenciones de una historia clínica en orden
        // cronológico, con el turno (especialidad y odontólogo interviniente) y los insumos utilizados.
        Task<IReadOnlyList<AtencionOdontologica>> GetByHistoriaClinicaAsync(int nroHC);

        // CUF03, alt 3.a.1: corrige el campo de observaciones de una atención ya registrada. Devuelve
        // false si la atención no existe. Lanza ConflictoConcurrenciaException si otro usuario modificó las
        // observaciones entre que se leyó la atención y este guardado.
        Task<bool> ActualizarObservacionesAsync(int idAtencion, string observaciones);
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

        public async Task<Valoracion> AgregarValoracionAsync(Valoracion valoracion)
        {
            _context.Valoraciones.Add(valoracion);
            await _context.SaveChangesAsync();
            return valoracion;
        }

        public async Task<IReadOnlyList<AtencionOdontologica>> GetByHistoriaClinicaAsync(int nroHC)
        {
            return await _context.AtencionesOdontologicas
                .Include(a => a.Turno)
                    .ThenInclude(t => t.Odontologo)
                .Include(a => a.Turno)
                    .ThenInclude(t => t.Especialidad)
                .Include(a => a.DetallesInsumos)
                    .ThenInclude(d => d.Insumo)
                .Where(a => a.NroHC == nroHC)
                .OrderBy(a => a.FechaHoraAtencionInicio)
                .ToListAsync();
        }

        public async Task<bool> ActualizarObservacionesAsync(int idAtencion, string observaciones)
        {
            var atencion = await _context.AtencionesOdontologicas
                .FirstOrDefaultAsync(a => a.IdAtencion == idAtencion);
            if (atencion == null)
                return false;

            atencion.Observaciones = observaciones;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _context.ChangeTracker.Clear();
                throw new ConflictoConcurrenciaException("Las observaciones de la atención fueron modificadas por otro usuario.", ex);
            }

            return true;
        }
    }
}
