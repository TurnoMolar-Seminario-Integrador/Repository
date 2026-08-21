using Domain.Model;

namespace Data
{
    public class PacienteRepository : IPacienteRepository
    {
        private static readonly List<Paciente> pacientes = new();

        public Task<Paciente> AddAsync(Paciente paciente)
        {
            pacientes.Add(paciente);
            return Task.FromResult(paciente);
        }

        public Task<bool> DeleteAsync(int nroDocumento)
        {
            var paciente = pacientes.FirstOrDefault(p => p.NroDocumento == nroDocumento);
            if (paciente != null)
            {
                pacientes.Remove(paciente);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<Paciente?> GetAsync(int nroDocumento)
        {
            var paciente = pacientes.FirstOrDefault(p => p.NroDocumento == nroDocumento);
            return Task.FromResult(paciente);
        }

        public Task<IEnumerable<Paciente>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<Paciente>>(pacientes);
        }

        public Task<bool> UpdateAsync(Paciente paciente)
        {
            var existing = pacientes.FirstOrDefault(p =>
                p.TipoDocumento == paciente.TipoDocumento && p.NroDocumento == paciente.NroDocumento);
            if (existing == null)
                return Task.FromResult(false);

            existing.SetNombre(paciente.Nombre);
            existing.SetApellido(paciente.Apellido);
            existing.SetNroDocumento(paciente.NroDocumento);
            existing.SetTelefono(paciente.Telefono);
            existing.SetEmail(paciente.Email);
            existing.SetDomicilio(paciente.Domicilio);
            existing.SetEstadoPaciente(paciente.EstadoPaciente);
            existing.SetMontoAdeudado(paciente.MontoAdeudado);
            existing.SetIdentificadorOS(paciente.IdentificadorOS);

            return Task.FromResult(true);
        }

        public Task<bool> EmailExistsAsync(string email, int? excludeNroDoc = null)
        {
            var exists = pacientes.Any(p =>
                p.Email.ToLower() == email.ToLower() &&
                (!excludeNroDoc.HasValue || p.NroDocumento != excludeNroDoc.Value));
            return Task.FromResult(exists);
        }

        public Task<IEnumerable<Paciente>> GetByCriteriaAsync(PacienteCriteria criteria)
        {
            var texto = criteria.Texto?.ToLower() ?? string.Empty;
            var result = pacientes.Where(p =>
                p.Nombre.ToLower().Contains(texto) ||
                p.Apellido.ToLower().Contains(texto) ||
                p.Email.ToLower().Contains(texto));
            return Task.FromResult<IEnumerable<Paciente>>(result);
        }
    }
}