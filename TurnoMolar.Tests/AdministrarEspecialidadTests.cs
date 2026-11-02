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
    }
}
