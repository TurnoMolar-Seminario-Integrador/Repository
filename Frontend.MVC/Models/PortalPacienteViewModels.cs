using Domain.Model;
using DTOs;

namespace Frontend.MVC.Models
{
    public class DashboardPacienteViewModel
    {
        public Paciente Paciente { get; set; } = null!;
        public Turno? ProximoTurno { get; set; }
        public int TurnosPendientesCount { get; set; }
        public int AtencionesRealizadasCount { get; set; }
        public List<Odontologo> Odontologos { get; set; } = new();
        public List<Especialidad> Especialidades { get; set; } = new();
    }

    public class MisTurnosViewModel
    {
        public Paciente Paciente { get; set; } = null!;
        public List<Turno> Turnos { get; set; } = new();
        public List<Odontologo> Odontologos { get; set; } = new();
        public List<Especialidad> Especialidades { get; set; } = new();

        // CUU04 - Valorar Atención Odontológica, camino básico paso 1: listado de atenciones
        // finalizadas pendientes de valoración (pestaña "Para Valorar").
        public List<AtencionPendienteValoracionDTO> PendientesDeValoracion { get; set; } = new();
    }

    public class MetodosPagoViewModel
    {
        public Paciente Paciente { get; set; } = null!;
        public decimal SaldoPendiente => Paciente.MontoAdeudado ?? 0m;
        public bool EstaInhabilitado => Paciente.EstadoPaciente == "INHABILITADO" || (Paciente.MontoAdeudado.HasValue && Paciente.MontoAdeudado.Value > 0);
        public List<Pago> HistorialPagos { get; set; } = new();
    }

    public class SegurosViewModel
    {
        public Paciente Paciente { get; set; } = null!;
        public ObraSocial? ObraSocialActual => Paciente.ObraSocial;
        public List<ObraSocial> ObrasSocialesDisponibles { get; set; } = new();
    }

    public class ConfiguracionViewModel
    {
        public string TipoDocumento { get; set; } = "DNI";
        public int NroDocumento { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Domicilio { get; set; } = string.Empty;
        public string EstadoPaciente { get; set; } = "HABILITADO";
        public string ObraSocialNombre { get; set; } = "Particular";
    }

    public class HistorialClinicoViewModel
    {
        public Paciente Paciente { get; set; } = null!;
        public HistoriaClinica? HistoriaClinica { get; set; }
        public List<AtencionOdontologica> Atenciones { get; set; } = new();
    }
}
