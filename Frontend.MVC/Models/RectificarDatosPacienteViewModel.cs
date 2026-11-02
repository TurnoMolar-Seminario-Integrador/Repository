using Domain.Model;

namespace Frontend.MVC.Models
{
    // CUF02 - Rectificar Datos del Paciente, desde la consola del responsable de la clínica (otro actor del
    // caso de uso): identifica al paciente por tipo y número de documento y rectifica sus datos
    // personales y su obra social.
    public class RectificarDatosPacienteViewModel
    {
        // Búsqueda del paciente (documento con el que figura hoy).
        public string TipoDocumento { get; set; } = "DNI";
        public string NroDocumento { get; set; } = string.Empty;
        public bool Busco { get; set; }
        public string? MensajeBusqueda { get; set; }

        // Nulo si no se buscó o no se encontró al paciente.
        public FormDatosPersonalesViewModel? Form { get; set; }

        public string EstadoPaciente { get; set; } = string.Empty;

        // Nulo = paciente particular.
        public string? IdentificadorOS { get; set; }
        public string? ObraSocialNombre { get; set; }

        // Obras sociales con acuerdo activo (las únicas a las que se puede cambiar, CUF02 alt 2.b.1).
        public IReadOnlyList<ObraSocial> ObrasSociales { get; set; } = new List<ObraSocial>();
    }
}
