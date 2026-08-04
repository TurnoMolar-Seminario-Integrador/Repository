using DTOs;

namespace DentalClinic.Application.Services
{
    public interface ITurnoOdontologicoService
    {
        Task<TurnoOdontologicoDTO> AddAsync(TurnoOdontologicoDTO dto);
        Task<bool> DeleteAsync(int id);
        Task<TurnoOdontologicoDTO?> GetAsync(int id);
        Task<IEnumerable<TurnoOdontologicoDTO>> GetAllAsync();
        Task<bool> UpdateAsync(TurnoOdontologicoDTO dto);
        Task<IEnumerable<TurnoOdontologicoDTO>> GetByCriteriaAsync(TurnoCriteriaDTO criteriaDTO);
    }
}