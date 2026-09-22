using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    public interface IValorarAtencionService
    {
        Task<PendientesDeValoracionResultDTO> ObtenerPendientesDeValoracionAsync(string tipoDocumentoPaciente, string nroDocumentoPaciente);

        Task<RegistrarValoracionResultDTO> RegistrarValoracionAsync(
            string tipoDocumentoPaciente,
            string nroDocumentoPaciente,
            int nroTurno,
            int calificacion,
            string? observaciones);
    }

    // =========================================================================
    // CUU04 - VALORAR ATENCIÓN ODONTOLÓGICA (v1.02)
    // Actor primario: Paciente de la clínica. Otros: <vacío> -- a diferencia de CUU02/CUU03,
    // acá no interviene el Responsable de la Clínica ni el Odontólogo.
    //
    // Precondiciones de sistema: el paciente inició sesión; el turno está registrado como
    // "Finalizado"; el paciente está "Habilitado". Estas dos últimas son condiciones
    // independientes (ver Turno.PendienteDeValoracion y el comentario de alcance en
    // Turno.Finalizar()); no se vuelve a validar acá si la atención fue efectivamente cobrada
    // -- eso ya es responsabilidad de CUU03 al llevar el turno a "Finalizado", y la Matriz
    // CRUD solo lista a CUU04 leyendo Turno y Pacientes, sin actualizar ninguno de los dos.
    //
    // RN14: una vez registrada la valoración, el sistema deshabilita al paciente para volver
    // a valorar la misma atención (relación 1:1 Atención-Valoración).
    // =========================================================================
    public class ValorarAtencionService : IValorarAtencionService
    {
        private readonly ITurnoRepository _turnoRepository;
        private readonly IAtencionOdontologicaRepository _atencionRepository;

        public ValorarAtencionService(
            ITurnoRepository turnoRepository,
            IAtencionOdontologicaRepository atencionRepository)
        {
            _turnoRepository = turnoRepository;
            _atencionRepository = atencionRepository;
        }

        // Camino básico, paso 1: "El paciente consulta las atenciones odontológicas
        // finalizadas pendientes de valoración. El sistema muestra el listado
        // correspondiente, indicando para cada atención el turno, la fecha y hora de inicio,
        // la especialidad y el odontólogo que la realizó." Alt 1.a: si no hay ninguna, se
        // informa con el mensaje literal del diccionario de datos.
        public async Task<PendientesDeValoracionResultDTO> ObtenerPendientesDeValoracionAsync(
            string tipoDocumentoPaciente, string nroDocumentoPaciente)
        {
            var turnos = await _turnoRepository.GetByPacienteAsync(tipoDocumentoPaciente, nroDocumentoPaciente);

            var pendientes = turnos
                .Where(t => t.PendienteDeValoracion)
                .OrderByDescending(t => t.Atencion!.FechaHoraAtencionInicio)
                .Select(t => new AtencionPendienteValoracionDTO
                {
                    NroTurno = t.NroTurno,
                    FechaHoraAtencionInicio = t.Atencion!.FechaHoraAtencionInicio,
                    NombreEspecialidad = t.Especialidad?.Nombre ?? string.Empty,
                    NombreOdontologo = t.Odontologo != null ? $"{t.Odontologo.Nombre} {t.Odontologo.Apellido}" : string.Empty
                })
                .ToList();

            if (pendientes.Count == 0)
            {
                // Alt 1.a.1, diccionario: sMensajeSinAtencionesPendientes.
                return new PendientesDeValoracionResultDTO
                {
                    Resultado = ResultadoPendientesDeValoracion.SinPendientes,
                    Mensaje = "No tiene atenciones odontológicas pendientes de valoración."
                };
            }

            return new PendientesDeValoracionResultDTO
            {
                Resultado = ResultadoPendientesDeValoracion.Ok,
                Mensaje = $"Tiene {pendientes.Count} atención(es) pendiente(s) de valoración.",
                Pendientes = pendientes
            };
        }

        // Camino básico, paso 2 / Alt 2.a: "El paciente selecciona la atención que desea
        // valorar e indica la calificación correspondiente [...]. El sistema registra la
        // valoración y deshabilita al paciente para volver a valorar la misma atención." Alt
        // 2.a: las observaciones son opcionales.
        public async Task<RegistrarValoracionResultDTO> RegistrarValoracionAsync(
            string tipoDocumentoPaciente,
            string nroDocumentoPaciente,
            int nroTurno,
            int calificacion,
            string? observaciones)
        {
            var turno = await _turnoRepository.GetAsync(nroTurno);

            // CUU04 no tiene otros actores (a diferencia de CUU02/CUU03): el turno tiene que
            // ser del paciente logueado, nadie puede valorar en nombre de otro paciente.
            var esDelPaciente = turno != null
                && turno.TipoDocumentoPaciente == tipoDocumentoPaciente
                && turno.NroDocumentoPaciente == nroDocumentoPaciente;

            if (turno == null || !esDelPaciente || !turno.PendienteDeValoracion)
            {
                return new RegistrarValoracionResultDTO
                {
                    Resultado = ResultadoRegistrarValoracion.NoDisponible,
                    Mensaje = "Esa atención ya no está disponible para valorar: puede que no te pertenezca, que la atención todavía no esté \"Finalizada\", o que ya tenga una valoración registrada."
                };
            }

            if (calificacion < 1 || calificacion > 5)
            {
                return new RegistrarValoracionResultDTO
                {
                    Resultado = ResultadoRegistrarValoracion.CalificacionInvalida,
                    Mensaje = "La calificación debe estar entre 1 y 5 estrellas."
                };
            }

            // MD/MDF: observaciones es un campo opcional de hasta 500 caracteres.
            var observacionesNormalizadas = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim();
            if (observacionesNormalizadas != null && observacionesNormalizadas.Length > 500)
            {
                return new RegistrarValoracionResultDTO
                {
                    Resultado = ResultadoRegistrarValoracion.ObservacionesInvalidas,
                    Mensaje = "Las observaciones no pueden superar los 500 caracteres."
                };
            }

            var valoracion = new Valoracion(0, calificacion, observacionesNormalizadas, turno.Atencion!.IdAtencion);
            await _atencionRepository.AgregarValoracionAsync(valoracion);

            return new RegistrarValoracionResultDTO
            {
                Resultado = ResultadoRegistrarValoracion.Registrada,
                Mensaje = "¡Gracias por tu valoración! Fue registrada correctamente."
            };
        }
    }
}
