using Data;
using Domain.Model;
using DTOs;

namespace Application.Services
{
    // =========================================================================
    // CUF03 - ADMINISTRAR HISTORIA CLÍNICA (E4.R01, v1.02)
    // Actor primario: Odontólogo. Meta: permitir la consulta del historial clínico completo de un paciente
    // y la corrección de observaciones ya registradas en atenciones previas.
    //
    // Camino básico:
    //   1. El odontólogo busca al paciente por tipo y número de documento o por número de historia clínica;
    //      el sistema valida que exista y muestra sus datos personales.
    //   2. El sistema muestra, EN ORDEN CRONOLÓGICO, las atenciones registradas del paciente: fecha,
    //      especialidad, odontólogo interviniente, observaciones e insumos utilizados.
    //                                                               -> BuscarHistoriaClinicaAsync.
    //   3. El odontólogo selecciona una atención (nroTurno) y el sistema muestra su información ampliada
    //                                                               -> ObtenerDetalleAtencionAsync.
    // Alt 2.a.1: sin atenciones -> "El paciente no posee atenciones odontológicas registradas."
    // Alt 3.a.1: el odontólogo corrige el campo de observaciones de la atención seleccionada y el sistema
    //            registra el cambio y lo confirma                       -> CorregirObservacionesAsync.
    //
    // Defecto corregido: la pantalla "Gestión Atenciones" era un mockup. Mostraba dos atenciones inventadas
    // (con pacientes, historias clínicas y tratamientos fijos) y su botón "Guardar Evolución" no guardaba
    // nada: respondía "registrada correctamente en la Historia Clínica" sin tocar la base. Ahora consulta
    // las historias clínicas reales y la corrección de observaciones se persiste. Cargar una atención nueva
    // no es parte de CUF03: es el camino básico de CUU03 (Agenda de hoy).
    //
    // Concurrencia (optimista): dos odontólogos pueden corregir a la vez las observaciones de la misma
    // atención. El detalle trae la huella de las observaciones que se mostraron (VersionObservaciones) y la
    // corrección la devuelve; si ya no coincide con la base, otro las modificó en el medio: no se guarda
    // nada y se devuelven las observaciones vigentes para que el odontólogo decida (ConflictoConcurrencia).
    // =========================================================================
    public interface IAdministrarHistoriaClinicaService
    {
        // Pasos 1 y 2 (y alt 2.a). Se busca por nroHC si viene; si no, por tipo y número de documento.
        Task<HistoriaClinicaPacienteDTO> BuscarHistoriaClinicaAsync(string? tipoDocumento, string? nroDocumento, int? nroHC);

        // Paso 3. Nulo si la atención no pertenece a esa historia clínica.
        Task<DetalleAtencionHistorialDTO?> ObtenerDetalleAtencionAsync(int nroHC, int nroTurno);

        // Alt 3.a.1. versionObservaciones: la VersionObservaciones del detalle que se mostró; sin ella (o si ya
        // no coincide) no se guarda nada y el resultado es ConflictoConcurrencia.
        Task<CorreccionObservacionesResultDTO> CorregirObservacionesAsync(
            int nroHC, int nroTurno, string? observaciones, string? versionObservaciones);
    }

    public class AdministrarHistoriaClinicaService : IAdministrarHistoriaClinicaService
    {
        // CUF03, alt 2.a.1 (diccionario de datos): mensajeHistoriaVacia.
        public const string MensajeHistoriaVacia = "El paciente no posee atenciones odontológicas registradas.";

        // CUF03, alt 3.a.1 (diccionario de datos): mensajeCorreccionExitosa.
        public const string MensajeCorreccionExitosa = "Las observaciones de la atención fueron actualizadas correctamente.";

        public const string MensajeBusquedaIncompleta =
            "Ingresá el tipo y el número de documento, o el número de historia clínica, del paciente.";

        public const string MensajePacienteNoEncontrado = "No se encontró un paciente con los datos ingresados.";

        public const string MensajeConflictoConcurrencia =
            "Las observaciones de la atención fueron modificadas por otro usuario mientras las editabas. No se guardó ningún cambio: compará con el texto vigente y volvé a corregir.";

        public const string MensajeSinHistoriaClinica =
            "El paciente no posee una historia clínica digital registrada en el sistema.";

        // Largo de AtencionesOdontologicas.Observaciones (ver TurnoMolarDbContext).
        public const int LargoMaxObservaciones = 1000;

