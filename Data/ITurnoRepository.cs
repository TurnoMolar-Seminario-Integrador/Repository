using Domain.Model;

namespace Data
{
  public interface ITurnoRepository
  {
    Task<IEnumerable<TurnoOdontologico>> GetAllAsync();
    Task<TurnoOdontologico?> GetAsync(int id);
    Task<TurnoOdontologico> AddAsync(TurnoOdontologico turno);
    Task<bool> UpdateAsync(TurnoOdontologico turno);
    Task<bool> DeleteAsync(int id);
    Task<bool> TurnoExistsAsync(DateTime fecha, TimeOnly horarioTurno, int? idExcluir = null);
    Task<IEnumerable<TurnoOdontologico>> GetByFechaAsync(DateTime fecha);
  }
}
