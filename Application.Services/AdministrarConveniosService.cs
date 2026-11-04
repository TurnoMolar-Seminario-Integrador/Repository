using Data;
using Domain.Model;
using DTOs;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    // =========================================================================
    // CUF06 - ADMINISTRAR CONVENIOS CON OBRAS SOCIALES (E4.R01, v1.04)
    // Actor primario: Responsable de la clínica.
    // Camino básico: 1. listado (identificador, nombre, plan, estado); 2. alta con identificador, nombre,
    //   plan y uno o más convenios (especialidad + arancel), validando que el identificador no exista;
    //   3. registra la obra social en estado "Disponible".
    // Alt 2.a: identificador repetido -> mensajeObraSocialExistente(d).
    // Alt 3.a: modificar plan de cobertura. Alt 3.b: dar de baja ("No Disponible", conserva el historial).
    // Alt 3.c: rehabilitar. Alt 3.d: agregar, modificar o eliminar un convenio.
    // =========================================================================
    public interface IAdministrarConveniosService
    {
        Task<IReadOnlyList<ObraSocialListadoDTO>> ListarAsync();
        Task<ObraSocialListadoDTO?> ObtenerAsync(string identificadorOS);
        Task<ResultadoOperacionDTO> RegistrarAsync(NuevaObraSocialDTO datos);
        Task<ResultadoOperacionDTO> ModificarPlanAsync(string identificadorOS, string? planCobertura);
        Task<ResultadoOperacionDTO> DarDeBajaAsync(string identificadorOS);
        Task<ResultadoOperacionDTO> RehabilitarAsync(string identificadorOS);
        Task<ResultadoOperacionDTO> GuardarConvenioAsync(string identificadorOS, int idEspecialidad, decimal? arancelConvenio);
        Task<ResultadoOperacionDTO> EliminarConvenioAsync(string identificadorOS, int idEspecialidad);
    }

    public class AdministrarConveniosService : IAdministrarConveniosService
    {
        public const string MensajeObraSocialExistente = "Ya existe una obra social registrada con ese identificador.";
        public const string MensajeConfirmacionPlan = "El plan de cobertura de la obra social fue actualizado correctamente.";
        public const string MensajeConfirmacionConvenio = "El convenio fue actualizado correctamente.";
        public const string MensajeDatosRequeridos = "Ingresá el identificador y el nombre de la obra social.";
        public const string MensajeConvenioRequerido = "Definí al menos un convenio (especialidad y arancel).";
        public const string MensajeArancelInvalido = "El arancel del convenio debe ser un monto mayor a cero.";
        public const string MensajeEspecialidadInexistente = "La especialidad indicada no existe en el catálogo.";
        public const string MensajeConvenioDuplicado = "Hay más de un convenio para la misma especialidad.";
        public const string MensajeObraSocialInexistente = "No se encontró la obra social indicada.";
        public const string MensajeConvenioInexistente = "La obra social no tiene un convenio para esa especialidad.";
        public const string MensajeConvenioConTurnos = "No se puede eliminar el convenio: hay turnos reservados por obra social para esa especialidad.";
        public const string MensajeUltimoConvenio = "La obra social debe conservar al menos un convenio.";
        public const string MensajeBaja = "La obra social fue dada de baja. Quedó como No Disponible y se conserva su historial.";
        public const string MensajeRehabilitacion = "La obra social fue rehabilitada y vuelve a estar Disponible.";

        private readonly TurnoMolarDbContext _contexto;

        public AdministrarConveniosService(TurnoMolarDbContext contexto) => _contexto = contexto;

        // Paso 1: sListadoObrasSociales = 0{identificadorOS + nombreOS + planCobertura + estadoOS(d)}N
        public async Task<IReadOnlyList<ObraSocialListadoDTO>> ListarAsync()
        {
            var obras = await _contexto.ObrasSociales.AsNoTracking()
                .Include(o => o.Convenios).ThenInclude(c => c.Especialidad)
                .OrderBy(o => o.NombreOS)
                .ToListAsync();
            return obras.Select(Mapear).ToList();
        }

        public async Task<ObraSocialListadoDTO?> ObtenerAsync(string identificadorOS)
        {
            var obra = await _contexto.ObrasSociales.AsNoTracking()
                .Include(o => o.Convenios).ThenInclude(c => c.Especialidad)
                .FirstOrDefaultAsync(o => o.IdentificadorOS == identificadorOS);
            return obra == null ? null : Mapear(obra);
        }

        public async Task<ResultadoOperacionDTO> RegistrarAsync(NuevaObraSocialDTO datos)
        {
            // Paso 2: eDatosNuevaObraSocial + eConveniosDefinidos.
            var identificador = datos.IdentificadorOS?.Trim() ?? string.Empty;
            var nombre = datos.NombreOS?.Trim() ?? string.Empty;
            if (identificador.Length == 0 || nombre.Length == 0)
                return ResultadoOperacionDTO.Error("2", MensajeDatosRequeridos);

            // Alt 2.a.
            if (await _contexto.ObrasSociales.AnyAsync(o => o.IdentificadorOS == identificador))
                return ResultadoOperacionDTO.Error("2.a.1", MensajeObraSocialExistente);

            // eConveniosDefinidos = 1{idEspecialidad + arancelConvenio}N
            if (datos.Convenios == null || datos.Convenios.Count == 0)
                return ResultadoOperacionDTO.Error("2", MensajeConvenioRequerido);
            if (datos.Convenios.Select(c => c.IdEspecialidad).Distinct().Count() != datos.Convenios.Count)
                return ResultadoOperacionDTO.Error("2", MensajeConvenioDuplicado);
            foreach (var convenio in datos.Convenios)
            {
                if (convenio.ArancelConvenio <= 0m)
                    return ResultadoOperacionDTO.Error("2", MensajeArancelInvalido);
                var especialidad = await _contexto.Especialidades.FindAsync(convenio.IdEspecialidad);
                if (especialidad == null)
                    return ResultadoOperacionDTO.Error("2", MensajeEspecialidadInexistente);
                // CUF12 E4.R02 alt 1.d.2: una especialidad deshabilitada no se ofrece para nuevos convenios.
                if (!especialidad.EstaHabilitada)
                    return ResultadoOperacionDTO.Error("2", AdministrarEspecialidadService.MensajeEspecialidadDeshabilitada);
            }

            // Paso 3: queda en estado "Disponible".
            var obra = new ObraSocial(identificador, nombre, datos.PlanCobertura?.Trim() ?? string.Empty, EstadosObraSocial.Disponible);
            _contexto.ObrasSociales.Add(obra);
            foreach (var convenio in datos.Convenios)
                _contexto.Convenios.Add(new Convenio(identificador, convenio.IdEspecialidad, convenio.ArancelConvenio));
            await _contexto.SaveChangesAsync();

            return ResultadoOperacionDTO.Ok(
                $"Obra social registrada: {obra.NombreOS} ({obra.IdentificadorOS}), plan {obra.PlanCobertura}, estado Disponible, con {datos.Convenios.Count} convenio(s).", "3");
        }

        public async Task<ResultadoOperacionDTO> ModificarPlanAsync(string identificadorOS, string? planCobertura)
        {
            // Alt 3.a.1: eModificacionPlan = identificadorOS + planCobertura.
            var obra = await _contexto.ObrasSociales.FindAsync(identificadorOS);
            if (obra == null)
                return ResultadoOperacionDTO.Error("3.a.1", MensajeObraSocialInexistente);
            obra.SetPlanCobertura(planCobertura?.Trim() ?? string.Empty);
            await _contexto.SaveChangesAsync();
            return ResultadoOperacionDTO.Ok(MensajeConfirmacionPlan, "3.a.1");
        }

        public async Task<ResultadoOperacionDTO> DarDeBajaAsync(string identificadorOS)
        {
            // Alt 3.b.1: cambia el estado a "No Disponible" conservando el historial.
            var obra = await _contexto.ObrasSociales.FindAsync(identificadorOS);
            if (obra == null)
                return ResultadoOperacionDTO.Error("3.b.1", MensajeObraSocialInexistente);
            obra.DarDeBaja();
            await _contexto.SaveChangesAsync();
            return ResultadoOperacionDTO.Ok(MensajeBaja, "3.b.1");
        }

        public async Task<ResultadoOperacionDTO> RehabilitarAsync(string identificadorOS)
        {
            // Alt 3.c.1: vuelve a "Disponible".
            var obra = await _contexto.ObrasSociales.FindAsync(identificadorOS);
            if (obra == null)
                return ResultadoOperacionDTO.Error("3.c.1", MensajeObraSocialInexistente);
            obra.Rehabilitar();
            await _contexto.SaveChangesAsync();
            return ResultadoOperacionDTO.Ok(MensajeRehabilitacion, "3.c.1");
        }

        // Alt 3.d.1: agregar (si no existe) o modificar (si existe) el convenio de una especialidad.
        public async Task<ResultadoOperacionDTO> GuardarConvenioAsync(string identificadorOS, int idEspecialidad, decimal? arancelConvenio)
        {
            var obra = await _contexto.ObrasSociales.Include(o => o.Convenios)
                .FirstOrDefaultAsync(o => o.IdentificadorOS == identificadorOS);
            if (obra == null)
                return ResultadoOperacionDTO.Error("3.d.1", MensajeObraSocialInexistente);
            if (arancelConvenio is null or <= 0m)
                return ResultadoOperacionDTO.Error("3.d.1", MensajeArancelInvalido);
            var especialidad = await _contexto.Especialidades.FindAsync(idEspecialidad);
            if (especialidad == null)
                return ResultadoOperacionDTO.Error("3.d.1", MensajeEspecialidadInexistente);

            // RN16: el cambio de arancel no afecta pagos ya registrados (cada AtencionOdontologica guarda
            // su ArancelAplicado y cada Pago su monto).
            var existente = obra.Convenios.FirstOrDefault(c => c.IdEspecialidad == idEspecialidad);
            if (existente != null)
                existente.SetArancelConvenio(arancelConvenio.Value);
            // CUF12 E4.R02 alt 1.d.2: no se agregan convenios nuevos para una especialidad deshabilitada
            // (el arancel de un convenio existente sí puede seguir ajustándose).
            else if (!especialidad.EstaHabilitada)
                return ResultadoOperacionDTO.Error("3.d.1", AdministrarEspecialidadService.MensajeEspecialidadDeshabilitada);
            else
                _contexto.Convenios.Add(new Convenio(identificadorOS, idEspecialidad, arancelConvenio.Value));

            await _contexto.SaveChangesAsync();
            return ResultadoOperacionDTO.Ok(MensajeConfirmacionConvenio, "3.d.1");
        }

        // Alt 3.d.1: eliminar un convenio.
        // Desvío documentado (DESVIOS.md D-CUF06-1): el CU no lo prevé, pero se rechaza la eliminación
        // si hay turnos RESERVADO por obra social de pacientes de esa OS para esa especialidad (su
        // penalización y su cobro dependen del arancel del convenio), o si es el último convenio
        // (eConveniosDefinidos exige 1 o más).
        public async Task<ResultadoOperacionDTO> EliminarConvenioAsync(string identificadorOS, int idEspecialidad)
        {
            var obra = await _contexto.ObrasSociales.Include(o => o.Convenios)
                .FirstOrDefaultAsync(o => o.IdentificadorOS == identificadorOS);
            if (obra == null)
                return ResultadoOperacionDTO.Error("3.d.1", MensajeObraSocialInexistente);
            var convenio = obra.Convenios.FirstOrDefault(c => c.IdEspecialidad == idEspecialidad);
            if (convenio == null)
                return ResultadoOperacionDTO.Error("3.d.1", MensajeConvenioInexistente);
            if (obra.Convenios.Count == 1)
                return ResultadoOperacionDTO.Error("3.d.1", MensajeUltimoConvenio);

            var hayTurnos = await _contexto.Turnos.AnyAsync(t =>
                t.IdEspecialidad == idEspecialidad &&
                t.ModalidadPagoElegida == ModalidadesPago.ObraSocial &&
                (t.EstadoTurno == "RESERVADO" || t.EstadoTurno == "PRESENTE" || t.EstadoTurno == "ATENCION_REGISTRADA") &&
                t.Paciente.IdentificadorOS == identificadorOS);
            if (hayTurnos)
                return ResultadoOperacionDTO.Error("3.d.1", MensajeConvenioConTurnos);

            _contexto.Convenios.Remove(convenio);
            await _contexto.SaveChangesAsync();
            return ResultadoOperacionDTO.Ok(MensajeConfirmacionConvenio, "3.d.1");
        }

        private static ObraSocialListadoDTO Mapear(ObraSocial o) => new()
        {
            IdentificadorOS = o.IdentificadorOS,
            NombreOS = o.NombreOS,
            PlanCobertura = o.PlanCobertura,
            EstadoOS = EstadosObraSocial.Legible(o.EstadoOS),
            Disponible = o.EstaDisponible,
            Convenios = o.Convenios
                .OrderBy(c => c.Especialidad?.Nombre)
                .Select(c => new ConvenioDTO
                {
                    IdentificadorOS = c.IdentificadorOS,
                    IdEspecialidad = c.IdEspecialidad,
                    NombreEspecialidad = c.Especialidad?.Nombre,
                    ArancelConvenio = c.ArancelConvenio
                }).ToList()
        };
    }
}
