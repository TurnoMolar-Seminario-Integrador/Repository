using Application.Services;
using Data;

namespace TurnoMolar.Tests.Infraestructura
{
    // Construye los servicios que trabajan con repositorios, como lo hace la inyección de dependencias.
    public static class Servicios
    {
        public static AgendaTurnoService Agenda(TurnoMolarDbContext c) => new(
            new PacienteRepository(c), new OdontologoRepository(c), new EspecialidadRepository(c),
            new ObraSocialRepository(c), new TurnoRepository(c), new ComprobanteTurnoRepository(c));

        public static AsistenciaTurnoService Asistencia(TurnoMolarDbContext c) => new(
            new TurnoRepository(c), new PacienteRepository(c), new ObraSocialRepository(c));

        public static GestionTurnoService GestionTurno(TurnoMolarDbContext c) => new(
            new TurnoRepository(c), new PacienteRepository(c), new OdontologoRepository(c),
            new ComprobanteTurnoRepository(c), new ObraSocialRepository(c));

        public static RegularizarDeudaService RegularizarDeuda(TurnoMolarDbContext c) => new(
            new PacienteRepository(c), new TurnoRepository(c), new PagoRepository(c));

        public static FinalizarAtencionService FinalizarAtencion(TurnoMolarDbContext c) => new(
            new TurnoRepository(c), new PacienteRepository(c), new HistoriaClinicaRepository(c),
            new AtencionOdontologicaRepository(c), new InsumoRepository(c), new ObraSocialRepository(c), new PagoRepository(c));
    }
}
