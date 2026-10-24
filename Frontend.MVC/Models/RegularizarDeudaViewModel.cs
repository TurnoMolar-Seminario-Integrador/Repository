using DTOs;

namespace Frontend.MVC.Models
{
    // CUF10 alt 2.a: pantalla del responsable para registrar el cobro presencial de la deuda de un
    // paciente. El responsable identifica al paciente por tipo y número de documento
    // (eRegistroCobroManual = tipoDocumento + nroDocumento).
    public class RegularizarDeudaViewModel
    {
        public string TipoDocumento { get; set; } = "DNI";
        public string NroDocumento { get; set; } = string.Empty;

        // true si se buscó un paciente (se enviaron tipo y número de documento).
        public bool Busco { get; set; }

        // Resultado de la búsqueda; nulo si no se buscó o no se encontró al paciente.
        public DeudaPacienteDTO? Consulta { get; set; }

        // Mensaje de la búsqueda cuando no hay nada para cobrar (paciente inexistente) -- sin deuda se usa
        // Consulta.Mensaje (mensajeSinDeuda).
        public string? MensajeBusqueda { get; set; }

        // Pacientes con saldo de deuda pendiente (atajo para no tener que tipear el documento).
        public IReadOnlyList<DeudaPacienteDTO> PacientesConDeuda { get; set; } = Array.Empty<DeudaPacienteDTO>();
    }
}
