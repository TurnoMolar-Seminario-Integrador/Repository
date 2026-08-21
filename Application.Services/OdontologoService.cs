using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    public interface IOdontologoService
    {
        Task<OdontologoDTO?> GetAsync(string tipoDocumento, int nroDocumento);
        Task<IEnumerable<OdontologoDTO>> GetAllAsync();
        Task<IEnumerable<OdontologoDTO>> GetByEspecialidadAsync(int codEspecialidad);
        Task<OdontologoDTO> AddAsync(OdontologoDTO dto);
        Task<bool> UpdateAsync(OdontologoDTO dto);
        Task<bool> DeleteAsync(string tipoDocumento, int nroDocumento);
    }

    public class OdontologoService : IOdontologoService
    {
        private readonly IOdontologoRepository _odontologoRepository;

        public OdontologoService(IOdontologoRepository odontologoRepository)
        {
            _odontologoRepository = odontologoRepository;
        }

        public async Task<OdontologoDTO?> GetAsync(string tipoDocumento, int nroDocumento)
        {
            var o = await _odontologoRepository.GetAsync(tipoDocumento, nroDocumento);
            return o == null ? null : MapToDTO(o);
        }

        public async Task<IEnumerable<OdontologoDTO>> GetAllAsync()
        {
            var list = await _odontologoRepository.GetAllAsync();
            return list.Select(MapToDTO).ToList();
        }

        public async Task<IEnumerable<OdontologoDTO>> GetByEspecialidadAsync(int codEspecialidad)
        {
            var list = await _odontologoRepository.GetByEspecialidadAsync(codEspecialidad);
            return list.Select(MapToDTO).ToList();
        }

        public async Task<OdontologoDTO> AddAsync(OdontologoDTO dto)
        {
            if (await _odontologoRepository.MatriculaExistsAsync(dto.Matricula))
                throw new InvalidOperationException($"Ya existe un profesional con la matrícula {dto.Matricula}.");

            var odontologo = new Odontologo(
                tipoDocumento: dto.TipoDocumento ?? "DNI",
                nroDocumento: dto.NroDocumento,
                nombre: dto.Nombre,
                apellido: dto.Apellido,
                telefono: dto.Telefono,
                email: dto.Email,
                domicilio: dto.Domicilio,
                fechaNacimiento: dto.FechaNacimiento,
                matricula: dto.Matricula,
                codEspecialidad: dto.CodEspecialidad
            );

            await _odontologoRepository.AddAsync(odontologo);
            return MapToDTO(odontologo);
        }

        public async Task<bool> UpdateAsync(OdontologoDTO dto)
        {
            var odontologo = new Odontologo(
                tipoDocumento: dto.TipoDocumento ?? "DNI",
                nroDocumento: dto.NroDocumento,
                nombre: dto.Nombre,
                apellido: dto.Apellido,
                telefono: dto.Telefono,
                email: dto.Email,
                domicilio: dto.Domicilio,
                fechaNacimiento: dto.FechaNacimiento,
                matricula: dto.Matricula,
                codEspecialidad: dto.CodEspecialidad
            );

            return await _odontologoRepository.UpdateAsync(odontologo);
        }

        public async Task<bool> DeleteAsync(string tipoDocumento, int nroDocumento)
        {
            return await _odontologoRepository.DeleteAsync(tipoDocumento, nroDocumento);
        }

        private static OdontologoDTO MapToDTO(Odontologo o)
        {
            return new OdontologoDTO
            {
                TipoDocumento = o.TipoDocumento,
                NroDocumento = o.NroDocumento,
                Nombre = o.Nombre,
                Apellido = o.Apellido,
                Telefono = o.Telefono,
                Email = o.Email,
                Domicilio = o.Domicilio,
                FechaNacimiento = o.FechaNacimiento,
                Matricula = o.Matricula,
                EstadoOdontologo = o.EstadoOdontologo,
                CodEspecialidad = o.CodEspecialidad,
                NombreEspecialidad = o.Especialidad?.Nombre ?? "Odontología General"
            };
        }
    }
}
