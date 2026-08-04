using Data;
using DentalClinic.Application.Services;
using Domain.Model;
using DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            // Validar que el email no esté duplicado
            if (await pacienteRepository.EmailExistsAsync(dto.Email))
            {
                throw new ArgumentException($"Ya existe un paciente con el Email '{dto.Email}'.");
            }

            var paciente = new Paciente(
                0,
                dto.Nombre,
                dto.Apellido,
                dto.Dni,
                dto.Telefono,
                dto.Email,
                dto.Domicilio,
                true
            );

            await pacienteRepository.AddAsync(paciente);

            dto.Id = paciente.Id;
            dto.EstadoHabilitado = paciente.EstadoHabilitado;

            return dto;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await pacienteRepository.DeleteAsync(id);
        }

        public async Task<PacienteDTO?> GetAsync(int id)
        {
            Paciente? paciente = await pacienteRepository.GetAsync(id);
            if (paciente == null)
                return null;

            return MapToDTO(paciente);
        }

        public async Task<IEnumerable<PacienteDTO>> GetAllAsync()
        {
            var pacientes = await pacienteRepository.GetAllAsync();
            return pacientes.Select(MapToDTO).ToList();
        }

        public async Task<bool> UpdateAsync(PacienteDTO dto)
        {
            // Validar que el email no esté duplicado (excluyendo el paciente actual)
            if (await pacienteRepository.EmailExistsAsync(dto.Email, dto.Id))
            {
                throw new ArgumentException($"Ya existe otro paciente con el Email '{dto.Email}'.");
            }

            Paciente paciente = new Paciente(
                dto.Id,
                dto.Nombre,
                dto.Apellido,
                dto.Dni,
                dto.Telefono,
                dto.Email,
                dto.Domicilio,
                dto.EstadoHabilitado
            );

            return await pacienteRepository.UpdateAsync(paciente);
        }

        public async Task<IEnumerable<PacienteDTO>> GetByCriteriaAsync(PacienteCriteriaDTO criteriaDTO)
        {
            var criteria = new PacienteCriteria(criteriaDTO.Texto);
            var pacientes = await pacienteRepository.GetByCriteriaAsync(criteria);
            return pacientes.Select(MapToDTO).ToList();
        }

        private static PacienteDTO MapToDTO(Paciente paciente)
        {
            return new PacienteDTO
            {
                Id = paciente.Id,
                Nombre = paciente.Nombre,
                Apellido = paciente.Apellido,
                Dni = paciente.Dni,
                Telefono = paciente.Telefono,
                Email = paciente.Mail, 
                Domicilio = paciente.Domicilio,
                EstadoHabilitado = paciente.EstadoHabilitado
            };
        }
    }
}