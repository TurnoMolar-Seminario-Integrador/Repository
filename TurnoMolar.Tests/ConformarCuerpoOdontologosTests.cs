using Application.Services;
using Domain.Model;
using DTOs;
using Microsoft.EntityFrameworkCore;
using TurnoMolar.Tests.Infraestructura;

namespace TurnoMolar.Tests
{
    // CUF04 - Conformar Cuerpo de Odontólogos
    public class ConformarCuerpoOdontologosTests
    {
        private static FranjaHorariaDTO Franja(string dia, int desde, int hasta) =>
            new() { DiaSemana = dia, HoraInicio = new TimeOnly(desde, 0), HoraFin = new TimeOnly(hasta, 0) };

        private static NuevoOdontologoDTO Nuevo(string matricula = "MP 9999", string documento = "40111222") => new()
        {
            TipoDocumento = "DNI",
            NroDocumento = documento,
            Nombre = "Ana",
            Apellido = "Paz",
            FechaNacimiento = new DateTime(1990, 1, 1),
            Telefono = "341-000",
            Email = "ana@clinica.com",
            Domicilio = "Mitre 100",
            Matricula = matricula,
            Especialidades =
            {
                new EspecialidadDisponibilidadDTO { IdEspecialidad = Datos.General, Franjas = { Franja("Lunes", 8, 12), Franja("Miercoles", 8, 12) } },
                new EspecialidadDisponibilidadDTO { IdEspecialidad = Datos.Endodoncia, Franjas = { Franja("Lunes", 13, 17) } }
            }
        };

        [Fact]
        public async Task Camino_basico_registra_al_odontologo_habilitado_con_su_disponibilidad()
        {
            using var bd = new BaseDeDatosDePrueba();
            var r = await new ConformarCuerpoOdontologosService(bd.NuevoContexto()).RegistrarAsync(Nuevo());

            Assert.True(r.Exito, r.Mensaje);
            using var ctx = bd.NuevoContexto();
            var o = await ctx.Odontologos.Include(x => x.DisponibilidadesHorarias).SingleAsync(x => x.NroDocumento == "40111222");
            Assert.Equal(EstadosOdontologo.Habilitado, o.EstadoOdontologo);
            Assert.Equal(3, o.DisponibilidadesHorarias.Count);
            Assert.Contains(o.DisponibilidadesHorarias, d => d.DiaSemana == "Miércoles"); // día normalizado
            Assert.True(PasswordHasher.Validar("40111222", o.Clave, o.SaltClave));
        }

        [Fact]
        public async Task El_alta_aparece_en_el_listado()
        {
            using var bd = new BaseDeDatosDePrueba();
            await new ConformarCuerpoOdontologosService(bd.NuevoContexto()).RegistrarAsync(Nuevo());
            var lista = await new ConformarCuerpoOdontologosService(bd.NuevoContexto()).ListarAsync();
            var ana = lista.Single(o => o.Matricula == "MP 9999");
            Assert.Equal("Habilitado", ana.EstadoOdontologo);
            Assert.Equal(2, ana.Especialidades.Count);
        }

        [Fact]
        public async Task Alt_3a_rechaza_matricula_repetida()
        {
            using var bd = new BaseDeDatosDePrueba();
            var r = await new ConformarCuerpoOdontologosService(bd.NuevoContexto()).RegistrarAsync(Nuevo("MP 3840"));
            Assert.False(r.Exito);
            Assert.Equal(ConformarCuerpoOdontologosService.MensajeMatriculaExistente, r.Mensaje);
        }

        [Fact]
        public async Task Rechaza_dias_fuera_de_lunes_a_viernes()
        {
            using var bd = new BaseDeDatosDePrueba();
            var datos = Nuevo();
            datos.Especialidades[0].Franjas.Add(Franja("Sábado", 9, 12));
            var r = await new ConformarCuerpoOdontologosService(bd.NuevoContexto()).RegistrarAsync(datos);
            Assert.Equal(ConformarCuerpoOdontologosService.MensajeDiaInvalido, r.Mensaje);
        }

        [Fact]
        public async Task Rechaza_franjas_superpuestas_entre_especialidades()
        {
            using var bd = new BaseDeDatosDePrueba();
            var datos = Nuevo();
            datos.Especialidades[1].Franjas.Add(Franja("Lunes", 11, 14));
            var r = await new ConformarCuerpoOdontologosService(bd.NuevoContexto()).RegistrarAsync(datos);
            Assert.Equal(ConformarCuerpoOdontologosService.MensajeSuperposicion, r.Mensaje);
        }

        [Fact]
        public async Task Alt_5a_ajusta_la_disponibilidad()
        {
            using var bd = new BaseDeDatosDePrueba();
            var r = await new ConformarCuerpoOdontologosService(bd.NuevoContexto())
                .AjustarDisponibilidadAsync("DNI", Datos.OdontologoB, Datos.Endodoncia, new[] { Franja("Martes", 9, 13) });

            Assert.Equal(ConformarCuerpoOdontologosService.MensajeConfirmacionDisponibilidad, r.Mensaje);
            using var ctx = bd.NuevoContexto();
            var franjas = await ctx.DisponibilidadesHorarias.Where(d => d.NroDocumentoOdontologo == Datos.OdontologoB).ToListAsync();
            Assert.Equal("Martes", Assert.Single(franjas).DiaSemana);
        }

