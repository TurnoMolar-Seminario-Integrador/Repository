using DTOs;

namespace Frontend.MVC.Models
{
    // CUF03 - Administrar Historia Clínica (odontólogo): búsqueda del paciente, historial cronológico de
    // atenciones, detalle de una atención y corrección de sus observaciones.
    public class GestionHistoriaClinicaViewModel
    {
        // Búsqueda por documento (eBusquedaPaciente = tipoDocumento + nroDocumento).
        public string TipoDocumento { get; set; } = "DNI";
        public string NroDocumento { get; set; } = string.Empty;

        // Búsqueda por número de historia clínica; se guarda como texto para conservar lo que se tipeó.
        public string NroHCBuscada { get; set; } = string.Empty;

        public bool Busco { get; set; }

        // Mensaje de una búsqueda que ni siquiera llegó al servicio (p. ej. un N° de HC que no es número).
        public string? MensajeBusqueda { get; set; }

        // Pasos 1 y 2: datos personales del paciente y su historial. Nulo si no se buscó.
        public HistoriaClinicaPacienteDTO? Historia { get; set; }

        // Paso 3: la atención que se pidió ver (nroTurno) y su detalle. El detalle es nulo si no se pidió
        // ninguna o si la atención no pertenece a esta historia clínica.
        public int? NroTurnoSolicitado { get; set; }
        public DetalleAtencionHistorialDTO? Detalle { get; set; }

        // Alt 3.a.1: texto con el que se rellena el formulario de corrección (si la corrección falló, el que
        // el odontólogo había escrito; si no, las observaciones registradas).
        public string? ObservacionesEnEdicion { get; set; }

        // Concurrencia: huella de las observaciones registradas AHORA; el formulario la devuelve al guardar.
        // Tras un conflicto es la de las observaciones vigentes, de modo que volver a guardar sea una decisión
        // consciente del odontólogo (que vio el texto vigente) y no pise sin saberlo lo que escribió el otro.
        public string? VersionObservaciones { get; set; }

        // Solo tras un conflicto: el texto que otro odontólogo dejó registrado mientras se editaba.
        public string? ObservacionesVigentesEnConflicto { get; set; }
    }
}
