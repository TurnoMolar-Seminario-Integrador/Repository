using Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(TurnoMolarDbContext context, ILogger? logger = null)
        {
            try
            {
                await context.Database.EnsureCreatedAsync();

                if (!await context.ObrasSociales.AnyAsync())
                {
                    await context.ObrasSociales.AddRangeAsync(
                        new ObraSocial("1", "OSDE", "Plan 210", "ACTIVA"),
                        new ObraSocial("2", "Swiss Medical", "Black SMG02", "ACTIVA"),
                        new ObraSocial("3", "IAPOS", "Plan General", "ACTIVA"),
                        new ObraSocial("4", "Galeno", "Plan Oro 330", "INACTIVA"));
                    await context.SaveChangesAsync();
                }

                if (!await context.Especialidades.AnyAsync())
                {
                    await InsertWithIdentityAsync(context, "Especialidades",
                        new Especialidad(101, "Odontologia General", 12000m),
                        new Especialidad(102, "Ortodoncia", 25000m),
                        new Especialidad(103, "Endodoncia", 22000m),
                        new Especialidad(104, "Cirugia Maxilofacial", 30000m));
                }

                if (!await context.Insumos.AnyAsync())
                {
                    await InsertWithIdentityAsync(context, "Insumos",
                        new Insumo(501, "Anestesia Cartucho Lidocaina 2%", 1450m, 120),
                        new Insumo(502, "Resina Compuesta Fotopolimerizable (jeringa)", 8500m, 155),
                        new Insumo(503, "Guantes de Latex Descartables (caja)", 250m, 500),
                        new Insumo(504, "Conos de Gutapercha Esterilizados", 3200m, 40));
                }

                if (!await context.Convenios.AnyAsync())
                {
                    await context.Convenios.AddRangeAsync(
                        new Convenio("1", 101, 7000m),
                        new Convenio("1", 102, 18000m),
                        new Convenio("2", 103, 10000m),
                        new Convenio("2", 101, 6600m));
                    await context.SaveChangesAsync();
                }

                if (!await context.ResponsablesClinica.AnyAsync())
                {
                    var (adminHash, adminSalt) = PasswordHasher.Generar("admin123");
                    var (responsableHash, responsableSalt) = PasswordHasher.Generar("resp123");

                    await context.ResponsablesClinica.AddRangeAsync(
                        new ResponsableClinica("DNI", "25412587", "Roberto", "Sanchez",
                            new DateTime(1976, 5, 10), "341-4889977", "rsanchez@clinica.com",
                            "San Luis 1520, Rosario", adminHash, adminSalt,
                            new DateTime(2022, 1, 15, 8, 0, 0), "Admin"),
                        new ResponsableClinica("DNI", "30123456", "Mariana", "Lopez",
                            new DateTime(1982, 11, 20), "341-4778899", "mlopez@clinica.com",
                            "Mendoza 2100, Rosario", responsableHash, responsableSalt,
                            new DateTime(2023, 3, 1, 9, 30, 0), "ResponsableClinica"));
                    await context.SaveChangesAsync();
                }

                if (!await context.Odontologos.AnyAsync())
                {
                    var (martinHash, martinSalt) = PasswordHasher.Generar("doc123");
                    var (valeriaHash, valeriaSalt) = PasswordHasher.Generar("doc123");
                    var (carlosHash, carlosSalt) = PasswordHasher.Generar("doc123");

                    await context.Odontologos.AddRangeAsync(
                        new Odontologo("DNI", "32145874", "MAT-8421", "Martin", "Gómez",
                            new DateTime(1986, 4, 12), "341-5982144", "mgomez@clinica.com",
                            "Bv. Oroño 845, Rosario", "ACTIVO", martinHash, martinSalt,
                            new DateTime(2020, 5, 10, 10, 0, 0), "Odontologo"),
                        new Odontologo("DNI", "35987123", "MAT-9130", "Valeria", "Rossi",
                            new DateTime(1991, 9, 25), "341-4329901", "vrossi@clinica.com",
                            "Cordoba 1820, Rosario", "ACTIVO", valeriaHash, valeriaSalt,
                            new DateTime(2021, 8, 15, 11, 30, 0), "Odontologo"),
                        new Odontologo("DNI", "28456123", "MAT-7112", "Carlos", "Benitez",
                            new DateTime(1980, 11, 3), "341-4112233", "cbenitez@clinica.com",
                            "Pellegrini 1420, Rosario", "LICENCIA", carlosHash, carlosSalt,
                            new DateTime(2015, 2, 20, 9, 15, 0), "Odontologo"));
                    await context.SaveChangesAsync();
                }

                if (!await context.DisponibilidadesHorarias.AnyAsync())
                {
                    await context.DisponibilidadesHorarias.AddRangeAsync(
                        new DisponibilidadHoraria("DNI", "32145874", "Lunes", new TimeOnly(8, 0), new TimeOnly(12, 0), 101),
                        new DisponibilidadHoraria("DNI", "32145874", "Miércoles", new TimeOnly(14, 0), new TimeOnly(18, 0), 102),
                        new DisponibilidadHoraria("DNI", "35987123", "Martes", new TimeOnly(9, 0), new TimeOnly(13, 0), 102),
                        new DisponibilidadHoraria("DNI", "35987123", "Jueves", new TimeOnly(16, 0), new TimeOnly(19, 0), 102));
                    await context.SaveChangesAsync();
                }

                if (!await context.Pacientes.AnyAsync())
                {
                    var pacientes = new[]
                    {
                        CrearPaciente("DNI", "44123890", "Lucia", "Fernández", new DateTime(2002, 5, 14),
                            "341-3568899", "lfernandez@gmail.com", "San Lorenzo 1240, Rosario", "HABILITADO", "2",
                            new DateTime(2024, 2, 15, 10, 0, 0)),
                        CrearPaciente("DNI", "40890123", "Esteban", "Martinez", new DateTime(1997, 12, 9),
                            "341-6789012", "emartinez@gmail.com", "Santa Fe 2150, Rosario", "HABILITADO", "1",
                            new DateTime(2024, 6, 10, 9, 0, 0)),
                        CrearPaciente("DNI", "38776543", "Camila", "Alvarez", new DateTime(1995, 3, 22),
                            "341-5234789", "calvarez@gmail.com", "Mitre 860, Rosario", "HABILITADO", null,
                            new DateTime(2025, 1, 20, 16, 30, 0)),
                        CrearPaciente("PAS", "A9876543", "John", "Miller", new DateTime(1988, 8, 19),
                            "341-4112233", "jmiller@outlook.com", "España 540, Rosario", "INHABILITADO", null,
                            new DateTime(2025, 8, 1, 14, 0, 0))
                    };
                    await context.Pacientes.AddRangeAsync(pacientes);
                    await context.SaveChangesAsync();

                    await context.HistoriasClinicas.AddRangeAsync(
                        new HistoriaClinica(1001, "DNI", "44123890", new DateTime(2024, 2, 15)),
                        new HistoriaClinica(1002, "DNI", "40890123", new DateTime(2024, 6, 10)),
                        new HistoriaClinica(1003, "DNI", "38776543", new DateTime(2025, 1, 20)),
                        new HistoriaClinica(1004, "PAS", "A9876543", new DateTime(2025, 8, 1)));
                    await InsertWithIdentityAsync(context, "HistoriasClinicas");
                }

                if (!await context.Turnos.AnyAsync())
                {
                    await InsertWithIdentityAsync(context, "Turnos",
                        new Turno(701, new DateTime(2025, 8, 10, 8, 30, 0), "OBRA_SOCIAL", 101,
                            "DNI", "32145874", "DNI", "44123890", "ATENDIDO", "Kit descartable odontológico"),
                        new Turno(702, new DateTime(2025, 8, 12, 14, 30, 0), "PARTICULAR", 103,
                            "DNI", "32145874", "DNI", "38776543", "ATENDIDO", "Instrumental endodoncia"),
                        new Turno(703, new DateTime(2025, 8, 15, 9, 30, 0), "OBRA_SOCIAL", 102,
                            "DNI", "35987123", "DNI", "40890123", "REPROGRAMADO", null, null,
                            null, new DateTime(2025, 8, 14, 11, 0, 0)),
                        new Turno(704, new DateTime(2025, 8, 22, 9, 30, 0), "OBRA_SOCIAL", 102,
                            "DNI", "35987123", "DNI", "40890123", "CONFIRMADO", "Brackets estéticos cerámicos",
                            null, 703),
                        new Turno(705, new DateTime(2025, 8, 18, 18, 0, 0), "PARTICULAR", 102,
                            "DNI", "35987123", "DNI", "38776543", "CANCELADO", null, 3500m, null,
                            null, new DateTime(2025, 8, 17, 18, 20, 0),
                            "Viaje imprevisto del paciente"));
                }

                if (!await context.ComprobantesTurnos.AnyAsync())
                {
                    await InsertWithIdentityAsync(context, "ComprobantesTurnos",
                        new ComprobanteDeTurno(9001, 701, new DateTime(2025, 8, 10, 15, 30, 0)),
                        new ComprobanteDeTurno(9002, 702, new DateTime(2025, 8, 12, 14, 40, 11)),
                        new ComprobanteDeTurno(9003, 703, new DateTime(2025, 8, 15, 16, 5, 45)),
                        new ComprobanteDeTurno(9004, 704, new DateTime(2025, 8, 14, 11, 2, 18)));
                }

                if (!await context.AtencionesOdontologicas.AnyAsync())
                {
                    await InsertWithIdentityAsync(context, "AtencionesOdontologicas",
                        new AtencionOdontologica(8001, new DateTime(2025, 8, 10, 8, 35, 0),
                            new DateTime(2025, 8, 10, 9, 15, 0), "Limpieza profunda", 8500m, 701, 1001),
                        new AtencionOdontologica(8002, new DateTime(2025, 8, 12, 14, 35, 0),
                            new DateTime(2025, 8, 12, 15, 45, 0), "Tratamiento de conducto", 22000m, 702, 1003));
                }

                if (!await context.Valoraciones.AnyAsync())
                {
                    await InsertWithIdentityAsync(context, "Valoraciones",
                        new Valoracion(301, 5, "Excelente atención y puntualidad del profesional.", 8001),
                        new Valoracion(302, 4, "Muy buen procedimiento, molestia mínima post-tratamiento", 8002));
                }

                if (!await context.DetallesInsumosUtilizados.AnyAsync())
                {
                    await context.DetallesInsumosUtilizados.AddRangeAsync(
                        new DetalleInsumoUtilizado(8001, 501, 1, 1450m),
                        new DetalleInsumoUtilizado(8001, 502, 1, 8500m),
                        new DetalleInsumoUtilizado(8001, 503, 2, 250m),
                        new DetalleInsumoUtilizado(8002, 501, 1, 1450m),
                        new DetalleInsumoUtilizado(8002, 503, 1, 250m),
                        new DetalleInsumoUtilizado(8002, 504, 1, 3200m));
                    await context.SaveChangesAsync();
                }

                if (!await context.Pagos.AnyAsync())
                {
                    await InsertWithIdentityAsync(context, "Pagos",
                        new Pago(6001, 701, new DateTime(2025, 8, 10, 9, 20, 0), 8500m,
                            "Tarjeta Débito", "1", 0m, 8500m),
                        new Pago(6002, 702, new DateTime(2025, 8, 12, 15, 50, 0), 22000m,
                            "Efectivo", null, 22000m, 0m),
                        new Pago(6003, 704, new DateTime(2025, 8, 22, 9, 15, 0), 15000m,
                            "Transferencia", "2", 7200m, 7800m));
                }

                logger?.LogInformation("Base de datos TurnoMolar inicializada con los datos de muestra.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error durante la inicialización de la base de datos.");
                throw;
            }
        }

        private static Paciente CrearPaciente(
            string tipoDocumento, string nroDocumento, string nombre, string apellido,
            DateTime fechaNacimiento, string telefono, string email, string domicilio,
            string estado, string? identificadorOS, DateTime fechaAlta)
        {
            var (hash, salt) = PasswordHasher.Generar("paciente123");
            return new Paciente(tipoDocumento, nroDocumento, nombre, apellido, fechaNacimiento, telefono, email,
                domicilio, estado, identificadorOS, 0m, hash, salt, fechaAlta, "Paciente");
        }

        private static async Task InsertWithIdentityAsync(TurnoMolarDbContext context, string table, params object[] entities)
        {
            await context.Database.OpenConnectionAsync();
            try
            {
                await context.Database.ExecuteSqlRawAsync(GetIdentityInsertSql(table, true));
                context.AddRange(entities);
                await context.SaveChangesAsync();
            }
            finally
            {
                await context.Database.ExecuteSqlRawAsync(GetIdentityInsertSql(table, false));
                await context.Database.CloseConnectionAsync();
            }
        }

        private static string GetIdentityInsertSql(string table, bool enabled) =>
            (table, enabled) switch
            {
                ("Especialidades", true) => "SET IDENTITY_INSERT [Especialidades] ON",
                ("Especialidades", false) => "SET IDENTITY_INSERT [Especialidades] OFF",
                ("Insumos", true) => "SET IDENTITY_INSERT [Insumos] ON",
                ("Insumos", false) => "SET IDENTITY_INSERT [Insumos] OFF",
                ("HistoriasClinicas", true) => "SET IDENTITY_INSERT [HistoriasClinicas] ON",
                ("HistoriasClinicas", false) => "SET IDENTITY_INSERT [HistoriasClinicas] OFF",
                ("Turnos", true) => "SET IDENTITY_INSERT [Turnos] ON",
                ("Turnos", false) => "SET IDENTITY_INSERT [Turnos] OFF",
                ("ComprobantesTurnos", true) => "SET IDENTITY_INSERT [ComprobantesTurnos] ON",
                ("ComprobantesTurnos", false) => "SET IDENTITY_INSERT [ComprobantesTurnos] OFF",
                ("AtencionesOdontologicas", true) => "SET IDENTITY_INSERT [AtencionesOdontologicas] ON",
                ("AtencionesOdontologicas", false) => "SET IDENTITY_INSERT [AtencionesOdontologicas] OFF",
                ("Valoraciones", true) => "SET IDENTITY_INSERT [Valoraciones] ON",
                ("Valoraciones", false) => "SET IDENTITY_INSERT [Valoraciones] OFF",
                ("Pagos", true) => "SET IDENTITY_INSERT [Pagos] ON",
                ("Pagos", false) => "SET IDENTITY_INSERT [Pagos] OFF",
                _ => throw new ArgumentOutOfRangeException(nameof(table), table, "Tabla no habilitada para identity insert.")
            };
    }
}
