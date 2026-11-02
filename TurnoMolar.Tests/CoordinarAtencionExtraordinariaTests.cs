using Application.Services;
using DTOs;
using Microsoft.EntityFrameworkCore;
using TurnoMolar.Tests.Infraestructura;

namespace TurnoMolar.Tests
{
    // CUF11 - Coordinar Atención Extraordinaria
    public class CoordinarAtencionExtraordinariaTests
    {
        private static AtencionExtraordinariaRequestDTO Solicitud(DateTime fecha, string paciente = Datos.PacienteParticular, string modalidad = "PARTICULAR") => new()
        {
            TipoDocumentoPaciente = "DNI",
            NroDocumentoPaciente = paciente,
            IdEspecialidad = Datos.General,
            OdontologoTipoDocumento = "DNI",
            OdontologoNroDocumento = Datos.OdontologoA,
            FechaHoraTurno = fecha,
            DescripcionMaterial = "Traer placa radiográfica",
            ModalidadPago = modalidad
        };

        [Fact]
        public async Task Camino_basico_registra_el_turno_fuera_de_la_grilla_y_emite_comprobante()
        {
            using var bd = new BaseDeDatosDePrueba();
            // Sábado a las 20: fuera de la disponibilidad estándar del odontólogo.
            var fecha = Datos.Proximo(DayOfWeek.Saturday, 20);

            var r = await new CoordinarAtencionExtraordinariaService(bd.NuevoContexto()).RegistrarAsync(Solicitud(fecha));

            Assert.True(r.Exito, r.Mensaje);
            Assert.NotNull(r.Comprobante);
            Assert.Equal("Traer placa radiográfica", r.DescripcionMaterial);
            using var ctx = bd.NuevoContexto();
            var turno = await ctx.Turnos.SingleAsync();
            Assert.Equal("RESERVADO", turno.EstadoTurno);
            Assert.Equal("Traer placa radiográfica", turno.DescripcionMaterial);
            Assert.True(await ctx.ComprobantesTurnos.AnyAsync(c => c.NroTurno == turno.NroTurno));
        }

        [Fact]
        public async Task No_aplica_la_antelacion_minima_RN19()
        {
            using var bd = new BaseDeDatosDePrueba();
            var r = await new CoordinarAtencionExtraordinariaService(bd.NuevoContexto())
                .RegistrarAsync(Solicitud(DateTime.Now.AddHours(2)));
            Assert.True(r.Exito, r.Mensaje);
        }

        [Fact]
        public async Task Alt_2a_rechaza_paciente_con_deuda()
        {
            using var bd = new BaseDeDatosDePrueba();
            using (var ctx = bd.NuevoContexto())
            {
                var p = await ctx.Pacientes.SingleAsync(x => x.NroDocumento == Datos.PacienteParticular);
                p.SetMontoAdeudado(15000m);
                p.SetEstadoPaciente("INHABILITADO");
                await ctx.SaveChangesAsync();
            }
            var r = await new CoordinarAtencionExtraordinariaService(bd.NuevoContexto()).ValidarPacienteAsync("DNI", Datos.PacienteParticular);
            Assert.Equal(CoordinarAtencionExtraordinariaService.MensajeDeudaPendiente, r.Mensaje);
        }

        [Fact]
        public async Task Alt_2b_rechaza_paciente_con_turno_pendiente()
        {
            using var bd = new BaseDeDatosDePrueba();
            using (var ctx = bd.NuevoContexto())
                Datos.AgregarTurno(ctx, Datos.Proximo(DayOfWeek.Monday, 9), Datos.PacienteParticular, Datos.OdontologoA, Datos.General);

            var r = await new CoordinarAtencionExtraordinariaService(bd.NuevoContexto())
                .RegistrarAsync(Solicitud(Datos.Proximo(DayOfWeek.Saturday, 20)));
            Assert.False(r.Exito);
            Assert.Equal(CoordinarAtencionExtraordinariaService.MensajeTurnoPendiente, r.Mensaje);
        }

        [Fact]
        public async Task Rechaza_odontologo_inhabilitado()
        {
            using var bd = new BaseDeDatosDePrueba();
            await new ConformarCuerpoOdontologosService(bd.NuevoContexto()).InhabilitarAsync("DNI", Datos.OdontologoA);
            var r = await new CoordinarAtencionExtraordinariaService(bd.NuevoContexto())
                .RegistrarAsync(Solicitud(Datos.Proximo(DayOfWeek.Saturday, 20)));
            Assert.Equal(CoordinarAtencionExtraordinariaService.MensajeOdontologoInhabilitado, r.Mensaje);
        }

        [Fact]
        public async Task Rechaza_choque_con_otro_turno_del_odontologo()
        {
            using var bd = new BaseDeDatosDePrueba();
            var fecha = Datos.Proximo(DayOfWeek.Monday, 9);
            using (var ctx = bd.NuevoContexto())
                Datos.AgregarTurno(ctx, fecha, Datos.PacienteSwiss, Datos.OdontologoA, Datos.General);

            var r = await new CoordinarAtencionExtraordinariaService(bd.NuevoContexto()).RegistrarAsync(Solicitud(fecha.AddMinutes(15)));
            Assert.Equal(CoordinarAtencionExtraordinariaService.MensajeChoqueAgenda, r.Mensaje);
        }

        [Fact]
        public async Task Obra_social_sin_convenio_vigente_se_rechaza()
        {
            using var bd = new BaseDeDatosDePrueba();
            await new AdministrarConveniosService(bd.NuevoContexto()).DarDeBajaAsync("OSDE");
            var r = await new CoordinarAtencionExtraordinariaService(bd.NuevoContexto())
                .RegistrarAsync(Solicitud(Datos.Proximo(DayOfWeek.Saturday, 20), Datos.PacienteOsde, "OBRA_SOCIAL"));
            Assert.Equal(CoordinarAtencionExtraordinariaService.MensajeSinConvenio, r.Mensaje);
        }
    }
}
