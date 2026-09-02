using Domain.Model;

namespace Data
{
    public interface IPacienteRepository
    {
        Task<Paciente> AddAsync(Paciente paciente);
        Task<bool> DeleteAsync(string tipoDocumento, string nroDocumento);
        Task<bool> DeleteAsync(int nroDocumento);
        Task<Paciente?> GetAsync(string tipoDocumento, string nroDocumento);
        Task<Paciente?> GetAsync(int nroDocumento);
        Task<IEnumerable<Paciente>> GetAllAsync();
        Task<bool> UpdateAsync(Paciente paciente);
        Task<bool> EmailExistsAsync(string email, string? excludeNroDoc = null);
        Task<bool> EmailExistsAsync(string email, int excludeNroDoc);
        Task<IEnumerable<Paciente>> GetByCriteriaAsync(PacienteCriteria criteria);
    }
}
