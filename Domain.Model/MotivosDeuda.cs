namespace Domain.Model
{
    // CUF10, diccionario de datos: motivoDeuda(d) = ["Inasistencia" | "Cancelación fuera de término" |
    // "Falta de pago de la atención"] (RN9: no presentarse, cancelar fuera de término o no abonar).
    public static class MotivosDeuda
    {
        public const string Inasistencia = "Inasistencia";
        public const string CancelacionFueraDeTermino = "Cancelación fuera de término";
        public const string FaltaDePago = "Falta de pago de la atención";
    }
}
