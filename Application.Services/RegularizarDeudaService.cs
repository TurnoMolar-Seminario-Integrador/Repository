using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    // =========================================================================
    // CUF10 - REGULARIZAR DEUDA DEL PACIENTE (R02, v1.03)
    // Actor primario: Paciente. Otros: Responsable de la Clínica.
    // Precondición: el paciente está INHABILITADO con una deuda mayor a cero, originada por una
    // inasistencia, una cancelación fuera de término o la falta de pago de una atención (RN9).
    //
    // Camino básico:
    //   1. El paciente entra a su cuenta; el sistema muestra el saldo de deuda y el detalle del
    //      turno que la originó (nroTurno, fecha, motivo)            -> ConsultarDeudaAsync.
    //   2. El paciente indica que abonará el total de la deuda a través de la plataforma.
    //   3. El sistema registra el pago del total, ASOCIADO AL TURNO QUE LA ORIGINÓ, y pasa al
    //      paciente a HABILITADO                                       -> PagarDeudaAsync.
    // Alt 1.a: sin deuda -> "No hay saldo de deuda pendiente para regularizar."
    // Alt 2.a <reemplaza>: pago presencial. El responsable identifica al paciente (tipo y número de
    //      documento) y registra el cobro en el sistema; sigue en el paso 3
    //                                                      -> RegistrarCobroPresencialAsync.
    //
    // Defecto corregido (C-08 y el hueco de CUF10): la versión anterior ("Pagar deuda") rehabilitaba a
    // cualquier paciente inhabilitado con solo apretar el botón, sin que nadie cobrara ni quedara
    // registrado con qué método; la deuda de una cancelación fuera de término no generaba ningún Pago
    // (el turno CANCELADO ni se buscaba); y la alternativa 2.a (cobro presencial del responsable) no
    // existía. Ahora todo camino que rehabilita deja un Pago real, por el total, con su método y
    // asociado al turno de origen; y sin turno de origen no se rehabilita (no hay dónde registrar el
    // pago).
    //
    // Nota de alcance: la plataforma no integra una pasarela de pagos; el pago "a través de la
    // plataforma" queda registrado con el método elegido (CUU01 alt 2.a.1.a.1: eDatosPagoDeuda =
    // tipoMetodoPago), pero ninguna entidad externa lo confirma.
    // =========================================================================
    public interface IRegularizarDeudaService
    {
        // Paso 1 / alt 1.a.
        Task<DeudaPacienteDTO> ConsultarDeudaAsync(string tipoDocumentoPaciente, string nroDocumentoPaciente);

        // Pasos 2 y 3: el paciente abona el total a través de la plataforma.
        Task<PagarDeudaResultDTO> PagarDeudaAsync(string tipoDocumentoPaciente, string nroDocumentoPaciente, string tipoMetodoPago);

        // Alt 2.a: el responsable registra el cobro presencial de la deuda del paciente.
        Task<PagarDeudaResultDTO> RegistrarCobroPresencialAsync(string tipoDocumentoPaciente, string nroDocumentoPaciente, string tipoMetodoPago);

        // Apoyo para la pantalla del responsable (alt 2.a): pacientes con saldo de deuda pendiente.
        Task<IReadOnlyList<DeudaPacienteDTO>> ListarPacientesConDeudaAsync();
    }

    public class RegularizarDeudaService : IRegularizarDeudaService
    {
        // CUF10, diccionario de datos: mensajeSinDeuda.
        public const string MensajeSinDeuda = "No hay saldo de deuda pendiente para regularizar.";

        private const string MensajeMetodoInvalido =
            "El método de pago indicado no es válido. Elegí Efectivo, Tarjeta de débito, Tarjeta de crédito o Transferencia.";

        private const string MensajeSinTurnoDeOrigen =
            "No se pudo identificar el turno que originó la deuda, por eso no se puede registrar el pago. Consultá con la clínica.";

        private readonly IPacienteRepository _pacienteRepository;
        private readonly ITurnoRepository _turnoRepository;
        private readonly IPagoRepository _pagoRepository;

        public RegularizarDeudaService(
            IPacienteRepository pacienteRepository,
            ITurnoRepository turnoRepository,
            IPagoRepository pagoRepository)
        {
            _pacienteRepository = pacienteRepository;
            _turnoRepository = turnoRepository;
            _pagoRepository = pagoRepository;
        }

        public async Task<DeudaPacienteDTO> ConsultarDeudaAsync(string tipoDocumentoPaciente, string nroDocumentoPaciente)
        {
            var paciente = await _pacienteRepository.GetAsync(tipoDocumentoPaciente, nroDocumentoPaciente)
                ?? throw new ArgumentException("No se encontró el paciente indicado.");

            return await ArmarDeudaAsync(paciente);
        }

        public Task<PagarDeudaResultDTO> PagarDeudaAsync(string tipoDocumentoPaciente, string nroDocumentoPaciente, string tipoMetodoPago)
            => RegistrarPagoDeudaAsync(tipoDocumentoPaciente, nroDocumentoPaciente, tipoMetodoPago, presencial: false);

        public Task<PagarDeudaResultDTO> RegistrarCobroPresencialAsync(string tipoDocumentoPaciente, string nroDocumentoPaciente, string tipoMetodoPago)
            => RegistrarPagoDeudaAsync(tipoDocumentoPaciente, nroDocumentoPaciente, tipoMetodoPago, presencial: true);

        public async Task<IReadOnlyList<DeudaPacienteDTO>> ListarPacientesConDeudaAsync()
        {
            var pacientes = (await _pacienteRepository.GetAllAsync())
                .Where(p => (p.MontoAdeudado ?? 0m) > 0m)
                .ToList();

            var resultado = new List<DeudaPacienteDTO>();
            foreach (var paciente in pacientes)
            {
                resultado.Add(await ArmarDeudaAsync(paciente));
            }

            return resultado
                .OrderBy(d => d.FechaHoraTurno ?? DateTime.MaxValue)
                .ToList();
        }

        // Pasos 2-3 (y alt 2.a, que continúa en el paso 3): registra el pago del TOTAL de la deuda
        // asociado al turno que la originó y habilita al paciente.
        private async Task<PagarDeudaResultDTO> RegistrarPagoDeudaAsync(
            string tipoDocumentoPaciente, string nroDocumentoPaciente, string tipoMetodoPago, bool presencial)
        {
            var paciente = await _pacienteRepository.GetAsync(tipoDocumentoPaciente, nroDocumentoPaciente)
                ?? throw new ArgumentException("No se encontró el paciente indicado.");

            var montoAdeudado = paciente.MontoAdeudado ?? 0m;
            if (montoAdeudado <= 0m)
            {
                // Alt 1.a. Si la deuda ya estaba saldada pero el estado quedó en INHABILITADO no se
                // toca nada: rehabilitar sin deuda ni pago no es parte de este caso de uso.
                return new PagarDeudaResultDTO
                {
                    Resultado = ResultadoPagarDeuda.SinDeuda,
                    Mensaje = MensajeSinDeuda,
                    MontoPagado = 0m
                };
            }

            // CUU01 alt 2.a.1.a.1: eDatosPagoDeuda = tipoMetodoPago.
            var metodo = MetodosPago.Normalizar(tipoMetodoPago);
            if (metodo == null)
            {
                return new PagarDeudaResultDTO
                {
                    Resultado = ResultadoPagarDeuda.MetodoInvalido,
                    Mensaje = MensajeMetodoInvalido,
                    MontoPagado = 0m
                };
            }

            var turnoOrigen = await BuscarTurnoDeOrigenAsync(paciente);
            if (turnoOrigen == null)
            {
                return new PagarDeudaResultDTO
                {
                    Resultado = ResultadoPagarDeuda.SinTurnoDeOrigen,
                    Mensaje = MensajeSinTurnoDeOrigen,
                    MontoPagado = 0m
                };
            }

            // Paso 3: el pago es por el TOTAL de la deuda (paso 2) y lo abona el propio paciente:
            // MU, pago particular -> AportePaciente = Monto, AporteObraSocial nulo y sin
            // IdentificadorOS. Esto incluye la deuda por falta de pago de una atención con obra
            // social: CUU03 alt 6.b la registra por el monto total, sin recalcularlo.
            var pago = new Pago(0, turnoOrigen.NroTurno, DateTime.Now, montoAdeudado, metodo, null, montoAdeudado, null);

            // Los cambios del paciente se dejan pendientes ANTES de guardar el Pago: ambos
            // repositorios comparten el DbContext de la solicitud, así que un único SaveChanges
            // persiste los dos o ninguno. Antes un fallo entre ambos pasos podía dejar un Pago sin
            // habilitar al paciente (o al revés: habilitado y sin Pago).
            paciente.SetMontoAdeudado(0m);
            paciente.SetEstadoPaciente("HABILITADO");
            await _pagoRepository.AddAsync(pago);
            await _pacienteRepository.UpdateAsync(paciente);

            var nombre = $"{paciente.Nombre} {paciente.Apellido}";
            return new PagarDeudaResultDTO
            {
                Resultado = ResultadoPagarDeuda.Regularizada,
                // Diccionario, paso 3: sConfirmacionPagoDeuda = monto + estadoPaciente(d).
                Mensaje = presencial
                    ? $"Se registró el cobro de ${montoAdeudado:N0} ({MetodosPago.Etiqueta(metodo)}) de la deuda del turno N° {turnoOrigen.NroTurno} de {nombre}. El paciente quedó Habilitado."
                    : $"¡Pago de ${montoAdeudado:N0} registrado ({MetodosPago.Etiqueta(metodo)})! Tu cuenta quedó Habilitada: ya podés agendar un nuevo turno.",
                MontoPagado = montoAdeudado,
                NroTurno = turnoOrigen.NroTurno,
                MetodoPago = metodo,
                EstadoPaciente = paciente.EstadoPaciente
            };
        }

        private async Task<DeudaPacienteDTO> ArmarDeudaAsync(Paciente paciente)
        {
            var dto = new DeudaPacienteDTO
            {
                TipoDocumento = paciente.TipoDocumento,
                NroDocumento = paciente.NroDocumento,
                NombrePaciente = $"{paciente.Apellido}, {paciente.Nombre}",
                EstadoPaciente = paciente.EstadoPaciente,
                MontoAdeudado = paciente.MontoAdeudado ?? 0m
            };

            if (dto.MontoAdeudado <= 0m)
            {
                dto.TieneDeuda = false;
                dto.Mensaje = MensajeSinDeuda;
                return dto;
            }

            dto.TieneDeuda = true;

            var turnoOrigen = await BuscarTurnoDeOrigenAsync(paciente);
            if (turnoOrigen != null)
            {
                dto.NroTurno = turnoOrigen.NroTurno;
                dto.FechaHoraTurno = turnoOrigen.FechaHoraTurno;
                dto.MotivoDeuda = turnoOrigen.MotivoDeudaPendiente;
            }

            return dto;
        }

        // El turno que originó la deuda (MD: "el Turno que originó la deuda"; RN8 y las reglas de
        // habilitación garantizan una sola deuda a la vez): el más reciente que dejó un saldo sin
        // pago -- ausente, cancelado fuera de término o finalizado sin cobro (Turno.MotivoDeudaPendiente).
        private async Task<Turno?> BuscarTurnoDeOrigenAsync(Paciente paciente)
        {
            var turnos = await _turnoRepository.GetByPacienteAsync(paciente.TipoDocumento, paciente.NroDocumento);

            return turnos
                .Where(t => t.MotivoDeudaPendiente != null)
                .OrderByDescending(t => t.FechaHoraTurno)
                .FirstOrDefault();
        }
    }
}
