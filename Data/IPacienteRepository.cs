using Domain.Model;

namespace Data
{
    public interface IPacienteRepository
    {
        Task<Paciente> AddAsync(Paciente paciente);
        Task<bool> DeleteAsync(int nroDocumento);
        Task<Paciente?> GetAsync(int nroDocumento);
        Task<IEnumerable<Paciente>> GetAllAsync();
        Task<bool> UpdateAsync(Paciente paciente);
        Task<bool> EmailExistsAsync(string email, int? excludeNroDoc = null);
        Task<IEnumerable<Paciente>> GetByCriteriaAsync(PacienteCriteria criteria);
    }
}
