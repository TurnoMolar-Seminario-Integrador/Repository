using System.Globalization;
using System.Text;

namespace Domain.Model
{
    // Métodos de pago admitidos al registrar un cobro: CUU03 paso 6 y alt 6.a (eDatosPago = monto +
    // tipoMetodoPago), CUU01 alt 2.a.1.a.1 (eDatosPagoDeuda = tipoMetodoPago) y CUF10.
    //
    // La documentación no enumera los valores; la lista cerrada es la que ya ofrecen las pantallas
    // (Turnos del Día del responsable y Métodos de Pago del paciente). Antes se guardaba cualquier
    // texto no vacío ("BITCOIN" quedaba registrado) y no había control de largo propio (la columna
    // Pagos.MetodoPago admite 30 caracteres).
    public static class MetodosPago
    {
        public const string Efectivo = "EFECTIVO";
        public const string TarjetaDebito = "TARJETA_DEBITO";
        public const string TarjetaCredito = "TARJETA_CREDITO";
        public const string Transferencia = "TRANSFERENCIA";

        // Valor que dejaba la versión anterior de "Pagar deuda" en los Pagos de regularización. No se
        // acepta para registrar pagos nuevos: solo se reconoce para mostrar los que ya existen.
        public const string RegularizacionDeudaHeredado = "REGULARIZACION_DEUDA";

        public static readonly IReadOnlyList<string> Validos =
            new[] { Efectivo, TarjetaDebito, TarjetaCredito, Transferencia };

        // Devuelve el código canónico ("Tarjeta Débito", "tarjeta_debito " -> "TARJETA_DEBITO") o null
        // si el valor está vacío o no es uno de los métodos admitidos.
        public static string? Normalizar(string? metodo)
        {
            if (string.IsNullOrWhiteSpace(metodo))
                return null;

            var clave = QuitarTildes(metodo).Trim().ToUpperInvariant().Replace(' ', '_');
            return Validos.Contains(clave) ? clave : null;
        }

        public static bool EsValido(string? metodo) => Normalizar(metodo) != null;

        // Texto para mostrar al usuario (los códigos son internos).
        public static string Etiqueta(string? metodo)
        {
            var codigo = QuitarTildes(metodo ?? string.Empty).Trim().ToUpperInvariant().Replace(' ', '_');
            return codigo switch
            {
                Efectivo => "Efectivo",
                TarjetaDebito => "Tarjeta de débito",
                TarjetaCredito => "Tarjeta de crédito",
                Transferencia => "Transferencia",
                RegularizacionDeudaHeredado => "Regularización de deuda",
                "" => "Sin especificar",
                _ => char.ToUpperInvariant(codigo[0]) + codigo.Substring(1).ToLowerInvariant().Replace('_', ' ')
            };
        }

        private static string QuitarTildes(string texto)
        {
            var descompuesto = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(descompuesto.Length);
            foreach (var c in descompuesto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
