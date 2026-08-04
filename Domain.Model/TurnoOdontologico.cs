namespace Domain.Model
{
    public class TurnoOdontologico
    {
        public int Id { get; private set; }
        public DateTime Fecha { get; private set; }
        public TimeOnly HorarioTurno { get; private set; }
        public Estadoturno EstadoTurno { get; private set; }
        public string MotivoCancelacion { get; private set; }

        public enum Estadoturno
        {
            Confirmado,
            Realizado,
            Cancelado,
            Reprogramado
        }

        public TurnoOdontologico(int id, DateTime fecha, TimeOnly horarioTurno, Estadoturno estadoTurno, string motivoCancelacion)
        {
            Id = id;
            SetFechaT(fecha);
            SetHoraT(horarioTurno);
            SetEstado(estadoTurno);
            SetMotivo(motivoCancelacion);
        }

        public void SetFechaT(DateTime fecha)
        {
            Fecha = fecha;
        }

        public void SetHoraT(TimeOnly horarioTurno)
        {

            HorarioTurno = horarioTurno;
        }
        public void SetId(int id)
        {
            Id = id;
        }
        public void SetEstado(Estadoturno estadoTurno)
        {
            EstadoTurno = estadoTurno;
        }

        public void SetMotivo(string motivoCancelacion)
        {
            MotivoCancelacion = motivoCancelacion;
        }
    }
}