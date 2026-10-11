namespace Frontend.MVC.Models
{
    // Turnos del Día del Responsable de la Clínica (y del Admin, que es un responsable con rango
    // superior). A diferencia de la agenda del odontólogo, que solo muestra los turnos propios,
    // esta pantalla reúne los de TODOS los odontólogos. Desde acá el responsable deriva el control
    // de asistencia (CUU02) y cobra las atenciones ya registradas (CUU03, pasos 5 y 6), que es lo
    // que lleva el turno a "Finalizado".
    public class AgendaResponsableViewModel
    {
        public DateTime Fecha { get; set; }

        // Turnos de hoy de todos los odontólogos, sin los cancelados ni los reprogramados (igual
        // criterio que la agenda del odontólogo), ordenados por hora.
        public List<GestionTurnoFilaViewModel> TurnosDeHoy { get; set; } = new();

        // Atenciones que el odontólogo registró en días anteriores y siguen sin cobrarse. Si no se
        // mostraran, el turno quedaría en "Atención Registrada" para siempre: la agenda de hoy no
        // lo incluye y, mientras tanto, el paciente no puede sacar otro turno (RN8).
        public List<GestionTurnoFilaViewModel> CobrosPendientesAnteriores { get; set; } = new();

        public int EnSalaDeEspera => TurnosDeHoy.Count(t => t.EstadoTurno == "PRESENTE");

        public int FinalizadosHoy => TurnosDeHoy.Count(t => t.EstadoTurno == "FINALIZADO");

        public int PendientesDeCobro =>
            TurnosDeHoy.Count(t => t.EstadoTurno == "ATENCION_REGISTRADA") + CobrosPendientesAnteriores.Count;
    }

    // Panel principal del Responsable de la Clínica.
    public class PanelResponsableViewModel
    {
        public int TurnosHoy { get; set; }
        public int PendientesDeCobro { get; set; }
    }
}
