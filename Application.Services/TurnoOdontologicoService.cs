using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    public class TurnoOdontologicoService : ITurnoOdontologicoService
    {
        private readonly ITurnoRepository turnoRepository;

        public TurnoOdontologicoService(ITurnoRepository turnoRepository)
        {
            this.turnoRepository = turnoRepository;
        }

        public async Task<TurnoOdontologicoDTO> AddAsync(TurnoOdontologicoDTO dto)
        {
            var fechaReserva = dto.Fecha.Add(dto.HorarioTurno.ToTimeSpan());

            if (await turnoRepository.TurnoExistsAsync(fechaReserva, "DNI", dto.OdontologoNroDoc?.ToString() ?? "0"))
            {
                throw new ArgumentException($"Ya existe un turno para ese odontólogo en la fecha/hora indicada.");
            }

            var turno = new Turno(
                0,
                fechaReserva,
                dto.ModalidadPago ?? "PARTICULAR",
                dto.CodEspecialidad ?? 1,
                "DNI",
                dto.OdontologoNroDoc?.ToString() ?? "0",
                "DNI",
                dto.PacienteNroDoc?.ToString() ?? "0",
                dto.EstadoTurno ?? "RESERVADO"
            );

            await turnoRepository.AddAsync(turno);
            return MapToDTO(turno);
        }

        public async Task<bool> DeleteAsync(int nroTurno)
        {
            return await turnoRepository.DeleteAsync(nroTurno);
        }

        public async Task<TurnoOdontologicoDTO?> GetAsync(int nroTurno)
        {
            var turno = await turnoRepository.GetAsync(nroTurno);
            return turno == null ? null : MapToDTO(turno);
        }

        public async Task<IEnumerable<TurnoOdontologicoDTO>> GetAllAsync()
        {
            var turnos = await turnoRepository.GetAllAsync();
            return turnos.Select(MapToDTO).ToList();
        }

        public async Task<bool> UpdateAsync(TurnoOdontologicoDTO dto)
        {
            var turno = new Turno(
                dto.Id,
                dto.Fecha.Add(dto.HorarioTurno.ToTimeSpan()),
                dto.ModalidadPago ?? "PARTICULAR",
                dto.CodEspecialidad ?? 1,
                "DNI",
                dto.OdontologoNroDoc?.ToString() ?? "0",
                "DNI",
                dto.PacienteNroDoc?.ToString() ?? "0",
                dto.EstadoTurno ?? "RESERVADO"
            );

            return await turnoRepository.UpdateAsync(turno);
        }

        public async Task<IEnumerable<TurnoOdontologicoDTO>> GetByCriteriaAsync(TurnoCriteriaDTO criteriaDTO)
        {
            IEnumerable<Turno> turnos = criteriaDTO.Fecha.HasValue
                ? await turnoRepository.GetByFechaAsync(criteriaDTO.Fecha.Value)
                : await turnoRepository.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(criteriaDTO.EstadoTurno))
            {
                var estadoBuscado = criteriaDTO.EstadoTurno.ToUpper().Trim();
                turnos = turnos.Where(t => t.EstadoTurno == estadoBuscado);
            }

            return turnos.Select(MapToDTO).ToList();
        }

        private static TurnoOdontologicoDTO MapToDTO(Turno turno)
        {
            return new TurnoOdontologicoDTO
            {
                Id = turno.NroTurno,
                Fecha = turno.FechaHoraTurno.Date,
                HorarioTurno = TimeOnly.FromDateTime(turno.FechaHoraTurno),
                EstadoTurno = turno.EstadoTurno,
                MotivoCancelacion = turno.MotivoCancelacion,
                ModalidadPago = turno.ModalidadPagoElegida,
                CodEspecialidad = turno.IdEspecialidad,
                OdontologoNroDoc = int.TryParse(turno.NroDocumentoOdontologo, out var doc) ? doc : 0,
                PacienteNroDoc = int.TryParse(turno.NroDocumentoPaciente, out var pac) ? pac : 0,
                NombrePaciente = turno.Paciente != null ? $"{turno.Paciente.Nombre} {turno.Paciente.Apellido}" : null,
                NombreOdontologo = turno.Odontologo != null ? $"Dr/a. {turno.Odontologo.Nombre} {turno.Odontologo.Apellido}" : null,
                NombreEspecialidad = turno.Especialidad?.Nombre,
                FechaCancelacion = turno.FechaHoraCancelacion,
                FechaSolicitudReprogramacion = turno.FechaHoraReprogramacion
            };
        }
    }
}