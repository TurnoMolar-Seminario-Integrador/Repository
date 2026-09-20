namespace DTOs
{
    // Diccionario de datos, paso 3: eDetalleInsumosUtilizados = 0{nombreInsumo + cantidadUtilizada}N.
    // El costoUnitarioAlMomento NO lo tipea el odontólogo: el sistema lo toma de
    // Insumo.costoUnitario vigente en este momento (RN16), por eso acá solo viaja la cantidad.
    public class DetalleInsumoInputDTO
    {
        public int IdInsumo { get; set; }
        public int Cantidad { get; set; }
    }

    // Catálogo de insumos disponible para el paso 3 (para poblar el formulario). No es parte
    // del diccionario de datos de CUU03: es la vista de consulta que el odontólogo necesita
    // para saber qué insumos existen, su costo y el stock antes de cargarlos.
    public class InsumoDisponibleDTO
    {
        public int IdInsumo { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal CostoUnitario { get; set; }
        public int StockDisponible { get; set; }
    }

    // Camino básico, paso 1: resultado de pedir los datos que el sistema debe mostrar ANTES de
    // habilitar el formulario de carga (nroHC, fecha de creación, atenciones previas).
    public enum ResultadoObtenerDatosAtencion
    {
        Ok,
        TurnoInvalido
    }

    // Diccionario de datos, paso 1: sHistoriaClinicaPaciente = nroHC + fechaCreacion +
    // 0{atencionOdontologica(e)}N.
    public class DatosParaAtencionResultDTO
    {
        public ResultadoObtenerDatosAtencion Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int NroHC { get; set; }
        public DateTime FechaCreacionHC { get; set; }
        public List<AtencionPreviaResumenDTO> AtencionesPrevias { get; set; } = new();
    }

    public class AtencionPreviaResumenDTO
    {
        public DateTime FechaHoraInicio { get; set; }
        public string Observaciones { get; set; } = string.Empty;
    }

    // Camino básico, pasos 1 a 4: resultado de registrar la atención (parte del Odontólogo).
    public enum ResultadoRegistrarAtencion
    {
        Registrada,         // Paso 4: éxito. Turno -> "Atención Registrada".
        TurnoInvalido,      // El turno no existe, no es de hoy, no es "Presente", o no es del odontólogo logueado.
        DatosInvalidos,     // HC inexistente, horarios inconsistentes, insumo inexistente, o sin convenio vigente.
        StockInsuficiente   // Alguno de los insumos solicitados no tiene stock suficiente.
    }

    // Diccionario de datos, paso 4: sMontoTotalAtencion = /montoTotal + estadoTurno(d).
    public class RegistrarAtencionOdontologicaResultDTO
    {
        public ResultadoRegistrarAtencion Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string EstadoTurno { get; set; } = string.Empty;
        public decimal? MontoTotal { get; set; }
    }

    // Camino básico, paso 5: resultado de pedir los datos que el sistema debe mostrar cuando el
    // responsable indica que va a proceder con el cobro (antes de habilitar el formulario de
    // cobro), igual que el paso 1 con la Historia Clínica.
    public enum ResultadoObtenerDatosCobro
    {
        Ok,
        TurnoInvalido
    }

    // Diccionario de datos, paso 5: sMontoAPagar = /montoTotal + modalidadPagoElegida(d).
    public class DatosParaCobroResultDTO
    {
        public ResultadoObtenerDatosCobro Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public decimal MontoTotal { get; set; }
        public string ModalidadPago { get; set; } = string.Empty; // "Particular" | "Obra Social"
        public string NombrePaciente { get; set; } = string.Empty;
    }

    // Camino básico / alt 6.a / alt 6.b / alt 6.c: resultado de procesar el cobro (parte del
    // Responsable de la Clínica).
    public enum ResultadoRegistrarCobro
    {
        Finalizado,          // Camino básico ó 6.a: pago exitoso -> Turno "Finalizado".
        FaltaDePago,         // Alt 6.b: deuda registrada -> Turno "Finalizado", Paciente "Inhabilitado".
        TurnoInvalido,       // El turno no existe o todavía no está en "Atención Registrada".
        ModalidadNoCoincide, // Alt 6.c: se intentó cobrar bajo una modalidad distinta a la del turno.
        AportesNoCoinciden   // Validación propia de 6.a: aportePaciente + aporteObraSocial != montoTotal.
    }

    // Diccionario de datos, paso 6 / 6.a: sConfirmacionPago = fechaYHoraPago + monto + estadoTurno(d).
    // Diccionario de datos, 6.b.1: sAvisoDeudaRegistrada = montoAdeudado + estadoPaciente(d).
    public class RegistrarCobroResultDTO
    {
        public ResultadoRegistrarCobro Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string EstadoTurno { get; set; } = string.Empty;

        // Solo se completan en el camino de pago exitoso (básico / 6.a): sConfirmacionPago.
        public DateTime? FechaHoraPago { get; set; }
        public decimal? Monto { get; set; }

        // Solo se completan en el camino de falta de pago (Alt 6.b): sAvisoDeudaRegistrada.
        public decimal? MontoAdeudado { get; set; }
        public string? EstadoPaciente { get; set; }
    }
}
