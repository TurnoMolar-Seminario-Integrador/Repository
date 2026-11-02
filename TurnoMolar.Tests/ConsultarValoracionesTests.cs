using Application.Services;
using Domain.Model;
using Microsoft.EntityFrameworkCore;
using TurnoMolar.Tests.Infraestructura;

namespace TurnoMolar.Tests
{
    // CUF05 - Consultar Valoraciones
    public class ConsultarValoracionesTests
    {
        private static int AgregarValoracion(BaseDeDatosDePrueba bd, string odontologo, int especialidad, int calificacion)
        {
            using var ctx = bd.NuevoContexto();
            var inicio = DateTime.Today.AddDays(-7).AddHours(9);
            var turno = Datos.AgregarTurno(ctx, inicio, Datos.PacienteParticular, odontologo, especialidad, estado: "FINALIZADO");
            var nroHC = ctx.HistoriasClinicas.Single(h => h.NroDocumentoPaciente == Datos.PacienteParticular).NroHC;
            var atencion = new AtencionOdontologica(0, inicio, inicio.AddMinutes(30), "Control", 15000m, turno.NroTurno, nroHC);
            ctx.AtencionesOdontologicas.Add(atencion);
            ctx.SaveChanges();
            ctx.Valoraciones.Add(new Valoracion(0, calificacion, "Muy buena atención", atencion.IdAtencion));
            ctx.SaveChanges();
            return turno.NroTurno;
        }

        [Fact]
        public async Task Camino_basico_el_odontologo_ve_solo_sus_valoraciones()
        {
            using var bd = new BaseDeDatosDePrueba();
            var nroTurno = AgregarValoracion(bd, Datos.OdontologoA, Datos.General, 5);
            AgregarValoracion(bd, Datos.OdontologoB, Datos.Endodoncia, 3);

            var r = await new ConsultarValoracionesService(bd.NuevoContexto()).ConsultarPorOdontologoAsync("DNI", Datos.OdontologoA);

            var v = Assert.Single(r.Valoraciones);
            Assert.Null(r.Mensaje);
            Assert.Equal(nroTurno, v.NroTurno);
            Assert.Equal(5, v.Calificacion);
            Assert.Equal("Rossi", v.ApellidoPaciente);
            Assert.Equal("Odontología General", v.NombreEspecialidad);
        }

        [Fact]
        public async Task Paso_2_muestra_el_detalle_y_no_deja_ver_el_de_otro_odontologo()
        {
            using var bd = new BaseDeDatosDePrueba();
            var nroTurno = AgregarValoracion(bd, Datos.OdontologoA, Datos.General, 4);
            var servicio = new ConsultarValoracionesService(bd.NuevoContexto());

            Assert.NotNull(await servicio.ObtenerDetalleAsync(nroTurno, "DNI", Datos.OdontologoA));
            Assert.Null(await servicio.ObtenerDetalleAsync(nroTurno, "DNI", Datos.OdontologoB));
        }

        [Fact]
        public async Task Alt_1a_informa_que_no_hay_valoraciones()
        {
            using var bd = new BaseDeDatosDePrueba();
            var r = await new ConsultarValoracionesService(bd.NuevoContexto()).ConsultarPorOdontologoAsync("DNI", Datos.OdontologoC);
            Assert.Empty(r.Valoraciones);
            Assert.Equal(ConsultarValoracionesService.MensajeSinValoraciones, r.Mensaje);
        }

        [Fact]
        public async Task Alt_1b_el_responsable_ve_todas_con_el_odontologo()
        {
            using var bd = new BaseDeDatosDePrueba();
            AgregarValoracion(bd, Datos.OdontologoA, Datos.General, 5);
            AgregarValoracion(bd, Datos.OdontologoB, Datos.Endodoncia, 3);

            var r = await new ConsultarValoracionesService(bd.NuevoContexto()).ConsultarClinicaAsync();

            Assert.Equal(2, r.Valoraciones.Count);
            Assert.All(r.Valoraciones, v => Assert.False(string.IsNullOrEmpty(v.ApellidoOdontologo)));
        }
    }
}
