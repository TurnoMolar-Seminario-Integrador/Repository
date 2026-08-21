using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    public interface IMultaService
    {
        Task<MultaDTO?> GetAsync(int id);
        Task<IEnumerable<MultaDTO>> GetAllAsync();
        Task<IEnumerable<MultaDTO>> GetByPacienteDocAsync(string tipoDoc, int nroDoc);
        Task<IEnumerable<MultaDTO>> GetImpagasByPacienteDocAsync(string tipoDoc, int nroDoc);
        Task<MultaDTO> CrearMultaAsync(MultaDTO dto);
        Task<bool> PagarMultaAsync(int id);
    }

    public class MultaService : IMultaService
    {
        private readonly IMultaRepository _multaRepository;

        public MultaService(IMultaRepository multaRepository)
        {
            _multaRepository = multaRepository;
        }

        public async Task<MultaDTO?> GetAsync(int id)
        {
            var m = await _multaRepository.GetAsync(id);
            return m == null ? null : MapToDTO(m);
        }

        public async Task<IEnumerable<MultaDTO>> GetAllAsync()
        {
            var list = await _multaRepository.GetAllAsync();
            return list.Select(MapToDTO).ToList();
        }

        public async Task<IEnumerable<MultaDTO>> GetByPacienteDocAsync(string tipoDoc, int nroDoc)
        {
            var list = await _multaRepository.GetByPacienteDocAsync(tipoDoc, nroDoc);
            return list.Select(MapToDTO).ToList();
        }

        public async Task<IEnumerable<MultaDTO>> GetImpagasByPacienteDocAsync(string tipoDoc, int nroDoc)
        {
            var list = await _multaRepository.GetImpagasByPacienteDocAsync(tipoDoc, nroDoc);
            return list.Select(MapToDTO).ToList();
        }

        public async Task<MultaDTO> CrearMultaAsync(MultaDTO dto)
        {
            var multa = new Multa(0, dto.Monto, dto.EstadoPago, dto.FechaPago);
            await _multaRepository.AddAsync(multa);
            return MapToDTO(multa);
        }

        public async Task<bool> PagarMultaAsync(int id)
        {
            return await _multaRepository.PagarMultaAsync(id);
        }

        private static MultaDTO MapToDTO(Multa m)
        {
            return new MultaDTO
            {
                Id = m.Id,
                Monto = m.Monto,
                EstadoPago = m.EstadoPago,
                FechaPago = m.FechaPago
            };
        }
    }
}
