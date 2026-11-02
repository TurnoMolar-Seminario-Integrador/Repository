using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Domain.Model;

namespace Application.Services
{
    // Control de concurrencia optimista entre pedidos (el formulario se muestra en un pedido y se guarda en otro).
    //
    // El modelo de datos (MDF v1.01) no tiene una columna de versión (rowversion) y el parche no cambia el esquema,
    // así que la "versión" de lo que se está editando es una huella (SHA-256, en hexadecimal) de los valores con
    // los que se mostró. El formulario la lleva en un campo oculto; al guardar, el servicio la recalcula con lo
    // que hay en la base y, si no coincide, otra persona modificó el dato en el medio: no se guarda nada y se
    // informa el conflicto. Complementa a los tokens de concurrencia de EF Core (TurnoMolarDbContext), que cubren
    // la carrera dentro de un mismo pedido, entre que el servicio lee la fila y la guarda.
    public static class VersionDeDatos
    {
        // Separador de unidad (U+001F): no aparece en ningún dato de texto de los formularios.
        private const string Separador = "\u001F";

        public static string Calcular(params string?[] valores)
        {
            var texto = string.Join(Separador, valores.Select(v => v ?? string.Empty));
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texto)));
        }

        // Los datos personales editables de CUF02 (el documento no entra: es la identidad con la que se busca).
        public static string DePaciente(Paciente paciente) => Calcular(
            paciente.Nombre,
            paciente.Apellido,
            paciente.FechaNacimiento.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            paciente.Telefono,
            paciente.Email,
            paciente.Domicilio);

        // El campo de observaciones de una atención (CUF03, alt 3.a.1).
        public static string DeObservaciones(string? observaciones) => Calcular(observaciones);

        // Una versión vacía (el cliente no la envió) nunca coincide: sin ella no se puede saber qué vio quien edita.
        public static bool Coincide(string? esperada, string vigente) =>
            !string.IsNullOrEmpty(esperada) && string.Equals(esperada.Trim(), vigente, StringComparison.OrdinalIgnoreCase);
    }
}
