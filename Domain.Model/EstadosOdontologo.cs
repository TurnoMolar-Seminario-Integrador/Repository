namespace Domain.Model
{
    // ME - Odontólogo / CUF04 (diccionario de datos): estadoOdontologo(d) = [ "Habilitado" | "Inhabilitado" ].
    // Antes se guardaba "ACTIVO" (vocabulario de la MU, que no coincide con la ME). Las bases ya creadas
    // pueden tener ese valor, por eso EsHabilitado lo sigue aceptando como sinónimo de "Habilitado".
    public static class EstadosOdontologo
    {
        public const string Habilitado = "HABILITADO";
        public const string Inhabilitado = "INHABILITADO";

        public static bool EsHabilitado(string? estado)
        {
            var e = estado?.Trim().ToUpperInvariant();
            return e == Habilitado || e == "ACTIVO";
        }

        public static string Legible(string? estado) => EsHabilitado(estado) ? "Habilitado" : "Inhabilitado";
    }

    // ME - Obra Social / CUF06 (diccionario de datos): estadoOS(d) = [ "Disponible" | "No Disponible" ].
    // Antes se guardaba "ACTIVA"; EsDisponible lo sigue aceptando por las bases ya creadas.
    public static class EstadosObraSocial
    {
        public const string Disponible = "DISPONIBLE";
        public const string NoDisponible = "NO DISPONIBLE";

        public static bool EsDisponible(string? estado)
        {
            var e = estado?.Trim().ToUpperInvariant();
            return e == Disponible || e == "ACTIVA";
        }

        public static string Legible(string? estado) => EsDisponible(estado) ? "Disponible" : "No Disponible";
    }
}
