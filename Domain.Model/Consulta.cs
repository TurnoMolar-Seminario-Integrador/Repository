namespace Domain.Model
{
    public class Consulta
    {
        public string Observaciones { get; private set; }
        public string Diagnostico { get; private set; }
        public bool Estado { get; private set; }

        public enum TipoTratamiento
        {
            Consulta, Limpieza, Restauracion, Endodoncia, Extraccion,
            Ortodoncia, Implante, Protesis, Blanqueamiento, Control, Otro
        }

        public TipoTratamiento Tratamiento { get; private set; }
        public string Valoracion { get; private set; }

        public Consulta(string observaciones, string diagnostico, bool estado, TipoTratamiento tratamiento, string valoracion)
        {
            SetObservac(observaciones);
            SetDiag(diagnostico);
            SetEstado(estado);
            SetTratamiento(tratamiento);
            SetValoracion(valoracion);
        }

        public void SetObservac(string observaciones)
        {
            if (string.IsNullOrWhiteSpace(observaciones))
                throw new ArgumentException("Las observaciones no pueden ser nulas o vacías.", nameof(observaciones));
            Observaciones = observaciones;
        }

        public void SetDiag(string diagnostico)
        {
            if (string.IsNullOrWhiteSpace(diagnostico))
                throw new ArgumentException("El diagnóstico no puede ser nulo o vacío.", nameof(diagnostico));
            Diagnostico = diagnostico;
        }

        public void SetEstado(bool estado)
        {
            
            Estado = estado;
        }

        public void SetTratamiento(TipoTratamiento tratamiento)
        {
            
            Tratamiento = tratamiento;
        }

        public void SetValoracion(string valoracion)
        {
            Valoracion = valoracion;
        }
    }
}
