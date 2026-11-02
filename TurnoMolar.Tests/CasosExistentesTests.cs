using Application.Services;
using Domain.Model;
using DTOs;
using Microsoft.EntityFrameworkCore;
using TurnoMolar.Tests.Infraestructura;

namespace TurnoMolar.Tests
{
    // Revisión de casos ya implementados: CUF07, CUF08, CUF10 y CUU03 (rechazo de pago parcial).
    public class CasosExistentesTests
    {
        // ----- CUF07 - Cancelar Turno -----

        [Fact]
        public async Task CUF07_cancela_en_termino_sin_penalizacion()
        {
            using var bd = new BaseDeDatosDePrueba();
            int nro;
            using (var ctx = bd.NuevoContexto())
                nro = Datos.AgregarTurno(ctx, Datos.Proximo(DayOfWeek.Monday, 9), Datos.PacienteParticular, Datos.OdontologoA, Datos.General).NroTurno;

            var r = await Servicios.GestionTurno(bd.NuevoContexto()).CancelarTurnoAsync(nro, "DNI", Datos.PacienteParticular, "Viaje");

            Assert.Equal(ResultadoCancelarTurno.Cancelado, r.Resultado);
            using var ctx2 = bd.NuevoContexto();
            Assert.Equal("CANCELADO", (await ctx2.Turnos.FindAsync(nro))!.EstadoTurno);
            Assert.Equal("HABILITADO", (await ctx2.Pacientes.SingleAsync(p => p.NroDocumento == Datos.PacienteParticular)).EstadoPaciente);
        }

        [Fact]
        public async Task CUF07_alt_3a_fuera_de_termino_pide_confirmacion_y_luego_penaliza()
        {
            using var bd = new BaseDeDatosDePrueba();
            int nro;
            using (var ctx = bd.NuevoContexto())
                nro = Datos.AgregarTurno(ctx, DateTime.Now.AddHours(3), Datos.PacienteParticular, Datos.OdontologoA, Datos.General).NroTurno;

            var aviso = await Servicios.GestionTurno(bd.NuevoContexto()).CancelarTurnoAsync(nro, "DNI", Datos.PacienteParticular, null);
            Assert.Equal(ResultadoCancelarTurno.RequiereConfirmacion, aviso.Resultado);
            using (var ctx = bd.NuevoContexto())
                Assert.Equal("RESERVADO", (await ctx.Turnos.FindAsync(nro))!.EstadoTurno);

            var r = await Servicios.GestionTurno(bd.NuevoContexto()).CancelarTurnoAsync(nro, "DNI", Datos.PacienteParticular, null, confirmaFueraDeTermino: true);
            Assert.Equal(ResultadoCancelarTurno.Cancelado, r.Resultado);
            using var ctx2 = bd.NuevoContexto();
            var paciente = await ctx2.Pacientes.SingleAsync(p => p.NroDocumento == Datos.PacienteParticular);
            Assert.Equal("INHABILITADO", paciente.EstadoPaciente);
            Assert.Equal(15000m, paciente.MontoAdeudado);
        }

        // ----- CUF08 - Reprogramar Turno -----

        [Fact]
        public async Task CUF08_reprograma_a_un_horario_disponible()
        {
            using var bd = new BaseDeDatosDePrueba();
            int nro;
            using (var ctx = bd.NuevoContexto())
                nro = Datos.AgregarTurno(ctx, Datos.Proximo(DayOfWeek.Monday, 9), Datos.PacienteParticular, Datos.OdontologoA, Datos.General).NroTurno;

            var r = await Servicios.GestionTurno(bd.NuevoContexto())
                .ReprogramarTurnoAsync(nro, "DNI", Datos.PacienteParticular, Datos.Proximo(DayOfWeek.Tuesday, 10));

            Assert.Equal(ResultadoReprogramarTurno.Reprogramado, r.Resultado);
        }

        [Fact]
        public async Task CUF08_rechaza_un_horario_fuera_de_la_disponibilidad()
        {
            using var bd = new BaseDeDatosDePrueba();
            int nro;
            using (var ctx = bd.NuevoContexto())
                nro = Datos.AgregarTurno(ctx, Datos.Proximo(DayOfWeek.Monday, 9), Datos.PacienteParticular, Datos.OdontologoA, Datos.General).NroTurno;

            var r = await Servicios.GestionTurno(bd.NuevoContexto())
                .ReprogramarTurnoAsync(nro, "DNI", Datos.PacienteParticular, Datos.Proximo(DayOfWeek.Tuesday, 18));

            Assert.Equal(ResultadoReprogramarTurno.HorarioNoDisponible, r.Resultado);
        }

        // ----- CUF10 - Regularizar Deuda -----

