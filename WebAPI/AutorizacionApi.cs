using System.Security.Claims;

namespace WebAPI
{
    // Autorización de la WebAPI: roles del sistema, políticas con nombre y comprobaciones de "es su propia
    // cuenta".
    //
    // Defecto corregido: /pacientes, /turnos y parte de /agenda se podían llamar sin token. Cualquiera, con
    // solo conocer la URL, podía listar y borrar pacientes, crear o modificar turnos y consultar el estado
    // (deuda, turno pendiente) de un paciente por su documento. Ahora la API niega por defecto (política
    // de respaldo en Program.cs: todo endpoint exige un usuario autenticado salvo que se declare lo
    // contrario) y cada grupo de endpoints exige además el rol que le corresponde. Quedan anónimos
    // solamente el inicio de sesión (/api/auth/login) y el alta de un paciente (POST /pacientes: CUF01, el
    // paciente se registra antes de tener cuenta).
    public static class AutorizacionApi
    {
        // Roles del sistema (Persona.Rol): los mismos valores que usan los [Authorize(Roles = ...)] del
        // frontend y que AuthService escribe en el claim de rol del token.
        public const string RolPaciente = "Paciente";
        public const string RolResponsable = "ResponsableClinica";
        public const string RolAdmin = "Admin";

        // Responsable de la clínica o Admin (el Admin es un Responsable con rol superior).
        public const string PoliticaPersonal = "PersonalDeLaClinica";

        // Solo el Admin (acciones destructivas que ningún caso de uso documentado pide).
        public const string PoliticaAdmin = "Administrador";

        public static bool EsPersonalDeLaClinica(ClaimsPrincipal user) =>
            user.IsInRole(RolResponsable) || user.IsInRole(RolAdmin);

        // El token trae el documento de quien inició sesión (claims TipoDocumento y NroDocumento, ver
        // AuthService). Un paciente solo puede operar sobre su propia cuenta. Tipo y número se guardan en
        // mayúsculas (Persona), así que se comparan sin distinguirlas.
        public static bool EsElPropioPaciente(ClaimsPrincipal user, string? tipoDocumento, string? nroDocumento) =>
            user.IsInRole(RolPaciente)
            && string.Equals(user.FindFirst("TipoDocumento")?.Value?.Trim(), tipoDocumento?.Trim(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(user.FindFirst("NroDocumento")?.Value?.Trim(), nroDocumento?.Trim(), StringComparison.OrdinalIgnoreCase);

        public static IResult SinPermiso(string mensaje) =>
            Results.Json(new { error = mensaje }, statusCode: StatusCodes.Status403Forbidden);
    }
}
