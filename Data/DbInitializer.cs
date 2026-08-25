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
                // Aplicar migraciones pendientes si las hubiera
                await context.Database.MigrateAsync();

                // 1. OBRAS SOCIALES
                if (!await context.ObrasSociales.AnyAsync())
                {
                    await context.ObrasSociales.AddRangeAsync(
                        new ObraSocial(1, "OSDE", "Plan 210 / 310", 18000m, "ACTIVA"),
                        new ObraSocial(2, "Swiss Medical", "Black / Gold", 22000m, "ACTIVA"),
                        new ObraSocial(3, "IOMA", "Afiliados Obligatorios", 12000m, "ACTIVA"),
                        new ObraSocial(4, "Particular", "Sin cobertura", 0m, "ACTIVA")
                    );
                    await context.SaveChangesAsync();
                }

                // 2. ESPECIALIDADES
                if (!await context.Especialidades.AnyAsync())
                {
                    await context.Especialidades.AddRangeAsync(
                        new Especialidad(1, "Odontología General", 15000m),
                        new Especialidad(2, "Endodoncia", 35000m),
                        new Especialidad(3, "Ortodoncia", 50000m),
                        new Especialidad(4, "Cirugía e Implantes", 80000m),
                        new Especialidad(5, "Odontopediatría", 12000m)
                    );
                    await context.SaveChangesAsync();
                }

                // 3. DISPONIBILIDADES HORARIAS
                if (!await context.DisponibilidadesHorarias.AnyAsync())
                {
                    await context.DisponibilidadesHorarias.AddRangeAsync(
                        new DisponibilidadHoraria(1, "Lunes a Viernes (Mañana)", new TimeOnly(8, 0), new TimeOnly(13, 0)),
                        new DisponibilidadHoraria(2, "Lunes a Viernes (Tarde)", new TimeOnly(14, 0), new TimeOnly(19, 0)),
                        new DisponibilidadHoraria(3, "Martes y Jueves (Completo)", new TimeOnly(9, 0), new TimeOnly(18, 0))
                    );
                    await context.SaveChangesAsync();
                }

                // 4. ODONTÓLOGOS (Responsables y Profesionales de la Clínica)
                if (!await context.Odontologos.AnyAsync())
                {
                    var doc1 = new Odontologo(
                        "DNI",
                        28456789,
                        "MP 3840",
                        "Karina",
                        "González",
                        new DateTime(1980, 5, 14),
                        "341-4567890",
                        "karina.gonzalez@turnomolar.com",
                        "Bv. Oroño 1234, Rosario",
                        "ACTIVO",
                        1,
                        1
                    );

                    var doc2 = new Odontologo(
                        "DNI",
                        30123456,
                        "MP 4512",
                        "Elena",
                        "Silva",
                        new DateTime(1983, 8, 22),
                        "341-5678901",
                        "elena.silva@turnomolar.com",
                        "Av. Pellegrini 850, Rosario",
                        "ACTIVO",
                        2,
                        3
                    );

                    var doc3 = new Odontologo(
                        "DNI",
                        26789012,
                        "MP 5120",
                        "Martín",
                        "López",
                        new DateTime(1978, 11, 30),
                        "341-6789012",
                        "martin.lopez@turnomolar.com",
                        "Santa Fe 2100, Rosario",
                        "ACTIVO",
                        3,
                        4
                    );

                    await context.Odontologos.AddRangeAsync(doc1, doc2, doc3);
                    await context.SaveChangesAsync();
                }

                // 5. PACIENTES
                if (!await context.Pacientes.AnyAsync())
                {
                    var pac1 = new Paciente(
                        "DNI",
                        34567890,
                        "Manuel",
                        "Fernández",
                        new DateTime(1989, 4, 15),
                        "341-3334455",
                        "manuel.fer@email.com",
                        "Córdoba 1540, Rosario",
                        "HABILITADO",
                        0m,
                        1 // OSDE
                    );

                    var pac2 = new Paciente(
                        "DNI",
                        38999111,
                        "Laura",
                        "Gómez",
                        new DateTime(1995, 9, 28),
                        "341-8889900",
                        "laura.gomez@gmail.com",
                        "Rioja 2230, Rosario",
                        "HABILITADO",
                        0m,
                        2 // Swiss Medical
                    );

                    var pac3 = new Paciente(
                        "DNI",
                        29888777,
                        "Carlos",
                        "Rossi",
                        new DateTime(1982, 12, 10),
                        "341-1112233",
                        "carlos.rossi@hotmail.com",
                        "San Lorenzo 890, Rosario",
                        "INHABILITADO",
                        15000m,
                        4 // Particular con deuda
                    );

                    await context.Pacientes.AddRangeAsync(pac1, pac2, pac3);
                    await context.SaveChangesAsync();

                    // Historias Clínicas para cada paciente (NroHC = 0 para que la BD genere el IDENTITY)
                    await context.HistoriasClinicas.AddRangeAsync(
                        new HistoriaClinica(0, "DNI", 34567890, new DateTime(2024, 1, 15), "Sin antecedentes relevantes", "Ninguna", "Paciente apto para ortodoncia y limpiezas regulares"),
                        new HistoriaClinica(0, "DNI", 38999111, new DateTime(2024, 3, 20), "Hipertensión leve controlada", "Penicilina", "Requiere control semestral"),
                        new HistoriaClinica(0, "DNI", 29888777, new DateTime(2024, 5, 10), "Bruxismo", "Ninguna", "Tratamiento de conducto pendiente")
                    );
                    await context.SaveChangesAsync();
                }

                // 6. USUARIOS Y CREDENCIALES DE AUTENTICACIÓN (EC03 - Login)
                // Seeders para Odontólogo / Responsable de Clínica, Pacientes y Administradores
                var usuariosDeseados = new List<Usuario>
                {
                    // Responsable de la Clínica / Odontólogo
                    new Usuario(0, "karina.gonzalez", "doc123", "ResponsableClinica", "Dra. Karina González", "karina.gonzalez@turnomolar.com", true, 28456789),
                    new Usuario(0, "28456789", "doc123", "ResponsableClinica", "Dra. Karina González", "karina.gonzalez@turnomolar.com", true, 28456789),
                    new Usuario(0, "doctor1", "doc123", "ResponsableClinica", "Dra. Karina González", "karina.gonzalez@turnomolar.com", true, 28456789),

                    // Otros Odontólogos
                    new Usuario(0, "elena.silva", "doc123", "Odontologo", "Dra. Elena Silva", "elena.silva@turnomolar.com", true, 30123456),
                    new Usuario(0, "30123456", "doc123", "Odontologo", "Dra. Elena Silva", "elena.silva@turnomolar.com", true, 30123456),
                    new Usuario(0, "martin.lopez", "doc123", "Odontologo", "Dr. Martín López", "martin.lopez@turnomolar.com", true, 26789012),
                    new Usuario(0, "26789012", "doc123", "Odontologo", "Dr. Martín López", "martin.lopez@turnomolar.com", true, 26789012),

                    // Pacientes
                    new Usuario(0, "manuel.fernandez", "paciente123", "Paciente", "Manuel Fernández", "manuel.fer@email.com", true, 34567890),
                    new Usuario(0, "34567890", "paciente123", "Paciente", "Manuel Fernández", "manuel.fer@email.com", true, 34567890),
                    new Usuario(0, "paciente1", "paciente123", "Paciente", "Manuel Fernández", "manuel.fer@email.com", true, 34567890),
                    new Usuario(0, "laura.gomez", "paciente123", "Paciente", "Laura Gómez", "laura.gomez@gmail.com", true, 38999111),
                    new Usuario(0, "38999111", "paciente123", "Paciente", "Laura Gómez", "laura.gomez@gmail.com", true, 38999111),
                    new Usuario(0, "carlos.rossi", "paciente123", "Paciente", "Carlos Rossi", "carlos.rossi@hotmail.com", true, 29888777),
                    new Usuario(0, "29888777", "paciente123", "Paciente", "Carlos Rossi", "carlos.rossi@hotmail.com", true, 29888777),

                    // Administrador general y Recepción
                    new Usuario(0, "admin", "admin123", "Admin", "Administrador Principal", "admin@turnomolar.com", true, null),
                    new Usuario(0, "recepcion", "recepcion123", "Recepcionista", "María López", "recepcion@turnomolar.com", true, null)
                };

                foreach (var u in usuariosDeseados)
                {
                    var existe = await context.Usuarios.FirstOrDefaultAsync(x => x.Username.ToLower() == u.Username.ToLower());
                    if (existe == null)
                    {
                        await context.Usuarios.AddAsync(u);
                    }
                    else
                    {
                        existe.PasswordHash = u.PasswordHash;
                        existe.Rol = u.Rol;
                        existe.NombreCompleto = u.NombreCompleto;
                        existe.Email = u.Email;
                        existe.Activo = true;
                        existe.EntidadId = u.EntidadId;
                    }
                }
                await context.SaveChangesAsync();

                logger?.LogInformation("Base de datos TurnoMolar inicializada y seeders aplicados correctamente.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error durante la inicialización de la base de datos.");
            }
        }
    }
}
