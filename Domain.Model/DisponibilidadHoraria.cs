namespace Domain.Model
{
    public class DisponibilidadHoraria
    {
        public int CodDisponibilidad { get; private set; }

        public string? OdontologoTipoDoc { get; private set; }
        public int? OdontologoNroDoc { get; private set; }
        public virtual Odontologo? Odontologo { get; private set; }

        public string DiaSemana { get; private set; } = string.Empty;
        public TimeOnly HoraInicio { get; private set; }
        public TimeOnly HoraFin { get; private set; }

        public int? CodEspecialidad { get; private set; }
        public virtual Especialidad? Especialidad { get; private set; }

        protected DisponibilidadHoraria() { }

        public DisponibilidadHoraria(int codDisponibilidad, string diaSemana, TimeOnly horaInicio, TimeOnly horaFin, string? odontologoTipoDoc = null, int? odontologoNroDoc = null, int? codEspecialidad = null)
        {
            CodDisponibilidad = codDisponibilidad;
            OdontologoTipoDoc = odontologoTipoDoc;
            OdontologoNroDoc = odontologoNroDoc;
            SetDiaSemana(diaSemana);
            SetHorario(horaInicio, horaFin);
            CodEspecialidad = codEspecialidad;
        }

        public void SetDiaSemana(string diaSemana)
        {
            if (string.IsNullOrWhiteSpace(diaSemana))
                throw new ArgumentException("El día de la semana no puede ser vacío.", nameof(diaSemana));
            DiaSemana = diaSemana.Trim();
        }

        public void SetHorario(TimeOnly horaInicio, TimeOnly horaFin)
        {
            if (horaFin <= horaInicio)
                throw new ArgumentException("La hora de fin debe ser posterior a la hora de inicio.");
            HoraInicio = horaInicio;
            HoraFin = horaFin;
        }
    }
}
