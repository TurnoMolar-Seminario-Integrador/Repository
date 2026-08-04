using Data;
using DentalClinic.Application.Services;
using Domain.Model;
using DTOs;
using static Domain.Model.TurnoOdontologico;

namespace Application.Services
{
    public class TurnoOdontologicoService : ITurnoOdontologicoService
    {
        private readonly ITurnoRepository turnoRepository;

        public TurnoOdontologicoService(ITurnoRepository turnoRepository)
        {
            this.turnoRepository = turnoRepository;
        }

        private static Estadoturno ParseEstado(string estado)
        {
            if (!Enum.TryParse<Estadoturno>(estado, ignoreCase: true, out var resultado))
            {
                throw new ArgumentException($"El estado '{estado}' no es válido. Valores permitidos: {string.Join(", ", Enum.GetNames<Estadoturno>())}.");
            }
            return resultado;
        }

        public async Task<TurnoOdontologicoDTO> AddAsync(TurnoOdontologicoDTO dto)
        {
            if (await turnoRepository.TurnoExistsAsync(dto.Fecha, dto.HorarioTurno))
            {
                throw new ArgumentException($"Ya existe un turno para el {dto.Fecha} en el horario '{dto.HorarioTurno}'.");
            }

            var turno = new TurnoOdontologico(
                0,
                dto.Fecha,
                dto.HorarioTurno,
                ParseEstado(dto.EstadoTurno),
                dto.MotivoCancelacion
            );

            await turnoRepository.AddAsync(turno);
            return MapToDTO(turno);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await turnoRepository.DeleteAsync(id);
        }

        public async Task<TurnoOdontologicoDTO?> GetAsync(int id)
        {
            var turno = await turnoRepository.GetAsync(id);
            return turno == null ? null : MapToDTO(turno);
        }

        public async Task<IEnumerable<TurnoOdontologicoDTO>> GetAllAsync()
        {
            var turnos = await turnoRepository.GetAllAsync();
            return turnos.Select(MapToDTO).ToList();
        }

        public async Task<bool> UpdateAsync(TurnoOdontologicoDTO dto)
        {
            if (await turnoRepository.TurnoExistsAsync(dto.Fecha, dto.HorarioTurno, dto.Id))
            {
                throw new ArgumentException($"Ya existe otro turno para el {dto.Fecha} en el horario '{dto.HorarioTurno}'.");
            }

            var turno = new TurnoOdontologico(
                dto.Id,
                dto.Fecha,
                dto.HorarioTurno,
                ParseEstado(dto.EstadoTurno),
                dto.MotivoCancelacion
            );

            return await turnoRepository.UpdateAsync(turno);
        }

        public async Task<IEnumerable<TurnoOdontologicoDTO>> GetByCriteriaAsync(TurnoCriteriaDTO criteriaDTO)
        {
            IEnumerable<TurnoOdontologico> turnos = criteriaDTO.Fecha.HasValue
                ? await turnoRepository.GetByFechaAsync(criteriaDTO.Fecha.Value)
                : await turnoRepository.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(criteriaDTO.EstadoTurno))
            {
                var estadoBuscado = ParseEstado(criteriaDTO.EstadoTurno);
                turnos = turnos.Where(t => t.EstadoTurno == estadoBuscado);
            }

            return turnos.Select(MapToDTO).ToList();
        }

        private static TurnoOdontologicoDTO MapToDTO(TurnoOdontologico turno)
        {
            return new TurnoOdontologicoDTO
            {
                Id = turno.Id,
                Fecha = turno.Fecha,
                HorarioTurno = turno.HorarioTurno,
                EstadoTurno = turno.EstadoTurno.ToString(),
                MotivoCancelacion = turno.MotivoCancelacion
            };
        }
    }
}