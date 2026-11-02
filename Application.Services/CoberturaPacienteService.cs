using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    // =========================================================================
    // OBRA SOCIAL DEL PACIENTE: alta (CUF01, RN6) y modificación (CUF02).
    //
    // Defectos corregidos (alta y pantalla Seguros):
    //  - El alta ofrecía una lista fija de obras sociales (IAPOS, Sancor, Federada, Jerárquicos, Galeno)
    //    que no existen en la base; el paciente quedaba como particular, con mensaje de éxito y sin
    //    aviso. "Otra" buscaba con Contains, así que escribir "s" asignaba OSDE.
    //  - Particular se guardaba como null en el alta pero como la "obra social" PARTICULAR en Seguros y
    //    en el paciente sembrado Carlos: con ese valor el sistema creía que había obra social que no
    //    cubría nada (y la reserva proponía la modalidad Obra Social).
    //  - ActualizarSeguro cambiaba la obra social sin controles: con un turno de obra social pendiente,
    //    el precio, la penalización y el Pago pasaban a calcularse con la obra social nueva (el Turno
    //    solo guarda "OBRA_SOCIAL", no cuál), y con una obra social sin convenio el turno quedaba
    //    trabado (no se podía atender, ni cancelar fuera de término, ni registrar su ausencia).
    //
    // Regla de resguardo (el modelo no guarda la obra social ni el arancel en el turno, RN16): no se
    // cambia la obra social mientras haya un turno con modalidad Obra Social que todavía puede
    // atenderse o cobrarse (Reservado, Presente o Atención Registrada). No toca el esquema.
    // =========================================================================
    public interface ICoberturaPacienteService
    {
        // Obras sociales con acuerdo activo con la clínica (listas del alta y de Seguros).
        Task<IReadOnlyList<ObraSocial>> ListarObrasSocialesConAcuerdoAsync();

        // CUF01: resuelve la obra social declarada en el alta. seleccion: "Particular", el
        // identificador de una obra social o "Otra" (con el nombre en otraObraSocial).
        Task<ObraSocialDeclaradaResultDTO> ResolverObraSocialParaAltaAsync(string? seleccion, string? otraObraSocial);

        // CUF02: el paciente cambia su obra social o pasa a particular (identificadorOS vacío).
        // Pasar a particular pide confirmación (alt 2.a.1): confirmaPasarAParticular = true.
        Task<ActualizarCoberturaResultDTO> ActualizarObraSocialAsync(
            string tipoDocumentoPaciente, string nroDocumentoPaciente, string? identificadorOS,
            bool confirmaPasarAParticular = false);
    }

    public class CoberturaPacienteService : ICoberturaPacienteService
    {
        // CUF01, alt 2.a.1.a.1 (diccionario de datos).
        public const string MensajeObraSocialSinAcuerdoEnAlta =
            "La obra social ingresada no posee un acuerdo activo. Seleccione la modalidad particular para continuar.";

        // CUF02, alt 2.b.1.a.1 (diccionario de datos).
        public const string MensajeObraSocialSinAcuerdoEnModificacion =
            "La obra social ingresada no posee un acuerdo activo. No se realizará la modificación.";

        // CUF02, alt 2.a.1 (diccionario de datos).
        public const string MensajeConfirmarPasarAParticular =
            "Se eliminará el acuerdo de obra social asociado a su cuenta. ¿Desea continuar?";

        // CUF02, mensaje de éxito (diccionario de datos).
        public const string MensajeDatosActualizados = "Sus datos fueron actualizados correctamente.";

        private readonly IObraSocialRepository _obraSocialRepository;
        private readonly IPacienteRepository _pacienteRepository;
        private readonly ITurnoRepository _turnoRepository;

        public CoberturaPacienteService(
            IObraSocialRepository obraSocialRepository,
            IPacienteRepository pacienteRepository,
            ITurnoRepository turnoRepository)
        {
            _obraSocialRepository = obraSocialRepository;
            _pacienteRepository = pacienteRepository;
            _turnoRepository = turnoRepository;
        }

        public async Task<IReadOnlyList<ObraSocial>> ListarObrasSocialesConAcuerdoAsync()
        {
            return (await _obraSocialRepository.GetAllAsync())
                .Where(o => o.TieneAcuerdoActivo)
                .OrderBy(o => o.NombreOS)
                .ToList();
        }

        public async Task<ObraSocialDeclaradaResultDTO> ResolverObraSocialParaAltaAsync(string? seleccion, string? otraObraSocial)
        {
            var elegida = seleccion?.Trim();
            if (string.IsNullOrEmpty(elegida))
            {
                return Invalida("Seleccioná una obra social o la modalidad particular.");
            }

            // RN6 / MU: el paciente particular se registra sin obra social.
            if (string.Equals(elegida, "Particular", StringComparison.OrdinalIgnoreCase) || ObraSocial.EsParticular(elegida))
            {
                return new ObraSocialDeclaradaResultDTO { Valida = true, IdentificadorOS = null };
            }

            var buscada = string.Equals(elegida, "Otra", StringComparison.OrdinalIgnoreCase)
                ? (otraObraSocial ?? string.Empty).Trim()
                : elegida;
            if (buscada.Length == 0)
            {
                return Invalida("Por favor, especificá el nombre de la obra social.");
            }

            // Coincidencia exacta (sin distinguir mayúsculas) con el identificador o el nombre; antes
            // "Otra" usaba Contains: escribir "s" asignaba OSDE.
            var todas = await _obraSocialRepository.GetAllAsync();
            var obraSocial = todas.FirstOrDefault(o =>
                !ObraSocial.EsParticular(o.IdentificadorOS) &&
                (string.Equals(o.IdentificadorOS, buscada, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(o.NombreOS, buscada, StringComparison.OrdinalIgnoreCase)));

            // Una obra social que no existe o no tiene acuerdo activo no se puede registrar: antes el
            // alta seguía como particular sin avisar.
            if (obraSocial == null || !obraSocial.TieneAcuerdoActivo)
            {
                return Invalida(MensajeObraSocialSinAcuerdoEnAlta);
            }

            return new ObraSocialDeclaradaResultDTO { Valida = true, IdentificadorOS = obraSocial.IdentificadorOS };
        }

        public async Task<ActualizarCoberturaResultDTO> ActualizarObraSocialAsync(
            string tipoDocumentoPaciente, string nroDocumentoPaciente, string? identificadorOS,
            bool confirmaPasarAParticular = false)
        {
            var paciente = await _pacienteRepository.GetAsync(tipoDocumentoPaciente, nroDocumentoPaciente)
                ?? throw new ArgumentException("No se encontró el paciente indicado.");

            var pasaAParticular = string.IsNullOrWhiteSpace(identificadorOS) || ObraSocial.EsParticular(identificadorOS);

            ObraSocial? nueva = null;
            if (!pasaAParticular)
            {
                nueva = await _obraSocialRepository.GetAsync(identificadorOS!.Trim());

                // CUF02 alt 2.b.1.a.1.
                if (nueva == null || !nueva.TieneAcuerdoActivo)
                {
                    return new ActualizarCoberturaResultDTO
                    {
                        Resultado = ResultadoActualizarCobertura.ObraSocialSinAcuerdo,
                        Mensaje = MensajeObraSocialSinAcuerdoEnModificacion
                    };
                }
            }

            // Sin cambios no hay nada que resguardar ni que confirmar.
            var cambia = pasaAParticular
                ? !string.IsNullOrWhiteSpace(paciente.IdentificadorOS)
                : !string.Equals(paciente.IdentificadorOS, nueva!.IdentificadorOS, StringComparison.OrdinalIgnoreCase);
            if (!cambia)
            {
                return new ActualizarCoberturaResultDTO
                {
                    Resultado = ResultadoActualizarCobertura.Actualizada,
                    Mensaje = MensajeDatosActualizados,
                    NombreObraSocial = nueva?.NombreOS
                };
            }

            // Resguardo (ver el comentario de la clase): el precio, la penalización y el Pago de un turno
            // de obra social se calculan con la obra social que tenga el paciente en ese momento.
            var turnos = await _turnoRepository.GetByPacienteAsync(paciente.TipoDocumento, paciente.NroDocumento);
            var turnoPendiente = turnos
                .Where(t => t.PendienteDeAtencion && t.ModalidadPagoElegida == ModalidadesPago.ObraSocial)
                .OrderBy(t => t.FechaHoraTurno)
                .FirstOrDefault();
            if (turnoPendiente != null)
            {
                return new ActualizarCoberturaResultDTO
                {
                    Resultado = ResultadoActualizarCobertura.TurnoObraSocialPendiente,
                    Mensaje = $"No se puede modificar la obra social mientras haya un turno con modalidad Obra Social pendiente de atención o de cobro (turno N° {turnoPendiente.NroTurno} del {turnoPendiente.FechaHoraTurno:dd/MM/yyyy HH:mm} hs). Podés hacerlo cuando ese turno se finalice o se cancele."
                };
            }

            // CUF02 alt 2.a.1: pasar a particular elimina el acuerdo de obra social de la cuenta.
            if (pasaAParticular && !confirmaPasarAParticular)
            {
                return new ActualizarCoberturaResultDTO
                {
                    Resultado = ResultadoActualizarCobertura.RequiereConfirmacion,
                    Mensaje = MensajeConfirmarPasarAParticular
                };
            }

            // CUF02: la obra social cambia "sin afectar su estado de habilitación".
            paciente.AsignarObraSocial(nueva);
            try
            {
                await _pacienteRepository.UpdateAsync(paciente);
            }
            catch (ConflictoConcurrenciaException)
            {
                // Otro usuario modificó al paciente entre la lectura y este guardado: no se guardó nada.
                return new ActualizarCoberturaResultDTO
                {
                    Resultado = ResultadoActualizarCobertura.ConflictoConcurrencia,
                    Mensaje = "Los datos del paciente fueron modificados por otro usuario mientras se guardaba la obra social. No se guardó ningún cambio: volvé a intentarlo."
                };
            }

            return new ActualizarCoberturaResultDTO
            {
                Resultado = ResultadoActualizarCobertura.Actualizada,
                Mensaje = MensajeDatosActualizados,
                NombreObraSocial = nueva?.NombreOS
            };
        }

        private static ObraSocialDeclaradaResultDTO Invalida(string mensaje) =>
            new() { Valida = false, Mensaje = mensaje };
    }
}
