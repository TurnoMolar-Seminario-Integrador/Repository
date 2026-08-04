using Domain.Model;

namespace Data
{
    public interface IPacienteRepository
    {
        Task<Paciente> AddAsync(Paciente paciente);
        Task<bool> DeleteAsync(int id);
        Task<Paciente?> GetAsync(int id);
        Task<IEnumerable<Paciente>> GetAllAsync();
        Task<bool> UpdateAsync(Paciente paciente);
        Task<bool> EmailExistsAsync(string email, int? excludeId = null);
        Task<IEnumerable<Paciente>> GetByCriteriaAsync(PacienteCriteria criteria);
    }
}