        [Fact]
        public async Task Alt_5a_rechaza_si_un_turno_reservado_queda_fuera_de_la_franja()
        {
            using var bd = new BaseDeDatosDePrueba();
            using (var ctx = bd.NuevoContexto())
                Datos.AgregarTurno(ctx, Datos.Proximo(DayOfWeek.Wednesday, 15), Datos.PacienteParticular, Datos.OdontologoB, Datos.Endodoncia);

            var r = await new ConformarCuerpoOdontologosService(bd.NuevoContexto())
                .AjustarDisponibilidadAsync("DNI", Datos.OdontologoB, Datos.Endodoncia, new[] { Franja("Miércoles", 16, 19) });

            Assert.False(r.Exito);
            Assert.Equal(ConformarCuerpoOdontologosService.MensajeTurnosFueraDeFranja, r.Mensaje);
        }

        [Fact]
        public async Task Alt_5b_agrega_y_quita_especialidad()
        {
            using var bd = new BaseDeDatosDePrueba();
            var agregar = await new ConformarCuerpoOdontologosService(bd.NuevoContexto())
                .AgregarEspecialidadAsync("DNI", Datos.OdontologoC, Datos.General, new[] { Franja("Lunes", 9, 12) });
            Assert.Equal(ConformarCuerpoOdontologosService.MensajeConfirmacionEspecialidad, agregar.Mensaje);

            var quitar = await new ConformarCuerpoOdontologosService(bd.NuevoContexto())
                .QuitarEspecialidadAsync("DNI", Datos.OdontologoC, Datos.General);
            Assert.True(quitar.Exito);
        }

        [Fact]
        public async Task Alt_5b_no_quita_una_especialidad_con_turnos_pendientes()
        {
            using var bd = new BaseDeDatosDePrueba();
            using (var ctx = bd.NuevoContexto())
                Datos.AgregarTurno(ctx, Datos.Proximo(DayOfWeek.Friday, 10), Datos.PacienteParticular, Datos.OdontologoA, Datos.Ortodoncia);

            var r = await new ConformarCuerpoOdontologosService(bd.NuevoContexto())
                .QuitarEspecialidadAsync("DNI", Datos.OdontologoA, Datos.Ortodoncia);
            Assert.Equal(ConformarCuerpoOdontologosService.MensajeTurnosPendientesEspecialidad, r.Mensaje);
        }

        [Fact]
        public async Task Alt_5b_no_quita_la_ultima_especialidad()
        {
            using var bd = new BaseDeDatosDePrueba();
            var r = await new ConformarCuerpoOdontologosService(bd.NuevoContexto())
                .QuitarEspecialidadAsync("DNI", Datos.OdontologoC, Datos.Ortodoncia);
            Assert.Equal(ConformarCuerpoOdontologosService.MensajeUltimaEspecialidad, r.Mensaje);
        }

        [Fact]
        public async Task Alt_5c_actualiza_el_contacto()
        {
            using var bd = new BaseDeDatosDePrueba();
            var r = await new ConformarCuerpoOdontologosService(bd.NuevoContexto())
                .ActualizarContactoAsync("DNI", Datos.OdontologoA, "341-999", null, null);
            Assert.Equal(ConformarCuerpoOdontologosService.MensajeConfirmacionContacto, r.Mensaje);
            using var ctx = bd.NuevoContexto();
            var o = await ctx.Odontologos.SingleAsync(x => x.NroDocumento == Datos.OdontologoA);
            Assert.Equal("341-999", o.Telefono);
            Assert.False(string.IsNullOrEmpty(o.Email)); // lo no informado se conserva
        }

        [Fact]
        public async Task Alt_5d_inhabilitado_sale_de_la_oferta_de_turnos_y_5e_lo_devuelve()
        {
            using var bd = new BaseDeDatosDePrueba();
            var lunes = Datos.Proximo(DayOfWeek.Monday, 0);

            using (var ctx = bd.NuevoContexto())
                Assert.NotEmpty(await Servicios.Agenda(ctx).ObtenerHorariosDisponiblesAsync(Datos.General, lunes));

            Assert.True((await new ConformarCuerpoOdontologosService(bd.NuevoContexto()).InhabilitarAsync("DNI", Datos.OdontologoA)).Exito);
            using (var ctx = bd.NuevoContexto())
                Assert.Empty(await Servicios.Agenda(ctx).ObtenerHorariosDisponiblesAsync(Datos.General, lunes));

            Assert.True((await new ConformarCuerpoOdontologosService(bd.NuevoContexto()).RehabilitarAsync("DNI", Datos.OdontologoA)).Exito);
            using (var ctx = bd.NuevoContexto())
                Assert.NotEmpty(await Servicios.Agenda(ctx).ObtenerHorariosDisponiblesAsync(Datos.General, lunes));
        }

        [Fact]
        public async Task Alt_5d_conserva_los_turnos_e_informa_los_pendientes()
        {
            using var bd = new BaseDeDatosDePrueba();
            using (var ctx = bd.NuevoContexto())
                Datos.AgregarTurno(ctx, Datos.Proximo(DayOfWeek.Monday, 9), Datos.PacienteParticular, Datos.OdontologoA, Datos.General);

            var r = await new ConformarCuerpoOdontologosService(bd.NuevoContexto()).InhabilitarAsync("DNI", Datos.OdontologoA);

            Assert.Contains("1 turno(s) pendiente(s)", r.Mensaje);
            using var ctx2 = bd.NuevoContexto();
            Assert.Equal("RESERVADO", (await ctx2.Turnos.SingleAsync()).EstadoTurno);
        }
    }
}
