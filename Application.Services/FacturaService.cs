using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    public interface IFacturaService
    {
        Task<FacturaDTO?> GetAsync(int id);
        Task<FacturaDTO?> GetByTurnoIdAsync(int turnoId);
        Task<IEnumerable<FacturaDTO>> GetAllAsync();
        Task<IEnumerable<FacturaDTO>> GetByPacienteIdAsync(int pacienteId);
        Task<FacturaDTO> CrearFacturaAsync(FacturaDTO dto);
        Task<bool> RegistrarPagoAsync(int id, string metodoPago);
    }

    public class FacturaService : IFacturaService
    {
        private readonly IFacturaRepository _facturaRepository;
        private readonly IPacienteRepository _pacienteRepository;

        public FacturaService(IFacturaRepository facturaRepository, IPacienteRepository pacienteRepository)
        {
            _facturaRepository = facturaRepository;
            _pacienteRepository = pacienteRepository;
        }

        public async Task<FacturaDTO?> GetAsync(int id)
        {
            var f = await _facturaRepository.GetAsync(id);
            return f == null ? null : MapToDTO(f);
        }

        public async Task<FacturaDTO?> GetByTurnoIdAsync(int turnoId)
        {
            var f = await _facturaRepository.GetByTurnoIdAsync(turnoId);
            return f == null ? null : MapToDTO(f);
        }

        public async Task<IEnumerable<FacturaDTO>> GetAllAsync()
        {
            var list = await _facturaRepository.GetAllAsync();
            return list.Select(MapToDTO).ToList();
        }

        public async Task<IEnumerable<FacturaDTO>> GetByPacienteIdAsync(int pacienteId)
        {
            var list = await _facturaRepository.GetByPacienteIdAsync(pacienteId);
            return list.Select(MapToDTO).ToList();
        }

        public async Task<FacturaDTO> CrearFacturaAsync(FacturaDTO dto)
        {
            var paciente = await _pacienteRepository.GetAsync(dto.PacienteId);
            decimal cobertura = paciente?.ObraSocial?.PorcentajeCobertura ?? 0m;
            decimal descuento = dto.Subtotal * cobertura;
            decimal montoPaciente = dto.Subtotal - descuento;

            var factura = new Factura(
                0,
                dto.TurnoId,
                dto.PacienteId,
                dto.Descripcion,
                dto.Subtotal,
                descuento,
                dto.Subtotal,
                montoPaciente,
                dto.EstadoPago,
                dto.MetodoPago
            );

            if (dto.Items != null && dto.Items.Any())
            {
                factura.Items = dto.Items.Select(i => new ItemFactura(0, 0, i.InsumoId, i.CantidadInsumo, i.PrecioUnitario)).ToList();
            }

            await _facturaRepository.AddAsync(factura);
            return MapToDTO(factura);
        }

        public async Task<bool> RegistrarPagoAsync(int id, string metodoPago)
        {
            var factura = await _facturaRepository.GetAsync(id);
            if (factura == null) return false;

            factura.EstadoPago = true;
            factura.MetodoPago = string.IsNullOrWhiteSpace(metodoPago) ? "Efectivo" : metodoPago;

            return await _facturaRepository.UpdateAsync(factura);
        }

        private static FacturaDTO MapToDTO(Factura f)
        {
            return new FacturaDTO
            {
                Id = f.Id,
                TurnoId = f.TurnoId,
                PacienteId = f.PacienteId,
                PacienteNombre = f.Paciente != null ? $"{f.Paciente.Apellido}, {f.Paciente.Nombre}" : $"Paciente #{f.PacienteId}",
                ObraSocialNombre = f.Paciente?.ObraSocial != null ? $"{f.Paciente.ObraSocial.Nombre} ({f.Paciente.ObraSocial.Plan})" : "Particular",
                Descripcion = f.Descripcion,
                Subtotal = f.Subtotal,
                DescuentoObraSocial = f.DescuentoObraSocial,
                Total = f.Total,
                MontoAPagarPaciente = f.MontoAPagarPaciente,
                EstadoPago = f.EstadoPago,
                MetodoPago = f.MetodoPago,
                FechaEmision = f.FechaEmision,
                Items = f.Items?.Select(i => new ItemFacturaDTO
                {
                    Id = i.Id,
                    FacturaId = i.FacturaId,
                    InsumoId = i.InsumoId,
                    InsumoNombre = i.Insumo?.Nombre ?? $"Insumo #{i.InsumoId}",
                    CantidadInsumo = i.CantidadInsumo,
                    PrecioUnitario = i.PrecioUnitario
                }).ToList() ?? new List<ItemFacturaDTO>()
            };
        }
    }
}
