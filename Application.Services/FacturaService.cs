using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    public interface IFacturaService
    {
        Task<FacturaDTO?> GetAsync(int codPago);
        Task<FacturaDTO?> GetByCodAtencionAsync(int codAtencion);
        Task<IEnumerable<FacturaDTO>> GetAllAsync();
        Task<IEnumerable<FacturaDTO>> GetByPacienteDocAsync(string tipoDoc, int nroDoc);
        Task<FacturaDTO> CrearFacturaAsync(FacturaDTO dto);
        Task<bool> RegistrarPagoAsync(int codPago, string tipoMetodoPago);
    }

    public class FacturaService : IFacturaService
    {
        private readonly IFacturaRepository _facturaRepository;

        public FacturaService(IFacturaRepository facturaRepository)
        {
            _facturaRepository = facturaRepository;
        }

        public async Task<FacturaDTO?> GetAsync(int codPago)
        {
            var p = await _facturaRepository.GetAsync(codPago);
            return p == null ? null : MapToDTO(p);
        }

        public async Task<FacturaDTO?> GetByCodAtencionAsync(int codAtencion)
        {
            var p = await _facturaRepository.GetByCodAtencionAsync(codAtencion);
            return p == null ? null : MapToDTO(p);
        }

        public async Task<IEnumerable<FacturaDTO>> GetAllAsync()
        {
            var list = await _facturaRepository.GetAllAsync();
            return list.Select(MapToDTO).ToList();
        }

        public async Task<IEnumerable<FacturaDTO>> GetByPacienteDocAsync(string tipoDoc, int nroDoc)
        {
            var list = await _facturaRepository.GetByPacienteDocAsync(tipoDoc, nroDoc);
            return list.Select(MapToDTO).ToList();
        }

        public async Task<FacturaDTO> CrearFacturaAsync(FacturaDTO dto)
        {
            var pago = new Pago(
                idPago: 0,
                nroTurno: dto.CodTurno ?? dto.CodAtencion ?? 1,
                fechaHoraPago: dto.FechaYHoraPago,
                monto: dto.Monto,
                metodoPago: dto.TipoMetodoPago
            );

            await _facturaRepository.AddAsync(pago);
            return MapToDTO(pago);
        }

        public async Task<bool> RegistrarPagoAsync(int codPago, string tipoMetodoPago)
        {
            var pago = await _facturaRepository.GetAsync(codPago);
            if (pago == null) return false;

            pago.SetMetodoPago(string.IsNullOrWhiteSpace(tipoMetodoPago) ? "EFECTIVO" : tipoMetodoPago);
            return await _facturaRepository.UpdateAsync(pago);
        }

        private static FacturaDTO MapToDTO(Pago p)
        {
            return new FacturaDTO
            {
                CodPago = p.IdPago,
                CodAtencion = p.CodAtencion,
                Monto = p.Monto,
                TipoMetodoPago = p.TipoMetodoPago,
                FechaYHoraPago = p.FechaHoraPago
            };
        }
    }
}
