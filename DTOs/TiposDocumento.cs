namespace DTOs
{
    // Tipos de documento admitidos en TODO el sistema (alta, rectificación de datos, login, búsquedas):
    // DNI, LC, LE y PAS (pasaporte). Antes cada pantalla ofrecía su propia lista (CUIL, Pasaporte, Otros,
    // PASAPORTE...): un paciente dado de alta con un tipo que el login no ofrecía no podía ingresar.
    // El valor se guarda en mayúsculas (ver Persona.SetTipoDocumento).
    public static class TiposDocumento
    {
        public const string DNI = "DNI";
        public const string LC = "LC";
        public const string LE = "LE";
        public const string PAS = "PAS";

        // (valor guardado, texto que ve el usuario), en el orden en que se ofrecen.
        public static readonly IReadOnlyList<(string Valor, string Texto)> Opciones = new List<(string, string)>
        {
            (DNI, "DNI"),
            (LC, "LC"),
            (LE, "LE"),
            (PAS, "Pasaporte")
        };

        public static bool EsValido(string? tipoDocumento) =>
            Opciones.Any(o => string.Equals(o.Valor, tipoDocumento?.Trim(), StringComparison.OrdinalIgnoreCase));

        // El número de DNI, LC y LE es solo numérico; el del pasaporte admite letras y números.
        public static bool EsNumerico(string? tipoDocumento)
        {
            var tipo = tipoDocumento?.Trim().ToUpperInvariant();
            return tipo == DNI || tipo == LC || tipo == LE;
        }
    }
}
