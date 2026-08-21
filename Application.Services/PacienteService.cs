using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    public class PacienteService : IPacienteService
    {
        private readonly IPacienteRepository pacienteRepository;

        public PacienteService(IPacienteRepository pacienteRepository)
        {
            this.pacienteRepository = pacienteRepository;
        }

        public async Task<PacienteDTO> AddAsync(PacienteDTO dto)
        {
            if (await pacienteRepository.EmailExistsAsync(dto.Email))
                throw new ArgumentException($"Ya existe un paciente con el Email '{dto.Email}'.");

            var paciente = new Paciente(
                tipoDocumento: dto.TipoDocumento ?? "DNI",
                nroDocumento: dto.NroDocumento,
                nombre: dto.Nombre,
                apellido: dto.Apellido,
                telefono: dto.Telefono,
                email: dto.Email,
                domicilio: dto.Domicilio,
                fechaNacimiento: dto.FechaNacimiento,
                estadoPaciente: "ACTIVO",
                montoAdeudado: 0,
                identificadorOS: dto.IdentificadorOS
            );

            await pacienteRepository.AddAsync(paciente);
            return MapToDTO(paciente);
        }

        public async Task<bool> DeleteAsync(int nroDocumento)
        {
            return await pacienteRepository.DeleteAsync(nroDocumento);
        }

        public async Task<PacienteDTO?> GetAsync(int nroDocumento)
        {
            var paciente = await pacienteRepository.GetAsync(nroDocumento);
            return paciente == null ? null : MapToDTO(paciente);
        }

        public async Task<IEnumerable<PacienteDTO>> GetAllAsync()
        {
            var pacientes = await pacienteRepository.GetAllAsync();
            return pacientes.Select(MapToDTO).ToList();
        }

        public async Task<bool> UpdateAsync(PacienteDTO dto)
        {
            if (await pacienteRepository.EmailExistsAsync(dto.Email, dto.NroDocumento))
                throw new ArgumentException($"Ya existe otro paciente con el Email '{dto.Email}'.");

            var paciente = new Paciente(
                tipoDocumento: dto.TipoDocumento ?? "DNI",
                nroDocumento: dto.NroDocumento,
                nombre: dto.Nombre,
                apellido: dto.Apellido,
                telefono: dto.Telefono,
                email: dto.Email,
                domicilio: dto.Domicilio,
                fechaNacimiento: dto.FechaNacimiento,
                estadoPaciente: dto.EstadoPaciente ?? "ACTIVO",
                montoAdeudado: dto.MontoAdeudado,
                identificadorOS: dto.IdentificadorOS
            );

            return await pacienteRepository.UpdateAsync(paciente);
        }

        public async Task<IEnumerable<PacienteDTO>> GetByCriteriaAsync(PacienteCriteriaDTO criteriaDTO)
        {
            var criteria = new PacienteCriteria(criteriaDTO.Texto);
            var pacientes = await pacienteRepository.GetByCriteriaAsync(criteria);
            return pacientes.Select(MapToDTO).ToList();
        }

        private static PacienteDTO MapToDTO(Paciente p)
        {
            return new PacienteDTO
            {
                TipoDocumento = p.TipoDocumento,
                NroDocumento = p.NroDocumento,
                Nombre = p.Nombre,
                Apellido = p.Apellido,
                Telefono = p.Telefono,
                Email = p.Email,
                Domicilio = p.Domicilio,
                FechaNacimiento = p.FechaNacimiento,
                EstadoPaciente = p.EstadoPaciente,
                MontoAdeudado = p.MontoAdeudado,
                IdentificadorOS = p.IdentificadorOS
            };
        }
    }
}