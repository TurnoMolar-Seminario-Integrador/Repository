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
                // Asegura la creación de las 15 tablas según el MDF
                await context.Database.EnsureCreatedAsync();

                // 1. OBRAS SOCIALES (PK NVARCHAR(20))
                if (!await context.ObrasSociales.AnyAsync())
                {
                    await context.ObrasSociales.AddRangeAsync(
                        new ObraSocial("OSDE", "OSDE Binario", "Plan 210 / 310", "ACTIVA"),
                        new ObraSocial("SWISS", "Swiss Medical", "Black / Gold", "ACTIVA"),
                        new ObraSocial("IOMA", "IOMA", "Afiliados Obligatorios", "ACTIVA"),
                        new ObraSocial("PARTICULAR", "Particular", "Sin cobertura", "ACTIVA")
                    );
                    await context.SaveChangesAsync();
                }

                // 2. ESPECIALIDADES
                if (!await context.Especialidades.AnyAsync())
                {
                    await context.Especialidades.AddRangeAsync(
                        new Especialidad("Odontología General", 15000m),
                        new Especialidad("Endodoncia", 35000m),
                        new Especialidad("Ortodoncia", 50000m),
                        new Especialidad("Cirugía e Implantes", 80000m),
                        new Especialidad("Odontopediatría", 12000m)
                    );
                    await context.SaveChangesAsync();
                }

                // 3. CONVENIOS (Obras Sociales con Especialidades)
                if (!await context.Convenios.AnyAsync())
                {
                    await context.Convenios.AddRangeAsync(
                        new Convenio("OSDE", 1, 18000m),
                        new Convenio("OSDE", 2, 40000m),
                        new Convenio("OSDE", 3, 58000m),
                        new Convenio("OSDE", 4, 90000m),
                        new Convenio("OSDE", 5, 14000m),
                        new Convenio("SWISS", 1, 20000m),
                        new Convenio("SWISS", 2, 45000m),
                        new Convenio("SWISS", 3, 62000m),
                        new Convenio("SWISS", 4, 95000m),
                        new Convenio("SWISS", 5, 16000m),
                        new Convenio("IOMA", 1, 12000m),
                        new Convenio("IOMA", 2, 28000m),
                        new Convenio("IOMA", 3, 42000m),
                        new Convenio("IOMA", 4, 65000m),
                        new Convenio("IOMA", 5, 10000m)
                    );
                    await context.SaveChangesAsync();
                }

                // 4. RESPONSABLES DE CLINICA
                if (!await context.ResponsablesClinica.AnyAsync())
                {
                    // Contraseña distinta a la de su fila en Odontologos ("doc123") a propósito:
                    // así se puede probar el login como cada rol por separado.
                    var (claveKarina, saltKarina) = PasswordHasher.Generar("resp123");
                    // TODO: reemplazar por una persona real — placeholder para que exista
                    // una fila con Rol="Admin" en ResponsablesClinica (ver MU: Admin y
                    // "Responsable de la Clínica" son dos filas distintas, mismo modelo).
                    var (claveAdmin, saltAdmin) = PasswordHasher.Generar("admin123");

                    await context.ResponsablesClinica.AddRangeAsync(
                        new ResponsableClinica(
                            "DNI",
                            "28456789",
                            "Karina",
                            "González",
                            new DateTime(1980, 5, 14),
                            "341-4567890",
                            "karina.gonzalez@turnomolar.com",
                            "Bv. Oroño 1234, Rosario",
                            claveKarina,
                            saltKarina,
                            DateTime.Now,
                            "ResponsableClinica"
                        ),
                        new ResponsableClinica(
                            "DNI",
                            "20000000",
                            "Administrador",
                            "General",
                            new DateTime(1985, 1, 1),
                            "341-0000000",
                            "admin@turnomolar.com",
                            "Sede Central, Rosario",
                            claveAdmin,
                            saltAdmin,
                            DateTime.Now,
                            "Admin"
                        )
                    );
                    await context.SaveChangesAsync();
                }

                // 5. ODONTÓLOGOS
                if (!await context.Odontologos.AnyAsync())
                {
                    var (clave1, salt1) = PasswordHasher.Generar("doc123");
                    var doc1 = new Odontologo(
                        "DNI",
                        "28456789",
                        "MP 3840",
                        "Karina",
                        "González",
                        new DateTime(1980, 5, 14),
                        "341-4567890",
                        "karina.gonzalez@turnomolar.com",
                        "Bv. Oroño 1234, Rosario",
                        "ACTIVO",
                        clave1,
                        salt1,
                        DateTime.Now,
                        "Odontologo"
                    );

                    var (clave2, salt2) = PasswordHasher.Generar("doc123");
                    var doc2 = new Odontologo(
                        "DNI",
                        "30123456",
                        "MP 4512",
                        "Elena",
                        "Silva",
                        new DateTime(1983, 8, 22),
                        "341-5678901",
                        "elena.silva@turnomolar.com",
                        "Av. Pellegrini 850, Rosario",
                        "ACTIVO",
                        clave2,
                        salt2,
                        DateTime.Now,
                        "Odontologo"
                    );

                    var (clave3, salt3) = PasswordHasher.Generar("doc123");
                    var doc3 = new Odontologo(
                        "DNI",
                        "26789012",
                        "MP 5120",
                        "Martín",
                        "López",
                        new DateTime(1978, 11, 30),
                        "341-6789012",
                        "martin.lopez@turnomolar.com",
                        "Santa Fe 2100, Rosario",
                        "ACTIVO",
                        clave3,
                        salt3,
                        DateTime.Now,
                        "Odontologo"
                    );

                    await context.Odontologos.AddRangeAsync(doc1, doc2, doc3);
                    await context.SaveChangesAsync();
                }

                // 6. DISPONIBILIDADES HORARIAS
                if (!await context.DisponibilidadesHorarias.AnyAsync())
                {
                    await context.DisponibilidadesHorarias.AddRangeAsync(
                        new DisponibilidadHoraria("DNI", "28456789", "Lunes", new TimeOnly(8, 0), new TimeOnly(13, 0), 1),
                        new DisponibilidadHoraria("DNI", "28456789", "Martes", new TimeOnly(8, 0), new TimeOnly(13, 0), 1),
                        new DisponibilidadHoraria("DNI", "30123456", "Miércoles", new TimeOnly(14, 0), new TimeOnly(19, 0), 2),
                        new DisponibilidadHoraria("DNI", "26789012", "Jueves", new TimeOnly(9, 0), new TimeOnly(18, 0), 3),
                        new DisponibilidadHoraria("DNI", "28456789", "Viernes", new TimeOnly(9, 0), new TimeOnly(18, 0), 3)
                    );
                    await context.SaveChangesAsync();
                }

                // 7. PACIENTES
                if (!await context.Pacientes.AnyAsync())
                {
                    var (clavePac1, saltPac1) = PasswordHasher.Generar("paciente123");
                    var pac1 = new Paciente(
                        "DNI",
                        "34567890",
                        "Manuel",
                        "Fernández",
                        new DateTime(1989, 4, 15),
                        "341-3334455",
                        "manuel.fer@email.com",
                        "Córdoba 1540, Rosario",
                        "HABILITADO",
                        "OSDE",
                        0m,
                        clavePac1,
                        saltPac1,
                        DateTime.Now,
                        "Paciente"
                    );

                    var (clavePac2, saltPac2) = PasswordHasher.Generar("paciente123");
                    var pac2 = new Paciente(
                        "DNI",
                        "38999111",
                        "Laura",
                        "Gómez",
                        new DateTime(1995, 9, 28),
                        "341-8889900",
                        "laura.gomez@gmail.com",
                        "Rioja 2230, Rosario",
                        "HABILITADO",
                        "SWISS",
                        0m,
                        clavePac2,
                        saltPac2,
                        DateTime.Now,
                        "Paciente"
                    );

                    var (clavePac3, saltPac3) = PasswordHasher.Generar("paciente123");
                    var pac3 = new Paciente(
                        "DNI",
                        "29888777",
                        "Carlos",
                        "Rossi",
                        new DateTime(1982, 12, 10),
                        "341-1112233",
                        "carlos.rossi@hotmail.com",
                        "San Lorenzo 890, Rosario",
                        "HABILITADO",
                        "PARTICULAR",
                        0m,
                        clavePac3,
                        saltPac3,
                        DateTime.Now,
                        "Paciente"
                    );

                    await context.Pacientes.AddRangeAsync(pac1, pac2, pac3);
                    await context.SaveChangesAsync();

                    // Historias Clínicas para cada paciente
                    await context.HistoriasClinicas.AddRangeAsync(
                        new HistoriaClinica(0, "DNI", "34567890", new DateTime(2024, 1, 15), "Sin antecedentes relevantes", "Ninguna", "Paciente apto para ortodoncia y limpiezas regulares"),
                        new HistoriaClinica(0, "DNI", "38999111", new DateTime(2024, 3, 20), "Hipertensión leve controlada", "Penicilina", "Requiere control semestral"),
                        new HistoriaClinica(0, "DNI", "29888777", new DateTime(2024, 5, 10), "Bruxismo", "Ninguna", "Tratamiento de conducto pendiente")
                    );
                    await context.SaveChangesAsync();
                }

                // 8. INSUMOS
                if (!await context.Insumos.AnyAsync())
                {
                    await context.Insumos.AddRangeAsync(
                        new Insumo("Kit de Anestesia Local (Mepivacaína)", 2500m, 120),
                        new Insumo("Resina Compuesta Fotocurable", 6800m, 45),
                        new Insumo("Película Radiográfica Periapical", 1500m, 200),
                        new Insumo("Guantes de Látex Descartables (Par)", 400m, 500),
                        new Insumo("Babero y Eyector Descartable", 300m, 350),
                        new Insumo("Pasta para Profilaxis Dental", 1200m, 60),
                        new Insumo("Conos de Gutapercha Endodoncia", 4500m, 30)
                    );
                    await context.SaveChangesAsync();
                }

                logger?.LogInformation("Base de datos TurnoMolar (MDF v1.01) inicializada y seeders aplicados correctamente.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error durante la inicialización de la base de datos.");
            }
        }
    }
}