using Microsoft.EntityFrameworkCore;
using TurnoMolar.Tests.Infraestructura;

namespace TurnoMolar.Tests
{
    public class SeedTests
    {
        [Fact]
        public async Task El_seed_crea_las_tablas_y_datos_basicos()
        {
            using var bd = new BaseDeDatosDePrueba();
            using var ctx = bd.NuevoContexto();
            Assert.True(await ctx.Especialidades.AnyAsync());
            Assert.True(await ctx.Odontologos.AnyAsync());
            Assert.True(await ctx.ObrasSociales.AnyAsync());
        }
    }
}
