using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    public interface IOdontologoService
    {
        Task<OdontologoDTO?> GetAsync(int id);
        Task<IEnumerable<OdontologoDTO>> GetAllAsync();
        Task<IEnumerable<OdontologoDTO>> GetByEspecialidadAsync(int especialidadId);
        Task<OdontologoDTO> AddAsync(OdontologoDTO dto);
        Task<bool> UpdateAsync(OdontologoDTO dto);
        Task<bool> DeleteAsync(int id);
    }

    public class OdontologoService : IOdontologoService
    {
        private readonly IOdontologoRepository _odontologoRepository;

        public OdontologoService(IOdontologoRepository odontologoRepository)
        {
            _odontologoRepository = odontologoRepository;
        }

        public async Task<OdontologoDTO?> GetAsync(int id)
        {
            var odontologo = await _odontologoRepository.GetAsync(id);
            return odontologo == null ? null : MapToDTO(odontologo);
        }

        public async Task<IEnumerable<OdontologoDTO>> GetAllAsync()
        {
            var list = await _odontologoRepository.GetAllAsync();
            return list.Select(MapToDTO).ToList();
        }

        public async Task<IEnumerable<OdontologoDTO>> GetByEspecialidadAsync(int especialidadId)
        {
            var list = await _odontologoRepository.GetByEspecialidadAsync(especialidadId);
            return list.Select(MapToDTO).ToList();
        }

        public async Task<OdontologoDTO> AddAsync(OdontologoDTO dto)
        {
            if (await _odontologoRepository.MatriculaExistsAsync(dto.NumMatricula))
            {
                throw new InvalidOperationException($"Ya existe un profesional con la matrícula {dto.NumMatricula}.");
            }

            var odontologo = new Odontologo(
                0,
                dto.NumMatricula,
                dto.Nombre,
                dto.Apellido,
                dto.Dni,
                dto.Telefono,
                dto.Mail,
                dto.Domicilio,
                dto.EspecialidadId > 0 ? dto.EspecialidadId : 1
            );

            await _odontologoRepository.AddAsync(odontologo);
            return MapToDTO(odontologo);
        }

        public async Task<bool> UpdateAsync(OdontologoDTO dto)
        {
            if (await _odontologoRepository.MatriculaExistsAsync(dto.NumMatricula, dto.Id))
            {
                throw new InvalidOperationException($"Ya existe otro profesional con la matrícula {dto.NumMatricula}.");
            }

            var odontologo = new Odontologo(
                dto.Id,
                dto.NumMatricula,
                dto.Nombre,
                dto.Apellido,
                dto.Dni,
                dto.Telefono,
                dto.Mail,
                dto.Domicilio,
                dto.EspecialidadId
            );

            return await _odontologoRepository.UpdateAsync(odontologo);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _odontologoRepository.DeleteAsync(id);
        }

        private static OdontologoDTO MapToDTO(Odontologo o)
        {
            return new OdontologoDTO
            {
                Id = o.Id,
                NumMatricula = o.NumMatricula,
                Nombre = o.Nombre,
                Apellido = o.Apellido,
                Dni = o.Dni,
                Telefono = o.Telefono,
                Mail = o.Mail,
                Domicilio = o.Domicilio,
                EspecialidadId = o.EspecialidadId,
                EspecialidadNombre = o.Especialidad?.Nombre ?? "Odontología General"
            };
        }
    }
}