        private readonly IPacienteRepository _pacienteRepository;
        private readonly IHistoriaClinicaRepository _historiaClinicaRepository;
        private readonly IAtencionOdontologicaRepository _atencionRepository;

        public AdministrarHistoriaClinicaService(
            IPacienteRepository pacienteRepository,
            IHistoriaClinicaRepository historiaClinicaRepository,
            IAtencionOdontologicaRepository atencionRepository)
        {
            _pacienteRepository = pacienteRepository;
            _historiaClinicaRepository = historiaClinicaRepository;
            _atencionRepository = atencionRepository;
        }

        public async Task<HistoriaClinicaPacienteDTO> BuscarHistoriaClinicaAsync(string? tipoDocumento, string? nroDocumento, int? nroHC)
        {
            Paciente? paciente;
            HistoriaClinica? historiaClinica;

            if (nroHC.HasValue)
            {
                historiaClinica = await _historiaClinicaRepository.GetByNroHCAsync(nroHC.Value);
                paciente = historiaClinica?.Paciente;
            }
            else
            {
                var tipo = Normalizar(tipoDocumento);
                var nro = Normalizar(nroDocumento);
                if (tipo.Length == 0 || nro.Length == 0)
                {
                    return new HistoriaClinicaPacienteDTO
                    {
                        Resultado = ResultadoBusquedaHistoriaClinica.BusquedaIncompleta,
                        Mensaje = MensajeBusquedaIncompleta
                    };
                }

                paciente = await _pacienteRepository.GetAsync(tipo, nro);
                historiaClinica = paciente?.HistoriaClinica;
            }

            // Paso 1: "El sistema valida la existencia del paciente".
            if (paciente == null)
            {
                return new HistoriaClinicaPacienteDTO
                {
                    Resultado = ResultadoBusquedaHistoriaClinica.PacienteNoEncontrado,
                    Mensaje = MensajePacienteNoEncontrado
                };
            }

            // Precondición de sistema: todo paciente tiene su historia clínica (CUF01 paso 3).
            if (historiaClinica == null)
            {
                return new HistoriaClinicaPacienteDTO
                {
                    Resultado = ResultadoBusquedaHistoriaClinica.SinHistoriaClinica,
                    Mensaje = MensajeSinHistoriaClinica
                };
            }

            // Paso 2: atenciones en orden cronológico.
            var atenciones = await _atencionRepository.GetByHistoriaClinicaAsync(historiaClinica.NroHC);

            var resultado = new HistoriaClinicaPacienteDTO
            {
                Resultado = ResultadoBusquedaHistoriaClinica.Encontrada,
                NroHC = historiaClinica.NroHC,
                FechaCreacion = historiaClinica.FechaCreacion,
                TipoDocumento = paciente.TipoDocumento,
                NroDocumento = paciente.NroDocumento,
                Nombre = paciente.Nombre,
                Apellido = paciente.Apellido,
                FechaNacimiento = paciente.FechaNacimiento,
                Telefono = paciente.Telefono,
                Email = paciente.Email,
                Domicilio = paciente.Domicilio,
                Atenciones = atenciones.Select(MapearResumen).ToList()
            };

            // Alt 2.a.1: la historia clínica se encuentra vacía.
            resultado.Mensaje = resultado.Atenciones.Count == 0 ? MensajeHistoriaVacia : string.Empty;
            return resultado;
        }

        public async Task<DetalleAtencionHistorialDTO?> ObtenerDetalleAtencionAsync(int nroHC, int nroTurno)
        {
            var atenciones = await _atencionRepository.GetByHistoriaClinicaAsync(nroHC);
            var atencion = atenciones.FirstOrDefault(a => a.NroTurno == nroTurno);
            if (atencion == null)
            {
                return null;
            }

            return new DetalleAtencionHistorialDTO
            {
                NroTurno = atencion.NroTurno,
                FechaHoraInicio = atencion.FechaHoraAtencionInicio,
                FechaHoraFin = atencion.FechaHoraAtencionFin,
                DuracionReal = atencion.DuracionReal,
                MontoTotal = atencion.MontoTotal,
                Observaciones = atencion.Observaciones ?? string.Empty,
                VersionObservaciones = VersionDeDatos.DeObservaciones(atencion.Observaciones),
                NombreEspecialidad = NombreEspecialidad(atencion),
                NombreOdontologo = NombreOdontologo(atencion),
                Insumos = MapearInsumos(atencion)
            };
        }

