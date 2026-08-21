using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    public interface IConsultaService
    {
        Task<ConsultaDTO?> GetAsync(int codAtencion);
        Task<ConsultaDTO?> GetByCodTurnoAsync(int codTurno);
        Task<IEnumerable<ConsultaDTO>> GetAllAsync();
        Task<IEnumerable<ConsultaDTO>> GetByPacienteDocAsync(string tipoDoc, int nroDoc);
        Task<ConsultaDTO> RegistrarConsultaAsync(ConsultaDTO dto);
        Task<bool> ValorarAtencionAsync(int codAtencion, int estrellas, string comentario);
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

        public async Task<ConsultaDTO?> GetAsync(int codAtencion)
        {
            var a = await _consultaRepository.GetAsync(codAtencion);
            return a == null ? null : MapToDTO(a);
        }

        public async Task<ConsultaDTO?> GetByCodTurnoAsync(int codTurno)
        {
            var a = await _consultaRepository.GetByCodTurnoAsync(codTurno);
            return a == null ? null : MapToDTO(a);
        }

        public async Task<IEnumerable<ConsultaDTO>> GetAllAsync()
        {
            var list = await _consultaRepository.GetAllAsync();
            return list.Select(MapToDTO).ToList();
        }

        public async Task<IEnumerable<ConsultaDTO>> GetByPacienteDocAsync(string tipoDoc, int nroDoc)
        {
            var list = await _consultaRepository.GetByPacienteDocAsync(tipoDoc, nroDoc);
            return list.Select(MapToDTO).ToList();
        }

        public async Task<ConsultaDTO> RegistrarConsultaAsync(ConsultaDTO dto)
        {
            var turno = await _turnoRepository.GetAsync(dto.CodTurno);
            if (turno == null)
            {
                throw new InvalidOperationException("El turno asociado no existe.");
            }

            turno.SetEstado("ATENDIDO");
            await _turnoRepository.UpdateAsync(turno);

            DateTime fechaInicio = dto.FechaYHoraAtencionInicio;
            DateTime fechaFin = dto.FechaYHoraAtencionFin > fechaInicio ? dto.FechaYHoraAtencionFin : fechaInicio.AddMinutes(30);

            var atencion = new AtencionOdontologica(
                codAtencion: 0,
                fechaInicio: fechaInicio,
                fechaFin: fechaFin,
                observaciones: dto.Observaciones,
                codTurno: dto.CodTurno,
                fechaYHoraReserva: turno.FechaYHoraReserva,
                nroHC: dto.PacienteNroDoc,
                pacienteTipoDoc: dto.PacienteTipoDoc ?? "DNI",
                pacienteNroDoc: dto.PacienteNroDoc
            );

            await _consultaRepository.AddAsync(atencion);

            if (dto.InsumosUtilizados != null && dto.InsumosUtilizados.Any())
            {
                foreach (var itemDto in dto.InsumosUtilizados)
                {
                    if (itemDto.InsumoId > 0 && itemDto.Cantidad > 0)
                    {
                        await _insumoRepository.DescontarStockAsync(itemDto.InsumoId, itemDto.Cantidad);
                    }
                }
            }

            dto.CodAtencion = atencion.CodAtencion;
            return dto;
        }

        public async Task<bool> ValorarAtencionAsync(int codAtencion, int estrellas, string comentario)
        {
            var atencion = await _consultaRepository.GetAsync(codAtencion);
            if (atencion == null) return false;

            atencion.Observaciones = $"{atencion.Observaciones} | Valoración ({estrellas}★): {comentario}";
            return await _consultaRepository.UpdateAsync(atencion);
        }

        private static ConsultaDTO MapToDTO(AtencionOdontologica a)
        {
            return new ConsultaDTO
            {
                CodAtencion = a.CodAtencion,
                CodTurno = a.CodTurno,
                PacienteTipoDoc = a.PacienteTipoDoc,
                PacienteNroDoc = a.PacienteNroDoc,
                PacienteNombre = a.Turno?.Paciente != null ? $"{a.Turno.Paciente.Apellido}, {a.Turno.Paciente.Nombre}" : "Paciente",
                OdontologoNombre = a.Turno?.Odontologo != null ? $"Dr/a. {a.Turno.Odontologo.Apellido}, {a.Turno.Odontologo.Nombre}" : "Odontólogo",
                Observaciones = a.Observaciones,
                FechaYHoraAtencionInicio = a.FechaYHoraAtencionInicio,
                FechaYHoraAtencionFin = a.FechaYHoraAtencionFin
            };
        }
    }
}
