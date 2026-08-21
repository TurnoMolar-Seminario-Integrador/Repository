using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    public interface IInsumoService
    {
        Task<InsumoDTO?> GetAsync(int codInsumo);
        Task<IEnumerable<InsumoDTO>> GetAllAsync();
        Task<InsumoDTO> AddAsync(InsumoDTO dto);
        Task<bool> UpdateAsync(InsumoDTO dto);
        Task<bool> DeleteAsync(int codInsumo);
    }

    public class InsumoService : IInsumoService
    {
        private readonly IInsumoRepository _insumoRepository;

        public InsumoService(IInsumoRepository insumoRepository)
        {
            _insumoRepository = insumoRepository;
        }

        public async Task<InsumoDTO?> GetAsync(int codInsumo)
        {
            var ins = await _insumoRepository.GetAsync(codInsumo);
            return ins == null ? null : MapToDTO(ins);
        }

        public async Task<IEnumerable<InsumoDTO>> GetAllAsync()
        {
            var list = await _insumoRepository.GetAllAsync();
            return list.Select(MapToDTO).ToList();
        }

        public async Task<InsumoDTO> AddAsync(InsumoDTO dto)
        {
            var ins = new Insumo(0, dto.Nombre, dto.CostoUnitario, dto.StockDisponible);
            await _insumoRepository.AddAsync(ins);
            return MapToDTO(ins);
        }

        public async Task<bool> UpdateAsync(InsumoDTO dto)
        {
            var ins = new Insumo(dto.CodInsumo, dto.Nombre, dto.CostoUnitario, dto.StockDisponible);
            return await _insumoRepository.UpdateAsync(ins);
        }

        public async Task<bool> DeleteAsync(int codInsumo)
        {
            return await _insumoRepository.DeleteAsync(codInsumo);
        }

        private static InsumoDTO MapToDTO(Insumo i)
        {
            return new InsumoDTO
            {
                CodInsumo = i.CodInsumo,
                Nombre = i.Nombre,
                CostoUnitario = i.CostoUnitario,
                StockDisponible = i.StockDisponible
            };
        }
    }
}
