namespace Domain.Model
{
    public class DisponibilidadHoraria
    {
        public string TipoDocumentoOdontologo { get; private set; } = "DNI";
        public string NroDocumentoOdontologo { get; private set; } = string.Empty;
        public string DiaSemana { get; private set; } = string.Empty;
        public TimeOnly HoraInicio { get; private set; }
        public TimeOnly HoraFin { get; private set; }
        public int IdEspecialidad { get; private set; }

        public virtual Odontologo Odontologo { get; private set; } = null!;
        public virtual Especialidad Especialidad { get; private set; } = null!;

        // Helpers de compatibilidad
        public string OdontologoTipoDoc => TipoDocumentoOdontologo;
        public string OdontologoNroDoc => NroDocumentoOdontologo;
        public int CodEspecialidad => IdEspecialidad;

        protected DisponibilidadHoraria() { }

        public DisponibilidadHoraria(
            string tipoDocumentoOdontologo,
            string nroDocumentoOdontologo,
            string diaSemana,
            TimeOnly horaInicio,
            TimeOnly horaFin,
            int idEspecialidad)
        {
            TipoDocumentoOdontologo = tipoDocumentoOdontologo?.ToUpper().Trim() ?? "DNI";
            SetNroDocumentoOdontologo(nroDocumentoOdontologo);
            SetDiaSemana(diaSemana);
            SetHorario(horaInicio, horaFin);
            IdEspecialidad = idEspecialidad;
        }

        public DisponibilidadHoraria(
            int codDisponibilidad,
            string diaSemana,
            TimeOnly horaInicio,
            TimeOnly horaFin,
            string? odontologoTipoDoc = null,
            int? odontologoNroDoc = null,
            int? codEspecialidad = null)
            : this(
                odontologoTipoDoc ?? "DNI",
                (odontologoNroDoc ?? 0).ToString(),
                diaSemana,
                horaInicio,
                horaFin,
                codEspecialidad ?? 1)
        {
        }

        public void SetNroDocumentoOdontologo(string nroDoc)
        {
            if (string.IsNullOrWhiteSpace(nroDoc))
                throw new ArgumentException("El número de documento del odontólogo no puede ser vacío.", nameof(nroDoc));
            NroDocumentoOdontologo = nroDoc.Trim();
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
