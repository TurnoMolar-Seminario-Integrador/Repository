using DTOs;

namespace DentalClinic.Application.Services
{
    public interface IPacienteService
    {
        Task<PacienteDTO> AddAsync(PacienteDTO dto);
        Task<bool> DeleteAsync(int id);
        Task<PacienteDTO?> GetAsync(int id);
        Task<IEnumerable<PacienteDTO>> GetAllAsync();
        Task<bool> UpdateAsync(PacienteDTO dto);
        Task<IEnumerable<PacienteDTO>> GetByCriteriaAsync(PacienteCriteriaDTO criteriaDTO);
    }
}