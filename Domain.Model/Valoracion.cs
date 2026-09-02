namespace Domain.Model
{
    public class Valoracion
    {
        public int IdValoracion { get; private set; }
        public int Calificacion { get; private set; }
        public string? Observaciones { get; set; }
        public int IdAtencion { get; private set; }

        public virtual AtencionOdontologica Atencion { get; private set; } = null!;

        // Aliases para retrocompatibilidad
        public int CodValoracion => IdValoracion;
        public int CodAtencion => IdAtencion;

        protected Valoracion() { }

        public Valoracion(int idValoracion, int calificacion, string? observaciones, int idAtencion)
        {
            IdValoracion = idValoracion;
            SetCalificacion(calificacion);
            Observaciones = observaciones;
            IdAtencion = idAtencion;
        }

        public void SetCalificacion(int calificacion)
        {
            if (calificacion < 1 || calificacion > 5)
                throw new ArgumentOutOfRangeException(nameof(calificacion), "La calificación debe estar entre 1 y 5 estrellas.");
            Calificacion = calificacion;
        }
    }
}
