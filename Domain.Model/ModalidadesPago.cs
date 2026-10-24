namespace Domain.Model
{
    // CUU01, diccionario de datos: modalidadPagoElegida(d) = ["Particular" | "Obra Social"].
    // Turno.ModalidadPagoElegida se guarda como PARTICULAR u OBRA_SOCIAL. Antes los servicios hacían
    // (valor ?? "PARTICULAR").ToUpper() sin validar: una request con "xyz" creaba un turno que después
    // no se podía cobrar ni como particular ni como obra social (alt 6.c lo rechaza), y la única salida
    // era registrar una falta de pago.
    public static class ModalidadesPago
    {
        public const string Particular = "PARTICULAR";
        public const string ObraSocial = "OBRA_SOCIAL";

        public static readonly IReadOnlyList<string> Validas = new[] { Particular, ObraSocial };

        // Código canónico ("Obra Social", "obra_social" -> "OBRA_SOCIAL") o null si no es válido.
        // Un valor vacío no es válido acá; usar Requerir para aplicar el valor por defecto.
        public static string? Normalizar(string? modalidad)
        {
            if (string.IsNullOrWhiteSpace(modalidad))
                return null;

            var clave = modalidad.Trim().ToUpperInvariant().Replace(' ', '_');
            return Validas.Contains(clave) ? clave : null;
        }

        // Para los puntos de entrada que reciben la modalidad de afuera: vacío = Particular (valor por
        // defecto que ya tenía el DTO de reserva); cualquier otro valor tiene que ser uno de los dos.
        // Lanza ArgumentException, que los endpoints y controladores ya traducen a un mensaje / 400.
        public static string Requerir(string? modalidad)
        {
            if (string.IsNullOrWhiteSpace(modalidad))
                return Particular;

            return Normalizar(modalidad)
                ?? throw new ArgumentException("La modalidad de pago indicada no es válida. Las opciones son Particular u Obra Social.", nameof(modalidad));
        }

        public static string Etiqueta(string? modalidad) =>
            string.Equals(modalidad, ObraSocial, StringComparison.OrdinalIgnoreCase) ? "Obra Social" : "Particular";
    }
}
