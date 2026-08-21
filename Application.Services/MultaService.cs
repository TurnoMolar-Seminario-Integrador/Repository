using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    public interface IMultaService
    {
        Task<MultaDTO?> GetAsync(int id);
        Task<IEnumerable<MultaDTO>> GetAllAsync();
        Task<IEnumerable<MultaDTO>> GetByPacienteIdAsync(int pacienteId);
        Task<IEnumerable<MultaDTO>> GetImpagasByPacienteIdAsync(int pacienteId);
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

        public async Task<IEnumerable<MultaDTO>> GetByPacienteIdAsync(int pacienteId)
        {
            var list = await _multaRepository.GetByPacienteIdAsync(pacienteId);
            return list.Select(MapToDTO).ToList();
        }

        public async Task<IEnumerable<MultaDTO>> GetImpagasByPacienteIdAsync(int pacienteId)
        {
            var list = await _multaRepository.GetImpagasByPacienteIdAsync(pacienteId);
            return list.Select(MapToDTO).ToList();
        }

        public async Task<MultaDTO> CrearMultaAsync(MultaDTO dto)
        {
            var multa = new Multa(0, dto.PacienteId, dto.Monto, dto.EstadoPago, dto.FechaPago, dto.Motivo);
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
                PacienteId = m.PacienteId,
                PacienteNombre = m.Paciente != null ? $"{m.Paciente.Apellido}, {m.Paciente.Nombre}" : $"Paciente #{m.PacienteId}",
                Monto = m.Monto,
                EstadoPago = m.EstadoPago,
                FechaEmision = m.FechaEmision,
                FechaPago = m.FechaPago,
                Motivo = m.Motivo
            };
        }
    }
}
