using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    public interface IOdontologoService
    {
        Task<OdontologoDTO?> GetAsync(string tipoDocumento, string nroDocumento);
        Task<OdontologoDTO?> GetAsync(string tipoDocumento, int nroDocumento);
        Task<IEnumerable<OdontologoDTO>> GetAllAsync();
        Task<IEnumerable<OdontologoDTO>> GetByEspecialidadAsync(int idEspecialidad);
        Task<OdontologoDTO> AddAsync(OdontologoDTO dto);
        Task<bool> UpdateAsync(OdontologoDTO dto);
        Task<bool> DeleteAsync(string tipoDocumento, string nroDocumento);
        Task<bool> DeleteAsync(string tipoDocumento, int nroDocumento);
    }

    public class OdontologoService : IOdontologoService
    {
        private readonly IOdontologoRepository _odontologoRepository;

        public OdontologoService(IOdontologoRepository odontologoRepository)
        {
            _odontologoRepository = odontologoRepository;
        }

        public async Task<OdontologoDTO?> GetAsync(string tipoDocumento, string nroDocumento)
        {
            var o = await _odontologoRepository.GetAsync(tipoDocumento, nroDocumento);
            return o == null ? null : MapToDTO(o);
        }

        public async Task<OdontologoDTO?> GetAsync(string tipoDocumento, int nroDocumento)
        {
            return await GetAsync(tipoDocumento, nroDocumento.ToString());
        }

        public async Task<IEnumerable<OdontologoDTO>> GetAllAsync()
        {
            var list = await _odontologoRepository.GetAllAsync();
            return list.Select(MapToDTO).ToList();
        }

        public async Task<IEnumerable<OdontologoDTO>> GetByEspecialidadAsync(int idEspecialidad)
        {
            var list = await _odontologoRepository.GetByEspecialidadAsync(idEspecialidad);
            return list.Select(MapToDTO).ToList();
        }

        public async Task<OdontologoDTO> AddAsync(OdontologoDTO dto)
        {
            if (await _odontologoRepository.MatriculaExistsAsync(dto.Matricula))
                throw new InvalidOperationException($"Ya existe un profesional con la matrícula {dto.Matricula}.");

            var odontologo = new Odontologo(
                tipoDocumento: dto.TipoDocumento ?? "DNI",
                nroDocumento: dto.NroDocumento,
                matricula: dto.Matricula,
                nombre: dto.Nombre,
                apellido: dto.Apellido,
                fechaNacimiento: dto.FechaNacimiento,
                telefono: dto.Telefono,
                email: dto.Email,
                domicilio: dto.Domicilio,
                estadoOdontologo: dto.EstadoOdontologo ?? "ACTIVO"
            );

            await _odontologoRepository.AddAsync(odontologo);
            return MapToDTO(odontologo);
        }

        public async Task<bool> UpdateAsync(OdontologoDTO dto)
        {
            var odontologo = new Odontologo(
                tipoDocumento: dto.TipoDocumento ?? "DNI",
                nroDocumento: dto.NroDocumento,
                matricula: dto.Matricula,
                nombre: dto.Nombre,
                apellido: dto.Apellido,
                fechaNacimiento: dto.FechaNacimiento,
                telefono: dto.Telefono,
                email: dto.Email,
                domicilio: dto.Domicilio,
                estadoOdontologo: dto.EstadoOdontologo ?? "ACTIVO"
            );

            return await _odontologoRepository.UpdateAsync(odontologo);
        }

        public async Task<bool> DeleteAsync(string tipoDocumento, string nroDocumento)
        {
            return await _odontologoRepository.DeleteAsync(tipoDocumento, nroDocumento);
        }

        public async Task<bool> DeleteAsync(string tipoDocumento, int nroDocumento)
        {
            return await DeleteAsync(tipoDocumento, nroDocumento.ToString());
        }

        private static OdontologoDTO MapToDTO(Odontologo o)
        {
            var especialidadPrincipal = o.DisponibilidadesHorarias.FirstOrDefault()?.Especialidad;
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
                CodEspecialidad = especialidadPrincipal?.IdEspecialidad ?? 1,
                NombreEspecialidad = especialidadPrincipal?.Nombre ?? "Odontología General"
            };
        }
    }
}
