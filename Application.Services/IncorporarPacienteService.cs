using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    // =========================================================================
    // CUF01 - INCORPORAR PACIENTE (E4.R01, v1.02)
    // Actor primario: Paciente. Meta: incorporar a un paciente a la clínica, validando su identidad y
    // dejando creada su historia clínica digital.
    //
    // Camino básico:
    //   1. El paciente ingresa sus datos personales; el sistema valida que no exista un paciente con el
    //      mismo tipo y número de documento.
    //   2. El paciente indica que abonará en forma particular.
    //   3. El paciente confirma el alta; el sistema lo registra HABILITADO, le asigna un número único de
    //      historia clínica, registra la misma -- INICIALMENTE VACÍA -- y confirma el alta informando el
    //      número de historia clínica asignado.
    // Alt 1.a.1: ya existe el paciente -> se informa y se sugiere iniciar sesión.
    // Alt 2.a <reemplaza>: el paciente indica que cuenta con obra social -> se valida el acuerdo; sin
    //      acuerdo activo se informa y se pide la modalidad particular (2.a.1.a.1, vuelve al paso 2).
    // Postcondición de fracaso: no se crea el paciente ni la historia clínica.
    //
    // Defectos corregidos (alta de PacienteController y alta de la WebAPI):
    //  - La historia clínica se creaba con textos inventados ("Sin antecedentes registrados en el alta.",
    //    "Ninguna declarada.", "Ficha clínica de registro inicial."): el documento pide una historia
    //    clínica inicialmente vacía. El alta de la WebAPI directamente no la creaba, y CUF03 parte de que
    //    todo paciente tiene su historia clínica.
    //  - La confirmación no informaba el número de historia clínica asignado.
    //  - El aviso de paciente ya registrado no era el del diccionario de datos y se evaluaba después de la
    //    obra social, no en el paso 1.
    //  - El número de documento se leía como int (ver DatosPersonalesPacienteValidador).
    //  - La WebAPI aceptaba el estado (y la deuda) que mandara el cliente; el alta siempre es HABILITADO.
    //
    //  - La contraseña del alta era una provisoria fija para todos los pacientes. RN3 pide que el paciente
    //    establezca la suya: ahora la elige en el alta (mínimo 6 caracteres) y se guarda hasheada. Para
    //    probar, el equipo usa "paciente123".
    //
    // Correo electrónico (el MDF no define UNIQUE sobre Email; regla del equipo): pertenece a una persona,
    // identificada por su clave primaria (tipo y número de documento). Una misma persona repite su correo en
    // todos sus roles (p. ej. Karina González, odontóloga y responsable); dos personas distintas no pueden
    // compartirlo. Ver IPersonaRepository.
    // =========================================================================
    public interface IIncorporarPacienteService
    {
        Task<IncorporarPacienteResultDTO> IncorporarAsync(IncorporarPacienteRequestDTO solicitud);
    }

    public class IncorporarPacienteService : IIncorporarPacienteService
    {
        // CUF01, alt 1.a.1 (diccionario de datos): mensajePacienteYaRegistrado.
        public const string MensajePacienteYaRegistrado =
            "Ya existe un paciente registrado con ese tipo y número de documento. Inicie sesión para continuar.";

        public const string MensajeDatosInvalidos = "Por favor, revisá los campos marcados en rojo.";

        // El correo ya lo usa otra persona (otra clave primaria) en cualquiera de sus roles.
        public const string MensajeCorreoDeOtraPersona =
            "Ese correo electrónico ya está registrado a nombre de otra persona.";

        // La misma persona figura en la clínica (odontólogo o responsable) con otro correo: un mismo usuario
        // tiene el mismo correo en todos sus roles. No se informa cuál es (el alta es pública).
        public const string MensajeCorreoDistintoAlDeLaPersona =
            "Ese documento ya figura en la clínica con otro correo electrónico. Un mismo usuario tiene el mismo correo en todos sus roles: ingresá el que tiene registrado.";

        // Paso 3: estadoPaciente(d) = "Habilitado".
        private const string EstadoAlta = "HABILITADO";

        private readonly IPacienteRepository _pacienteRepository;
        private readonly IPersonaRepository _personaRepository;
        private readonly ICoberturaPacienteService _coberturaService;

        public IncorporarPacienteService(
            IPacienteRepository pacienteRepository,
            IPersonaRepository personaRepository,
            ICoberturaPacienteService coberturaService)
        {
            _pacienteRepository = pacienteRepository;
            _personaRepository = personaRepository;
            _coberturaService = coberturaService;
        }

        public async Task<IncorporarPacienteResultDTO> IncorporarAsync(IncorporarPacienteRequestDTO solicitud)
        {
            // Paso 1: datos personales.
            var datos = DatosPersonalesPacienteValidador.Normalizar(solicitud.DatosPersonales);
            var errores = DatosPersonalesPacienteValidador.Validar(datos);

            // RN3: la contraseña la establece el paciente.
            var errorClave = DatosPersonalesPacienteValidador.ValidarClave(solicitud.Password);
            if (errorClave != null)
            {
                errores["Password"] = errorClave;
            }

            if (errores.Count > 0)
            {
                return new IncorporarPacienteResultDTO
                {
                    Resultado = ResultadoIncorporarPaciente.DatosInvalidos,
                    Mensaje = MensajeDatosInvalidos,
                    Errores = errores
                };
            }

            var tipoDocumento = datos.TipoDocumento!;
            var nroDocumento = datos.NroDocumento!;

            // Paso 1 / alt 1.a.1: no puede haber otro paciente con el mismo tipo y número de documento.
            if (await _pacienteRepository.GetAsync(tipoDocumento, nroDocumento) != null)
            {
                return new IncorporarPacienteResultDTO
                {
                    Resultado = ResultadoIncorporarPaciente.PacienteYaRegistrado,
                    Mensaje = MensajePacienteYaRegistrado
                };
            }

            // Paso 1: el correo es de una sola persona (clave primaria), con el mismo valor en todos sus roles.
            var errorCorreo = await ValidarCorreoAsync(datos.Email!, tipoDocumento, nroDocumento);
            if (errorCorreo != null)
            {
                return new IncorporarPacienteResultDTO
                {
                    Resultado = ResultadoIncorporarPaciente.DatosInvalidos,
                    Mensaje = MensajeDatosInvalidos,
                    Errores = new Dictionary<string, string> { ["Email"] = errorCorreo }
                };
            }

            // Paso 2 / alt 2.a: modalidad particular u obra social con acuerdo activo (RN6).
            var cobertura = await _coberturaService.ResolverObraSocialParaAltaAsync(solicitud.ObraSocial, solicitud.OtraObraSocial);
            if (!cobertura.Valida)
            {
                return new IncorporarPacienteResultDTO
                {
                    Resultado = ResultadoIncorporarPaciente.ObraSocialInvalida,
                    Mensaje = cobertura.Mensaje
                };
            }

            // Paso 3: el paciente queda Habilitado, sin deuda, con su historia clínica vacía. El propio
            // Paciente es la cuenta de acceso (Clave, SaltClave y Rol viven en Pacientes, MDF).
            var (clave, salt) = PasswordHasher.Generar(solicitud.Password!);
            var paciente = new Paciente(
                tipoDocumento,
                nroDocumento,
                datos.Nombre!,
                datos.Apellido!,
                datos.FechaNacimiento!.Value,
                datos.Telefono!,
                datos.Email!,
                datos.Domicilio!,
                EstadoAlta,
                cobertura.IdentificadorOS,
                0m,
                clave,
                salt,
                DateTime.Now,
                "Paciente");

            // NroHC en 0: lo asigna la base (identidad), único por construcción (RN5). Sin antecedentes,
            // alergias ni observaciones: la historia clínica nace vacía.
            var historiaClinica = new HistoriaClinica(0, paciente.TipoDocumento, paciente.NroDocumento, DateTime.Now);

            await _pacienteRepository.AddConHistoriaClinicaAsync(paciente, historiaClinica);

            return new IncorporarPacienteResultDTO
            {
                Resultado = ResultadoIncorporarPaciente.Incorporado,
                // Diccionario, paso 3: sConfirmacionAltaPaciente = estadoPaciente(d) + nroHC.
                Mensaje = $"El alta fue confirmada. Estado: Habilitado. Número de historia clínica asignado: {historiaClinica.NroHC:D5}.",
                TipoDocumento = paciente.TipoDocumento,
                NroDocumento = paciente.NroDocumento,
                EstadoPaciente = paciente.EstadoPaciente,
                NroHC = historiaClinica.NroHC
            };
        }

        private async Task<string?> ValidarCorreoAsync(string email, string tipoDocumento, string nroDocumento)
        {
            if (await _personaRepository.CorreoEnUsoPorOtraPersonaAsync(email, tipoDocumento, nroDocumento))
            {
                return MensajeCorreoDeOtraPersona;
            }

            if (await _personaRepository.TieneOtroCorreoEnRolesDelPersonalAsync(email, tipoDocumento, nroDocumento))
            {
                return MensajeCorreoDistintoAlDeLaPersona;
            }

            return null;
        }
    }
}
