namespace Domain.Model
{
    // Alias / Compatibilidad con código previo
    public class TurnoOdontologico : Turno
    {
        public enum EstadoTurnoEnum
        {
            Pendiente,
            Presente,
            Atendido,
            Cancelado,
            Reprogramado
        }

        public TurnoOdontologico() : base() { }

        public TurnoOdontologico(int codTurno, DateTime fechaReserva, TimeOnly horarioTurno, EstadoTurnoEnum estado, string? motivo, int especialidadId, int odontologoNroDoc, int pacienteNroDoc, decimal monto)
            : base(codTurno, fechaReserva, "PARTICULAR", especialidadId, "DNI", odontologoNroDoc, "DNI", pacienteNroDoc, estado.ToString().ToUpper())
        {
        }
    }
}