namespace Application.Services
{
    // Antelación mínima de 24 horas del circuito de turnos. Un único lugar para el valor:
    //
    // RN19: "Los turnos deberán ser solicitados con una antelación mínima de 24 horas."
    // RN10: "Toda cancelación o reprogramación de un turno por parte de un paciente debe
    //        realizarse con una antelación mínima de 24 horas."
    //
    // "Mínima de 24 horas" incluye el límite: un turno que empieza exactamente dentro de 24 hs
    // cumple la regla; uno que empieza dentro de 23 h 59 min no.
    public static class ReglaAntelacion
    {
        public const int HorasMinimas = 24;

        public static bool CumpleAntelacionMinima(DateTime fechaHoraTurno, DateTime ahora)
            => fechaHoraTurno - ahora >= TimeSpan.FromHours(HorasMinimas);
    }
}
