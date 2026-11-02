using Application.Services;
using Domain.Model;
using DTOs;
using Microsoft.EntityFrameworkCore;
using TurnoMolar.Tests.Infraestructura;

namespace TurnoMolar.Tests
{
    // CUF06 - Administrar Convenios con Obras Sociales
    public class AdministrarConveniosTests
    {
        private static NuevaObraSocialDTO Nueva(string id = "PAMI") => new()
        {
            IdentificadorOS = id,
            NombreOS = "PAMI",
            PlanCobertura = "Básico",
            Convenios = { new ConvenioDTO { IdEspecialidad = Datos.General, ArancelConvenio = 9000m } }
        };

        [Fact]
        public async Task Camino_basico_registra_la_obra_social_disponible_con_sus_convenios()
        {
            using var bd = new BaseDeDatosDePrueba();
            var r = await new AdministrarConveniosService(bd.NuevoContexto()).RegistrarAsync(Nueva());

            Assert.True(r.Exito);
            using var ctx = bd.NuevoContexto();
            var os = await ctx.ObrasSociales.Include(o => o.Convenios).SingleAsync(o => o.IdentificadorOS == "PAMI");
            Assert.Equal(EstadosObraSocial.Disponible, os.EstadoOS);
            Assert.Single(os.Convenios);
        }

        [Fact]
        public async Task Alt_2a_rechaza_identificador_repetido()
        {
            using var bd = new BaseDeDatosDePrueba();
            var r = await new AdministrarConveniosService(bd.NuevoContexto()).RegistrarAsync(Nueva("OSDE"));
            Assert.False(r.Exito);
            Assert.Equal(AdministrarConveniosService.MensajeObraSocialExistente, r.Mensaje);
        }

        [Fact]
        public async Task El_alta_exige_al_menos_un_convenio()
        {
            using var bd = new BaseDeDatosDePrueba();
            var datos = Nueva();
            datos.Convenios.Clear();
            var r = await new AdministrarConveniosService(bd.NuevoContexto()).RegistrarAsync(datos);
            Assert.False(r.Exito);
        }

        [Fact]
        public async Task Alt_3a_modifica_el_plan()
        {
            using var bd = new BaseDeDatosDePrueba();
            var r = await new AdministrarConveniosService(bd.NuevoContexto()).ModificarPlanAsync("OSDE", "Plan 310");
            Assert.Equal(AdministrarConveniosService.MensajeConfirmacionPlan, r.Mensaje);
            using var ctx = bd.NuevoContexto();
            Assert.Equal("Plan 310", (await ctx.ObrasSociales.FindAsync("OSDE"))!.PlanCobertura);
        }

        [Fact]
        public async Task Alt_3b_y_3c_baja_y_rehabilitacion()
        {
            using var bd = new BaseDeDatosDePrueba();
            var servicio = new AdministrarConveniosService(bd.NuevoContexto());

            Assert.True((await servicio.DarDeBajaAsync("IOMA")).Exito);
            using (var ctx = bd.NuevoContexto())
                Assert.Equal(EstadosObraSocial.NoDisponible, (await ctx.ObrasSociales.FindAsync("IOMA"))!.EstadoOS);

            Assert.True((await new AdministrarConveniosService(bd.NuevoContexto()).RehabilitarAsync("IOMA")).Exito);
            using (var ctx = bd.NuevoContexto())
                Assert.Equal(EstadosObraSocial.Disponible, (await ctx.ObrasSociales.FindAsync("IOMA"))!.EstadoOS);
        }

        [Fact]
        public async Task La_obra_social_dada_de_baja_deja_de_cubrir_especialidades()
        {
            using var bd = new BaseDeDatosDePrueba();
            await new AdministrarConveniosService(bd.NuevoContexto()).DarDeBajaAsync("OSDE");
            using var ctx = bd.NuevoContexto();
            var os = await ctx.ObrasSociales.Include(o => o.Convenios).SingleAsync(o => o.IdentificadorOS == "OSDE");
            Assert.False(os.TieneAcuerdoActivo);
            Assert.False(await Servicios.Agenda(ctx).ObraSocialCubreEspecialidadAsync("OSDE", Datos.General));
        }

        [Fact]
        public async Task Alt_3d_modifica_y_agrega_convenios()
        {
            using var bd = new BaseDeDatosDePrueba();
            await new AdministrarConveniosService(bd.NuevoContexto()).RegistrarAsync(Nueva());

            var r1 = await new AdministrarConveniosService(bd.NuevoContexto()).GuardarConvenioAsync("PAMI", Datos.General, 9500m);
            var r2 = await new AdministrarConveniosService(bd.NuevoContexto()).GuardarConvenioAsync("PAMI", Datos.Endodoncia, 20000m);

            Assert.Equal(AdministrarConveniosService.MensajeConfirmacionConvenio, r1.Mensaje);
            Assert.True(r2.Exito);
            using var ctx = bd.NuevoContexto();
            var convenios = await ctx.Convenios.Where(c => c.IdentificadorOS == "PAMI").ToListAsync();
            Assert.Equal(2, convenios.Count);
            Assert.Equal(9500m, convenios.Single(c => c.IdEspecialidad == Datos.General).ArancelConvenio);
        }

        [Fact]
        public async Task Alt_3d_elimina_convenio_sin_turnos()
        {
            using var bd = new BaseDeDatosDePrueba();
            var r = await new AdministrarConveniosService(bd.NuevoContexto()).EliminarConvenioAsync("IOMA", 5);
            Assert.True(r.Exito);
            using var ctx = bd.NuevoContexto();
            Assert.False(await ctx.Convenios.AnyAsync(c => c.IdentificadorOS == "IOMA" && c.IdEspecialidad == 5));
        }

        [Fact]
        public async Task No_elimina_un_convenio_con_turnos_reservados_por_obra_social()
        {
            using var bd = new BaseDeDatosDePrueba();
            using (var ctx = bd.NuevoContexto())
                Datos.AgregarTurno(ctx, Datos.Proximo(DayOfWeek.Monday, 9), Datos.PacienteOsde, Datos.OdontologoA, Datos.General, "OBRA_SOCIAL");

            var r = await new AdministrarConveniosService(bd.NuevoContexto()).EliminarConvenioAsync("OSDE", Datos.General);
            Assert.False(r.Exito);
            Assert.Equal(AdministrarConveniosService.MensajeConvenioConTurnos, r.Mensaje);
        }
    }
}
