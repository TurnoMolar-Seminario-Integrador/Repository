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

            if (await turnoRepository.TurnoExistsAsync(fechaReserva, "DNI", dto.OdontologoNroDoc ?? 0))
            {
                throw new ArgumentException($"Ya existe un turno para ese odontólogo en la fecha/hora indicada.");
            }

            var turno = new Turno(
                0,
                fechaReserva,
                dto.ModalidadPago ?? "PARTICULAR",
                dto.CodEspecialidad ?? 1,
                "DNI",
                dto.OdontologoNroDoc ?? 0,
                "DNI",
                dto.PacienteNroDoc ?? 0,
                dto.EstadoTurno ?? "RESERVADO"
            );

            await turnoRepository.AddAsync(turno);
            return MapToDTO(turno);
        }

        public async Task<bool> DeleteAsync(int codTurno)
        {
            return await turnoRepository.DeleteAsync(codTurno);
        }

        public async Task<TurnoOdontologicoDTO?> GetAsync(int codTurno)
        {
            var turno = await turnoRepository.GetAsync(codTurno);
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
                dto.OdontologoNroDoc ?? 0,
                "DNI",
                dto.PacienteNroDoc ?? 0,
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
                turnos = turnos.Where(t => t.Estado == estadoBuscado);
            }

            return turnos.Select(MapToDTO).ToList();
        }

        private static TurnoOdontologicoDTO MapToDTO(Turno turno)
        {
            return new TurnoOdontologicoDTO
            {
                Id = turno.CodTurno,
                Fecha = turno.FechaYHoraReserva.Date,
                HorarioTurno = TimeOnly.FromDateTime(turno.FechaYHoraReserva),
                EstadoTurno = turno.Estado,
                MotivoCancelacion = turno.MotivoCancelacion,
                ModalidadPago = turno.ModalidadPagoElegida,
                CodEspecialidad = turno.CodEspecialidad,
                OdontologoNroDoc = turno.OdontologoNroDoc,
                PacienteNroDoc = turno.PacienteNroDoc
            };
        }
    }
}