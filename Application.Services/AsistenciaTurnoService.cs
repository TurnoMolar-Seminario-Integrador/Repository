using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    public interface IAsistenciaTurnoService
    {
        Task<BuscarTurnoAsistenciaResultDTO> BuscarTurnoDelDiaAsync(string tipoDocumentoPaciente, string nroDocumentoPaciente);
        Task<RegistrarAsistenciaResultDTO> RegistrarPresenteAsync(int nroTurno);
        Task<RegistrarAsistenciaResultDTO> RegistrarAusenteAsync(int nroTurno);
    }

    // =========================================================================
    // CUU02 - GESTIONAR ASISTENCIA A TURNO ODONTOLÓGICO (v1.04)
    // Actor primario: Responsable de la Clínica. Otros: Odontólogo (recibe la notificación de
    // que el paciente llegó — diccionario de datos: sNotificacionPacientePresente).
    //
    // ME - Máquina de Estados (Turno): RESERVADO -> [evalúa asistencia] -> PRESENTE | AUSENTE.
    // ME - Máquina de Estados (Paciente): HABILITADO -> [se registra la ausencia] -> INHABILITADO.
    // =========================================================================
    public class AsistenciaTurnoService : IAsistenciaTurnoService
    {
        private readonly ITurnoRepository _turnoRepository;
        private readonly IPacienteRepository _pacienteRepository;

        public AsistenciaTurnoService(
            ITurnoRepository turnoRepository,
            IPacienteRepository pacienteRepository)
        {
            _turnoRepository = turnoRepository;
            _pacienteRepository = pacienteRepository;
        }

        public async Task<BuscarTurnoAsistenciaResultDTO> BuscarTurnoDelDiaAsync(string tipoDocumentoPaciente, string nroDocumentoPaciente)
        {
            // Camino básico, paso 1: "El responsable de la clínica ingresa tipo y número de
            // documento del paciente para buscar su turno. El sistema valida que corresponda a
            // un turno programado para el día de la fecha. El sistema muestra los datos del
            // turno [...]".
            var turnosPaciente = await _turnoRepository.GetByPacienteAsync(tipoDocumentoPaciente, nroDocumentoPaciente);
            var turno = turnosPaciente.FirstOrDefault(t => t.EstadoTurno == "RESERVADO" && t.FechaHoraTurno.Date == DateTime.Today);

            if (turno == null)
            {
                // Alt 1.a <durante> "El paciente no tiene turno asignado en ese día y hora":
                // 1.a.1: "El sistema informa que no existe un turno registrado con los datos
                // ingresados." FCU.
                return new BuscarTurnoAsistenciaResultDTO
                {
                    Resultado = ResultadoBusquedaTurnoAsistencia.NoEncontrado,
                    Mensaje = "No se encontró un turno registrado con los datos ingresados."
                };
            }

            return new BuscarTurnoAsistenciaResultDTO
            {
                Resultado = ResultadoBusquedaTurnoAsistencia.Encontrado,
                Mensaje = "Turno encontrado.",
                NroTurno = turno.NroTurno,
                NombrePaciente = turno.Paciente?.Nombre,
                ApellidoPaciente = turno.Paciente?.Apellido,
                FechaHoraTurno = turno.FechaHoraTurno,
                NombreEspecialidad = turno.Especialidad?.Nombre,
                NombreOdontologo = turno.Odontologo?.Nombre,
                ApellidoOdontologo = turno.Odontologo?.Apellido,
                DescripcionMaterial = turno.DescripcionMaterial
            };
        }

        public async Task<RegistrarAsistenciaResultDTO> RegistrarPresenteAsync(int nroTurno)
        {
            // Camino básico, paso 2: "El responsable de la clínica confirma la llegada del
            // paciente. El sistema registra y cambia el estado del turno a 'Presente' y envía
            // una notificación a la agenda del odontólogo asignado [...] informando que el
            // paciente ya se encuentra en la clínica listo para la atención."
            //
            // Nota de alcance: la "notificación a la agenda del odontólogo" se resuelve con el
            // turno pasando a "Presente" y quedando visible así en su agenda del día; el
            // sistema no cuenta con un mecanismo de notificaciones push/correo independiente,
            // por lo que no se modela una entidad aparte para esto.
            var turno = await ObtenerTurnoParaAsistenciaAsync(nroTurno);
            if (turno == null)
            {
                return TurnoInvalido();
            }

            turno.MarcarPresente();
            await _turnoRepository.UpdateAsync(turno);

            return new RegistrarAsistenciaResultDTO
            {
                Resultado = ResultadoRegistrarAsistencia.Presente,
                Mensaje = $"Se registró la llegada de {turno.Paciente.Nombre} {turno.Paciente.Apellido}. El odontólogo fue notificado en su agenda.",
                EstadoTurno = turno.EstadoTurno
            };
        }

        public async Task<RegistrarAsistenciaResultDTO> RegistrarAusenteAsync(int nroTurno)
        {
            // Alt 1.b <reemplaza> "El paciente no se presenta a su turno":
            // 1.b.1: "El responsable de la clínica registra la ausencia del paciente en el
            // sistema. El sistema cambia el estado del turno a 'Ausente', registra en la
            // cuenta del paciente una deuda por el monto de penalización correspondiente,
            // cambia el estado del paciente a 'Inhabilitado' e informa al responsable de la
            // clínica que la ausencia quedó registrada." FCU.
            var turno = await ObtenerTurnoParaAsistenciaAsync(nroTurno);
            if (turno == null)
            {
                return TurnoInvalido();
            }

            var paciente = await _pacienteRepository.GetAsync(turno.TipoDocumentoPaciente, turno.NroDocumentoPaciente)
                ?? throw new InvalidOperationException("No se encontró el paciente asociado al turno.");

            // RN11: "[...] habilitan el cobro del arancel de consulta correspondiente a la
            // especialidad [...] Este monto no incluye insumos ni materiales, dado que la
            // atención no llegó a realizarse."
            //
            // Es siempre Especialidad.arancelParticular, sin mirar la modalidad de pago ni el
            // Convenio de obra social: la Matriz CRUD (CRUD-1) no lista "Convenio" ni "Obra
            // Social" como leídos en CUU02 (sí en CUU01/CUU03, donde se calcula el monto de una
            // atención real), y sus Consideraciones aclaran que Obra Social solo se lee "para
            // calcular el monto a abonar" en esos dos casos de uso. Tiene sentido de negocio
            // además: una obra social no reembolsa una consulta que nunca se realizó, así que
            // la penalización por inasistencia se cobra siempre al paciente, al arancel
            // particular de la especialidad.
            var montoPenalizacion = turno.Especialidad.ArancelParticular;

            turno.MarcarAusente(montoPenalizacion);
            await _turnoRepository.UpdateAsync(turno);

            // MD - Modelo del Dominio, nota sobre Paciente.montoAdeudado (atributo derivado):
            // "Si el Turno que originó la deuda no dio lugar a una Atención Odontológica
            // (motivo 'Inasistencia' o 'Cancelación fuera de término'), entonces
            // /montoAdeudado = /montoPenalizacion de ese Turno." No es un acumulador: se fija
            // en el monto de ESTE turno. En la práctica un paciente no puede llegar acá con una
            // deuda previa sin saldar, porque CUU01 ya le impide reservar un nuevo turno
            // mientras esté "Inhabilitado".
            //
            // RN9: el paciente pasa a "Inhabilitado" hasta que pague la deuda o regularice su
            // situación con el odontólogo/responsable de la clínica.
            paciente.SetMontoAdeudado(montoPenalizacion);
            paciente.SetEstadoPaciente("INHABILITADO");
            await _pacienteRepository.UpdateAsync(paciente);

            return new RegistrarAsistenciaResultDTO
            {
                Resultado = ResultadoRegistrarAsistencia.Ausente,
                Mensaje = $"Se registró la ausencia de {paciente.Nombre} {paciente.Apellido}. Queda una deuda de ${montoPenalizacion:N0} y el paciente fue inhabilitado hasta regularizar su situación.",
                EstadoTurno = turno.EstadoTurno,
                MontoPenalizacion = montoPenalizacion,
                MontoAdeudadoTotal = montoPenalizacion,
                EstadoPaciente = paciente.EstadoPaciente
            };
        }

        // Validación común a los pasos 2 y alt 1.b: el turno debe existir y seguir "Reservado"
        // para el día de hoy (evita doble marcación o marcar un turno de otro día).
        private async Task<Turno?> ObtenerTurnoParaAsistenciaAsync(int nroTurno)
        {
            var turno = await _turnoRepository.GetAsync(nroTurno);
            if (turno == null || turno.EstadoTurno != "RESERVADO" || turno.FechaHoraTurno.Date != DateTime.Today)
            {
                return null;
            }
            return turno;
        }

        private static RegistrarAsistenciaResultDTO TurnoInvalido()
        {
            return new RegistrarAsistenciaResultDTO
            {
                Resultado = ResultadoRegistrarAsistencia.TurnoInvalido,
                Mensaje = "El turno indicado ya no está disponible para registrar la asistencia. Volvé a buscarlo.",
                EstadoTurno = string.Empty
            };
        }
    }
}
