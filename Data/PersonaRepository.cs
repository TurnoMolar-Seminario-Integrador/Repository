using Microsoft.EntityFrameworkCore;

namespace Data
{
    // Consultas que cruzan los tres roles de una persona: Pacientes, Odontologos y ResponsablesClinica.
    // No hay una tabla de personas (MDF v1.01): cada rol es una fila independiente, con la misma clave primaria
    // compuesta (TipoDocumento, NroDocumento) cuando se trata de la misma persona (p. ej. Karina González,
    // odontóloga y responsable de la clínica).
    //
    // Regla del equipo (el MDF no define UNIQUE sobre Email): el correo electrónico pertenece a UNA persona,
    // identificada por su clave primaria. Una misma persona puede repetir su correo en todos sus roles (y debe
    // tener el mismo en todos); dos personas distintas no pueden compartirlo.
    public interface IPersonaRepository
    {
        // ¿Otra persona (clave primaria distinta) ya usa ese correo en cualquiera de los tres roles?
        Task<bool> CorreoEnUsoPorOtraPersonaAsync(string email, string tipoDocumento, string nroDocumento);

        // ¿La misma persona figura como odontólogo o como responsable de la clínica con un correo distinto?
        Task<bool> TieneOtroCorreoEnRolesDelPersonalAsync(string email, string tipoDocumento, string nroDocumento);

        // Deja el mismo correo en las filas de la misma persona como odontólogo y como responsable de la
        // clínica, para que un mismo usuario tenga el mismo correo en todos sus roles.
        Task SincronizarCorreoEnRolesDelPersonalAsync(string tipoDocumento, string nroDocumento, string email);
    }

    public class PersonaRepository : IPersonaRepository
    {
        private readonly TurnoMolarDbContext _context;

        public PersonaRepository(TurnoMolarDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CorreoEnUsoPorOtraPersonaAsync(string email, string tipoDocumento, string nroDocumento)
        {
            var correo = Normalizar(email);

            return await _context.Pacientes.AnyAsync(p =>
                       p.Email.ToLower() == correo && !(p.TipoDocumento == tipoDocumento && p.NroDocumento == nroDocumento))
                || await _context.Odontologos.AnyAsync(o =>
                       o.Email.ToLower() == correo && !(o.TipoDocumento == tipoDocumento && o.NroDocumento == nroDocumento))
                || await _context.ResponsablesClinica.AnyAsync(r =>
                       r.Email.ToLower() == correo && !(r.TipoDocumento == tipoDocumento && r.NroDocumento == nroDocumento));
        }

        public async Task<bool> TieneOtroCorreoEnRolesDelPersonalAsync(string email, string tipoDocumento, string nroDocumento)
        {
            var correo = Normalizar(email);

            return await _context.Odontologos.AnyAsync(o =>
                       o.TipoDocumento == tipoDocumento && o.NroDocumento == nroDocumento && o.Email.ToLower() != correo)
                || await _context.ResponsablesClinica.AnyAsync(r =>
                       r.TipoDocumento == tipoDocumento && r.NroDocumento == nroDocumento && r.Email.ToLower() != correo);
        }

        public async Task SincronizarCorreoEnRolesDelPersonalAsync(string tipoDocumento, string nroDocumento, string email)
        {
            var correo = email.Trim();

            await _context.Odontologos
                .Where(o => o.TipoDocumento == tipoDocumento && o.NroDocumento == nroDocumento && o.Email != correo)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.Email, correo));

            await _context.ResponsablesClinica
                .Where(r => r.TipoDocumento == tipoDocumento && r.NroDocumento == nroDocumento && r.Email != correo)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.Email, correo));
        }

        private static string Normalizar(string email) => (email ?? string.Empty).Trim().ToLower();
    }
}
