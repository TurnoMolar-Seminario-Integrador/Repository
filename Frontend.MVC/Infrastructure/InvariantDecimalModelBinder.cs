using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Frontend.MVC.Infrastructure
{
    // Los importes (cobros del responsable, etc.) llegan de campos <input type="number">, que el
    // navegador envía SIEMPRE con punto decimal ("12345.50"), sin importar el idioma del usuario.
    // El binder de decimales por defecto los interpreta con la cultura del servidor: en un servidor
    // es-AR el punto es separador de miles y "12345.50" se leía como 1.234.550, y en un servidor con
    // cultura invariante o en-US una coma decimal ("12345,50") se rechazaba. Es decir, el mismo cobro
    // valía distinto según dónde se desplegara el sistema.
    //
    // Este binder no depende de la cultura del servidor:
    //   - "12345.50"      -> 12345,50  (formato de los campos numéricos del navegador)
    //   - "12345,50"      -> 12345,50  (coma decimal, como se escribe en Argentina)
    //   - "12.345,50"     -> 12345,50  (con separador de miles; solo si hay coma decimal)
    //   - cualquier otra cosa (letras, dos puntos decimales, exponentes) -> error de validación.
    // Un texto con un único punto y sin coma ("1.234") se toma como decimal, igual que lo envía el
    // navegador; los separadores de miles solo se admiten junto con una coma decimal.
    public class InvariantDecimalModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ArgumentNullException.ThrowIfNull(bindingContext);

            var proveedor = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (proveedor == ValueProviderResult.None)
            {
                // No vino el campo: se deja el comportamiento por defecto (valor por defecto del
                // parámetro o error de "requerido").
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, proveedor);

            var texto = proveedor.FirstValue;
            var mensajes = bindingContext.ModelMetadata.ModelBindingMessageProvider;

            if (string.IsNullOrWhiteSpace(texto))
            {
                if (bindingContext.ModelMetadata.IsReferenceOrNullableType)
                {
                    bindingContext.Result = ModelBindingResult.Success(null);
                }
                else
                {
                    bindingContext.ModelState.TryAddModelError(
                        bindingContext.ModelName,
                        mensajes.ValueMustNotBeNullAccessor(proveedor.ToString()));
                }
                return Task.CompletedTask;
            }

            if (TryParse(texto, out var valor))
            {
                bindingContext.Result = ModelBindingResult.Success(valor);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    mensajes.AttemptedValueIsInvalidAccessor(texto, bindingContext.ModelMetadata.GetDisplayName()));
            }

            return Task.CompletedTask;
        }

        // Expuesto para poder probarlo sin armar un contexto de binding.
        public static bool TryParse(string? texto, out decimal valor)
        {
            valor = 0m;
            if (string.IsNullOrWhiteSpace(texto))
                return false;

            var normalizado = texto.Trim();

            if (normalizado.Contains(','))
            {
                // Formato local: la coma es el decimal y el punto, si aparece, es separador de miles.
                if (normalizado.Count(c => c == ',') != 1)
                    return false;
                normalizado = normalizado.Replace(".", string.Empty).Replace(',', '.');
            }

            return decimal.TryParse(
                normalizado,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out valor);
        }
    }

    public class InvariantDecimalModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var tipo = context.Metadata.UnderlyingOrModelType;
            return tipo == typeof(decimal) ? new InvariantDecimalModelBinder() : null;
        }
    }
}
