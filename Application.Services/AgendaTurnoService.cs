using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    // Esta interfaz modela específicamente los pasos del caso de uso: consultar estado
    // del paciente, listar días/horarios disponibles, informar la política de cancelación
    // y confirmar la reserva, devolviendo en cada paso el camino alternativo correspondiente
    // cuando aplica (paciente inhabilitado, turno pendiente, obra social sin convenio, etc.).
    public interface IAgendaTurnoService
    {
        // Camino básico, paso 2: valida el estado del paciente antes de mostrarle el calendario.
        Task<EstadoPacienteTurnoDTO> ConsultarEstadoParaAgendarAsync(string tipoDocumentoPaciente, string nroDocumentoPaciente);

        // Camino básico, pasos 2 y 3: calendario de días y horarios disponibles para una especialidad.
        Task<IEnumerable<DateTime>> ObtenerDiasDisponiblesAsync(int idEspecialidad, int diasHaciaAdelante = 30);
        Task<IEnumerable<HorarioDisponibleDTO>> ObtenerHorariosDisponiblesAsync(int idEspecialidad, DateTime dia);

        // Camino básico, paso 5: texto exacto de la política de cancelación (diccionario de datos).
        string ObtenerMensajePoliticaCancelacion();

        // Alt 3.a.2.a / 5.b: valida convenio vigente de la obra social para la especialidad.
        Task<bool> ObraSocialCubreEspecialidadAsync(string identificadorOS, int idEspecialidad);

        // Camino básico, pasos 4 a 6: confirma y registra el turno (actor: Paciente).
        Task<AgendarTurnoResultDTO> AgendarTurnoAsync(AgendarTurnoRequestDTO request);

        // Alt 3.a: carga manual del turno por el Responsable de la Clínica.
        Task<AgendarTurnoResultDTO> AgendarTurnoManualAsync(AgendarTurnoRequestDTO request);

        // Alt 2.a.1.a: el paciente abona la deuda y vuelve a quedar Habilitado.
        Task PagarDeudaAsync(string tipoDocumentoPaciente, string nroDocumentoPaciente);
    }

    public class AgendaTurnoService : IAgendaTurnoService
    {
        // El CUU01 y las Reglas de Negocio no especifican una duración fija de turno;
        // se asume 30 minutos para poder particionar cada DisponibilidadHoraria
        // (HoraInicio-HoraFin) de un odontólogo en franjas reservables concretas.
        private const int DuracionTurnoMinutos = 30;

        // Debe coincidir exactamente con los valores de DisponibilidadHoraria.DiaSemana
        // sembrados en DbInitializer ("Lunes", "Martes", "Miércoles", "Jueves", "Viernes").
        private static readonly string[] DiasSemanaEs =
            { "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" };

        private readonly IPacienteRepository _pacienteRepository;
        private readonly IOdontologoRepository _odontologoRepository;
        private readonly IEspecialidadRepository _especialidadRepository;
        private readonly IObraSocialRepository _obraSocialRepository;
        private readonly ITurnoRepository _turnoRepository;
        private readonly IComprobanteTurnoRepository _comprobanteRepository;

        public AgendaTurnoService(
            IPacienteRepository pacienteRepository,
            IOdontologoRepository odontologoRepository,
            IEspecialidadRepository especialidadRepository,
            IObraSocialRepository obraSocialRepository,
            ITurnoRepository turnoRepository,
            IComprobanteTurnoRepository comprobanteRepository)
        {
            _pacienteRepository = pacienteRepository;
            _odontologoRepository = odontologoRepository;
            _especialidadRepository = especialidadRepository;
            _obraSocialRepository = obraSocialRepository;
            _turnoRepository = turnoRepository;
            _comprobanteRepository = comprobanteRepository;
        }

        public string ObtenerMensajePoliticaCancelacion() =>
            "La cancelación o reprogramación del turno debe solicitarse con una antelación mínima de 24 horas. " +
            "Si no se presenta ni cancela dentro de ese plazo, el turno se registrará como \"Ausente\", se le " +
            "asignará la multa correspondiente y quedará inhabilitado para agendar nuevos turnos hasta " +
            "regularizar su situación con el odontólogo o el responsable de la clínica.";

        public async Task<EstadoPacienteTurnoDTO> ConsultarEstadoParaAgendarAsync(string tipoDocumentoPaciente, string nroDocumentoPaciente)
        {
            var paciente = await _pacienteRepository.GetAsync(tipoDocumentoPaciente, nroDocumentoPaciente)
                ?? throw new ArgumentException("No se encontró el paciente indicado.");

            var turnoPendiente = await ObtenerTurnoPendienteAsync(tipoDocumentoPaciente, nroDocumentoPaciente);

            return new EstadoPacienteTurnoDTO
            {
                Habilitado = paciente.EstadoPaciente == "HABILITADO",
                MontoAdeudado = paciente.MontoAdeudado ?? 0m,
                TieneTurnoPendiente = turnoPendiente != null,
                TurnoPendiente = turnoPendiente == null ? null : MapTurno(turnoPendiente)
            };
        }

        public async Task<IEnumerable<DateTime>> ObtenerDiasDisponiblesAsync(int idEspecialidad, int diasHaciaAdelante = 30)
        {
            var odontologos = (await _odontologoRepository.GetByEspecialidadAsync(idEspecialidad)).ToList();

            var diasConDisponibilidad = odontologos
                .SelectMany(o => o.DisponibilidadesHorarias)
                .Where(d => d.IdEspecialidad == idEspecialidad)
                .Select(d => d.DiaSemana)
                .Distinct()
                .ToHashSet();

            var resultado = new List<DateTime>();
            var hoy = DateTime.Today;

            for (var i = 0; i <= diasHaciaAdelante; i++)
            {
                var fecha = hoy.AddDays(i);
                if (!diasConDisponibilidad.Contains(DiasSemanaEs[(int)fecha.DayOfWeek]))
                    continue;

                var horarios = await ObtenerHorariosDisponiblesAsync(idEspecialidad, fecha);
                if (horarios.Any())
                    resultado.Add(fecha);
            }

            return resultado;
        }

        public async Task<IEnumerable<HorarioDisponibleDTO>> ObtenerHorariosDisponiblesAsync(int idEspecialidad, DateTime dia)
        {
            var especialidad = await _especialidadRepository.GetAsync(idEspecialidad);
            var nombreEspecialidad = especialidad?.Nombre ?? string.Empty;
            var nombreDia = DiasSemanaEs[(int)dia.DayOfWeek];

            var odontologos = (await _odontologoRepository.GetByEspecialidadAsync(idEspecialidad)).ToList();
            var turnosDelDia = (await _turnoRepository.GetByFechaAsync(dia))
                .Where(t => t.EstadoTurno != "CANCELADO")
                .ToList();

            var resultado = new List<HorarioDisponibleDTO>();

            foreach (var odontologo in odontologos)
            {
                var disponibilidadesDelDia = odontologo.DisponibilidadesHorarias
                    .Where(d => d.IdEspecialidad == idEspecialidad && d.DiaSemana == nombreDia);

                foreach (var disponibilidad in disponibilidadesDelDia)
                {
                    var horaActual = disponibilidad.HoraInicio;

                    while (horaActual.Add(TimeSpan.FromMinutes(DuracionTurnoMinutos)) <= disponibilidad.HoraFin)
                    {
                        var fechaHoraTurno = dia.Date.Add(horaActual.ToTimeSpan());

                        var yaOcupado = turnosDelDia.Any(t =>
                            t.FechaHoraTurno == fechaHoraTurno &&
                            t.TipoDocumentoOdontologo == odontologo.TipoDocumento &&
                            t.NroDocumentoOdontologo == odontologo.NroDocumento);

                        if (!yaOcupado && fechaHoraTurno > DateTime.Now)
                        {
                            resultado.Add(new HorarioDisponibleDTO
                            {
                                FechaHoraTurno = fechaHoraTurno,
                                IdEspecialidad = idEspecialidad,
                                NombreEspecialidad = nombreEspecialidad,
                                OdontologoTipoDocumento = odontologo.TipoDocumento,
                                OdontologoNroDocumento = odontologo.NroDocumento,
                                NombreOdontologo = $"{odontologo.Nombre} {odontologo.Apellido}"
                            });
                        }

                        horaActual = horaActual.Add(TimeSpan.FromMinutes(DuracionTurnoMinutos));
                    }
                }
            }

            return resultado.OrderBy(h => h.FechaHoraTurno).ToList();
        }

        public async Task<bool> ObraSocialCubreEspecialidadAsync(string identificadorOS, int idEspecialidad)
        {
            if (string.IsNullOrWhiteSpace(identificadorOS))
                return false;

            var obraSocial = await _obraSocialRepository.GetAsync(identificadorOS);
            return obraSocial != null
                && obraSocial.EstadoOS == "ACTIVA"
                && obraSocial.Convenios.Any(c => c.IdEspecialidad == idEspecialidad);
        }

        public async Task<AgendarTurnoResultDTO> AgendarTurnoAsync(AgendarTurnoRequestDTO request)
        {
            var paciente = await _pacienteRepository.GetAsync(request.TipoDocumentoPaciente, request.NroDocumentoPaciente)
                ?? throw new ArgumentException("No se encontró el paciente indicado.");

            // Alt 2.a: paciente en estado "Inhabilitado" por deuda pendiente.
            if (paciente.EstadoPaciente != "HABILITADO")
            {
                return new AgendarTurnoResultDTO
                {
                    Resultado = ResultadoAgendarTurno.Inhabilitado,
                    Mensaje = "Tenés una deuda pendiente. Regularizala para poder agendar un nuevo turno.",
                    MontoAdeudado = paciente.MontoAdeudado ?? 0m
                };
            }

            // Alt 2.b / RN8: no puede reservar si ya tiene un turno pendiente de atención.
            var turnoPendiente = await ObtenerTurnoPendienteAsync(request.TipoDocumentoPaciente, request.NroDocumentoPaciente);
            if (turnoPendiente != null)
            {
                return new AgendarTurnoResultDTO
                {
                    Resultado = ResultadoAgendarTurno.TurnoPendienteExistente,
                    Mensaje = "Ya tenés un turno reservado pendiente de atención.",
                    TurnoPendiente = MapTurno(turnoPendiente)
                };
            }

            var odontologo = await _odontologoRepository.GetAsync(request.OdontologoTipoDocumento, request.OdontologoNroDocumento)
                ?? throw new ArgumentException("No se encontró el odontólogo indicado.");

            if (!TieneDisponibilidadValida(odontologo, request.IdEspecialidad, request.FechaHoraTurno))
            {
                return new AgendarTurnoResultDTO
                {
                    Resultado = ResultadoAgendarTurno.HorarioNoDisponible,
                    Mensaje = "El horario seleccionado ya no está disponible para ese odontólogo. Elegí otro horario."
                };
            }

            var modalidad = (request.ModalidadPago ?? "PARTICULAR").ToUpper();

            // Alt 5.b: obra social sin convenio vigente para la especialidad del turno.
            if (modalidad == "OBRA_SOCIAL" && !await ObraSocialCubreEspecialidadAsync(paciente.IdentificadorOS ?? string.Empty, request.IdEspecialidad))
            {
                return new AgendarTurnoResultDTO
                {
                    Resultado = ResultadoAgendarTurno.ObraSocialSinConvenio,
                    Mensaje = "La obra social seleccionada no tiene un convenio vigente para la especialidad de este turno. Seleccione otra forma de abonar."
                };
            }

            // Choque de agenda (otro paciente reservó el mismo horario mientras se completaba el wizard).
            if (await _turnoRepository.TurnoExistsAsync(request.FechaHoraTurno, request.OdontologoTipoDocumento, request.OdontologoNroDocumento))
            {
                return new AgendarTurnoResultDTO
                {
                    Resultado = ResultadoAgendarTurno.HorarioNoDisponible,
                    Mensaje = "Ese horario ya fue reservado por otro paciente. Elegí otro horario."
                };
            }

            return await RegistrarTurnoAsync(request, paciente, odontologo, modalidad, "¡Turno agendado exitosamente!");
        }

        public async Task<AgendarTurnoResultDTO> AgendarTurnoManualAsync(AgendarTurnoRequestDTO request)
        {
            // Alt 3.a: el paciente no encuentra turnos disponibles/convenientes y se comunica con
            // la clínica. El Responsable identifica al paciente, asigna especialidad, odontólogo,
            // fecha/hora acordadas y forma de pago "por fuera" del calendario de autoservicio: por
            // eso NO se valida acá habilitación del paciente, turno pendiente previo, ni la grilla
            // de disponibilidad horaria (son válidas solo para el circuito estándar de reservas).
            // Sí se preserva la validación de convenio de obra social (3.a.2.a) y de choque de agenda.
            var paciente = await _pacienteRepository.GetAsync(request.TipoDocumentoPaciente, request.NroDocumentoPaciente)
                ?? throw new ArgumentException("No se encontró el paciente indicado.");

            var odontologo = await _odontologoRepository.GetAsync(request.OdontologoTipoDocumento, request.OdontologoNroDocumento)
                ?? throw new ArgumentException("No se encontró el odontólogo indicado.");

            var modalidad = (request.ModalidadPago ?? "PARTICULAR").ToUpper();

            if (modalidad == "OBRA_SOCIAL" && !await ObraSocialCubreEspecialidadAsync(paciente.IdentificadorOS ?? string.Empty, request.IdEspecialidad))
            {
                return new AgendarTurnoResultDTO
                {
                    Resultado = ResultadoAgendarTurno.ObraSocialSinConvenio,
                    Mensaje = "La obra social seleccionada no tiene un convenio vigente para la especialidad de este turno. Seleccione otra forma de abonar."
                };
            }

            if (await _turnoRepository.TurnoExistsAsync(request.FechaHoraTurno, request.OdontologoTipoDocumento, request.OdontologoNroDocumento))
            {
                return new AgendarTurnoResultDTO
                {
                    Resultado = ResultadoAgendarTurno.HorarioNoDisponible,
                    Mensaje = "Ese odontólogo ya tiene un turno registrado en ese horario."
                };
            }

            return await RegistrarTurnoAsync(request, paciente, odontologo, modalidad, "Turno registrado manualmente por el responsable de la clínica.");
        }

        public async Task PagarDeudaAsync(string tipoDocumentoPaciente, string nroDocumentoPaciente)
        {
            // Alt 2.a.1.a: "El paciente abona la deuda. El sistema registra el pago y cambia el
            // estado del paciente a Habilitado." Implementación mínima acotada al alcance del
            // CUU01 (salda el total adeudado y rehabilita al paciente); el circuito completo de
            // medios de pago corresponde a la Gestión de Estado de Cuenta del paciente.
            var paciente = await _pacienteRepository.GetAsync(tipoDocumentoPaciente, nroDocumentoPaciente)
                ?? throw new ArgumentException("No se encontró el paciente indicado.");

            paciente.SetMontoAdeudado(0m);
            paciente.SetEstadoPaciente("HABILITADO");
            await _pacienteRepository.UpdateAsync(paciente);
        }

        private async Task<AgendarTurnoResultDTO> RegistrarTurnoAsync(
            AgendarTurnoRequestDTO request, Paciente paciente, Odontologo odontologo, string modalidad, string mensajeExito)
        {
            // Camino básico, paso 6: se registra el turno en estado "Reservado" (ME - Turno,
            // estado inicial) asociado a paciente, odontólogo, especialidad, fecha/hora y
            // modalidad de pago, y se emite el comprobante correspondiente.
            var turno = new Turno(
                0,
                request.FechaHoraTurno,
                modalidad,
                request.IdEspecialidad,
                request.OdontologoTipoDocumento,
                request.OdontologoNroDocumento,
                request.TipoDocumentoPaciente,
                request.NroDocumentoPaciente,
                "RESERVADO");

            await _turnoRepository.AddAsync(turno);

            var comprobante = new ComprobanteDeTurno(0, turno.NroTurno, DateTime.Now);
            await _comprobanteRepository.AddAsync(comprobante);

            var especialidad = await _especialidadRepository.GetAsync(request.IdEspecialidad);

            return new AgendarTurnoResultDTO
            {
                Resultado = ResultadoAgendarTurno.Reservado,
                Mensaje = mensajeExito,
                NroTurno = turno.NroTurno,
                Comprobante = new ComprobanteTurnoDTO
                {
                    NroComprobante = comprobante.NroComprobante,
                    FechaHoraEmision = comprobante.FechaHoraEmision,
                    FechaHoraTurno = turno.FechaHoraTurno,
                    NombrePaciente = paciente.Nombre,
                    ApellidoPaciente = paciente.Apellido,
                    NombreEspecialidad = especialidad?.Nombre ?? string.Empty,
                    NombreOdontologo = odontologo.Nombre,
                    ApellidoOdontologo = odontologo.Apellido
                }
            };
        }

        private static bool TieneDisponibilidadValida(Odontologo odontologo, int idEspecialidad, DateTime fechaHoraTurno)
        {
            var nombreDia = DiasSemanaEs[(int)fechaHoraTurno.DayOfWeek];
            var horaTurno = TimeOnly.FromDateTime(fechaHoraTurno);
            var horaFinTurno = horaTurno.Add(TimeSpan.FromMinutes(DuracionTurnoMinutos));

            return odontologo.DisponibilidadesHorarias.Any(d =>
                d.IdEspecialidad == idEspecialidad &&
                d.DiaSemana == nombreDia &&
                horaTurno >= d.HoraInicio &&
                horaFinTurno <= d.HoraFin);
        }

        private async Task<Turno?> ObtenerTurnoPendienteAsync(string tipoDocumentoPaciente, string nroDocumentoPaciente)
        {
            var turnosPaciente = await _turnoRepository.GetByPacienteAsync(tipoDocumentoPaciente, nroDocumentoPaciente);
            // "Turno pendiente de atención" (RN8 / CUU01 alt 2.b) = turno vigente en estado "Reservado".
            return turnosPaciente.FirstOrDefault(t => t.EstadoTurno == "RESERVADO");
        }

        private static TurnoOdontologicoDTO MapTurno(Turno turno)
        {
            return new TurnoOdontologicoDTO
            {
                Id = turno.NroTurno,
                Fecha = turno.FechaHoraTurno.Date,
                HorarioTurno = TimeOnly.FromDateTime(turno.FechaHoraTurno),
                EstadoTurno = turno.EstadoTurno,
                ModalidadPago = turno.ModalidadPagoElegida,
                CodEspecialidad = turno.IdEspecialidad,
                OdontologoNroDoc = int.TryParse(turno.NroDocumentoOdontologo, out var docOdont) ? docOdont : null,
                PacienteNroDoc = int.TryParse(turno.NroDocumentoPaciente, out var docPac) ? docPac : null,
                NombrePaciente = turno.Paciente != null ? $"{turno.Paciente.Nombre} {turno.Paciente.Apellido}" : null,
                NombreOdontologo = turno.Odontologo != null ? $"{turno.Odontologo.Nombre} {turno.Odontologo.Apellido}" : null,
                NombreEspecialidad = turno.Especialidad?.Nombre
            };
        }
    }
}