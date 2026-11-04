using Data;
using Domain.Model;
using DTOs;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    // =========================================================================
    // CUF12 - ADMINISTRAR ESPECIALIDAD (E4.R01, v1.01)
    // Actor primario: Responsable de la clínica.
    // Camino básico: 1. ingresa nombre y arancel particular; 2. el sistema valida que el nombre no exista
    //   y que el arancel sea un número positivo; 3. registra la especialidad y confirma.
    // Alt 1.a: nombre repetido -> mensajeSobreNombre(d).
    // Alt 1.b: arancel inválido -> mensajeSobreArancel(d).
    // Alt 1.c: modificar el arancel particular de una especialidad registrada, "sin afectar los pagos ya
    //   registrados" (RN16): las atenciones guardan su ArancelAplicado y los turnos su
    //   ArancelPenalizacionAplicado, así que el cambio solo rige para cálculos futuros.
    // E4.R02 (v1.02):
    // Alt 1.d: deshabilitar, si no tiene turnos pendientes de atención (Reservado, Presente o Atención
    //   Registrada) -> estado "Deshabilitada"; deja de ofrecerse para turnos, odontólogos y convenios.
    // Alt 1.e: rehabilitar -> estado "Habilitada".
    // Alt 1.f: eliminar, solo si no tiene odontólogos, convenios ni turnos; si no, se ofrece deshabilitarla.
    // =========================================================================
    public interface IAdministrarEspecialidadService
    {
        Task<IReadOnlyList<EspecialidadListadoDTO>> ListarAsync();
        Task<ResultadoOperacionDTO> RegistrarAsync(string? nombre, decimal? arancelParticular);
        Task<ResultadoOperacionDTO> ModificarArancelAsync(int idEspecialidad, decimal? arancelParticular);
        Task<IReadOnlyList<EspecialidadListadoDTO>> ListarHabilitadasAsync();
        Task<ResultadoOperacionDTO> DeshabilitarAsync(int idEspecialidad);
        Task<ResultadoOperacionDTO> RehabilitarAsync(int idEspecialidad);
        Task<ResultadoOperacionDTO> EliminarAsync(int idEspecialidad);
    }

    public class AdministrarEspecialidadService : IAdministrarEspecialidadService
    {
        public const string MensajeConfirmacionAlta = "La especialidad se ha registrado exitosamente.";
        public const string MensajeSobreNombre = "La especialidad ingresada ya se encuentra registrada en el catálogo.";
        public const string MensajeSobreArancel = "El arancel particular debe ser un monto numérico mayor a cero.";
        public const string MensajeConfirmacionUpdate = "El arancel particular de la especialidad se ha actualizado exitosamente.";
        public const string MensajeNombreRequerido = "Ingresá el nombre de la especialidad.";
        public const string MensajeEspecialidadInexistente = "No se encontró la especialidad indicada.";
        public const string MensajeConfirmacionDeshabilitacion = "La especialidad se ha deshabilitado. Ya no se ofrece para nuevos turnos y se conserva su historial.";
        public const string MensajeTurnosPendientes = "La especialidad tiene turnos pendientes de atención. Deben atenderse, reprogramarse o cancelarse antes de deshabilitarla.";
        public const string MensajeConfirmacionRehabilitacion = "La especialidad se ha rehabilitado y vuelve a estar disponible.";
        public const string MensajeConfirmacionEliminacion = "La especialidad se ha eliminado del catálogo.";
        public const string MensajeEspecialidadConHistorial = "La especialidad tiene odontólogos, convenios o turnos asociados y no puede eliminarse. Puede deshabilitarla para que deje de ofrecerse.";
        public const string MensajeYaDeshabilitada = "La especialidad ya se encuentra deshabilitada.";
        public const string MensajeYaHabilitada = "La especialidad ya se encuentra habilitada.";
        // Para los CU que ofrecen especialidades (CUU01, CUF04, CUF06, CUF11).
        public const string MensajeEspecialidadDeshabilitada = "La especialidad seleccionada está deshabilitada y no se ofrece.";

        private static readonly string[] EstadosTurnoPendiente = { "RESERVADO", "PRESENTE", "ATENCION_REGISTRADA" };

        private readonly TurnoMolarDbContext _contexto;

        public AdministrarEspecialidadService(TurnoMolarDbContext contexto) => _contexto = contexto;

        public async Task<IReadOnlyList<EspecialidadListadoDTO>> ListarAsync() =>
            (await _contexto.Especialidades.AsNoTracking().OrderBy(e => e.Nombre).ToListAsync())
                .Select(e => new EspecialidadListadoDTO
                {
                    IdEspecialidad = e.IdEspecialidad,
                    Nombre = e.Nombre,
                    ArancelParticular = e.ArancelParticular,
                    Habilitada = e.EstaHabilitada
                })
                .ToList();

        // Las especialidades deshabilitadas no se ofrecen para odontólogos, convenios ni turnos (alt 1.d.2).
        public async Task<IReadOnlyList<EspecialidadListadoDTO>> ListarHabilitadasAsync() =>
            (await ListarAsync()).Where(e => e.Habilitada).ToList();

        public async Task<ResultadoOperacionDTO> RegistrarAsync(string? nombre, decimal? arancelParticular)
        {
            // Paso 1: eDatosNuevaEspecialidad = nombre + arancelParticular.
            var nombreNormalizado = nombre?.Trim();
            if (string.IsNullOrEmpty(nombreNormalizado))
                return ResultadoOperacionDTO.Error("1", MensajeNombreRequerido);

            // Paso 2 / alt 1.a: no puede existir otra especialidad con el mismo nombre (sin distinguir mayúsculas).
            var nombreMinusculas = nombreNormalizado.ToLower();
            if (await _contexto.Especialidades.AnyAsync(e => e.Nombre.ToLower() == nombreMinusculas))
                return ResultadoOperacionDTO.Error("1.a.1", MensajeSobreNombre);

            // Paso 2 / alt 1.b: arancel numérico mayor a cero (MDF: CHECK > 0).
            if (arancelParticular is null or <= 0m)
                return ResultadoOperacionDTO.Error("1.b.1", MensajeSobreArancel);

            // Paso 3.
            _contexto.Especialidades.Add(new Especialidad(nombreNormalizado, arancelParticular.Value));
            await _contexto.SaveChangesAsync();
            return ResultadoOperacionDTO.Ok(MensajeConfirmacionAlta, "3");
        }

        public async Task<ResultadoOperacionDTO> ModificarArancelAsync(int idEspecialidad, decimal? arancelParticular)
        {
            // Alt 1.c.1: eDatosModificacionEspecialidad = nombre + arancelParticular.
            var especialidad = await _contexto.Especialidades.FindAsync(idEspecialidad);
            if (especialidad == null)
                return ResultadoOperacionDTO.Error("1.c.1", MensajeEspecialidadInexistente);

            // Alt 1.c.2.a: arancel inválido o nulo.
            if (arancelParticular is null or <= 0m)
                return ResultadoOperacionDTO.Error("1.c.2.a.1", MensajeSobreArancel);

            // Alt 1.c.2: registra el cambio sin afectar los pagos ya registrados (RN16).
            especialidad.SetArancelParticular(arancelParticular.Value);
            await _contexto.SaveChangesAsync();
            return ResultadoOperacionDTO.Ok(MensajeConfirmacionUpdate, "1.c.2");
        }

        public async Task<ResultadoOperacionDTO> DeshabilitarAsync(int idEspecialidad)
        {
            // Alt 1.d.1: eDeshabilitacionEspecialidad = nombre.
            var especialidad = await _contexto.Especialidades.FindAsync(idEspecialidad);
            if (especialidad == null)
                return ResultadoOperacionDTO.Error("1.d.1", MensajeEspecialidadInexistente);
            if (!especialidad.EstaHabilitada)
                return ResultadoOperacionDTO.Error("1.d.1", MensajeYaDeshabilitada);

            // Alt 1.d.2.a: turnos pendientes de atención -> sTurnosPendientesEspecialidad.
            var pendientes = await _contexto.Turnos
                .CountAsync(t => t.IdEspecialidad == idEspecialidad && EstadosTurnoPendiente.Contains(t.EstadoTurno));
            if (pendientes > 0)
                return ResultadoOperacionDTO.Error("1.d.2.a.1", $"{MensajeTurnosPendientes} Turnos pendientes: {pendientes}.");

            // Alt 1.d.2.
            especialidad.Deshabilitar();
            await _contexto.SaveChangesAsync();
            return ResultadoOperacionDTO.Ok(MensajeConfirmacionDeshabilitacion, "1.d.2");
        }

        public async Task<ResultadoOperacionDTO> RehabilitarAsync(int idEspecialidad)
        {
            // Alt 1.e.1: eRehabilitacionEspecialidad = nombre.
            var especialidad = await _contexto.Especialidades.FindAsync(idEspecialidad);
            if (especialidad == null)
                return ResultadoOperacionDTO.Error("1.e.1", MensajeEspecialidadInexistente);
            if (especialidad.EstaHabilitada)
                return ResultadoOperacionDTO.Error("1.e.1", MensajeYaHabilitada);

            // Alt 1.e.2.
            especialidad.Rehabilitar();
            await _contexto.SaveChangesAsync();
            return ResultadoOperacionDTO.Ok(MensajeConfirmacionRehabilitacion, "1.e.2");
        }

        public async Task<ResultadoOperacionDTO> EliminarAsync(int idEspecialidad)
        {
            // Alt 1.f.1: eEliminacionEspecialidad = nombre.
            var especialidad = await _contexto.Especialidades.FindAsync(idEspecialidad);
            if (especialidad == null)
                return ResultadoOperacionDTO.Error("1.f.1", MensajeEspecialidadInexistente);

            // Alt 1.f.2.a: odontólogos (DisponibilidadHoraria es la asociativa Odontólogo-Especialidad),
            // convenios o turnos asociados -> no se elimina, se ofrece deshabilitarla.
            var tieneHistorial =
                await _contexto.DisponibilidadesHorarias.AnyAsync(d => d.IdEspecialidad == idEspecialidad) ||
                await _contexto.Convenios.AnyAsync(c => c.IdEspecialidad == idEspecialidad) ||
                await _contexto.Turnos.AnyAsync(t => t.IdEspecialidad == idEspecialidad);
            if (tieneHistorial)
                return ResultadoOperacionDTO.Error("1.f.2.a.1", MensajeEspecialidadConHistorial);

            // Alt 1.f.2.
            _contexto.Especialidades.Remove(especialidad);
            await _contexto.SaveChangesAsync();
            return ResultadoOperacionDTO.Ok(MensajeConfirmacionEliminacion, "1.f.2");
        }
    }
}
