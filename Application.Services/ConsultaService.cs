using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    public interface IConsultaService
    {
        Task<ConsultaDTO?> GetAsync(int id);
        Task<ConsultaDTO?> GetByTurnoIdAsync(int turnoId);
        Task<IEnumerable<ConsultaDTO>> GetAllAsync();
        Task<IEnumerable<ConsultaDTO>> GetByPacienteIdAsync(int pacienteId);
        Task<ConsultaDTO> RegistrarConsultaAsync(ConsultaDTO dto);
        Task<bool> ValorarAtencionAsync(int id, int estrellas, string comentario);
    }

    public class ConsultaService : IConsultaService
    {
        private readonly IConsultaRepository _consultaRepository;
        private readonly ITurnoRepository _turnoRepository;
        private readonly IInsumoRepository _insumoRepository;
        private readonly IFacturaRepository _facturaRepository;
        private readonly IPacienteRepository _pacienteRepository;

        public ConsultaService(
            IConsultaRepository consultaRepository,
            ITurnoRepository turnoRepository,
            IInsumoRepository insumoRepository,
            IFacturaRepository facturaRepository,
            IPacienteRepository pacienteRepository)
        {
            _consultaRepository = consultaRepository;
            _turnoRepository = turnoRepository;
            _insumoRepository = insumoRepository;
            _facturaRepository = facturaRepository;
            _pacienteRepository = pacienteRepository;
        }

        public async Task<ConsultaDTO?> GetAsync(int id)
        {
            var c = await _consultaRepository.GetAsync(id);
            return c == null ? null : MapToDTO(c);
        }

        public async Task<ConsultaDTO?> GetByTurnoIdAsync(int turnoId)
        {
            var c = await _consultaRepository.GetByTurnoIdAsync(turnoId);
            return c == null ? null : MapToDTO(c);
        }

        public async Task<IEnumerable<ConsultaDTO>> GetAllAsync()
        {
            var list = await _consultaRepository.GetAllAsync();
            return list.Select(MapToDTO).ToList();
        }

        public async Task<IEnumerable<ConsultaDTO>> GetByPacienteIdAsync(int pacienteId)
        {
            var list = await _consultaRepository.GetByPacienteIdAsync(pacienteId);
            return list.Select(MapToDTO).ToList();
        }

        public async Task<ConsultaDTO> RegistrarConsultaAsync(ConsultaDTO dto)
        {
            var turno = await _turnoRepository.GetAsync(dto.TurnoId);
            if (turno == null)
            {
                throw new InvalidOperationException("El turno asociado no existe.");
            }

            // Marcar el turno como Atendido
            turno.EstadoTurno = TurnoOdontologico.EstadoTurnoEnum.Atendido;
            await _turnoRepository.UpdateAsync(turno);

            var consulta = new Consulta(
                0,
                dto.TurnoId,
                dto.Diagnostico,
                dto.Tratamiento,
                dto.Observaciones,
                dto.AnestesiaLocal,
                dto.Radiografias,
                dto.Valoracion,
                dto.CalificacionEstrellas
            );

            await _consultaRepository.AddAsync(consulta);

            // Descontar stock de insumos utilizados y crear factura proforma
            decimal totalInsumos = 0m;
            var facturaItems = new List<ItemFactura>();

            if (dto.InsumosUtilizados != null && dto.InsumosUtilizados.Any())
            {
                foreach (var itemDto in dto.InsumosUtilizados)
                {
                    if (itemDto.InsumoId > 0 && itemDto.CantidadInsumo > 0)
                    {
                        var insumo = await _insumoRepository.GetAsync(itemDto.InsumoId);
                        if (insumo != null)
                        {
                            await _insumoRepository.DescontarStockAsync(itemDto.InsumoId, itemDto.CantidadInsumo);
                            var sub = insumo.Precio * itemDto.CantidadInsumo;
                            totalInsumos += sub;
                            facturaItems.Add(new ItemFactura(0, 0, itemDto.InsumoId, itemDto.CantidadInsumo, insumo.Precio));
                        }
                    }
                }
            }

            // Generar factura inicial / proforma para cobro
            var paciente = await _pacienteRepository.GetAsync(turno.PacienteId);
            decimal honorarios = turno.MontoEstimado > 0 ? turno.MontoEstimado : 12000m;
            decimal subtotal = honorarios + totalInsumos;
            decimal coberturaOS = paciente?.ObraSocial?.PorcentajeCobertura ?? 0m;
            decimal descuento = subtotal * coberturaOS;
            decimal montoPaciente = subtotal - descuento;

            var factura = new Factura(
                0,
                turno.Id,
                turno.PacienteId,
                $"Atención Odontológica - {dto.Tratamiento} (Diag: {dto.Diagnostico})",
                subtotal,
                descuento,
                subtotal,
                montoPaciente,
                false,
                "Pendiente de Pago"
            );
            factura.Items = facturaItems;

            await _facturaRepository.AddAsync(factura);

            dto.Id = consulta.Id;
            return dto;
        }

        public async Task<bool> ValorarAtencionAsync(int id, int estrellas, string comentario)
        {
            var consulta = await _consultaRepository.GetAsync(id);
            if (consulta == null) return false;

            consulta.CalificacionEstrellas = Math.Clamp(estrellas, 1, 5);
            consulta.Valoracion = comentario;
            return await _consultaRepository.UpdateAsync(consulta);
        }

        private static ConsultaDTO MapToDTO(Consulta c)
        {
            return new ConsultaDTO
            {
                Id = c.Id,
                TurnoId = c.TurnoId,
                PacienteId = c.Turno?.PacienteId ?? 0,
                PacienteNombre = c.Turno?.Paciente != null ? $"{c.Turno.Paciente.Apellido}, {c.Turno.Paciente.Nombre}" : "Paciente",
                OdontologoNombre = c.Turno?.Odontologo != null ? $"Dr/a. {c.Turno.Odontologo.Apellido}, {c.Turno.Odontologo.Nombre}" : "Odontólogo",
                Diagnostico = c.Diagnostico,
                Tratamiento = c.Tratamiento,
                Observaciones = c.Observaciones,
                AnestesiaLocal = c.AnestesiaLocal,
                Radiografias = c.Radiografias,
                Valoracion = c.Valoracion,
                CalificacionEstrellas = c.CalificacionEstrellas,
                Fecha = c.Fecha
            };
        }
    }
}
