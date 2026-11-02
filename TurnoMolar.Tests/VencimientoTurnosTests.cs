using Application.Services;
using Microsoft.EntityFrameworkCore;
using TurnoMolar.Tests.Infraestructura;

namespace TurnoMolar.Tests
{
    // Cierre de jornada: turno "Reservado" vencido -> "Ausente" con penalización (decisión del usuario).
    public class VencimientoTurnosTests
    {
        [Fact]
        public async Task Turno_particular_vencido_pasa_a_ausente_con_arancel_particular()
        {
            using var bd = new BaseDeDatosDePrueba();
            using (var ctx = bd.NuevoContexto())
                Datos.AgregarTurno(ctx, DateTime.Today.AddDays(-1).AddHours(9), Datos.PacienteParticular, Datos.OdontologoA, Datos.General);

            var cantidad = await new VencimientoTurnosService(bd.NuevoContexto()).ProcesarTurnosVencidosAsync(DateTime.Now);

            Assert.Equal(1, cantidad);
            using var ctx2 = bd.NuevoContexto();
            var turno = await ctx2.Turnos.SingleAsync();
            Assert.Equal("AUSENTE", turno.EstadoTurno);
            Assert.Equal(15000m, turno.ArancelPenalizacionAplicado);
            var paciente = await ctx2.Pacientes.SingleAsync(p => p.NroDocumento == Datos.PacienteParticular);
            Assert.Equal("INHABILITADO", paciente.EstadoPaciente);
            Assert.Equal(15000m, paciente.MontoAdeudado);
        }

        [Fact]
        public async Task Turno_por_obra_social_usa_el_arancel_del_convenio()
        {
            using var bd = new BaseDeDatosDePrueba();
            using (var ctx = bd.NuevoContexto())
                Datos.AgregarTurno(ctx, DateTime.Today.AddDays(-2).AddHours(9), Datos.PacienteOsde, Datos.OdontologoA, Datos.General, "OBRA_SOCIAL");

            await new VencimientoTurnosService(bd.NuevoContexto()).ProcesarTurnosVencidosAsync(DateTime.Now);

            using var ctx2 = bd.NuevoContexto();
            Assert.Equal(18000m, (await ctx2.Turnos.SingleAsync()).ArancelPenalizacionAplicado);
        }

        [Fact]
        public async Task No_toca_turnos_de_hoy_ni_otros_estados()
        {
            using var bd = new BaseDeDatosDePrueba();
            using (var ctx = bd.NuevoContexto())
            {
                Datos.AgregarTurno(ctx, DateTime.Today.AddHours(1), Datos.PacienteParticular, Datos.OdontologoA, Datos.General);
                Datos.AgregarTurno(ctx, DateTime.Today.AddDays(-1).AddHours(9), Datos.PacienteSwiss, Datos.OdontologoA, Datos.General, estado: "FINALIZADO");
            }

            var cantidad = await new VencimientoTurnosService(bd.NuevoContexto()).ProcesarTurnosVencidosAsync(DateTime.Now);

            Assert.Equal(0, cantidad);
        }
    }
}
