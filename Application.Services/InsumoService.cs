using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    public interface IInsumoService
    {
        Task<InsumoDTO?> GetAsync(int id);
        Task<IEnumerable<InsumoDTO>> GetAllAsync();
        Task<InsumoDTO> AddAsync(InsumoDTO dto);
        Task<bool> UpdateAsync(InsumoDTO dto);
        Task<bool> DeleteAsync(int id);
    }

    public class InsumoService : IInsumoService
    {
        private readonly IInsumoRepository _insumoRepository;

        public InsumoService(IInsumoRepository insumoRepository)
        {
            _insumoRepository = insumoRepository;
        }

        public async Task<InsumoDTO?> GetAsync(int id)
        {
            var ins = await _insumoRepository.GetAsync(id);
            return ins == null ? null : MapToDTO(ins);
        }

        public async Task<IEnumerable<InsumoDTO>> GetAllAsync()
        {
            var list = await _insumoRepository.GetAllAsync();
            return list.Select(MapToDTO).ToList();
        }

        public async Task<InsumoDTO> AddAsync(InsumoDTO dto)
        {
            var ins = new Insumo(0, dto.Nombre, dto.Descripcion, dto.Precio, dto.Stock);
            await _insumoRepository.AddAsync(ins);
            dto.Id = ins.Id;
            return dto;
        }

        public async Task<bool> UpdateAsync(InsumoDTO dto)
        {
            var ins = new Insumo(dto.Id, dto.Nombre, dto.Descripcion, dto.Precio, dto.Stock);
            return await _insumoRepository.UpdateAsync(ins);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _insumoRepository.DeleteAsync(id);
        }

        private static InsumoDTO MapToDTO(Insumo i)
        {
            return new InsumoDTO
            {
                Id = i.Id,
                Nombre = i.Nombre,
                Descripcion = i.Descripcion,
                Precio = i.Precio,
                Stock = i.Stock
            };
        }
    }
}
