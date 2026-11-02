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
    // =========================================================================
    public interface IAdministrarEspecialidadService
    {
        Task<IReadOnlyList<EspecialidadListadoDTO>> ListarAsync();
        Task<ResultadoOperacionDTO> RegistrarAsync(string? nombre, decimal? arancelParticular);
        Task<ResultadoOperacionDTO> ModificarArancelAsync(int idEspecialidad, decimal? arancelParticular);
    }

    public class AdministrarEspecialidadService : IAdministrarEspecialidadService
    {
        public const string MensajeConfirmacionAlta = "La especialidad se ha registrado exitosamente.";
        public const string MensajeSobreNombre = "La especialidad ingresada ya se encuentra registrada en el catálogo.";
        public const string MensajeSobreArancel = "El arancel particular debe ser un monto numérico mayor a cero.";
        public const string MensajeConfirmacionUpdate = "El arancel particular de la especialidad se ha actualizado exitosamente.";
        public const string MensajeNombreRequerido = "Ingresá el nombre de la especialidad.";
        public const string MensajeEspecialidadInexistente = "No se encontró la especialidad indicada.";

        private readonly TurnoMolarDbContext _contexto;

        public AdministrarEspecialidadService(TurnoMolarDbContext contexto) => _contexto = contexto;

        public async Task<IReadOnlyList<EspecialidadListadoDTO>> ListarAsync() =>
            await _contexto.Especialidades.AsNoTracking()
                .OrderBy(e => e.Nombre)
                .Select(e => new EspecialidadListadoDTO
                {
                    IdEspecialidad = e.IdEspecialidad,
                    Nombre = e.Nombre,
                    ArancelParticular = e.ArancelParticular
                })
                .ToListAsync();

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
    }
}
