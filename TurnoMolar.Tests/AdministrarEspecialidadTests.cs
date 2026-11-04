using Application.Services;
using Microsoft.EntityFrameworkCore;
using TurnoMolar.Tests.Infraestructura;

namespace TurnoMolar.Tests
{
    // CUF12 - Administrar Especialidad
    public class AdministrarEspecialidadTests
    {
        [Fact]
        public async Task Camino_basico_registra_la_especialidad()
        {
            using var bd = new BaseDeDatosDePrueba();
            var r = await new AdministrarEspecialidadService(bd.NuevoContexto()).RegistrarAsync("Periodoncia", 30000m);

            Assert.True(r.Exito);
            Assert.Equal(AdministrarEspecialidadService.MensajeConfirmacionAlta, r.Mensaje);
            using var ctx = bd.NuevoContexto();
            Assert.Equal(30000m, (await ctx.Especialidades.SingleAsync(e => e.Nombre == "Periodoncia")).ArancelParticular);
        }

        [Fact]
        public async Task Alt_1a_rechaza_nombre_repetido_sin_distinguir_mayusculas()
        {
            using var bd = new BaseDeDatosDePrueba();
            var r = await new AdministrarEspecialidadService(bd.NuevoContexto()).RegistrarAsync("  endodoncia ", 30000m);

            Assert.False(r.Exito);
            Assert.Equal(AdministrarEspecialidadService.MensajeSobreNombre, r.Mensaje);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        [InlineData(-10)]
        public async Task Alt_1b_rechaza_arancel_no_positivo(int? arancel)
        {
            using var bd = new BaseDeDatosDePrueba();
            var r = await new AdministrarEspecialidadService(bd.NuevoContexto()).RegistrarAsync("Periodoncia", arancel.HasValue ? arancel.Value : null);

            Assert.False(r.Exito);
            Assert.Equal(AdministrarEspecialidadService.MensajeSobreArancel, r.Mensaje);
        }

        [Fact]
        public async Task Alt_1c_modifica_el_arancel()
        {
            using var bd = new BaseDeDatosDePrueba();
            var r = await new AdministrarEspecialidadService(bd.NuevoContexto()).ModificarArancelAsync(Datos.General, 17000m);

            Assert.True(r.Exito);
            Assert.Equal(AdministrarEspecialidadService.MensajeConfirmacionUpdate, r.Mensaje);
            using var ctx = bd.NuevoContexto();
            Assert.Equal(17000m, (await ctx.Especialidades.FindAsync(Datos.General))!.ArancelParticular);
        }

        [Fact]
        public async Task Alt_1c2a_rechaza_arancel_invalido_al_modificar()
        {
            using var bd = new BaseDeDatosDePrueba();
            var r = await new AdministrarEspecialidadService(bd.NuevoContexto()).ModificarArancelAsync(Datos.General, 0m);
            Assert.False(r.Exito);
            Assert.Equal(AdministrarEspecialidadService.MensajeSobreArancel, r.Mensaje);
        }

        // ===== E4.R02: alt 1.d (deshabilitar), 1.e (rehabilitar), 1.f (eliminar) =====

        [Fact]
        public async Task Alt_1d_deshabilita_y_deja_de_ofrecerse()
        {
            using var bd = new BaseDeDatosDePrueba();
            var servicio = new AdministrarEspecialidadService(bd.NuevoContexto());
            var r = await servicio.DeshabilitarAsync(Datos.Ortodoncia);

            Assert.True(r.Exito);
            Assert.Equal(AdministrarEspecialidadService.MensajeConfirmacionDeshabilitacion, r.Mensaje);
            Assert.DoesNotContain(await servicio.ListarHabilitadasAsync(), e => e.IdEspecialidad == Datos.Ortodoncia);
            Assert.False((await servicio.ListarAsync()).Single(e => e.IdEspecialidad == Datos.Ortodoncia).Habilitada);

            // CUU01: no se ofrecen días para agendar ni se puede reservar.
            using var ctx = bd.NuevoContexto();
            Assert.Empty(await Servicios.Agenda(ctx).ObtenerDiasDisponiblesAsync(Datos.Ortodoncia));
        }

        [Fact]
        public async Task Alt_1d2a_no_deshabilita_con_turnos_pendientes()
        {
            using var bd = new BaseDeDatosDePrueba();
            using (var ctx = bd.NuevoContexto())
                Datos.AgregarTurno(ctx, Datos.Proximo(DayOfWeek.Monday, 9), Datos.PacienteParticular, Datos.OdontologoA, Datos.General);

            var r = await new AdministrarEspecialidadService(bd.NuevoContexto()).DeshabilitarAsync(Datos.General);

            Assert.False(r.Exito);
            Assert.Equal("1.d.2.a.1", r.Codigo);
            Assert.StartsWith(AdministrarEspecialidadService.MensajeTurnosPendientes, r.Mensaje);
            using var ctx2 = bd.NuevoContexto();
            Assert.True((await ctx2.Especialidades.FindAsync(Datos.General))!.EstaHabilitada);
        }

        [Fact]
        public async Task Alt_1d_especialidad_deshabilitada_no_se_ofrece_para_convenios_nuevos()
        {
            using var bd = new BaseDeDatosDePrueba();
            await new AdministrarEspecialidadService(bd.NuevoContexto()).DeshabilitarAsync(Datos.Ortodoncia);

            var nueva = new DTOs.NuevaObraSocialDTO
            {
                IdentificadorOS = "PAMI", NombreOS = "PAMI", PlanCobertura = "Jubilados",
                Convenios = { new DTOs.ConvenioDTO { IdEspecialidad = Datos.Ortodoncia, ArancelConvenio = 1000m } }
            };
            var r = await new AdministrarConveniosService(bd.NuevoContexto()).RegistrarAsync(nueva);
            Assert.False(r.Exito);
            Assert.Equal(AdministrarEspecialidadService.MensajeEspecialidadDeshabilitada, r.Mensaje);

            // El arancel de un convenio que ya existía puede seguir ajustándose.
            var ajuste = await new AdministrarConveniosService(bd.NuevoContexto()).GuardarConvenioAsync("SWISS", Datos.Ortodoncia, 1000m);
            Assert.True(ajuste.Exito);
        }

        [Fact]
        public async Task Alt_1e_rehabilita()
        {
            using var bd = new BaseDeDatosDePrueba();
            var servicio = new AdministrarEspecialidadService(bd.NuevoContexto());
            await servicio.DeshabilitarAsync(Datos.Ortodoncia);
            var r = await servicio.RehabilitarAsync(Datos.Ortodoncia);

            Assert.True(r.Exito);
            Assert.Equal(AdministrarEspecialidadService.MensajeConfirmacionRehabilitacion, r.Mensaje);
            Assert.Contains(await servicio.ListarHabilitadasAsync(), e => e.IdEspecialidad == Datos.Ortodoncia);
        }

        [Fact]
        public async Task Alt_1f_elimina_una_especialidad_sin_uso()
        {
            using var bd = new BaseDeDatosDePrueba();
            var servicio = new AdministrarEspecialidadService(bd.NuevoContexto());
            await servicio.RegistrarAsync("Periodoncia", 30000m);
            int id;
            using (var ctx = bd.NuevoContexto())
                id = (await ctx.Especialidades.SingleAsync(e => e.Nombre == "Periodoncia")).IdEspecialidad;

            var r = await new AdministrarEspecialidadService(bd.NuevoContexto()).EliminarAsync(id);

            Assert.True(r.Exito);
            Assert.Equal(AdministrarEspecialidadService.MensajeConfirmacionEliminacion, r.Mensaje);
            using var ctx2 = bd.NuevoContexto();
            Assert.False(await ctx2.Especialidades.AnyAsync(e => e.IdEspecialidad == id));
        }

        [Fact]
        public async Task Alt_1f2a_no_elimina_una_especialidad_con_historial()
        {
            using var bd = new BaseDeDatosDePrueba();
            var r = await new AdministrarEspecialidadService(bd.NuevoContexto()).EliminarAsync(Datos.General);

            Assert.False(r.Exito);
            Assert.Equal("1.f.2.a.1", r.Codigo);
            Assert.Equal(AdministrarEspecialidadService.MensajeEspecialidadConHistorial, r.Mensaje);
        }
    }
}
