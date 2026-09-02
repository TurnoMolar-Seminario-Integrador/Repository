using Domain.Model;

namespace Data
{
    public interface ITurnoRepository
    {
        Task<IEnumerable<Turno>> GetAllAsync();
        Task<Turno?> GetAsync(int nroTurno);
        Task<Turno> AddAsync(Turno turno);
        Task<bool> UpdateAsync(Turno turno);
        Task<bool> DeleteAsync(int nroTurno);
        Task<bool> TurnoExistsAsync(DateTime fechaReserva, string odontologoTipoDoc, string odontologoNroDoc);
        Task<bool> TurnoExistsAsync(DateTime fechaReserva, string odontologoTipoDoc, int odontologoNroDoc);
        Task<IEnumerable<Turno>> GetByFechaAsync(DateTime fecha);
        Task<IEnumerable<Turno>> GetByOdontologoAsync(string tipoDoc, string nroDoc);
        Task<IEnumerable<Turno>> GetByOdontologoAsync(string tipoDoc, int nroDoc);
        Task<IEnumerable<Turno>> GetByPacienteAsync(string tipoDoc, string nroDoc);
        Task<IEnumerable<Turno>> GetByPacienteAsync(string tipoDoc, int nroDoc);
    }
}
