using Domain.Model;

namespace Data
{
  public class TurnoRepository : ITurnoRepository
  {
    private static readonly List<TurnoOdontologico> turnos = new List<TurnoOdontologico>();
    private static int nextId = 1;

    

        public Task<TurnoOdontologico> AddAsync(TurnoOdontologico turno)
        {
            turno.SetId(nextId);
            nextId++;
            turnos.Add(turno);
            return Task.FromResult(turno);
        }
        public Task<bool> DeleteAsync(int id)
    {
      var turno = turnos.FirstOrDefault(t => t.Id == id);
      if (turno != null)
      {
        turnos.Remove(turno);
        return Task.FromResult(true);
      }
      return Task.FromResult(false);
    }

    public Task<TurnoOdontologico?> GetAsync(int id)
    {
      return Task.FromResult(turnos.FirstOrDefault(t => t.Id == id));
    }

    public Task<IEnumerable<TurnoOdontologico>> GetAllAsync()
    {
      return Task.FromResult<IEnumerable<TurnoOdontologico>>(turnos.ToList());
    }

    public Task<bool> UpdateAsync(TurnoOdontologico turno)
    {
      var existing = turnos.FirstOrDefault(t => t.Id == turno.Id);
      if (existing != null)
      {
        existing.SetFechaT(turno.Fecha);
        existing.SetHoraT(turno.HorarioTurno);
        existing.SetEstado(turno.EstadoTurno);
        existing.SetMotivo(turno.MotivoCancelacion);

        return Task.FromResult(true);
      }
      return Task.FromResult(false);
    }
        
    public Task<bool> TurnoExistsAsync(DateTime fecha, TimeOnly horarioTurno, int? excludeId = null)
    {
      var query = turnos.Where(t => t.Fecha == fecha && t.HorarioTurno == horarioTurno);
      if (excludeId.HasValue)
      {
        query = query.Where(t => t.Id != excludeId.Value);
      }
      return Task.FromResult(query.Any());
    }

    public Task<IEnumerable<TurnoOdontologico>> GetByFechaAsync(DateTime fecha)
    {
      IEnumerable<TurnoOdontologico> result = turnos
      .Where(t => t.Fecha.Date == fecha.Date)
      .ToList();

      return Task.FromResult(result);

    }
  }
}
