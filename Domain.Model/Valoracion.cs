namespace Domain.Model
{
    public class Valoracion
    {
        public int CodValoracion { get; private set; }
        public int Calificacion { get; private set; }
        public string? Observaciones { get; private set; }

        public int CodAtencion { get; private set; }
        public virtual AtencionOdontologica Atencion { get; private set; }

        protected Valoracion() { }

        public Valoracion(int codValoracion, int calificacion, string? observaciones, int codAtencion)
        {
            CodValoracion = codValoracion;
            SetCalificacion(calificacion);
            Observaciones = observaciones;
            CodAtencion = codAtencion;
        }

        public void SetCalificacion(int calificacion)
        {
            if (calificacion < 1 || calificacion > 5)
                throw new ArgumentOutOfRangeException(nameof(calificacion), "La calificación debe estar entre 1 y 5 estrellas.");
            Calificacion = calificacion;
        }
    }
}
