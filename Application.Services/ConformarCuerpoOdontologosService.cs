using Data;
using Domain.Model;
using DTOs;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    // =========================================================================
    // CUF04 - CONFORMAR CUERPO DE ODONTÓLOGOS (E4.R01, v1.04)
    // Actor primario: Responsable de la clínica.
    // Camino básico: 1-2. listado (nombre, apellido, matrícula, estado); 3. alta con datos personales,
    //   matrícula y una o más especialidades; 4. disponibilidad horaria por especialidad;
    //   5. registra al odontólogo en estado "Habilitado".
    // Alt 3.a: matrícula repetida -> mensajeMatriculaExistente(d).
    // Alt 5.a: ajustar disponibilidad (sin dejar turnos confirmados fuera de la nueva franja).
    // Alt 5.b: agregar o quitar especialidad (sin turnos pendientes en la que se quita).
    // Alt 5.c: datos de contacto. Alt 5.d: inhabilitar (RN15). Alt 5.e: rehabilitar.
    // =========================================================================
    public interface IConformarCuerpoOdontologosService
    {
        Task<IReadOnlyList<OdontologoListadoDTO>> ListarAsync();
        Task<OdontologoListadoDTO?> ObtenerAsync(string tipoDocumento, string nroDocumento);
        Task<ResultadoOperacionDTO> RegistrarAsync(NuevoOdontologoDTO datos);
        Task<ResultadoOperacionDTO> AjustarDisponibilidadAsync(string tipoDocumento, string nroDocumento, int idEspecialidad, IReadOnlyList<FranjaHorariaDTO> franjas);
        Task<ResultadoOperacionDTO> AgregarEspecialidadAsync(string tipoDocumento, string nroDocumento, int idEspecialidad, IReadOnlyList<FranjaHorariaDTO> franjas);
        Task<ResultadoOperacionDTO> QuitarEspecialidadAsync(string tipoDocumento, string nroDocumento, int idEspecialidad);
        Task<ResultadoOperacionDTO> ActualizarContactoAsync(string tipoDocumento, string nroDocumento, string? telefono, string? email, string? domicilio);
        Task<ResultadoOperacionDTO> InhabilitarAsync(string tipoDocumento, string nroDocumento);
        Task<ResultadoOperacionDTO> RehabilitarAsync(string tipoDocumento, string nroDocumento);
    }

    public class ConformarCuerpoOdontologosService : IConformarCuerpoOdontologosService
    {
        public const string MensajeMatriculaExistente = "Ya existe un odontólogo registrado con esa matrícula.";
        public const string MensajeConfirmacionDisponibilidad = "La disponibilidad horaria del odontólogo fue actualizada correctamente.";
        public const string MensajeConfirmacionEspecialidad = "La especialidad del odontólogo fue actualizada correctamente.";
        public const string MensajeConfirmacionContacto = "Los datos de contacto del odontólogo fueron actualizados correctamente.";
        public const string MensajeDocumentoExistente = "Ya existe un odontólogo registrado con ese tipo y número de documento.";
        public const string MensajeDatosRequeridos = "Completá documento, nombre, apellido y matrícula del odontólogo.";
        public const string MensajeEspecialidadRequerida = "Asigná al menos una especialidad con su disponibilidad horaria.";
        public const string MensajeFranjaRequerida = "Cada especialidad necesita al menos una franja horaria.";
        public const string MensajeDiaInvalido = "Los días de atención van de lunes a viernes.";
        public const string MensajeHorarioInvalido = "La hora de fin de cada franja debe ser posterior a la hora de inicio.";
        public const string MensajeSuperposicion = "Hay franjas horarias superpuestas en el mismo día.";
        public const string MensajeEspecialidadInexistente = "La especialidad indicada no existe en el catálogo.";
        public const string MensajeOdontologoInexistente = "No se encontró el odontólogo indicado.";
        public const string MensajeTurnosFueraDeFranja = "No se puede ajustar la disponibilidad: hay turnos reservados que quedarían fuera de la nueva franja horaria.";
        public const string MensajeEspecialidadYaAsignada = "El odontólogo ya tiene asignada esa especialidad.";
        public const string MensajeEspecialidadNoAsignada = "El odontólogo no tiene asignada esa especialidad.";
        public const string MensajeTurnosPendientesEspecialidad = "No se puede quitar la especialidad: el odontólogo tiene turnos pendientes en ella.";
        public const string MensajeUltimaEspecialidad = "El odontólogo debe conservar al menos una especialidad.";
        public const string MensajeInhabilitado = "El odontólogo fue inhabilitado y quedó fuera de la oferta de turnos. Se conservan sus turnos, atenciones y pagos previos.";
        public const string MensajeRehabilitado = "El odontólogo fue rehabilitado y vuelve a la oferta de turnos.";

        // RN1: la clínica atiende de lunes a viernes.
        private static readonly string[] DiasHabiles = { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" };

        private readonly TurnoMolarDbContext _contexto;

        public ConformarCuerpoOdontologosService(TurnoMolarDbContext contexto) => _contexto = contexto;

        // Pasos 1-2: sListadoOdontologos.
        public async Task<IReadOnlyList<OdontologoListadoDTO>> ListarAsync()
        {
            var odontologos = await _contexto.Odontologos.AsNoTracking()
                .Include(o => o.DisponibilidadesHorarias).ThenInclude(d => d.Especialidad)
                .OrderBy(o => o.Apellido).ThenBy(o => o.Nombre)
                .ToListAsync();
            return odontologos.Select(Mapear).ToList();
        }

        public async Task<OdontologoListadoDTO?> ObtenerAsync(string tipoDocumento, string nroDocumento)
        {
            var odontologo = await _contexto.Odontologos.AsNoTracking()
                .Include(o => o.DisponibilidadesHorarias).ThenInclude(d => d.Especialidad)
                .FirstOrDefaultAsync(o => o.TipoDocumento == tipoDocumento && o.NroDocumento == nroDocumento);
            return odontologo == null ? null : Mapear(odontologo);
        }

        public async Task<ResultadoOperacionDTO> RegistrarAsync(NuevoOdontologoDTO datos)
        {
            // Paso 3: eDatosNuevoOdontologo = datosPersonales(e) + matricula + 1{nombreEspecialidad}N
            var tipo = (datos.TipoDocumento ?? "DNI").Trim().ToUpper();
            var nro = datos.NroDocumento?.Trim() ?? string.Empty;
            var matricula = datos.Matricula?.Trim() ?? string.Empty;
            if (nro.Length == 0 || matricula.Length == 0 ||
                string.IsNullOrWhiteSpace(datos.Nombre) || string.IsNullOrWhiteSpace(datos.Apellido))
                return ResultadoOperacionDTO.Error("3", MensajeDatosRequeridos);

            // Alt 3.a.
            if (await _contexto.Odontologos.AnyAsync(o => o.Matricula == matricula))
                return ResultadoOperacionDTO.Error("3.a.1", MensajeMatriculaExistente);

            // Desvío D-CUF04-1: el CU no lo prevé, pero la PK de Odontologos es el documento.
            if (await _contexto.Odontologos.AnyAsync(o => o.TipoDocumento == tipo && o.NroDocumento == nro))
                return ResultadoOperacionDTO.Error("3", MensajeDocumentoExistente);

            // Paso 4: eDisponibilidadHoraria = 1{nombreEspecialidad + 1{disponibilidadHoraria(e)}N}N
            if (datos.Especialidades == null || datos.Especialidades.Count == 0)
                return ResultadoOperacionDTO.Error("3", MensajeEspecialidadRequerida);
            if (datos.Especialidades.Select(e => e.IdEspecialidad).Distinct().Count() != datos.Especialidades.Count)
                return ResultadoOperacionDTO.Error("3", MensajeEspecialidadYaAsignada);
            foreach (var esp in datos.Especialidades)
            {
                if (!await _contexto.Especialidades.AnyAsync(e => e.IdEspecialidad == esp.IdEspecialidad))
                    return ResultadoOperacionDTO.Error("3", MensajeEspecialidadInexistente);
                if (esp.Franjas == null || esp.Franjas.Count == 0)
                    return ResultadoOperacionDTO.Error("4", MensajeFranjaRequerida);
            }
            var error = ValidarFranjas(datos.Especialidades.SelectMany(e => e.Franjas));
            if (error != null)
                return ResultadoOperacionDTO.Error("4", error);

            // Paso 5: registra en estado "Habilitado".
            // Desvío D-CUF04-2: el CU no define la clave de acceso; la inicial es el número de documento.
            var (clave, salt) = PasswordHasher.Generar(nro);
            var odontologo = new Odontologo(tipo, nro, matricula, datos.Nombre.Trim(), datos.Apellido.Trim(),
                datos.FechaNacimiento, datos.Telefono?.Trim() ?? string.Empty, datos.Email?.Trim() ?? string.Empty,
                datos.Domicilio?.Trim() ?? string.Empty, EstadosOdontologo.Habilitado, clave, salt, DateTime.Now, "Odontologo");
            _contexto.Odontologos.Add(odontologo);
            foreach (var esp in datos.Especialidades)
                foreach (var f in esp.Franjas)
                    _contexto.DisponibilidadesHorarias.Add(new DisponibilidadHoraria(tipo, nro, NormalizarDia(f.DiaSemana), f.HoraInicio, f.HoraFin, esp.IdEspecialidad));
            await _contexto.SaveChangesAsync();

            return ResultadoOperacionDTO.Ok(
                $"Odontólogo registrado: {odontologo.Nombre} {odontologo.Apellido}, matrícula {odontologo.Matricula}, estado Habilitado.", "5");
        }

        public async Task<ResultadoOperacionDTO> AjustarDisponibilidadAsync(string tipoDocumento, string nroDocumento, int idEspecialidad, IReadOnlyList<FranjaHorariaDTO> franjas)
        {
            // Alt 5.a.1: eAjusteDisponibilidad = matricula + nombreEspecialidad + 1{disponibilidadHoraria(e)}N
            var odontologo = await CargarAsync(tipoDocumento, nroDocumento);
            if (odontologo == null)
                return ResultadoOperacionDTO.Error("5.a.1", MensajeOdontologoInexistente);
            if (!odontologo.DisponibilidadesHorarias.Any(d => d.IdEspecialidad == idEspecialidad))
                return ResultadoOperacionDTO.Error("5.a.1", MensajeEspecialidadNoAsignada);
            if (franjas == null || franjas.Count == 0)
                return ResultadoOperacionDTO.Error("5.a.1", MensajeFranjaRequerida);

            var otras = odontologo.DisponibilidadesHorarias.Where(d => d.IdEspecialidad != idEspecialidad)
                .Select(d => new FranjaHorariaDTO { DiaSemana = d.DiaSemana, HoraInicio = d.HoraInicio, HoraFin = d.HoraFin });
            var error = ValidarFranjas(otras.Concat(franjas));
            if (error != null)
                return ResultadoOperacionDTO.Error("5.a.1", error);

            // Alt 5.a.2: valida que los turnos ya confirmados no queden fuera de la nueva franja.
            var turnos = await TurnosPendientesAsync(tipoDocumento, nroDocumento, idEspecialidad);
            foreach (var turno in turnos)
            {
                var dia = AgendaTurnoService.DiasSemanaEs[(int)turno.FechaHoraTurno.DayOfWeek];
                var inicio = TimeOnly.FromDateTime(turno.FechaHoraTurno);
                var fin = inicio.AddMinutes(AgendaTurnoService.DuracionTurnoMinutos);
                var cubierto = franjas.Any(f => MismoDia(f.DiaSemana, dia) && f.HoraInicio <= inicio && fin <= f.HoraFin);
                if (!cubierto)
                    return ResultadoOperacionDTO.Error("5.a.2", MensajeTurnosFueraDeFranja);
            }

            _contexto.DisponibilidadesHorarias.RemoveRange(odontologo.DisponibilidadesHorarias.Where(d => d.IdEspecialidad == idEspecialidad).ToList());
            await _contexto.SaveChangesAsync();
            foreach (var f in franjas)
                _contexto.DisponibilidadesHorarias.Add(new DisponibilidadHoraria(odontologo.TipoDocumento, odontologo.NroDocumento, NormalizarDia(f.DiaSemana), f.HoraInicio, f.HoraFin, idEspecialidad));
            await _contexto.SaveChangesAsync();
            return ResultadoOperacionDTO.Ok(MensajeConfirmacionDisponibilidad, "5.a.2");
        }

        public async Task<ResultadoOperacionDTO> AgregarEspecialidadAsync(string tipoDocumento, string nroDocumento, int idEspecialidad, IReadOnlyList<FranjaHorariaDTO> franjas)
        {
            // Alt 5.b.1 (agregar): la especialidad se incorpora con su disponibilidad horaria.
            var odontologo = await CargarAsync(tipoDocumento, nroDocumento);
            if (odontologo == null)
                return ResultadoOperacionDTO.Error("5.b.1", MensajeOdontologoInexistente);
            if (!await _contexto.Especialidades.AnyAsync(e => e.IdEspecialidad == idEspecialidad))
                return ResultadoOperacionDTO.Error("5.b.1", MensajeEspecialidadInexistente);
            if (odontologo.DisponibilidadesHorarias.Any(d => d.IdEspecialidad == idEspecialidad))
                return ResultadoOperacionDTO.Error("5.b.1", MensajeEspecialidadYaAsignada);
            if (franjas == null || franjas.Count == 0)
                return ResultadoOperacionDTO.Error("5.b.1", MensajeFranjaRequerida);

            var actuales = odontologo.DisponibilidadesHorarias
                .Select(d => new FranjaHorariaDTO { DiaSemana = d.DiaSemana, HoraInicio = d.HoraInicio, HoraFin = d.HoraFin });
            var error = ValidarFranjas(actuales.Concat(franjas));
            if (error != null)
                return ResultadoOperacionDTO.Error("5.b.1", error);

            foreach (var f in franjas)
                _contexto.DisponibilidadesHorarias.Add(new DisponibilidadHoraria(odontologo.TipoDocumento, odontologo.NroDocumento, NormalizarDia(f.DiaSemana), f.HoraInicio, f.HoraFin, idEspecialidad));
            await _contexto.SaveChangesAsync();
            return ResultadoOperacionDTO.Ok(MensajeConfirmacionEspecialidad, "5.b.2");
        }

        public async Task<ResultadoOperacionDTO> QuitarEspecialidadAsync(string tipoDocumento, string nroDocumento, int idEspecialidad)
        {
            // Alt 5.b.1 (quitar) / 5.b.2: valida que no haya turnos pendientes en esa especialidad.
            var odontologo = await CargarAsync(tipoDocumento, nroDocumento);
            if (odontologo == null)
                return ResultadoOperacionDTO.Error("5.b.1", MensajeOdontologoInexistente);
            var franjas = odontologo.DisponibilidadesHorarias.Where(d => d.IdEspecialidad == idEspecialidad).ToList();
            if (franjas.Count == 0)
                return ResultadoOperacionDTO.Error("5.b.1", MensajeEspecialidadNoAsignada);
            if (odontologo.DisponibilidadesHorarias.Select(d => d.IdEspecialidad).Distinct().Count() == 1)
                return ResultadoOperacionDTO.Error("5.b.1", MensajeUltimaEspecialidad);
            if ((await TurnosPendientesAsync(tipoDocumento, nroDocumento, idEspecialidad)).Count > 0)
                return ResultadoOperacionDTO.Error("5.b.2", MensajeTurnosPendientesEspecialidad);

            _contexto.DisponibilidadesHorarias.RemoveRange(franjas);
            await _contexto.SaveChangesAsync();
            return ResultadoOperacionDTO.Ok(MensajeConfirmacionEspecialidad, "5.b.2");
        }

        public async Task<ResultadoOperacionDTO> ActualizarContactoAsync(string tipoDocumento, string nroDocumento, string? telefono, string? email, string? domicilio)
        {
            // Alt 5.c.1: eActualizacionContacto = matricula + (telefono) + (email) + (domicilio).
            var odontologo = await CargarAsync(tipoDocumento, nroDocumento);
            if (odontologo == null)
                return ResultadoOperacionDTO.Error("5.c.1", MensajeOdontologoInexistente);
            odontologo.ActualizarContacto(telefono?.Trim(), email?.Trim(), domicilio?.Trim());
            await _contexto.SaveChangesAsync();
            return ResultadoOperacionDTO.Ok(MensajeConfirmacionContacto, "5.c.1");
        }

        public async Task<ResultadoOperacionDTO> InhabilitarAsync(string tipoDocumento, string nroDocumento)
        {
            // Alt 5.d.1 / RN15: queda desafectado de la oferta de turnos, sin afectar turnos, atenciones
            // ni pagos previos. Si tiene turnos pendientes se informan para que la clínica los coordine.
            var odontologo = await CargarAsync(tipoDocumento, nroDocumento);
            if (odontologo == null)
                return ResultadoOperacionDTO.Error("5.d.1", MensajeOdontologoInexistente);
            odontologo.Inhabilitar();
            await _contexto.SaveChangesAsync();

            var pendientes = (await TurnosPendientesAsync(tipoDocumento, nroDocumento, null)).Count;
            var mensaje = pendientes == 0
                ? MensajeInhabilitado
                : $"{MensajeInhabilitado} Atención: tiene {pendientes} turno(s) pendiente(s) que conviene reprogramar o cancelar.";
            return ResultadoOperacionDTO.Ok(mensaje, "5.d.1");
        }

        public async Task<ResultadoOperacionDTO> RehabilitarAsync(string tipoDocumento, string nroDocumento)
        {
            // Alt 5.e.1.
            var odontologo = await CargarAsync(tipoDocumento, nroDocumento);
            if (odontologo == null)
                return ResultadoOperacionDTO.Error("5.e.1", MensajeOdontologoInexistente);
            odontologo.Rehabilitar();
            await _contexto.SaveChangesAsync();
            return ResultadoOperacionDTO.Ok(MensajeRehabilitado, "5.e.1");
        }

        // ----- Auxiliares -----

        private Task<Odontologo?> CargarAsync(string tipoDocumento, string nroDocumento) =>
            _contexto.Odontologos.Include(o => o.DisponibilidadesHorarias)
                .FirstOrDefaultAsync(o => o.TipoDocumento == tipoDocumento && o.NroDocumento == nroDocumento);

        // Turnos futuros todavía no atendidos (RESERVADO o PRESENTE), opcionalmente de una especialidad.
        private async Task<List<Turno>> TurnosPendientesAsync(string tipoDocumento, string nroDocumento, int? idEspecialidad)
        {
            var desde = DateTime.Today;
            return await _contexto.Turnos.AsNoTracking()
                .Where(t => t.TipoDocumentoOdontologo == tipoDocumento && t.NroDocumentoOdontologo == nroDocumento
                            && (idEspecialidad == null || t.IdEspecialidad == idEspecialidad)
                            && (t.EstadoTurno == "RESERVADO" || t.EstadoTurno == "PRESENTE")
                            && t.FechaHoraTurno >= desde)
                .ToListAsync();
        }

        // disponibilidadHoraria(e) = diaSemana + horaInicio + horaFin. RN1 (lunes a viernes),
        // fin posterior al inicio y sin superposición en el mismo día (un odontólogo no puede atender
        // dos especialidades a la vez).
        internal static string? ValidarFranjas(IEnumerable<FranjaHorariaDTO> franjas)
        {
            var lista = franjas.ToList();
            foreach (var f in lista)
            {
                if (NormalizarDia(f.DiaSemana) is not { } dia || !DiasHabiles.Contains(dia))
                    return MensajeDiaInvalido;
                if (f.HoraFin <= f.HoraInicio)
                    return MensajeHorarioInvalido;
            }
            foreach (var grupo in lista.GroupBy(f => NormalizarDia(f.DiaSemana)))
            {
                var ordenadas = grupo.OrderBy(f => f.HoraInicio).ToList();
                for (var i = 1; i < ordenadas.Count; i++)
                    if (ordenadas[i].HoraInicio < ordenadas[i - 1].HoraFin)
                        return MensajeSuperposicion;
            }
            return null;
        }

        private static string NormalizarDia(string? dia)
        {
            var d = (dia ?? string.Empty).Trim();
            return DiasHabiles.Concat(new[] { "Sábado", "Domingo" })
                       .FirstOrDefault(x => MismoDia(x, d)) ?? d;
        }

        private static bool MismoDia(string a, string b) =>
            string.Compare(a, b, System.Globalization.CultureInfo.GetCultureInfo("es-AR"),
                System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreNonSpace) == 0;

        private static OdontologoListadoDTO Mapear(Odontologo o) => new()
        {
            TipoDocumento = o.TipoDocumento,
            NroDocumento = o.NroDocumento,
            Nombre = o.Nombre,
            Apellido = o.Apellido,
            Matricula = o.Matricula,
            EstadoOdontologo = EstadosOdontologo.Legible(o.EstadoOdontologo),
            Habilitado = o.EstaHabilitado,
            Telefono = o.Telefono,
            Email = o.Email,
            Domicilio = o.Domicilio,
            Especialidades = o.DisponibilidadesHorarias
                .GroupBy(d => d.IdEspecialidad)
                .Select(g => new EspecialidadDisponibilidadDTO
                {
                    IdEspecialidad = g.Key,
                    NombreEspecialidad = g.First().Especialidad?.Nombre ?? string.Empty,
                    Franjas = g.OrderBy(d => Array.IndexOf(AgendaTurnoService.DiasSemanaEs, d.DiaSemana)).ThenBy(d => d.HoraInicio)
                        .Select(d => new FranjaHorariaDTO { DiaSemana = d.DiaSemana, HoraInicio = d.HoraInicio, HoraFin = d.HoraFin })
                        .ToList()
                })
                .OrderBy(e => e.NombreEspecialidad)
                .ToList()
        };
    }
}
