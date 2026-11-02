namespace DTOs
{
    // Resultado común de las operaciones de los CUF de administración (CUF04, 06, 09, 11 y 12).
    // Codigo nombra el paso o la alternativa del caso de uso que se dio (p. ej. "1.a", "3.a.1").
    public class ResultadoOperacionDTO
    {
        public bool Exito { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;

        public static ResultadoOperacionDTO Ok(string mensaje, string codigo = "OK") =>
            new() { Exito = true, Codigo = codigo, Mensaje = mensaje };

        public static ResultadoOperacionDTO Error(string codigo, string mensaje) =>
            new() { Exito = false, Codigo = codigo, Mensaje = mensaje };
    }

    // ===== CUF12 - Administrar Especialidad =====
    public class EspecialidadListadoDTO
    {
        public int IdEspecialidad { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal ArancelParticular { get; set; }
    }

    // ===== CUF09 - Abastecer Insumos y Materiales =====
    // sListadoInsumos = 0{nombre + costoUnitario + stockDisponible}N
    public class InsumoListadoDTO
    {
        public int IdInsumo { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal CostoUnitario { get; set; }
        public int StockDisponible { get; set; }
    }

    // ===== CUF06 - Administrar Convenios de Obra Social =====
    // sListadoObrasSociales = 0{identificadorOS + nombreOS + planCobertura + estadoOS(d)}N
    public class ObraSocialListadoDTO
    {
        public string IdentificadorOS { get; set; } = string.Empty;
        public string NombreOS { get; set; } = string.Empty;
        public string PlanCobertura { get; set; } = string.Empty;
        public string EstadoOS { get; set; } = string.Empty;
        public bool Disponible { get; set; }
        public List<ConvenioDTO> Convenios { get; set; } = new();
    }

    // eConveniosDefinidos = 1{idEspecialidad + arancelConvenio}N -> se reutiliza ConvenioDTO (ObraSocialDTO.cs).


    // eDatosNuevaObraSocial = identificadorOS + nombreOS + planCobertura
    public class NuevaObraSocialDTO
    {
        public string IdentificadorOS { get; set; } = string.Empty;
        public string NombreOS { get; set; } = string.Empty;
        public string PlanCobertura { get; set; } = string.Empty;
        public List<ConvenioDTO> Convenios { get; set; } = new();
    }

    // ===== CUF04 - Conformar Cuerpo de Odontólogos =====
    // sListadoOdontologos = 0{nombre + apellido + matricula + estadoOdontologo(d)}N
    public class OdontologoListadoDTO
    {
        public string TipoDocumento { get; set; } = string.Empty;
        public string NroDocumento { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Matricula { get; set; } = string.Empty;
        public string EstadoOdontologo { get; set; } = string.Empty;
        public bool Habilitado { get; set; }
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Domicilio { get; set; } = string.Empty;
        public List<EspecialidadDisponibilidadDTO> Especialidades { get; set; } = new();
    }

    // eDisponibilidadHoraria = 1{nombreEspecialidad + 1{disponibilidadHoraria(e)}N}N
    public class EspecialidadDisponibilidadDTO
    {
        public int IdEspecialidad { get; set; }
        public string NombreEspecialidad { get; set; } = string.Empty;
        public List<FranjaHorariaDTO> Franjas { get; set; } = new();
    }

    // disponibilidadHoraria(e) = diaSemana + horaInicio + horaFin
    public class FranjaHorariaDTO
    {
        public string DiaSemana { get; set; } = string.Empty;
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
    }

    // eDatosNuevoOdontologo = datosPersonales(e) + matricula + 1{nombreEspecialidad}N
    public class NuevoOdontologoDTO
    {
        public string TipoDocumento { get; set; } = "DNI";
        public string NroDocumento { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public DateTime FechaNacimiento { get; set; }
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Domicilio { get; set; } = string.Empty;
        public string Matricula { get; set; } = string.Empty;
        public List<EspecialidadDisponibilidadDTO> Especialidades { get; set; } = new();
    }

    // ===== CUF05 - Consultar Valoraciones =====
    // sListadoValoracionesClinica = 1{nroTurno + fechaYHoraAtencionInicio + nombreEspecialidad + nombrePaciente
    //   + apellidoPaciente + nombreOdontologo + apellidoOdontologo + valoracion(e)}N
    public class ValoracionListadoDTO
    {
        public int NroTurno { get; set; }
        public DateTime FechaYHoraAtencionInicio { get; set; }
        public string NombreEspecialidad { get; set; } = string.Empty;
        public string NombrePaciente { get; set; } = string.Empty;
        public string ApellidoPaciente { get; set; } = string.Empty;
        public string NombreOdontologo { get; set; } = string.Empty;
        public string ApellidoOdontologo { get; set; } = string.Empty;
        public int Calificacion { get; set; }
        public string? Observaciones { get; set; }
    }

    public class ConsultaValoracionesResultDTO
    {
        public List<ValoracionListadoDTO> Valoraciones { get; set; } = new();
        // Alt 1.a.1: mensajeSinValoraciones(d), o null si hay valoraciones.
        public string? Mensaje { get; set; }
    }

    // ===== CUF11 - Coordinar Atención Extraordinaria =====
    // eDatosAtencionExtraordinaria = nombreEspecialidad + nombreOdontologo + apellidoOdontologo
    //   + fechaYHoraTurno + descripcionMaterial + modalidadPagoElegida(d)
    public class AtencionExtraordinariaRequestDTO
    {
        public string TipoDocumentoPaciente { get; set; } = "DNI";
        public string NroDocumentoPaciente { get; set; } = string.Empty;
        public int IdEspecialidad { get; set; }
        public string OdontologoTipoDocumento { get; set; } = "DNI";
        public string OdontologoNroDocumento { get; set; } = string.Empty;
        public DateTime FechaHoraTurno { get; set; }
        public string DescripcionMaterial { get; set; } = string.Empty;
        public string ModalidadPago { get; set; } = string.Empty;
    }

    public class AtencionExtraordinariaResultDTO
    {
        public bool Exito { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public int? NroTurno { get; set; }
        // sComprobanteTurnoExtraordinario
        public ComprobanteTurnoDTO? Comprobante { get; set; }
        public string? DescripcionMaterial { get; set; }
        public decimal? MontoAdeudado { get; set; }
    }
}
