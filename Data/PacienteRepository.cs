using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public class PacienteRepository : IPacienteRepository
    {
        private readonly TurnoMolarDbContext _context;

        public PacienteRepository(TurnoMolarDbContext context)
        {
            _context = context;
        }

        public async Task<Paciente> AddAsync(Paciente paciente)
        {
            _context.Pacientes.Add(paciente);
            await _context.SaveChangesAsync();
            return paciente;
        }

        public async Task<bool> DeleteAsync(string tipoDocumento, string nroDocumento)
        {
            var paciente = await _context.Pacientes
                .FirstOrDefaultAsync(p => p.TipoDocumento == tipoDocumento && p.NroDocumento == nroDocumento);
            if (paciente != null)
            {
                _context.Pacientes.Remove(paciente);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> DeleteAsync(int nroDocumento)
        {
            return await DeleteAsync("DNI", nroDocumento.ToString());
        }

        public async Task<Paciente?> GetAsync(string tipoDocumento, string nroDocumento)
        {
            return await _context.Pacientes
                .Include(p => p.ObraSocial)
                .Include(p => p.HistoriaClinica)
                .FirstOrDefaultAsync(p => p.TipoDocumento == tipoDocumento && p.NroDocumento == nroDocumento);
        }

        public async Task<Paciente?> GetAsync(int nroDocumento)
        {
            var strDoc = nroDocumento.ToString();
            return await _context.Pacientes
                .Include(p => p.ObraSocial)
                .Include(p => p.HistoriaClinica)
                .FirstOrDefaultAsync(p => p.NroDocumento == strDoc);
        }

        public async Task<IEnumerable<Paciente>> GetAllAsync()
        {
            return await _context.Pacientes
                .Include(p => p.ObraSocial)
                .Include(p => p.HistoriaClinica)
                .OrderBy(p => p.Apellido)
                .ThenBy(p => p.Nombre)
                .ToListAsync();
        }

        public async Task<bool> UpdateAsync(Paciente paciente)
        {
            var existing = await _context.Pacientes
                .FirstOrDefaultAsync(p => p.TipoDocumento == paciente.TipoDocumento && p.NroDocumento == paciente.NroDocumento);
            if (existing == null)
                return false;

            existing.SetNombre(paciente.Nombre);
            existing.SetApellido(paciente.Apellido);
            // CUF02: la fecha de nacimiento también se rectifica (antes no se copiaba, así que un
            // paciente que llegara como instancia nueva, como en el PUT de la WebAPI, nunca la cambiaba).
            existing.SetFechaNacimiento(paciente.FechaNacimiento);
            existing.SetTelefono(paciente.Telefono);
            existing.SetEmail(paciente.Email);
            existing.SetDomicilio(paciente.Domicilio);
            existing.SetEstadoPaciente(paciente.EstadoPaciente);
            existing.SetIdentificadorOS(paciente.IdentificadorOS);
            existing.SetMontoAdeudado(paciente.MontoAdeudado);
            existing.SetCredenciales(paciente.Clave, paciente.SaltClave, paciente.Rol);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _context.ChangeTracker.Clear();
                throw new ConflictoConcurrenciaException("Los datos del paciente fueron modificados por otro usuario.", ex);
            }

            return true;
        }

        public async Task<HistoriaClinica> AddConHistoriaClinicaAsync(Paciente paciente, HistoriaClinica historiaClinica)
        {
            // Un único SaveChanges: EF Core guarda las dos filas en la misma transacción (primero el
            // paciente, después su historia clínica, que lo referencia).
            _context.Pacientes.Add(paciente);
            _context.HistoriasClinicas.Add(historiaClinica);
            await _context.SaveChangesAsync();
            return historiaClinica;
        }

        public async Task CambiarDocumentoAsync(Paciente paciente, string nuevoTipoDocumento, string nuevoNroDocumento)
        {
            // La clave del paciente (TipoDocumento, NroDocumento) es la FK de su historia clínica y de sus
            // turnos, y EF Core no permite modificar una clave ya guardada. Se resuelve en una sola
            // transacción: se guarda la fila con la clave nueva, se pasan a ella la historia clínica y los
            // turnos, y recién entonces se elimina la fila vieja.
            var tipoViejo = paciente.TipoDocumento;
            var nroViejo = paciente.NroDocumento;

            // La estrategia de reintentos de SQL Server no admite transacciones propias si no es de esta forma.
            var estrategia = _context.Database.CreateExecutionStrategy();
            try
            {
                await estrategia.ExecuteAsync(async () =>
                {
                    await using var transaccion = await _context.Database.BeginTransactionAsync();

                    // Cambios pendientes del paciente (datos personales), todavía bajo la clave vigente.
                    await _context.SaveChangesAsync();

                    // Misma cuenta con la clave nueva: contraseña, rol, alta, estado, deuda y obra social.
                    var nuevo = new Paciente(
                        nuevoTipoDocumento,
                        nuevoNroDocumento,
                        paciente.Nombre,
                        paciente.Apellido,
                        paciente.FechaNacimiento,
                        paciente.Telefono,
                        paciente.Email,
                        paciente.Domicilio,
                        paciente.EstadoPaciente,
                        paciente.IdentificadorOS,
                        paciente.MontoAdeudado,
                        paciente.Clave,
                        paciente.SaltClave,
                        paciente.FechaAlta,
                        paciente.Rol);
                    _context.Pacientes.Add(nuevo);
                    await _context.SaveChangesAsync();

                    // Lo que referencia al paciente pasa a la clave nueva.
                    await _context.HistoriasClinicas
                        .Where(h => h.TipoDocumentoPaciente == tipoViejo && h.NroDocumentoPaciente == nroViejo)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(h => h.TipoDocumentoPaciente, nuevoTipoDocumento)
                            .SetProperty(h => h.NroDocumentoPaciente, nuevoNroDocumento));

                    await _context.Turnos
                        .Where(t => t.TipoDocumentoPaciente == tipoViejo && t.NroDocumentoPaciente == nroViejo)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(t => t.TipoDocumentoPaciente, nuevoTipoDocumento)
                            .SetProperty(t => t.NroDocumentoPaciente, nuevoNroDocumento));

                    // Con la historia clínica y los turnos ya trasladados, la fila vieja no tiene nada que
                    // arrastrar (la baja en cascada de la historia clínica no encuentra filas).
                    await _context.Pacientes
                        .Where(p => p.TipoDocumento == tipoViejo && p.NroDocumento == nroViejo)
                        .ExecuteDeleteAsync();

                    await transaccion.CommitAsync();
                });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Otro usuario modificó al paciente después de leerlo: la transacción se deshizo entera.
                _context.ChangeTracker.Clear();
                throw new ConflictoConcurrenciaException("Los datos del paciente fueron modificados por otro usuario.", ex);
            }

            // Las entidades que quedaron en memoria describen la clave vieja.
            _context.ChangeTracker.Clear();
        }

        public async Task<IEnumerable<Paciente>> GetByCriteriaAsync(PacienteCriteria criteria)
        {
            var texto = criteria.Texto?.ToLower() ?? string.Empty;
            return await _context.Pacientes
                .Include(p => p.ObraSocial)
                .Where(p =>
                    p.Nombre.ToLower().Contains(texto) ||
                    p.Apellido.ToLower().Contains(texto) ||
                    p.Email.ToLower().Contains(texto) ||
                    p.NroDocumento.Contains(texto))
                .ToListAsync();
        }
    }
}