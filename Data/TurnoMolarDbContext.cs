using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public class TurnoMolarDbContext : DbContext
    {
        public TurnoMolarDbContext(DbContextOptions<TurnoMolarDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Paciente> Pacientes => Set<Paciente>();
        public DbSet<Odontologo> Odontologos => Set<Odontologo>();
        public DbSet<Especialidad> Especialidades => Set<Especialidad>();
        public DbSet<Insumo> Insumos => Set<Insumo>();
        public DbSet<TurnoOdontologico> Turnos => Set<TurnoOdontologico>();
        public DbSet<Consulta> Consultas => Set<Consulta>();
        public DbSet<Factura> Facturas => Set<Factura>();
        public DbSet<ItemFactura> ItemsFactura => Set<ItemFactura>();
        public DbSet<Multa> Multas => Set<Multa>();
        public DbSet<ObraSocial> ObrasSociales => Set<ObraSocial>();
        public DbSet<HistoriaClinica> HistoriasClinicas => Set<HistoriaClinica>();
        public DbSet<Consultorio> Consultorios => Set<Consultorio>();
        public DbSet<HorarioOdont> HorariosOdont => Set<HorarioOdont>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuraciones de entidades
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Rol).IsRequired().HasMaxLength(30);
            });

            modelBuilder.Entity<Paciente>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.ObraSocial)
                      .WithMany()
                      .HasForeignKey(e => e.ObraSocialId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Odontologo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Especialidad)
                      .WithMany()
                      .HasForeignKey(e => e.EspecialidadId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TurnoOdontologico>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Paciente)
                      .WithMany()
                      .HasForeignKey(e => e.PacienteId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Odontologo)
                      .WithMany()
                      .HasForeignKey(e => e.OdontologoId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Especialidad)
                      .WithMany()
                      .HasForeignKey(e => e.EspecialidadId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.MontoEstimado).HasPrecision(18, 2);
            });

            modelBuilder.Entity<Factura>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Subtotal).HasPrecision(18, 2);
                entity.Property(e => e.DescuentoObraSocial).HasPrecision(18, 2);
                entity.Property(e => e.Total).HasPrecision(18, 2);
                entity.Property(e => e.MontoAPagarPaciente).HasPrecision(18, 2);
                entity.HasMany(e => e.Items)
                      .WithOne(e => e.Factura)
                      .HasForeignKey(e => e.FacturaId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ItemFactura>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PrecioUnitario).HasPrecision(18, 2);
                entity.Property(e => e.Subtotal).HasPrecision(18, 2);
                entity.HasOne(e => e.Insumo)
                      .WithMany()
                      .HasForeignKey(e => e.InsumoId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Insumo>(entity =>
            {
                entity.Property(e => e.Precio).HasPrecision(18, 2);
            });

            modelBuilder.Entity<Multa>(entity =>
            {
                entity.Property(e => e.Monto).HasPrecision(18, 2);
            });

            modelBuilder.Entity<ObraSocial>(entity =>
            {
                entity.Property(e => e.PorcentajeCobertura).HasPrecision(5, 2);
            });

            // Seed Data Inicial
            SeedDatabase(modelBuilder);
        }

        private static void SeedDatabase(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Especialidad>().HasData(
                new Especialidad(1, "Odontología General", "Atención primaria, limpiezas y controles preventivos"),
                new Especialidad(2, "Endodoncia", "Tratamiento de conducto y pulpa dental"),
                new Especialidad(3, "Ortodoncia", "Alineación de piezas dentales y brackets"),
                new Especialidad(4, "Cirugía e Implantes", "Extracciones complejas y colocación de implantes"),
                new Especialidad(5, "Odontopediatría", "Atención odontológica integral infantil")
            );

            modelBuilder.Entity<ObraSocial>().HasData(
                new ObraSocial(1, "OSDE", "Plan 210 / 310", 0.70m),
                new ObraSocial(2, "Swiss Medical", "Black / Gold", 0.65m),
                new ObraSocial(3, "IOMA", "Afiliados Obligatorios", 0.50m),
                new ObraSocial(4, "Particular", "Sin cobertura (100% particular)", 0.00m)
            );

            modelBuilder.Entity<Insumo>().HasData(
                new Insumo(1, "Kit de Anestesia Local (Mepivacaína)", "Anestesia cartucho dental x 1.8ml", 2500m, 120),
                new Insumo(2, "Resina Compuesta Fotocurable", "Material de restauración estética", 6800m, 45),
                new Insumo(3, "Película Radiográfica Periapical", "Radiografía periapical digitalizada", 1500m, 200),
                new Insumo(4, "Guantes de Látex Descartables (Par)", "Bioseguridad descartable", 400m, 500),
                new Insumo(5, "Babero y Eyetor Descartable", "Kit de aislamiento para paciente", 300m, 350),
                new Insumo(6, "Pasta para Profilaxis Dental", "Pasta abrasiva para limpieza profunda", 1200m, 60),
                new Insumo(7, "Conos de Gutapercha Endodoncia", "Obturación de conductos radiculares", 4500m, 30)
            );

            modelBuilder.Entity<Usuario>().HasData(
                new Usuario(1, "admin", "admin123", "Admin", "Administrador Principal", "admin@turnomolar.com", true),
                new Usuario(2, "recepcion", "recepcion123", "Recepcionista", "María López", "recepcion@turnomolar.com", true),
                new Usuario(3, "doctor1", "doc123", "Odontologo", "Dr. Martín Gómez", "mgomez@turnomolar.com", true, 1),
                new Usuario(4, "doctor2", "doc123", "Odontologo", "Dra. Laura Rossi", "lrossi@turnomolar.com", true, 2),
                new Usuario(5, "paciente1", "paciente123", "Paciente", "Juan Pérez", "juan.perez@gmail.com", true, 1)
            );

            modelBuilder.Entity<Odontologo>().HasData(
                new Odontologo(1, 10234, "Martín", "Gómez", 28345678, "11-4567-8901", "mgomez@turnomolar.com", "Av. Santa Fe 1234, CABA", 1),
                new Odontologo(2, 10456, "Laura", "Rossi", 31456789, "11-5678-9012", "lrossi@turnomolar.com", "Corrientes 2450, CABA", 2),
                new Odontologo(3, 10789, "Esteban", "Díaz", 29876543, "11-6789-0123", "ediaz@turnomolar.com", "Callao 890, CABA", 3)
            );

            modelBuilder.Entity<Paciente>().HasData(
                new Paciente(1, "Juan", "Pérez", 35123456, "11-2345-6789", "juan.perez@gmail.com", "Belgrano 450, Quilmes", true, 1, "OSDE-987654"),
                new Paciente(2, "Ana", "Martínez", 38765432, "11-3456-7890", "ana.martinez@hotmail.com", "Mitre 780, Avellaneda", true, 2, "SM-543210"),
                new Paciente(3, "Carlos", "Sánchez", 27987654, "11-4567-8901", "csanchez@yahoo.com", "Rivadavia 1200, Lanús", true, 4, null),
                new Paciente(4, "Sofía", "Rodríguez", 40123987, "11-5678-1234", "sofia.rodriguez@gmail.com", "San Martín 320, Bernal", false, 3, "IOMA-334455") // Inhabilitada por deuda/multa
            );

            modelBuilder.Entity<Multa>().HasData(
                new Multa(1, 4, 3500m, false, null, "Ausencia no justificada a turno programado el 10/08/2026")
            );

            modelBuilder.Entity<HistoriaClinica>().HasData(
                new HistoriaClinica(1, 1001, 1, new DateTime(2025, 1, 15), "Hipertensión leve controlada", "Penicilina", "Paciente con buena salud bucal previa"),
                new HistoriaClinica(2, 1002, 2, new DateTime(2025, 3, 20), "Ninguno", "Ninguna", "Tratamiento de ortodoncia en curso"),
                new HistoriaClinica(3, 1003, 3, new DateTime(2025, 5, 10), "Diabetes Tipo 2", "Aspirina", "Higiene regular"),
                new HistoriaClinica(4, 1004, 4, new DateTime(2025, 6, 01), "Ninguno", "Ninguna", "Inhabilitado por inasistencia sin aviso")
            );

            modelBuilder.Entity<TurnoOdontologico>().HasData(
                new TurnoOdontologico(1, DateTime.Today.AddHours(9), new TimeOnly(9, 0), TurnoOdontologico.EstadoTurnoEnum.Pendiente, null, 1, 1, 1, 12000m),
                new TurnoOdontologico(2, DateTime.Today.AddHours(10), new TimeOnly(10, 0), TurnoOdontologico.EstadoTurnoEnum.Presente, null, 2, 2, 2, 25000m),
                new TurnoOdontologico(3, DateTime.Today.AddHours(11), new TimeOnly(11, 0), TurnoOdontologico.EstadoTurnoEnum.Atendido, null, 3, 1, 1, 15000m),
                new TurnoOdontologico(4, DateTime.Today.AddDays(1).AddHours(14), new TimeOnly(14, 0), TurnoOdontologico.EstadoTurnoEnum.Pendiente, null, 1, 3, 3, 18000m)
            );
        }
    }
}
