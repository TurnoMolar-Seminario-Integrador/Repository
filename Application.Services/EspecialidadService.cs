using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    public interface IEspecialidadService
    {
        Task<EspecialidadDTO?> GetAsync(int id);
        Task<IEnumerable<EspecialidadDTO>> GetAllAsync();
        Task<EspecialidadDTO> AddAsync(EspecialidadDTO dto);
        Task<bool> UpdateAsync(EspecialidadDTO dto);
        Task<bool> DeleteAsync(int id);
    }

    public class EspecialidadService : IEspecialidadService
    {
        private readonly IEspecialidadRepository _especialidadRepository;

        public EspecialidadService(IEspecialidadRepository especialidadRepository)
        {
            _especialidadRepository = especialidadRepository;
        }

        public async Task<EspecialidadDTO?> GetAsync(int id)
        {
            var esp = await _especialidadRepository.GetAsync(id);
            return esp == null ? null : new EspecialidadDTO { Id = esp.Id, Nombre = esp.Nombre, Descripcion = esp.Descripcion };
        }

        public async Task<IEnumerable<EspecialidadDTO>> GetAllAsync()
        {
            var list = await _especialidadRepository.GetAllAsync();
            return list.Select(e => new EspecialidadDTO { Id = e.Id, Nombre = e.Nombre, Descripcion = e.Descripcion }).ToList();
        }

        public async Task<EspecialidadDTO> AddAsync(EspecialidadDTO dto)
        {
            var esp = new Especialidad(0, dto.Nombre, dto.Descripcion);
            await _especialidadRepository.AddAsync(esp);
            dto.Id = esp.Id;
            return dto;
        }

        public async Task<bool> UpdateAsync(EspecialidadDTO dto)
        {
            var esp = new Especialidad(dto.Id, dto.Nombre, dto.Descripcion);
            return await _especialidadRepository.UpdateAsync(esp);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _especialidadRepository.DeleteAsync(id);
        }
    }
}
