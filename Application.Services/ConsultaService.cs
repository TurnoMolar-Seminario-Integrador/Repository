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
        private readonly IHistoriaClinicaRepository _historiaClinicaRepository;
        private readonly IInsumoRepository _insumoRepository;

        public ConsultaService(
            IConsultaRepository consultaRepository,
            ITurnoRepository turnoRepository,
            IHistoriaClinicaRepository historiaClinicaRepository,
            IInsumoRepository insumoRepository)
        {
            _consultaRepository = consultaRepository;
            _turnoRepository = turnoRepository;
            _historiaClinicaRepository = historiaClinicaRepository;
            _insumoRepository = insumoRepository;
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
                throw new InvalidOperationException($"No se encontró el turno con código {dto.CodTurno}.");

            var hc = await _historiaClinicaRepository.GetByPacienteDocAsync(dto.PacienteTipoDoc, dto.PacienteNroDoc);
            int nroHC = hc?.NroHC ?? 1;

            var atencion = new AtencionOdontologica(
                0,
                dto.FechaYHoraAtencionInicio,
                dto.FechaYHoraAtencionFin,
                dto.Observaciones,
                0m,
                dto.CodTurno,
                nroHC
            );

            await _consultaRepository.AddAsync(atencion);

            // Descontar insumos utilizados si los hay
            if (dto.InsumosUtilizados != null && dto.InsumosUtilizados.Any())
            {
                foreach (var ins in dto.InsumosUtilizados)
                {
                    try
                    {
                        var insumoDb = await _insumoRepository.GetAsync(ins.InsumoId);
                        if (insumoDb != null)
                        {
                            insumoDb.DescontarStock(ins.Cantidad);
                            await _insumoRepository.UpdateAsync(insumoDb);
                        }
                    }
                    catch { /* log warning if insufficient stock */ }
                }
            }

            dto.CodAtencion = atencion.IdAtencion;
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
                CodAtencion = a.IdAtencion,
                CodTurno = a.NroTurno,
                PacienteTipoDoc = a.PacienteTipoDoc,
                PacienteNroDoc = int.TryParse(a.PacienteNroDoc, out var doc) ? doc : 0,
                PacienteNombre = a.Turno?.Paciente != null ? $"{a.Turno.Paciente.Apellido}, {a.Turno.Paciente.Nombre}" : "Paciente",
                OdontologoNombre = a.Turno?.Odontologo != null ? $"Dr/a. {a.Turno.Odontologo.Apellido}, {a.Turno.Odontologo.Nombre}" : "Odontólogo",
                Observaciones = a.Observaciones,
                FechaYHoraAtencionInicio = a.FechaHoraAtencionInicio,
                FechaYHoraAtencionFin = a.FechaHoraAtencionFin
            };
        }
    }
}
