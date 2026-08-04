using Domain.Model;

namespace Data
{
    public class PacienteRepository : IPacienteRepository
    {

        private static readonly List<Paciente> pacientes = new();

        private static int nextId = 1;

        public Task<Paciente> AddAsync(Paciente paciente)
        {
            paciente.SetId(nextId);
            nextId++;
            pacientes.Add(paciente);
            return Task.FromResult(paciente);
        }

        public Task<bool> DeleteAsync(int id)
        {
            var paciente = pacientes.FirstOrDefault(p => p.Id == id);
            if (paciente != null)
            {
                pacientes.Remove(paciente);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<Paciente?> GetAsync(int id)
        {
            var paciente = pacientes.FirstOrDefault(p => p.Id == id);
            return Task.FromResult(paciente);
        }

        public Task<IEnumerable<Paciente>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<Paciente>>(pacientes);
        }

        public Task<bool> UpdateAsync(Paciente paciente)
        {
            var existingPaciente = pacientes.FirstOrDefault(p => p.Id == paciente.Id);
            if (existingPaciente == null)
                return Task.FromResult(false);

            existingPaciente.SetNom(paciente.Nombre);
            existingPaciente.SetApe(paciente.Apellido);
            existingPaciente.SetDni(paciente.Dni);
            existingPaciente.SetTel(paciente.Telefono);
            existingPaciente.SetMail(paciente.Mail);
            existingPaciente.SetDom(paciente.Domicilio);
            existingPaciente.SetEstadoHabilitado(paciente.EstadoHabilitado);

            return Task.FromResult(true);
        }

        public Task<bool> EmailExistsAsync(string email, int? excludeId = null)
        {
            var exists = pacientes.Any(p =>
                p.Mail.ToLower() == email.ToLower() &&
                (!excludeId.HasValue || p.Id != excludeId.Value));
            return Task.FromResult(exists);
        }

        public Task<IEnumerable<Paciente>> GetByCriteriaAsync(PacienteCriteria criteria)
        {
            var texto = criteria.Texto?.ToLower() ?? string.Empty;
            var result = pacientes.Where(p =>
                p.Nombre.ToLower().Contains(texto) ||
                p.Apellido.ToLower().Contains(texto) ||
                p.Mail.ToLower().Contains(texto));
            return Task.FromResult<IEnumerable<Paciente>>(result);
        }
    }

}