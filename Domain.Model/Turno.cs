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

        public void Confirmar() => EstadoTurno = "CONFIRMADO";
        public void MarcarEnEspera() => EstadoTurno = "EN_ESPERA";
        public void Atender() => EstadoTurno = "ATENDIDO";
        public void SetEstado(string estado) => EstadoTurno = estado;
        public void SetDescripcionMaterial(string? mat) => DescripcionMaterial = mat;
    }
}