        [Fact]
        public async Task CUF10_pagar_la_deuda_rehabilita_al_paciente()
        {
            using var bd = new BaseDeDatosDePrueba();
            using (var ctx = bd.NuevoContexto())
                Datos.AgregarTurno(ctx, DateTime.Today.AddDays(-1).AddHours(9), Datos.PacienteParticular, Datos.OdontologoA, Datos.General);
            await new VencimientoTurnosService(bd.NuevoContexto()).ProcesarTurnosVencidosAsync(DateTime.Now);

            var deuda = await Servicios.RegularizarDeuda(bd.NuevoContexto()).ConsultarDeudaAsync("DNI", Datos.PacienteParticular);
            Assert.Equal(15000m, deuda.MontoAdeudado);

            var r = await Servicios.RegularizarDeuda(bd.NuevoContexto()).PagarDeudaAsync("DNI", Datos.PacienteParticular, MetodosPago.Transferencia);

            Assert.Equal(ResultadoPagarDeuda.Regularizada, r.Resultado);
            using var ctx2 = bd.NuevoContexto();
            var paciente = await ctx2.Pacientes.SingleAsync(p => p.NroDocumento == Datos.PacienteParticular);
            Assert.Equal("HABILITADO", paciente.EstadoPaciente);
            Assert.True((paciente.MontoAdeudado ?? 0m) == 0m);
        }

        [Fact]
        public async Task CUF10_alt_1a_sin_deuda()
        {
            using var bd = new BaseDeDatosDePrueba();
            var r = await Servicios.RegularizarDeuda(bd.NuevoContexto()).PagarDeudaAsync("DNI", Datos.PacienteParticular, MetodosPago.Efectivo);
            Assert.Equal(ResultadoPagarDeuda.SinDeuda, r.Resultado);
        }

        // ----- CUU03 - Finalizar Atención: el pago parcial se rechaza -----

        private static int TurnoConAtencionRegistrada(BaseDeDatosDePrueba bd, string paciente, string modalidad)
        {
            using var ctx = bd.NuevoContexto();
            var inicio = DateTime.Today.AddHours(9);
            var turno = Datos.AgregarTurno(ctx, inicio, paciente, Datos.OdontologoA, Datos.General, modalidad, "ATENCION_REGISTRADA");
            var nroHC = ctx.HistoriasClinicas.Single(h => h.NroDocumentoPaciente == paciente).NroHC;
            var arancel = modalidad == "OBRA_SOCIAL" ? 18000m : 15000m;
            ctx.AtencionesOdontologicas.Add(new AtencionOdontologica(0, inicio, inicio.AddMinutes(30), "Control", arancel, turno.NroTurno, nroHC));
            ctx.SaveChanges();
            return turno.NroTurno;
        }

        [Fact]
        public async Task CUU03_rechaza_pago_particular_parcial()
        {
            using var bd = new BaseDeDatosDePrueba();
            var nro = TurnoConAtencionRegistrada(bd, Datos.PacienteParticular, "PARTICULAR");

            var r = await Servicios.FinalizarAtencion(bd.NuevoContexto()).RegistrarCobroParticularAsync(nro, 10000m, MetodosPago.Efectivo);

            Assert.Equal(ResultadoRegistrarCobro.MontoNoCoincide, r.Resultado);
            using var ctx = bd.NuevoContexto();
            Assert.Equal("ATENCION_REGISTRADA", (await ctx.Turnos.FindAsync(nro))!.EstadoTurno);
            Assert.False(await ctx.Pagos.AnyAsync());
        }

        [Fact]
        public async Task CUU03_acepta_pago_particular_total()
        {
            using var bd = new BaseDeDatosDePrueba();
            var nro = TurnoConAtencionRegistrada(bd, Datos.PacienteParticular, "PARTICULAR");

            var r = await Servicios.FinalizarAtencion(bd.NuevoContexto()).RegistrarCobroParticularAsync(nro, 15000m, MetodosPago.Efectivo);

            Assert.Equal(ResultadoRegistrarCobro.Finalizado, r.Resultado);
        }

        [Fact]
        public async Task CUU03_rechaza_aportes_de_obra_social_que_no_suman_el_total()
        {
            using var bd = new BaseDeDatosDePrueba();
            var nro = TurnoConAtencionRegistrada(bd, Datos.PacienteOsde, "OBRA_SOCIAL");

            var r = await Servicios.FinalizarAtencion(bd.NuevoContexto()).RegistrarCobroObraSocialAsync(nro, MetodosPago.Efectivo, 2000m, 10000m);

            Assert.NotEqual(ResultadoRegistrarCobro.Finalizado, r.Resultado);
            using var ctx = bd.NuevoContexto();
            Assert.False(await ctx.Pagos.AnyAsync());
        }
    }
}
