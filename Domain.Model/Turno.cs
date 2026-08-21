namespace Domain.Model
{
    public class Turno
    {
        public int CodTurno { get; private set; }
        public DateTime FechaYHoraReserva { get; private set; }

        public string ModalidadPagoElegida { get; private set; } = "PARTICULAR"; // PARTICULAR, OBRA_SOCIAL
        public DateTime? FechaYHoraSolicitudReprogramacion { get; private set; }
        public DateTime? FechaYHoraCancelacion { get; private set; }
        public string? MotivoCancelacion { get; private set; }
        public string Estado { get; private set; } = "RESERVADO";

        // Foreign Keys
        public int CodEspecialidad { get; private set; }
        public virtual Especialidad Especialidad { get; private set; }

        public string OdontologoTipoDoc { get; private set; } = "DNI";
        public int OdontologoNroDoc { get; private set; }
        public virtual Odontologo Odontologo { get; private set; }

        public string PacienteTipoDoc { get; private set; } = "DNI";
        public int PacienteNroDoc { get; private set; }
        public virtual Paciente Paciente { get; private set; }

        // Autoreferencia para Turno Reprogramado
        public int? TurnoOriginalCod { get; private set; }
        public DateTime? TurnoOriginalFecha { get; private set; }
        public virtual Turno? TurnoOriginal { get; private set; }

        public virtual ComprobanteDeTurno? Comprobante { get; private set; }
        public virtual AtencionOdontologica? Atencion { get; private set; }

        protected Turno() { }

        public Turno(int codTurno, DateTime fechaYHoraReserva, string modalidadPagoElegida, int codEspecialidad, string odontologoTipoDoc, int odontologoNroDoc, string pacienteTipoDoc, int pacienteNroDoc, string estado = "RESERVADO")
        {
            CodTurno = codTurno;
            FechaYHoraReserva = fechaYHoraReserva;
            ModalidadPagoElegida = modalidadPagoElegida;
            CodEspecialidad = codEspecialidad;
            OdontologoTipoDoc = odontologoTipoDoc;
            OdontologoNroDoc = odontologoNroDoc;
            PacienteTipoDoc = pacienteTipoDoc;
            PacienteNroDoc = pacienteNroDoc;
            Estado = estado;
        }

        public void Reprogramar(DateTime nuevaFechaHora, int nuevoCodTurno)
        {
            FechaYHoraSolicitudReprogramacion = DateTime.Now;
            Estado = "REPROGRAMADO";
        }

        public void Cancelar(string motivo)
        {
            FechaYHoraCancelacion = DateTime.Now;
            MotivoCancelacion = motivo;
            Estado = "CANCELADO";
        }

        public void Confirmar()
        {
            Estado = "CONFIRMADO";
        }

        public void MarcarEnEspera()
        {
            Estado = "EN_ESPERA";
        }

        public void Atender()
        {
            Estado = "ATENDIDO";
        }

        public void SetEstado(string estado)
        {
            Estado = estado;
        }
    }
}
