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
            int? nroTurnoOriginal = null)
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

        public void Reprogramar(DateTime nuevaFechaHora, int? nuevoNroTurno = null)
        {
            FechaHoraReprogramacion = DateTime.Now;
            EstadoTurno = "REPROGRAMADO";
        }

        public void Cancelar(string motivo, decimal? penalizacion = null)
        {
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

        // CUU03 - Finalizar Atención Odontológica, camino básico paso 6 / alt 6.a / alt 6.b:
        // "el responsable de la clínica le cobra la atención al paciente" (ME - Turno) -> el
        // turno pasa a "Finalizado".
        //
        // Nota de alcance: el texto de CUU03 dice explícitamente "cambia el estado del turno a
        // Finalizado" en el camino básico y en 6.a (obra social); en 6.b (falta de pago) solo
        // menciona el cambio de estado del Paciente a "Inhabilitado" y no dice nada del Turno.
        // Se interpreta que el Turno pasa a "Finalizado" en los tres casos, porque (a) la
        // Máquina de Estados dibuja una única transición ATENCIÓN REGISTRADA -> FINALIZADO sin
        // bifurcar según si hubo cobro efectivo, y (b) la precondición de CUU04 exige
        // Turno = "Finalizado" Y Paciente = "Habilitado" como dos condiciones independientes: si
        // el turno nunca llegara a "Finalizado" en 6.b, esa atención jamás podría valorarse ni
        // siquiera después de que el paciente pague la deuda (CUF10). Lo único que varía entre
        // los tres caminos es si se creó o no un Pago (relación Turno-Pago 0..1) y el estado del
        // Paciente. Confirmar con el equipo si esta lectura no es la buscada.
        public void Finalizar() => EstadoTurno = "FINALIZADO";

        public void SetEstado(string estado) => EstadoTurno = estado;
        public void SetDescripcionMaterial(string? mat) => DescripcionMaterial = mat;
    }
}
