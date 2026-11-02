namespace Data
{
    // Control de concurrencia optimista: el registro que se iba a modificar fue modificado por otro usuario
    // después de leerlo. Los repositorios traducen DbUpdateConcurrencyException de EF Core a esta excepción
    // para que la capa de servicios no dependa de Entity Framework.
    public class ConflictoConcurrenciaException : Exception
    {
        public ConflictoConcurrenciaException(string mensaje, Exception? causa = null) : base(mensaje, causa) { }
    }
}
