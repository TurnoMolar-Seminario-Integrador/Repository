using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    // =========================================================================
    // CUF07 - CANCELAR TURNO / CUF08 - REPROGRAMAR TURNO
    //
    // Corrección de defecto (arquitectura): a diferencia de CUU01-04, esta lógica vivía
    // inline en TurnoController, manipulando el DbContext directo en vez de pasar por
    // Application.Services -- el único lugar del proyecto que no seguía el criterio ya
    // aplicado al login y al resto de los casos de uso (ver comentarios en AuthService /
    // AgendaTurnoService). Se corrige acá con el mismo criterio.
    //
    // De paso quedan corregidos dos defectos puntuales que estaban en esa versión inline:
    //   1) La penalización por cancelación tardía era un monto fijo ($5000) sin relación con
    //      la especialidad ni la modalidad de pago del turno. Ahora sigue la fórmula del MD
    //      para Turno.montoPenalizacion (arancelConvenio u arancelParticular según
    //      modalidadPagoElegida), el mismo criterio que AsistenciaTurnoService.
    //      RegistrarAusenteAsync (CUU02) y FinalizarAtencionService.RegistrarAtencionAsync
    //      (CUU03, para /montoTotal).
    //   2) Reprogramar no validaba que el nuevo horario estuviera dentro de la disponibilidad
    //      real del odontólogo ni que no chocara con otro turno ya existente (a diferencia de
    //      AgendaTurnoService.AgendarTurnoAsync, que sí valida ambas cosas). Se reutiliza el
    //      mismo helper (AgendaTurnoService.TieneDisponibilidadValida, ahora internal).
    // =========================================================================
    public interface IGestionTurnoService
    {
        // CUF07. Con menos de 24 hs de anticipación (o con el turno ya pasado) la cancelación no se
        // ejecuta hasta que se confirme: sin confirmaFueraDeTermino devuelve RequiereConfirmacion con la
        // advertencia del cobro (alt 3.a.1); con confirmaFueraDeTermino = true cancela, cobra la
        // penalización e inhabilita al paciente (alt 3.a.2).
        Task<CancelarTurnoResultDTO> CancelarTurnoAsync(
            int nroTurno, string tipoDocumentoPaciente, string nroDocumentoPaciente, string? motivoCancelacion,
            bool confirmaFueraDeTermino = false);

        Task<ReprogramarTurnoResultDTO> ReprogramarTurnoAsync(
            int nroTurnoOriginal, string tipoDocumentoPaciente, string nroDocumentoPaciente, DateTime nuevaFechaHoraTurno);
    }

    public class GestionTurnoService : IGestionTurnoService
    {
        // CUF07, diccionario de datos, alt 3.a.1: sAvisoCancelacionFueraDeTermino.
        public const string MensajeCancelacionFueraDeTermino =
            "La cancelación se está solicitando fuera de término. Se aplicará el cobro correspondiente por inasistencia sin aviso.";

        private readonly ITurnoRepository _turnoRepository;
        private readonly IPacienteRepository _pacienteRepository;
        private readonly IOdontologoRepository _odontologoRepository;
        private readonly IComprobanteTurnoRepository _comprobanteRepository;
        private readonly IObraSocialRepository _obraSocialRepository;

        public GestionTurnoService(
            ITurnoRepository turnoRepository,
            IPacienteRepository pacienteRepository,
            IOdontologoRepository odontologoRepository,
            IComprobanteTurnoRepository comprobanteRepository,
            IObraSocialRepository obraSocialRepository)
        {
            _turnoRepository = turnoRepository;
            _pacienteRepository = pacienteRepository;
            _odontologoRepository = odontologoRepository;
            _comprobanteRepository = comprobanteRepository;
            _obraSocialRepository = obraSocialRepository;
        }

        public async Task<CancelarTurnoResultDTO> CancelarTurnoAsync(
            int nroTurno, string tipoDocumentoPaciente, string nroDocumentoPaciente, string? motivoCancelacion,
            bool confirmaFueraDeTermino = false)
        {
            var turno = await _turnoRepository.GetAsync(nroTurno);
            var esDelPaciente = turno != null
                && turno.TipoDocumentoPaciente == tipoDocumentoPaciente
                && turno.NroDocumentoPaciente == nroDocumentoPaciente;

            if (turno == null || !esDelPaciente)
            {
                return new CancelarTurnoResultDTO
                {
                    Resultado = ResultadoCancelarTurno.TurnoInvalido,
                    Mensaje = "No se encontró el turno a cancelar."
                };
            }

            // Solo un turno "Reservado" puede cancelarse (ME - Turno): desde que el
            // responsable registra la asistencia el paciente ya no puede modificarlo.
            if (!turno.PermiteCancelarOReprogramar)
            {
                return new CancelarTurnoResultDTO
                {
                    Resultado = ResultadoCancelarTurno.TurnoInvalido,
                    Mensaje = "Este turno ya no admite cancelación: solo pueden cancelarse los turnos en estado Reservado."
                };
            }

            // CUF07 paso 3: se valida la anticipación (RN10: 24 hs o más sin costo).
            //
            // Defecto corregido: la condición era "horasRestantes < 24 && horasRestantes > 0", así que un
            // turno cuya hora ya había pasado (y que el responsable todavía no marcó como ausente) se
            // cancelaba gratis, mientras que la ausencia equivalente (CUU02) sí cobra la penalización.
            // Un turno vencido está, con más razón, "fuera de término" (RN9 / RN11: inasistencia sin
            // aviso).
            var fueraDeTermino = !ReglaAntelacion.CumpleAntelacionMinima(turno.FechaHoraTurno, DateTime.Now);
            decimal? penalizacion = null;

            if (fueraDeTermino)
            {
                // RN11 / MD - nota sobre Turno.montoPenalizacion (atributo derivado): depende de
                // la modalidadPagoElegida del turno -- arancelConvenio para Obra Social,
                // arancelParticular para Particular. Mismo criterio que
                // AsistenciaTurnoService.RegistrarAusenteAsync y FinalizarAtencionService.
                // RegistrarAtencionAsync (para /montoTotal).
                //
                // Defecto corregido: antes era un $5000 fijo, sin relación con la especialidad
                // ni con la modalidad de pago.
                if (turno.ModalidadPagoElegida == "OBRA_SOCIAL")
                {
                    var obraSocial = turno.Paciente.IdentificadorOS != null
                        ? await _obraSocialRepository.GetAsync(turno.Paciente.IdentificadorOS)
                        : null;
                    var convenio = obraSocial?.Convenios.FirstOrDefault(c => c.IdEspecialidad == turno.IdEspecialidad);
                    if (convenio == null)
                    {
                        return new CancelarTurnoResultDTO
                        {
                            Resultado = ResultadoCancelarTurno.TurnoInvalido,
                            Mensaje = "El paciente no tiene un convenio vigente con su obra social para esta especialidad. No se puede calcular la penalización."
                        };
                    }
                    penalizacion = convenio.ArancelConvenio;
                }
                else
                {
                    penalizacion = turno.Especialidad.ArancelParticular;
                }

                // Un arancel en cero (Especialidad.ArancelParticular admite 0) no deja nada que cobrar:
                // no hay deuda que registrar ni motivo para advertir o inhabilitar.
                if (penalizacion <= 0m)
                {
                    penalizacion = null;
                }
            }

            if (penalizacion.HasValue)
            {
                // Alt 3.a.1: "El sistema informa que la cancelación se está solicitando fuera de
                // término y advierte sobre el cobro correspondiente." Alt 3.a.2: "El paciente
                // confirma." Defecto corregido: el cobro y la inhabilitación se aplicaban en el mismo
                // paso, sin que el paciente (o el responsable en su nombre) hubiera visto el aviso.
                if (!confirmaFueraDeTermino)
                {
                    return new CancelarTurnoResultDTO
                    {
                        Resultado = ResultadoCancelarTurno.RequiereConfirmacion,
                        Mensaje = MensajeCancelacionFueraDeTermino,
                        Penalizacion = penalizacion,
                        FechaHoraTurno = turno.FechaHoraTurno,
                        NombreEspecialidad = turno.Especialidad?.Nombre
                    };
                }

                var paciente = await _pacienteRepository.GetAsync(tipoDocumentoPaciente, nroDocumentoPaciente)
                    ?? throw new InvalidOperationException("No se encontró el paciente asociado al turno.");

                // MD - nota sobre Paciente.montoAdeudado (atributo derivado): "/montoAdeudado =
                // /montoPenalizacion de ese Turno" -- asignación directa, no acumulada.
                paciente.SetMontoAdeudado(penalizacion.Value);
                paciente.SetEstadoPaciente("INHABILITADO");
                await _pacienteRepository.UpdateAsync(paciente);
            }

            turno.Cancelar(motivoCancelacion ?? "Cancelado por el paciente", penalizacion);
            await _turnoRepository.UpdateAsync(turno);

            return new CancelarTurnoResultDTO
            {
                Resultado = ResultadoCancelarTurno.Cancelado,
                // Alt 3.a.2, diccionario: sConfirmacionCancelacionFueraDeTermino = estadoTurno +
                // montoAdeudado + estadoPaciente.
                Mensaje = penalizacion.HasValue
                    ? $"El turno fue cancelado fuera de término. Se registró una deuda de ${penalizacion.Value:N0} por la penalización y quedaste Inhabilitado para agendar nuevos turnos hasta regularizarla."
                    : "El turno fue cancelado correctamente sin penalizaciones.",
                Penalizacion = penalizacion,
                EstadoTurno = turno.EstadoTurno,
                MontoAdeudado = penalizacion,
                EstadoPaciente = penalizacion.HasValue ? "INHABILITADO" : null
            };
        }

        public async Task<ReprogramarTurnoResultDTO> ReprogramarTurnoAsync(
            int nroTurnoOriginal, string tipoDocumentoPaciente, string nroDocumentoPaciente, DateTime nuevaFechaHoraTurno)
        {
            var turnoOriginal = await _turnoRepository.GetAsync(nroTurnoOriginal);
            var esDelPaciente = turnoOriginal != null
                && turnoOriginal.TipoDocumentoPaciente == tipoDocumentoPaciente
                && turnoOriginal.NroDocumentoPaciente == nroDocumentoPaciente;

            if (turnoOriginal == null || !esDelPaciente)
            {
                return new ReprogramarTurnoResultDTO
                {
                    Resultado = ResultadoReprogramarTurno.TurnoInvalido,
                    Mensaje = "No se encontró el turno original para reprogramar."
                };
            }

            if (!turnoOriginal.PermiteCancelarOReprogramar)
            {
                return new ReprogramarTurnoResultDTO
                {
                    Resultado = ResultadoReprogramarTurno.TurnoInvalido,
                    Mensaje = "Este turno ya no admite reprogramación: solo pueden reprogramarse los turnos en estado Reservado."
                };
            }

            // CUF08 alt. 3.a: "El sistema informa que la reprogramación está fuera de término y
            // no permite continuar con el cambio de fecha, sugiriendo la cancelación del turno
            // con el arancel correspondiente." A diferencia de Cancelar (CUF07 alt. 3.a), que sí
            // permite continuar cobrando la penalización, acá se bloquea directamente: no se
            // llega a validar disponibilidad ni a crear el turno nuevo.
            //
            // Defecto corregido: esta validación no existía -- se detectó al citar el texto de
            // CUF08 para el resumen de esta ronda, no en la revisión de código original.
            var horasRestantes = (turnoOriginal.FechaHoraTurno - DateTime.Now).TotalHours;
            if (horasRestantes < 24)
            {
                return new ReprogramarTurnoResultDTO
                {
                    Resultado = ResultadoReprogramarTurno.FueraDeTermino,
                    Mensaje = "La reprogramación está fuera de término: no se puede reprogramar un turno con menos de 24 horas de anticipación. Podés cancelarlo en su lugar; se aplicará el arancel de penalización correspondiente."
                };
            }

            // Defecto corregido: antes se creaba el turno nuevo sin validar que el horario
            // elegido estuviera dentro de la disponibilidad real del odontólogo, ni que no
            // chocara con otro turno ya existente (a diferencia de AgendarTurnoAsync).
            var odontologo = await _odontologoRepository.GetAsync(turnoOriginal.TipoDocumentoOdontologo, turnoOriginal.NroDocumentoOdontologo)
                ?? throw new InvalidOperationException("No se encontró el odontólogo asociado al turno original.");

            // RN19: "Los turnos deberán ser solicitados con una antelación mínima de 24 horas."
            // El chequeo de arriba (CUF08 alt. 3.a) mira cuánto falta para el turno ORIGINAL;
            // este mira la fecha NUEVA que se está eligiendo -- reprogramar también "solicita"
            // un turno, igual que AgendarTurnoAsync, así que le aplica la misma regla.
            if (!ReglaAntelacion.CumpleAntelacionMinima(nuevaFechaHoraTurno, DateTime.Now))
            {
                return new ReprogramarTurnoResultDTO
                {
                    Resultado = ResultadoReprogramarTurno.FueraDeTermino,
                    Mensaje = $"Los turnos deben solicitarse con una antelación mínima de {ReglaAntelacion.HorasMinimas} horas. Elegí un horario que cumpla ese plazo."
                };
            }

            if (!AgendaTurnoService.TieneDisponibilidadValida(odontologo, turnoOriginal.IdEspecialidad, nuevaFechaHoraTurno))
            {
                return new ReprogramarTurnoResultDTO
                {
                    Resultado = ResultadoReprogramarTurno.HorarioNoDisponible,
                    Mensaje = "El horario elegido no está dentro de la disponibilidad del odontólogo para esta especialidad. Elegí otro horario."
                };
            }

            if (await _turnoRepository.TurnoExistsAsync(nuevaFechaHoraTurno, turnoOriginal.TipoDocumentoOdontologo, turnoOriginal.NroDocumentoOdontologo))
            {
                return new ReprogramarTurnoResultDTO
                {
                    Resultado = ResultadoReprogramarTurno.HorarioNoDisponible,
                    Mensaje = "Ese horario ya fue reservado por otro paciente. Elegí otro horario."
                };
            }

            // Marcar turno original como REPROGRAMADO.
            turnoOriginal.Reprogramar(nuevaFechaHoraTurno);
            await _turnoRepository.UpdateAsync(turnoOriginal);

            // Crear nuevo turno vinculado al original.
            var nuevoTurno = new Turno(
                0,
                nuevaFechaHoraTurno,
                turnoOriginal.ModalidadPagoElegida,
                turnoOriginal.IdEspecialidad,
                turnoOriginal.TipoDocumentoOdontologo,
                turnoOriginal.NroDocumentoOdontologo,
                turnoOriginal.TipoDocumentoPaciente,
                turnoOriginal.NroDocumentoPaciente,
                "RESERVADO",
                null,
                null,
                turnoOriginal.NroTurno);

            await _turnoRepository.AddAsync(nuevoTurno);

            var comprobante = new ComprobanteDeTurno(0, nuevoTurno.NroTurno, DateTime.Now);
            await _comprobanteRepository.AddAsync(comprobante);

            return new ReprogramarTurnoResultDTO
            {
                Resultado = ResultadoReprogramarTurno.Reprogramado,
                Mensaje = $"¡Turno reprogramado exitosamente para el {nuevaFechaHoraTurno:dd/MM/yyyy HH:mm} hs!",
                NroTurnoNuevo = nuevoTurno.NroTurno
            };
        }
    }
}
