using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public interface IUsuarioRepository
    {
        // Busca (TipoDocumento, NroDocumento) en Pacientes, Odontologos y
        // ResponsablesClinica. Devuelve todas las coincidencias: normalmente 0 o 1,
        // pero puede haber más de una si la misma persona tiene cuentas
        // independientes en más de un rol (ej. es Odontólogo y también Paciente).
        Task<IEnumerable<Usuario>> BuscarCandidatosAsync(string tipoDocumento, string nroDocumento);
    }
 
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly TurnoMolarDbContext _context;
 
        public UsuarioRepository(TurnoMolarDbContext context)
        {
            _context = context;
        }
 
        public async Task<IEnumerable<Usuario>> BuscarCandidatosAsync(string tipoDocumento, string nroDocumento)
        {
            var candidatos = new List<Usuario>();
 
            var paciente = await _context.Pacientes.AsNoTracking()
                .FirstOrDefaultAsync(p => p.TipoDocumento == tipoDocumento && p.NroDocumento == nroDocumento);
            if (paciente != null)
            {
                candidatos.Add(new Usuario
                {
                    TipoDocumento = paciente.TipoDocumento,
                    NroDocumento = paciente.NroDocumento,
                    Nombre = paciente.Nombre,
                    Apellido = paciente.Apellido,
                    Email = paciente.Email,
                    Rol = paciente.Rol,
                    Clave = paciente.Clave,
                    SaltClave = paciente.SaltClave
                });
            }
 
            var odontologo = await _context.Odontologos.AsNoTracking()
                .FirstOrDefaultAsync(o => o.TipoDocumento == tipoDocumento && o.NroDocumento == nroDocumento);
            if (odontologo != null)
            {
                candidatos.Add(new Usuario
                {
                    TipoDocumento = odontologo.TipoDocumento,
                    NroDocumento = odontologo.NroDocumento,
                    Nombre = odontologo.Nombre,
                    Apellido = odontologo.Apellido,
                    Email = odontologo.Email,
                    Rol = odontologo.Rol,
                    Clave = odontologo.Clave,
                    SaltClave = odontologo.SaltClave
                });
            }
 
            var responsable = await _context.ResponsablesClinica.AsNoTracking()
                .FirstOrDefaultAsync(r => r.TipoDocumento == tipoDocumento && r.NroDocumento == nroDocumento);
            if (responsable != null)
            {
                candidatos.Add(new Usuario
                {
                    TipoDocumento = responsable.TipoDocumento,
                    NroDocumento = responsable.NroDocumento,
                    Nombre = responsable.Nombre,
                    Apellido = responsable.Apellido,
                    Email = responsable.Email,
                    Rol = responsable.Rol,
                    Clave = responsable.Clave,
                    SaltClave = responsable.SaltClave
                });
            }
 
            return candidatos;
        }
    }
}
