namespace DTOs
{
    public class ConsultaDTO
    {
        public int CodAtencion { get; set; }
        public int Id { get => CodAtencion; set => CodAtencion = value; }
        public int CodTurno { get; set; }
        public int TurnoId { get => CodTurno; set => CodTurno = value; }
        public string PacienteTipoDoc { get; set; } = "DNI";
        public int PacienteNroDoc { get; set; }
        public int PacienteId { get => PacienteNroDoc; set => PacienteNroDoc = value; }
        public string? PacienteNombre { get; set; }
        public string? OdontologoNombre { get; set; }
        public string Diagnostico { get; set; } = string.Empty;
        public string Tratamiento { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public bool AnestesiaLocal { get; set; }
        public bool Radiografias { get; set; }
        public string? Valoracion { get; set; }
        public int? CalificacionEstrellas { get; set; }
        public DateTime FechaYHoraAtencionInicio { get; set; } = DateTime.Now;
        public DateTime FechaYHoraAtencionFin { get; set; } = DateTime.Now;
        public DateTime Fecha { get => FechaYHoraAtencionInicio; set => FechaYHoraAtencionInicio = value; }

        public List<ItemFacturaDTO> InsumosUtilizados { get; set; } = new();
    }

    public class ItemFacturaDTO
    {
        public int InsumoId { get; set; }
        public string? InsumoNombre { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal => Cantidad * PrecioUnitario;
    }
}
