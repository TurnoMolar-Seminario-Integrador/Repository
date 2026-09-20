using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    public interface IFinalizarAtencionService
    {
        Task<DatosParaAtencionResultDTO> ObtenerDatosParaAtencionAsync(
            int nroTurno,
            string tipoDocumentoOdontologoLogueado,
            string nroDocumentoOdontologoLogueado);

        Task<RegistrarAtencionOdontologicaResultDTO> RegistrarAtencionAsync(
            int nroTurno,
            string tipoDocumentoOdontologoLogueado,
            string nroDocumentoOdontologoLogueado,
            DateTime fechaHoraInicio,
            DateTime fechaHoraFin,
            string observaciones,
            List<DetalleInsumoInputDTO> insumosUtilizados);

        Task<DatosParaCobroResultDTO> ObtenerDatosParaCobroAsync(int nroTurno);

        Task<RegistrarCobroResultDTO> RegistrarCobroParticularAsync(int nroTurno, decimal monto, string tipoMetodoPago);

        Task<RegistrarCobroResultDTO> RegistrarCobroObraSocialAsync(
            int nroTurno, string tipoMetodoPago, decimal aportePaciente, decimal aporteObraSocial);

        Task<RegistrarCobroResultDTO> RegistrarFaltaDePagoAsync(int nroTurno);
    }

    // =========================================================================
    // CUU03 - FINALIZAR ATENCIÓN ODONTOLÓGICA (v1.04)
    // Actor primario: Odontólogo (pasos 1-4). Otros: Paciente de la clínica, Responsable de la
    // Clínica (pasos 5-6, el cobro).
    //
    // ME - Máquina de Estados (Turno): PRESENTE -> [el odontólogo confirma el registro de la
    // atención] -> ATENCIÓN REGISTRADA -> [el responsable de la clínica le cobra la atención al
    // paciente] -> FINALIZADO.
    // ME - Máquina de Estados (Paciente): HABILITADO -> [responsable registra la falta de pago]
    // -> INHABILITADO (alt 6.b).
    // =========================================================================
    public class FinalizarAtencionService : IFinalizarAtencionService
    {
        private readonly ITurnoRepository _turnoRepository;
        private readonly IPacienteRepository _pacienteRepository;
        private readonly IHistoriaClinicaRepository _historiaClinicaRepository;
        private readonly IAtencionOdontologicaRepository _atencionRepository;
        private readonly IInsumoRepository _insumoRepository;
        private readonly IObraSocialRepository _obraSocialRepository;
        private readonly IPagoRepository _pagoRepository;

        public FinalizarAtencionService(
            ITurnoRepository turnoRepository,
            IPacienteRepository pacienteRepository,
            IHistoriaClinicaRepository historiaClinicaRepository,
            IAtencionOdontologicaRepository atencionRepository,
            IInsumoRepository insumoRepository,
            IObraSocialRepository obraSocialRepository,
            IPagoRepository pagoRepository)
        {
            _turnoRepository = turnoRepository;
            _pacienteRepository = pacienteRepository;
            _historiaClinicaRepository = historiaClinicaRepository;
            _atencionRepository = atencionRepository;
            _insumoRepository = insumoRepository;
            _obraSocialRepository = obraSocialRepository;
            _pagoRepository = pagoRepository;
        }

        // Camino básico, paso 1: "El odontólogo selecciona el turno [...] indica al sistema que
        // desea registrar la atención e ingresa la hora de inicio y de fin [...]. El sistema
        // muestra el número de historia clínica, la fecha de creación y las atenciones
        // odontológicas previas del paciente, y habilita el formulario de carga de datos de la
        // consulta." Esto se pide ANTES de habilitar el resto del formulario (pasos 2 a 4), no
        // junto con ellos.
        public async Task<DatosParaAtencionResultDTO> ObtenerDatosParaAtencionAsync(
            int nroTurno, string tipoDocumentoOdontologoLogueado, string nroDocumentoOdontologoLogueado)
        {
            var (turno, mensajeError) = await ValidarTurnoParaAtencionAsync(nroTurno, tipoDocumentoOdontologoLogueado, nroDocumentoOdontologoLogueado);
            if (turno == null)
            {
                return new DatosParaAtencionResultDTO { Resultado = ResultadoObtenerDatosAtencion.TurnoInvalido, Mensaje = mensajeError };
            }

            var historiaClinica = await _historiaClinicaRepository.GetByPacienteDocAsync(turno.TipoDocumentoPaciente, turno.NroDocumentoPaciente);
            if (historiaClinica == null)
            {
                return new DatosParaAtencionResultDTO
                {
                    Resultado = ResultadoObtenerDatosAtencion.TurnoInvalido,
                    Mensaje = "El paciente no tiene una Historia Clínica registrada. No se puede continuar."
                };
            }

            return new DatosParaAtencionResultDTO
            {
                Resultado = ResultadoObtenerDatosAtencion.Ok,
                NroHC = historiaClinica.NroHC,
                FechaCreacionHC = historiaClinica.FechaCreacion,
                AtencionesPrevias = historiaClinica.Atenciones
                    .OrderByDescending(a => a.FechaHoraAtencionInicio)
                    .Select(a => new AtencionPreviaResumenDTO { FechaHoraInicio = a.FechaHoraAtencionInicio, Observaciones = a.Observaciones })
                    .ToList()
            };
        }

        public async Task<RegistrarAtencionOdontologicaResultDTO> RegistrarAtencionAsync(
            int nroTurno,
            string tipoDocumentoOdontologoLogueado,
            string nroDocumentoOdontologoLogueado,
            DateTime fechaHoraInicio,
            DateTime fechaHoraFin,
            string observaciones,
            List<DetalleInsumoInputDTO> insumosUtilizados)
        {
            var (turno, mensajeError) = await ValidarTurnoParaAtencionAsync(nroTurno, tipoDocumentoOdontologoLogueado, nroDocumentoOdontologoLogueado);
            if (turno == null)
            {
                return new RegistrarAtencionOdontologicaResultDTO { Resultado = ResultadoRegistrarAtencion.TurnoInvalido, Mensaje = mensajeError };
            }

            if (fechaHoraFin <= fechaHoraInicio)
            {
                return new RegistrarAtencionOdontologicaResultDTO
                {
                    Resultado = ResultadoRegistrarAtencion.DatosInvalidos,
                    Mensaje = "La hora de fin de la atención debe ser posterior a la hora de inicio.",
                    EstadoTurno = turno.EstadoTurno
                };
            }

            // Paso 1, diccionario sHistoriaClinicaPaciente: la Historia Clínica ya existe (se
            // crea en CUF01, al incorporar al paciente); acá solo se lee.
            var historiaClinica = await _historiaClinicaRepository.GetByPacienteDocAsync(turno.TipoDocumentoPaciente, turno.NroDocumentoPaciente);
            if (historiaClinica == null)
            {
                return new RegistrarAtencionOdontologicaResultDTO
                {
                    Resultado = ResultadoRegistrarAtencion.DatosInvalidos,
                    Mensaje = "El paciente no tiene una Historia Clínica registrada. No se puede continuar.",
                    EstadoTurno = turno.EstadoTurno
                };
            }

            // Paso 3 / Alt 3.a (sin insumos): se valida contra el catálogo y el stock ANTES de
            // registrar nada, para no dejar una atención a mitad de camino si algún insumo no
            // alcanza.
            var detallesConfirmados = new List<(Insumo Insumo, int Cantidad)>();
            foreach (var input in insumosUtilizados.Where(i => i.Cantidad > 0))
            {
                var insumo = await _insumoRepository.GetAsync(input.IdInsumo);
                if (insumo == null)
                {
                    return new RegistrarAtencionOdontologicaResultDTO
                    {
                        Resultado = ResultadoRegistrarAtencion.DatosInvalidos,
                        Mensaje = $"No se encontró el insumo seleccionado (código {input.IdInsumo}).",
                        EstadoTurno = turno.EstadoTurno
                    };
                }
                if (insumo.StockDisponible < input.Cantidad)
                {
                    return new RegistrarAtencionOdontologicaResultDTO
                    {
                        Resultado = ResultadoRegistrarAtencion.StockInsuficiente,
                        Mensaje = $"No hay stock suficiente de \"{insumo.Nombre}\" (disponible: {insumo.StockDisponible}, solicitado: {input.Cantidad}).",
                        EstadoTurno = turno.EstadoTurno
                    };
                }
                detallesConfirmados.Add((insumo, input.Cantidad));
            }

            // RN12 / MD (nota de /montoTotal): el arancel base depende de la modalidad de pago
            // elegida al agendar el turno (CUU01), no de una elección en este paso.
            decimal arancelAplicado;
            if (turno.ModalidadPagoElegida == "OBRA_SOCIAL")
            {
                var obraSocial = turno.Paciente.IdentificadorOS != null
                    ? await _obraSocialRepository.GetAsync(turno.Paciente.IdentificadorOS)
                    : null;
                var convenio = obraSocial?.Convenios.FirstOrDefault(c => c.IdEspecialidad == turno.IdEspecialidad);
                if (convenio == null)
                {
                    return new RegistrarAtencionOdontologicaResultDTO
                    {
                        Resultado = ResultadoRegistrarAtencion.DatosInvalidos,
                        Mensaje = "El paciente no tiene un convenio vigente con su obra social para esta especialidad. No se puede calcular el monto de la atención.",
                        EstadoTurno = turno.EstadoTurno
                    };
                }
                arancelAplicado = convenio.ArancelConvenio;
            }
            else
            {
                arancelAplicado = turno.Especialidad.ArancelParticular;
            }

            // Pasos 1-2: se crea la Atención Odontológica (diccionario: atencionOdontologica(e))
            // con las observaciones clínicas cargadas en el paso 2, y el arancel correspondiente
            // ya fijado (snapshot, RN16: no se recalcula si después cambia el arancel).
            var atencion = new AtencionOdontologica(0, fechaHoraInicio, fechaHoraFin, observaciones, arancelAplicado, turno.NroTurno, historiaClinica.NroHC);
            await _atencionRepository.AddAsync(atencion);

            decimal totalInsumos = 0m;
            foreach (var (insumo, cantidad) in detallesConfirmados)
            {
                // RN16 / MD: costoUnitarioAlMomento se toma AHORA de Insumo.costoUnitario y
                // queda fijo (no se recalcula después, aunque cambie el costo del insumo).
                var detalle = new DetalleInsumoUtilizado(atencion.IdAtencion, insumo.IdInsumo, cantidad, insumo.CostoUnitario);
                await _atencionRepository.AgregarDetalleInsumoAsync(detalle);

                // Matriz CRUD, Consideraciones ("Insumo, CUF09 vs. CUU03"): "En CUU03 el Insumo
                // se lee para seleccionarlo del catálogo [...] y se actualiza para descontar del
                // stock disponible la cantidad utilizada." Esto no está en el texto de CUU03.
                await _insumoRepository.DescontarStockAsync(insumo.IdInsumo, cantidad);
                totalInsumos += cantidad * insumo.CostoUnitario;
            }

            // Paso 4: "El sistema calcula el monto total [...] y cambia el estado del turno a
            // 'Atención Registrada'".
            turno.RegistrarAtencion();
            await _turnoRepository.UpdateAsync(turno);

            return new RegistrarAtencionOdontologicaResultDTO
            {
                Resultado = ResultadoRegistrarAtencion.Registrada,
                Mensaje = $"Atención de {turno.Paciente.Nombre} {turno.Paciente.Apellido} registrada en la Historia Clínica. Monto total a pagar: ${(arancelAplicado + totalInsumos):N0}. Queda pendiente el cobro por parte del responsable de la clínica para finalizar el turno.",
                EstadoTurno = turno.EstadoTurno,
                MontoTotal = arancelAplicado + totalInsumos
            };
        }

        // Camino básico, paso 5: "El responsable de la clínica indica al sistema que se
        // procederá al cobro. El sistema muestra el monto total a pagar según la modalidad de
        // pago del paciente." Se consulta ANTES de habilitar el formulario de cobro (paso 6 /
        // 6.a / 6.b), con el mismo criterio que el paso 1.
        public async Task<DatosParaCobroResultDTO> ObtenerDatosParaCobroAsync(int nroTurno)
        {
            var turno = await ObtenerTurnoParaCobroAsync(nroTurno);
            if (turno == null)
            {
                return new DatosParaCobroResultDTO
                {
                    Resultado = ResultadoObtenerDatosCobro.TurnoInvalido,
                    Mensaje = "El turno indicado no está disponible para cobrar (no existe o todavía no tiene una atención registrada)."
                };
            }

            var atencion = await _atencionRepository.GetByNroTurnoAsync(nroTurno)
                ?? throw new InvalidOperationException("El turno está en 'Atención Registrada' pero no tiene una Atención Odontológica asociada.");

            // Diccionario, paso 5: sMontoAPagar = /montoTotal + modalidadPagoElegida(d).
            return new DatosParaCobroResultDTO
            {
                Resultado = ResultadoObtenerDatosCobro.Ok,
                MontoTotal = atencion.MontoTotal,
                ModalidadPago = turno.ModalidadPagoElegida == "OBRA_SOCIAL" ? "Obra Social" : "Particular",
                NombrePaciente = $"{turno.Paciente.Nombre} {turno.Paciente.Apellido}"
            };
        }

        public async Task<RegistrarCobroResultDTO> RegistrarCobroParticularAsync(int nroTurno, decimal monto, string tipoMetodoPago)
        {
            // Camino básico, paso 6: "El paciente realiza el pago en forma particular. El
            // responsable de la clínica registra el cobro en el sistema, indicando el monto
            // abonado y el método de pago utilizado [...] cambia el estado del turno a
            // 'Finalizado'". Diccionario, paso 6: eDatosPago = monto + tipoMetodoPago -- el
            // monto es un dato que ingresa el responsable, no un valor fijo tomado del sistema
            // (a diferencia de 6.a, acá el texto no exige que coincida con el monto total).
            var turno = await ObtenerTurnoParaCobroAsync(nroTurno);
            if (turno == null)
            {
                return TurnoInvalidoCobro();
            }
            if (turno.ModalidadPagoElegida != "PARTICULAR")
            {
                // Alt 6.c: la modalidad de pago no coincide con la registrada para el turno.
                return ModalidadNoCoincide(turno.EstadoTurno);
            }

            // Se sigue necesitando la Atención para verificar que el paso 4 ya se haya hecho.
            _ = await _atencionRepository.GetByNroTurnoAsync(nroTurno)
                ?? throw new InvalidOperationException("El turno está en 'Atención Registrada' pero no tiene una Atención Odontológica asociada.");

            var fechaHoraPago = DateTime.Now;
            var pago = new Pago(0, turno.NroTurno, fechaHoraPago, monto, tipoMetodoPago);
            await _pagoRepository.AddAsync(pago);

            turno.Finalizar();
            await _turnoRepository.UpdateAsync(turno);

            // Diccionario, paso 6: sConfirmacionPago = fechaYHoraPago + monto + estadoTurno(d).
            return new RegistrarCobroResultDTO
            {
                Resultado = ResultadoRegistrarCobro.Finalizado,
                Mensaje = $"Cobro registrado el {fechaHoraPago:dd/MM/yyyy HH:mm} por ${monto:N0}. El turno quedó Finalizado.",
                EstadoTurno = turno.EstadoTurno,
                FechaHoraPago = fechaHoraPago,
                Monto = monto
            };
        }

        public async Task<RegistrarCobroResultDTO> RegistrarCobroObraSocialAsync(
            int nroTurno, string tipoMetodoPago, decimal aportePaciente, decimal aporteObraSocial)
        {
            // Alt 6.a <reemplaza> "El paciente abona con cobertura de obra social": "El sistema
            // calcula el monto a abonar [...] el responsable de la clínica registra el aporte
            // del paciente y de la obra social [...] cambia el estado del turno a 'Finalizado'".
            var turno = await ObtenerTurnoParaCobroAsync(nroTurno);
            if (turno == null)
            {
                return TurnoInvalidoCobro();
            }
            if (turno.ModalidadPagoElegida != "OBRA_SOCIAL")
            {
                return ModalidadNoCoincide(turno.EstadoTurno);
            }

            var atencion = await _atencionRepository.GetByNroTurnoAsync(nroTurno)
                ?? throw new InvalidOperationException("El turno está en 'Atención Registrada' pero no tiene una Atención Odontológica asociada.");

            // "aportePaciente + aporteObraSocial" deben igualar el monto total calculado en el
            // paso 4; si no coinciden, el sistema no debe permitir continuar. Se compara con
            // una tolerancia mínima por redondeo de centavos.
            if (Math.Abs((aportePaciente + aporteObraSocial) - atencion.MontoTotal) > 0.01m)
            {
                return new RegistrarCobroResultDTO
                {
                    Resultado = ResultadoRegistrarCobro.AportesNoCoinciden,
                    Mensaje = $"La suma del aporte del paciente y de la obra social (${aportePaciente + aporteObraSocial:N2}) no coincide con el monto total de la atención (${atencion.MontoTotal:N2}).",
                    EstadoTurno = turno.EstadoTurno
                };
            }

            var pago = new Pago(0, turno.NroTurno, DateTime.Now, atencion.MontoTotal, tipoMetodoPago, turno.Paciente.IdentificadorOS, aportePaciente, aporteObraSocial);
            await _pagoRepository.AddAsync(pago);

            turno.Finalizar();
            await _turnoRepository.UpdateAsync(turno);

            // Diccionario, 6.a.2: sConfirmacionPago = fechaYHoraPago + monto + estadoTurno(d).
            return new RegistrarCobroResultDTO
            {
                Resultado = ResultadoRegistrarCobro.Finalizado,
                Mensaje = $"Cobro registrado el {pago.FechaYHoraPago:dd/MM/yyyy HH:mm} por ${atencion.MontoTotal:N0} (paciente: ${aportePaciente:N0} + obra social: ${aporteObraSocial:N0}). El turno quedó Finalizado.",
                EstadoTurno = turno.EstadoTurno,
                FechaHoraPago = pago.FechaYHoraPago,
                Monto = atencion.MontoTotal
            };
        }

        public async Task<RegistrarCobroResultDTO> RegistrarFaltaDePagoAsync(int nroTurno)
        {
            // Alt 6.b <reemplaza> "El paciente no realiza el pago de la atención": "El
            // responsable de la clínica registra la falta de pago [...] el sistema registra en
            // la cuenta del paciente una deuda por el monto total ya calculado en el Paso 4
            // [...] sin recalcularlo, cambia el estado del paciente a 'Inhabilitado'". FCU.
            var turno = await ObtenerTurnoParaCobroAsync(nroTurno);
            if (turno == null)
            {
                return TurnoInvalidoCobro();
            }

            var atencion = await _atencionRepository.GetByNroTurnoAsync(nroTurno)
                ?? throw new InvalidOperationException("El turno está en 'Atención Registrada' pero no tiene una Atención Odontológica asociada.");

            var paciente = await _pacienteRepository.GetAsync(turno.TipoDocumentoPaciente, turno.NroDocumentoPaciente)
                ?? throw new InvalidOperationException("No se encontró el paciente asociado al turno.");

            // MD - nota sobre Paciente.montoAdeudado: "Si el Turno que originó la deuda dio
            // lugar a una Atención Odontológica (motivo 'Falta de pago de la atención'),
            // entonces /montoAdeudado = /montoTotal de esa Atención Odontológica."
            paciente.SetMontoAdeudado(atencion.MontoTotal);
            paciente.SetEstadoPaciente("INHABILITADO");
            await _pacienteRepository.UpdateAsync(paciente);

            // No se crea ningún Pago: la relación Turno-Pago es 0..1 y en este camino no hubo
            // cobro efectivo; si el paciente salda la deuda más adelante, eso es CUF10 (fuera de
            // alcance de CUU03), y ahí sí se crearía el único Pago de este turno.
            //
            // El turno igual pasa a "Finalizado": ver el comentario en Turno.Finalizar().
            turno.Finalizar();
            await _turnoRepository.UpdateAsync(turno);

            return new RegistrarCobroResultDTO
            {
                Resultado = ResultadoRegistrarCobro.FaltaDePago,
                Mensaje = $"Se registró la falta de pago. Queda una deuda de ${atencion.MontoTotal:N0} y el paciente {paciente.Nombre} {paciente.Apellido} fue inhabilitado hasta regularizar su situación.",
                EstadoTurno = turno.EstadoTurno,
                MontoAdeudado = atencion.MontoTotal,
                EstadoPaciente = paciente.EstadoPaciente
            };
        }

        // Precondición de sistema (CUU03): "El paciente está registrado como 'Presente' en el
        // sistema". Además, el turno tiene que ser el de HOY y ser del odontólogo logueado: es
        // el actor primario que ejecuta los pasos 1 a 4, no cualquier otro odontólogo ni el
        // responsable de la clínica (a diferencia de CUU02, acá sí importa A QUIÉN pertenece el
        // turno). Compartida entre el paso 1 (mostrar datos) y los pasos 2-4 (registrar), para
        // no validar de una forma en la pantalla y de otra al confirmar.
        private async Task<(Turno? Turno, string MensajeError)> ValidarTurnoParaAtencionAsync(
            int nroTurno, string tipoDocumentoOdontologoLogueado, string nroDocumentoOdontologoLogueado)
        {
            var turno = await _turnoRepository.GetAsync(nroTurno);
            var esDelOdontologoLogueado = turno != null
                && turno.TipoDocumentoOdontologo == tipoDocumentoOdontologoLogueado
                && turno.NroDocumentoOdontologo == nroDocumentoOdontologoLogueado;

            if (turno == null || !esDelOdontologoLogueado || turno.FechaHoraTurno.Date != DateTime.Today || turno.EstadoTurno != "PRESENTE")
            {
                return (null, "No se puede continuar: el turno no está disponible, ya fue procesado, o el paciente todavía no fue marcado como \"Presente\".");
            }
            return (turno, string.Empty);
        }

        // Validación común a los tres caminos de cobro (básico, 6.a, 6.b): el turno debe existir
        // y estar en "Atención Registrada" (paso 4 ya hecho por el odontólogo).
        private async Task<Turno?> ObtenerTurnoParaCobroAsync(int nroTurno)
        {
            var turno = await _turnoRepository.GetAsync(nroTurno);
            if (turno == null || turno.EstadoTurno != "ATENCION_REGISTRADA")
            {
                return null;
            }
            return turno;
        }

        private static RegistrarCobroResultDTO TurnoInvalidoCobro()
        {
            return new RegistrarCobroResultDTO
            {
                Resultado = ResultadoRegistrarCobro.TurnoInvalido,
                Mensaje = "El turno indicado no está disponible para cobrar (no existe o todavía no tiene una atención registrada).",
                EstadoTurno = string.Empty
            };
        }

        private static RegistrarCobroResultDTO ModalidadNoCoincide(string estadoTurno)
        {
            // Alt 6.c, diccionario mensajeModalidadNoCoincide(d): "La modalidad de pago
            // seleccionada no coincide con la registrada para este turno. No se puede continuar
            // con el cobro bajo esa modalidad."
            return new RegistrarCobroResultDTO
            {
                Resultado = ResultadoRegistrarCobro.ModalidadNoCoincide,
                Mensaje = "La modalidad de pago seleccionada no coincide con la registrada para este turno. No se puede continuar con el cobro bajo esa modalidad.",
                EstadoTurno = estadoTurno
            };
        }
    }
}
