using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    public interface IEspecialidadService
    {
        Task<EspecialidadDTO?> GetAsync(int codEspecialidad);
        Task<IEnumerable<EspecialidadDTO>> GetAllAsync();
        Task<EspecialidadDTO> AddAsync(EspecialidadDTO dto);
        Task<bool> UpdateAsync(EspecialidadDTO dto);
        Task<bool> DeleteAsync(int codEspecialidad);
    }

    public class EspecialidadService : IEspecialidadService
    {
        private readonly IEspecialidadRepository _especialidadRepository;

        public EspecialidadService(IEspecialidadRepository especialidadRepository)
        {
            _especialidadRepository = especialidadRepository;
        }

        public async Task<EspecialidadDTO?> GetAsync(int codEspecialidad)
        {
            var esp = await _especialidadRepository.GetAsync(codEspecialidad);
            return esp == null ? null : MapToDTO(esp);
        }

        public async Task<IEnumerable<EspecialidadDTO>> GetAllAsync()
        {
            var list = await _especialidadRepository.GetAllAsync();
            return list.Select(MapToDTO).ToList();
        }

        public async Task<EspecialidadDTO> AddAsync(EspecialidadDTO dto)
        {
            var esp = new Especialidad(0, dto.Nombre, dto.ArancelParticular);
            await _especialidadRepository.AddAsync(esp);
            return MapToDTO(esp);
        }

        public async Task<bool> UpdateAsync(EspecialidadDTO dto)
        {
            var esp = new Especialidad(dto.CodEspecialidad, dto.Nombre, dto.ArancelParticular);
            return await _especialidadRepository.UpdateAsync(esp);
        }

        public async Task<bool> DeleteAsync(int codEspecialidad)
        {
            return await _especialidadRepository.DeleteAsync(codEspecialidad);
        }

        private static EspecialidadDTO MapToDTO(Especialidad esp)
        {
            return new EspecialidadDTO
            {
                CodEspecialidad = esp.CodEspecialidad,
                Nombre = esp.Nombre,
                ArancelParticular = esp.ArancelParticular
            };
        }
    }
}
