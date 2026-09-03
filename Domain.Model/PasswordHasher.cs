using System.Security.Cryptography;

namespace Domain.Model
{
    // Hash + salt para las columnas Clave/SaltClave (Pacientes, Odontologos,
    // ResponsablesClinica). Vive en Domain.Model (no en Application.Services) porque
    // Data también la necesita para sembrar datos, y Data no puede referenciar
    // Application.Services sin crear una dependencia circular.
    public static class PasswordHasher
    {
        private const int Iteraciones = 100_000;
        private const int TamañoHash = 32; // 256 bits

        public static (string Hash, string Salt) Generar(string password)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(16);
            using var derive = new Rfc2898DeriveBytes(password, saltBytes, Iteraciones, HashAlgorithmName.SHA256);
            var hashBytes = derive.GetBytes(TamañoHash);
            return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
        }

        public static bool Validar(string password, string hashAlmacenado, string saltAlmacenado)
        {
            if (string.IsNullOrEmpty(hashAlmacenado) || string.IsNullOrEmpty(saltAlmacenado))
                return false;

            byte[] saltBytes, hashEsperado;
            try
            {
                saltBytes = Convert.FromBase64String(saltAlmacenado);
                hashEsperado = Convert.FromBase64String(hashAlmacenado);
            }
            catch (FormatException)
            {
                return false; // datos sembrados en un formato viejo/no compatible
            }

            using var derive = new Rfc2898DeriveBytes(password, saltBytes, Iteraciones, HashAlgorithmName.SHA256);
            var hashIngresado = derive.GetBytes(TamañoHash);

            return CryptographicOperations.FixedTimeEquals(hashIngresado, hashEsperado);
        }
    }
}