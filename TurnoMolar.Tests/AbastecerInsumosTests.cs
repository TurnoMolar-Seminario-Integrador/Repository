using Application.Services;
using Microsoft.EntityFrameworkCore;
using TurnoMolar.Tests.Infraestructura;

namespace TurnoMolar.Tests
{
    // CUF09 - Abastecer Insumos y Materiales
    public class AbastecerInsumosTests
    {
        [Fact]
        public async Task Paso_1_lista_los_insumos()
        {
            using var bd = new BaseDeDatosDePrueba();
            var lista = await new AbastecerInsumosService(bd.NuevoContexto()).ListarAsync();
            Assert.Equal(7, lista.Count);
        }

        [Fact]
        public async Task Camino_basico_registra_el_insumo()
        {
            using var bd = new BaseDeDatosDePrueba();
            var r = await new AbastecerInsumosService(bd.NuevoContexto()).RegistrarAsync("Hilo dental", 900m, 40);
            Assert.True(r.Exito);
            using var ctx = bd.NuevoContexto();
            Assert.Equal(40, (await ctx.Insumos.SingleAsync(i => i.Nombre == "Hilo dental")).StockDisponible);
        }

        [Fact]
        public async Task Alt_2a_rechaza_nombre_repetido()
        {
            using var bd = new BaseDeDatosDePrueba();
            var r = await new AbastecerInsumosService(bd.NuevoContexto()).RegistrarAsync("resina compuesta fotocurable", 900m, 40);
            Assert.False(r.Exito);
            Assert.Equal(AbastecerInsumosService.MensajeInsumoYaRegistrado, r.Mensaje);
        }

        [Fact]
        public async Task Alt_3a_repone_stock_e_informa_el_total()
        {
            using var bd = new BaseDeDatosDePrueba();
            int id;
            using (var ctx = bd.NuevoContexto())
                id = (await ctx.Insumos.SingleAsync(i => i.Nombre == "Conos de Gutapercha Endodoncia")).IdInsumo;

            var r = await new AbastecerInsumosService(bd.NuevoContexto()).ReponerStockAsync(id, 20);

            Assert.True(r.Exito);
            Assert.Contains("50", r.Mensaje);
            using var ctx2 = bd.NuevoContexto();
            Assert.Equal(50, (await ctx2.Insumos.FindAsync(id))!.StockDisponible);
        }

        [Fact]
        public async Task Alt_3a_rechaza_cantidad_no_positiva()
        {
            using var bd = new BaseDeDatosDePrueba();
            var r = await new AbastecerInsumosService(bd.NuevoContexto()).ReponerStockAsync(1, 0);
            Assert.False(r.Exito);
        }

        [Fact]
        public async Task Alt_3b_modifica_el_costo()
        {
            using var bd = new BaseDeDatosDePrueba();
            var r = await new AbastecerInsumosService(bd.NuevoContexto()).ModificarCostoAsync(1, 3000m);
            Assert.True(r.Exito);
            Assert.Equal(AbastecerInsumosService.MensajeAjusteCostoExitoso, r.Mensaje);
            using var ctx = bd.NuevoContexto();
            Assert.Equal(3000m, (await ctx.Insumos.FindAsync(1))!.CostoUnitario);
        }
    }
}
