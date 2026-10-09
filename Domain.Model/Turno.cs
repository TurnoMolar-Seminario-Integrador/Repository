namespace Domain.Model
{
    public class Turno
    {
        public int NroTurno { get; private set; }
        public string ModalidadPagoElegida { get; private set; } = "PARTICULAR"; // PARTICULAR, OBRA_SOCIAL
        public DateTime FechaHoraTurno { get; private set; }
        public DateTime? FechaHoraReprogramacion { get; private set; }
        public DateTime? FechaHoraCancelacion { get; private set; }
        public string? MotivoCancelacion { get; private set; }
        public string EstadoTurno { get; private set; } = "RESERVADO";
        public string? DescripcionMaterial { get; private set; }
        public decimal? ArancelPenalizacionAplicado { get; private set; }

        // FKs
        public string TipoDocumentoPaciente { get; private set; } = "DNI";
        public string NroDocumentoPaciente { get; private set; } = string.Empty;
        public virtual Paciente Paciente { get; private set; } = null!;

        public string TipoDocumentoOdontologo { get; private set; } = "DNI";
        public string NroDocumentoOdontologo { get; private set; } = string.Empty;
        public virtual Odontologo Odontologo { get; private set; } = null!;

        public int IdEspecialidad { get; private set; }
        public virtual Especialidad Especialidad { get; private set; } = null!;

        public int? NroTurnoOriginal { get; private set; }
        public virtual Turno? TurnoOriginal { get; private set; }

        public virtual ComprobanteDeTurno? Comprobante { get; private set; }
        public virtual AtencionOdontologica? Atencion { get; private set; }
        public virtual Pago? Pago { get; private set; }

        // Aliases para retrocompatibilidad
        public int CodTurno => NroTurno;
        public DateTime FechaYHoraReserva => FechaHoraTurno;
        public DateTime? FechaYHoraSolicitudReprogramacion => FechaHoraReprogramacion;
        public DateTime? FechaYHoraCancelacion => FechaHoraCancelacion;
        public string Estado => EstadoTurno;
        public int CodEspecialidad => IdEspecialidad;
        public string PacienteTipoDoc => TipoDocumentoPaciente;
        public string PacienteNroDoc => NroDocumentoPaciente;
        public string OdontologoTipoDoc => TipoDocumentoOdontologo;
        public string OdontologoNroDoc => NroDocumentoOdontologo;
        public int? TurnoOriginalCod => NroTurnoOriginal;

        // Defecto corregido: MisTurnos.cshtml (vista del paciente) mostraba el código interno
        // de EstadoTurno tal cual -- con el resto de los estados no se notaba porque son una
        // sola palabra sin tilde ("RESERVADO", "PRESENTE", etc.), pero "ATENCION_REGISTRADA"
        // se veía literal, con guión bajo y sin la tilde de "Atención". Esta propiedad es solo
        // para texto mostrado al usuario; las comparaciones de estado deben seguir usando
        // EstadoTurno/Estado, no esta.
        public string EstadoLegible => EstadoTurno == "ATENCION_REGISTRADA" ? "ATENCIÓN REGISTRADA" : EstadoTurno;

        protected Turno() { }

        public Turno(
            int nroTurno,
            DateTime fechaHoraTurno,
            string modalidadPagoElegida,
            int idEspecialidad,
            string tipoDocumentoOdontologo,
            string nroDocumentoOdontologo,
            string tipoDocumentoPaciente,
            string nroDocumentoPaciente,
            string estadoTurno = "RESERVADO",
            string? descripcionMaterial = null,
            decimal? arancelPenalizacionAplicado = null,
            int? nroTurnoOriginal = null,
            DateTime? fechaHoraReprogramacion = null,
            DateTime? fechaHoraCancelacion = null,
            string? motivoCancelacion = null)
        {
            NroTurno = nroTurno;
            FechaHoraTurno = fechaHoraTurno;
            ModalidadPagoElegida = modalidadPagoElegida;
            IdEspecialidad = idEspecialidad;
            TipoDocumentoOdontologo = tipoDocumentoOdontologo;
            NroDocumentoOdontologo = nroDocumentoOdontologo;
            TipoDocumentoPaciente = tipoDocumentoPaciente;
            NroDocumentoPaciente = nroDocumentoPaciente;
            EstadoTurno = estadoTurno;
            DescripcionMaterial = descripcionMaterial;
            ArancelPenalizacionAplicado = arancelPenalizacionAplicado;
            NroTurnoOriginal = nroTurnoOriginal;
            FechaHoraReprogramacion = fechaHoraReprogramacion;
            FechaHoraCancelacion = fechaHoraCancelacion;
            MotivoCancelacion = motivoCancelacion;
        }

        // Constructor para retrocompatibilidad numérica
        public Turno(
            int codTurno,
            DateTime fechaYHoraReserva,
            string modalidadPagoElegida,
            int codEspecialidad,
            string odontologoTipoDoc,
            int odontologoNroDoc,
            string pacienteTipoDoc,
            int pacienteNroDoc,
            string estado = "RESERVADO")
            : this(
                codTurno,
                fechaYHoraReserva,
                modalidadPagoElegida,
                codEspecialidad,
                odontologoTipoDoc,
                odontologoNroDoc.ToString(),
                pacienteTipoDoc,
                pacienteNroDoc.ToString(),
                estado)
        {
        }

        // ME - Máquina de Estados (Turno): el paciente solo puede cancelar o reprogramar un turno
        // mientras está "Reservado" (RESERVADO -> CANCELADO | REPROGRAMADO). Desde que el
        // responsable registra su asistencia (PRESENTE / AUSENTE), y en los estados posteriores
        // (ATENCION_REGISTRADA, FINALIZADO), ya no puede modificarlo: solo consultar el comprobante.
        // Propiedad calculada de solo lectura: EF Core no la mapea (igual que los alias de arriba).
        public bool PermiteCancelarOReprogramar => EstadoTurno == "RESERVADO";

        // CUU04 - Valorar Atención Odontológica (v1.03), precondiciones de sistema: "El turno
        // del paciente está registrado como 'Finalizado'" y "El turno tiene registrado el pago
        // de la atención y la atención aún no cuenta con una valoración registrada".
        // "Habilitado para valorar" es una condición derivada de ESA atención (cobro registrado
        // y sin valoración previa, RN14), no el estado general del Paciente: desde CUU03 v1.05
        // un turno "Finalizado" puede no estar cobrado (alt 6.b, falta de pago) y recién tiene
        // Pago cuando el paciente regulariza la deuda (CUF10). Que el paciente tenga una deuda
        // por OTRO turno no le quita la posibilidad de valorar una atención que sí abonó.
        // Requiere Pago y Atencion.Valoracion cargados para evaluarse bien.
        public bool PendienteDeValoracion =>
            EstadoTurno == "FINALIZADO" &&
            Pago != null &&
            Atencion != null &&
            Atencion.Valoracion == null;

        // RN8: "Un paciente no puede reservar un nuevo turno teniendo ya un turno previo que esté
        // pendiente de atención". Se considera pendiente de atención todo turno que todavía no
        // concluyó, es decir, en estado "Reservado", "Presente" o "Atención Registrada" (ME - Turno).
        // Un turno "Atención Registrada" todavía puede terminar en deuda (CUU03 alt 6.b), y el
        // modelo admite una sola deuda a la vez (MD: /montoAdeudado, "el Turno que originó la
        // deuda"): por eso el paciente no puede tener otro turno reservado mientras tanto.
        // "Finalizado", "Ausente", "Cancelado" y "Reprogramado" ya no están pendientes.
        public bool PendienteDeAtencion =>
            EstadoTurno == "RESERVADO" ||
            EstadoTurno == "PRESENTE" ||
            EstadoTurno == "ATENCION_REGISTRADA";

        // Atención finalizada cuyo cobro quedó pendiente (CUU03 alt 6.b, falta de pago) y que
        // todavía no tiene valoración. No es valorable (ver PendienteDeValoracion), pero el
        // paciente tiene que poder enterarse de que la tiene pendiente: se habilita cuando
        // regulariza la deuda y queda registrado el Pago del turno (CUF10).
        public bool PendienteDeValoracionPorDeuda =>
            EstadoTurno == "FINALIZADO" &&
            Pago == null &&
            Atencion != null &&
            Atencion.Valoracion == null;

        public void Reprogramar(DateTime nuevaFechaHora, int? nuevoNroTurno = null)
        {
            if (!PermiteCancelarOReprogramar)
                throw new InvalidOperationException("Solo un turno en estado RESERVADO puede reprogramarse.");

            FechaHoraReprogramacion = DateTime.Now;
            EstadoTurno = "REPROGRAMADO";
        }

        public void Cancelar(string motivo, decimal? penalizacion = null)
        {
            if (!PermiteCancelarOReprogramar)
                throw new InvalidOperationException("Solo un turno en estado RESERVADO puede cancelarse.");

            FechaHoraCancelacion = DateTime.Now;
            MotivoCancelacion = motivo;
            ArancelPenalizacionAplicado = penalizacion;
            EstadoTurno = "CANCELADO";
        }

        // CUU02 - Gestionar Asistencia a Turno Odontológico.
        // ME - Máquina de Estados (Turno): "Responsable de la Clínica evalúa la asistencia
        // del Paciente" -> [Paciente asiste] RESERVADO -> PRESENTE.
        // Camino básico, paso 2: el responsable confirma la llegada del paciente.
        public void MarcarPresente() => EstadoTurno = "PRESENTE";

        // ME - Máquina de Estados (Turno): [Paciente no asiste] RESERVADO -> AUSENTE.
        // Alt 1.b.1: se registra la ausencia y la penalización correspondiente (RN11) en el
        // mismo movimiento; el turno guarda el arancel aplicado igual que en una cancelación
        // tardía (mismo campo, mismo motivo de negocio: falta de aviso oportuno).
        public void MarcarAusente(decimal montoPenalizacion)
        {
            EstadoTurno = "AUSENTE";
            ArancelPenalizacionAplicado = montoPenalizacion;
        }

        // CUU03 - Finalizar Atención Odontológica, camino básico, paso 4: "El odontólogo
        // confirma el registro de la atención [...] cambia el estado del turno a 'Atención
        // Registrada'". El paso 4 es responsabilidad del Odontólogo; el pasaje a "Finalizado"
        // (pasos 5-6) es responsabilidad del Responsable de la Clínica y se procesa por
        // separado, más abajo, en Finalizar().
        public void RegistrarAtencion() => EstadoTurno = "ATENCION_REGISTRADA";

        // CUU03 - Finalizar Atención Odontológica (v1.05), camino básico paso 6 / alt 6.a /
        // alt 6.b: "el responsable de la clínica gestiona el cobro de la atención" (ME - Turno,
        // transición ATENCIÓN REGISTRADA -> FINALIZADO) -> el turno pasa a "Finalizado".
        //
        // El turno queda "Finalizado" en los tres caminos: el texto de CUU03 lo dice
        // explícitamente en el paso 6, en 6.a (obra social) y, desde v1.05, también en 6.b
        // (falta de pago). Lo único que varía entre los tres caminos es si se creó o no un
        // Pago (relación Turno-Pago 0..1) y el estado del Paciente: "Finalizado" no implica
        // "cobrado".
        //
        // Nota (CUU04 v1.03): un turno "Finalizado" sin Pago (alt 6.b) todavía no se puede
        // valorar; pasa a ser valorable cuando se registra su Pago al regularizar la deuda
        // (ver PendienteDeValoracion).
        public void Finalizar() => EstadoTurno = "FINALIZADO";

        public void SetEstado(string estado) => EstadoTurno = estado;
        public void SetDescripcionMaterial(string? mat) => DescripcionMaterial = mat;
    }
}