        public async Task<CorreccionObservacionesResultDTO> CorregirObservacionesAsync(
            int nroHC, int nroTurno, string? observaciones, string? versionObservaciones)
        {
            var texto = (observaciones ?? string.Empty).Trim();
            if (texto.Length == 0)
            {
                return new CorreccionObservacionesResultDTO
                {
                    Resultado = ResultadoCorreccionObservaciones.ObservacionesInvalidas,
                    Mensaje = "Las observaciones no pueden quedar vacías."
                };
            }

            if (texto.Length > LargoMaxObservaciones)
            {
                return new CorreccionObservacionesResultDTO
                {
                    Resultado = ResultadoCorreccionObservaciones.ObservacionesInvalidas,
                    Mensaje = $"Las observaciones no pueden superar los {LargoMaxObservaciones} caracteres."
                };
            }

            // La atención tiene que ser de la historia clínica que el odontólogo está mirando.
            var atenciones = await _atencionRepository.GetByHistoriaClinicaAsync(nroHC);
            var atencion = atenciones.FirstOrDefault(a => a.NroTurno == nroTurno);
            if (atencion == null)
            {
                return new CorreccionObservacionesResultDTO
                {
                    Resultado = ResultadoCorreccionObservaciones.AtencionNoEncontrada,
                    Mensaje = "No se encontró la atención seleccionada en la historia clínica del paciente."
                };
            }

            // Concurrencia: las observaciones que vio el odontólogo tienen que ser las que hay en la base.
            if (!VersionDeDatos.Coincide(versionObservaciones, VersionDeDatos.DeObservaciones(atencion.Observaciones)))
            {
                return Conflicto(atencion.Observaciones);
            }

            // Solo cambia el campo de observaciones: ni los montos ni la valoración ni el pago se tocan.
            try
            {
                await _atencionRepository.ActualizarObservacionesAsync(atencion.IdAtencion, texto);
            }
            catch (ConflictoConcurrenciaException)
            {
                // Otro odontólogo guardó entre la lectura y este guardado (el repositorio no escribió nada).
                var vigentes = (await _atencionRepository.GetByHistoriaClinicaAsync(nroHC))
                    .FirstOrDefault(a => a.NroTurno == nroTurno)?.Observaciones;
                return Conflicto(vigentes);
            }

            return new CorreccionObservacionesResultDTO
            {
                Resultado = ResultadoCorreccionObservaciones.Corregida,
                Mensaje = MensajeCorreccionExitosa
            };
        }

        private static CorreccionObservacionesResultDTO Conflicto(string? observacionesVigentes)
        {
            return new CorreccionObservacionesResultDTO
            {
                Resultado = ResultadoCorreccionObservaciones.ConflictoConcurrencia,
                Mensaje = MensajeConflictoConcurrencia,
                ObservacionesVigentes = observacionesVigentes ?? string.Empty,
                VersionObservaciones = VersionDeDatos.DeObservaciones(observacionesVigentes)
            };
        }

        private static AtencionHistorialDTO MapearResumen(AtencionOdontologica atencion)
        {
            return new AtencionHistorialDTO
            {
                NroTurno = atencion.NroTurno,
                FechaHoraInicio = atencion.FechaHoraAtencionInicio,
                NombreEspecialidad = NombreEspecialidad(atencion),
                NombreOdontologo = NombreOdontologo(atencion),
                Observaciones = atencion.Observaciones ?? string.Empty,
                Insumos = MapearInsumos(atencion)
            };
        }

        private static List<InsumoUtilizadoDTO> MapearInsumos(AtencionOdontologica atencion)
        {
            return atencion.DetallesInsumos
                .Select(d => new InsumoUtilizadoDTO
                {
                    NombreInsumo = d.Insumo?.Nombre ?? string.Empty,
                    CantidadUtilizada = d.CantidadUtilizada,
                    CostoUnitarioAlMomento = d.CostoUnitarioAlMomento
                })
                .OrderBy(i => i.NombreInsumo)
                .ToList();
        }

        private static string NombreEspecialidad(AtencionOdontologica atencion) =>
            atencion.Turno?.Especialidad?.Nombre ?? string.Empty;

        private static string NombreOdontologo(AtencionOdontologica atencion) =>
            atencion.Turno?.Odontologo != null
                ? $"{atencion.Turno.Odontologo.Nombre} {atencion.Turno.Odontologo.Apellido}"
                : string.Empty;

        // El tipo y el número de documento se guardan en mayúsculas (Persona).
        private static string Normalizar(string? valor) => (valor ?? string.Empty).Trim().ToUpperInvariant();
    }
}
