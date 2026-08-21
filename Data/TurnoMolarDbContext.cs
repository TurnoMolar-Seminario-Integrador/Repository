using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public class TurnoMolarDbContext : DbContext
    {
        public TurnoMolarDbContext(DbContextOptions<TurnoMolarDbContext> options) : base(options) { }

        // === DbSets ===
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<ObraSocial> ObrasSociales => Set<ObraSocial>();
        public DbSet<Especialidad> Especialidades => Set<Especialidad>();
        public DbSet<DisponibilidadHoraria> DisponibilidadesHorarias => Set<DisponibilidadHoraria>();
        public DbSet<Paciente> Pacientes => Set<Paciente>();
        public DbSet<Odontologo> Odontologos => Set<Odontologo>();
        public DbSet<HistoriaClinica> HistoriasClinicas => Set<HistoriaClinica>();
        public DbSet<Turno> Turnos => Set<Turno>();
        public DbSet<ComprobanteDeTurno> Comprobantes => Set<ComprobanteDeTurno>();
        public DbSet<Insumo> Insumos => Set<Insumo>();
        public DbSet<AtencionOdontologica> Atenciones => Set<AtencionOdontologica>();
        public DbSet<DetalleInsumoUtilizado> DetallesInsumos => Set<DetalleInsumoUtilizado>();
        public DbSet<Valoracion> Valoraciones => Set<Valoracion>();
        public DbSet<Pago> Pagos => Set<Pago>();
        public DbSet<Multa> Multas => Set<Multa>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // === USUARIO ===
            modelBuilder.Entity<Usuario>(e =>
            {
                e.HasKey(u => u.Id);
                e.Property(u => u.Username).IsRequired().HasMaxLength(50);
                e.Property(u => u.Rol).IsRequired().HasMaxLength(30);
                e.HasIndex(u => u.Username).IsUnique();
            });

            // === OBRA SOCIAL ===
            modelBuilder.Entity<ObraSocial>(e =>
            {
                e.HasKey(o => o.IdentificadorOS);
                e.Property(o => o.NombreOS).IsRequired().HasMaxLength(100);
                e.Property(o => o.PlanCobertura).HasMaxLength(100);
                e.Property(o => o.ArancelOS).HasPrecision(18, 2);
                e.Property(o => o.EstadoOS).HasMaxLength(20).HasDefaultValue("ACTIVA");
            });

            // === ESPECIALIDAD ===
            modelBuilder.Entity<Especialidad>(e =>
            {
                e.HasKey(es => es.CodEspecialidad);
                e.Property(es => es.Nombre).IsRequired().HasMaxLength(100);
                e.Property(es => es.ArancelParticular).HasPrecision(18, 2);
            });

            // === DISPONIBILIDAD HORARIA ===
            modelBuilder.Entity<DisponibilidadHoraria>(e =>
            {
                e.HasKey(d => d.CodDisponibilidad);
                e.Property(d => d.DiaSemana).IsRequired().HasMaxLength(20);
            });

            // === PERSONA / PACIENTE / ODONTOLOGO (TPT - Table Per Type) ===
            // Tabla base Personas con PK compuesta
            modelBuilder.Entity<Persona>(e =>
            {
                e.UseTptMappingStrategy();
                e.HasKey(p => new { p.TipoDocumento, p.NroDocumento });
                e.Property(p => p.TipoDocumento).HasMaxLength(15);
                e.Property(p => p.Nombre).IsRequired().HasMaxLength(100);
                e.Property(p => p.Apellido).IsRequired().HasMaxLength(100);
                e.Property(p => p.Telefono).HasMaxLength(30);
                e.Property(p => p.Email).HasMaxLength(150);
                e.Property(p => p.Domicilio).HasMaxLength(200);
                e.ToTable("Personas");
            });

            // === PACIENTE ===
            modelBuilder.Entity<Paciente>(e =>
            {
                e.ToTable("Pacientes");
                e.Property(p => p.EstadoPaciente).IsRequired().HasMaxLength(20).HasDefaultValue("HABILITADO");
                e.Property(p => p.MontoAdeudado).HasPrecision(18, 2);
                e.HasOne(p => p.ObraSocial)
                    .WithMany()
                    .HasForeignKey(p => p.IdentificadorOS)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // === ODONTOLOGO ===
            modelBuilder.Entity<Odontologo>(e =>
            {
                e.ToTable("Odontologos");
                e.Property(o => o.Matricula).IsRequired().HasMaxLength(20);
                e.Property(o => o.EstadoOdontologo).IsRequired().HasMaxLength(20).HasDefaultValue("ACTIVO");
                e.HasOne(o => o.Especialidad)
                    .WithMany()
                    .HasForeignKey(o => o.CodEspecialidad)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(o => o.Disponibilidad)
                    .WithMany()
                    .HasForeignKey(o => o.CodDisponibilidad)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // === HISTORIA CLINICA ===
            modelBuilder.Entity<HistoriaClinica>(e =>
            {
                e.HasKey(h => h.NroHC);
                e.Property(h => h.AntecedentesMedicos).HasMaxLength(500);
                e.Property(h => h.Alergias).HasMaxLength(300);
                e.Property(h => h.ObservacionesGeneral).HasMaxLength(500);
                e.HasOne(h => h.Paciente)
                    .WithOne(p => p.HistoriaClinica)
                    .HasForeignKey<HistoriaClinica>(h => new { h.PacienteTipoDoc, h.PacienteNroDoc })
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // === TURNO ===
            // Ignoramos TurnoOdontologico (herencia TPH en misma tabla Turnos)
            modelBuilder.Entity<Turno>(e =>
            {
                e.HasKey(t => t.CodTurno);
                e.Property(t => t.ModalidadPagoElegida).IsRequired().HasMaxLength(20);
                e.Property(t => t.Estado).IsRequired().HasMaxLength(20);
                e.Property(t => t.MotivoCancelacion).HasMaxLength(300);

                // FK -> Especialidad
                e.HasOne(t => t.Especialidad)
                    .WithMany()
                    .HasForeignKey(t => t.CodEspecialidad)
                    .OnDelete(DeleteBehavior.Restrict);

                // FK -> Odontologo (PK compuesta)
                e.HasOne(t => t.Odontologo)
                    .WithMany()
                    .HasForeignKey(t => new { t.OdontologoTipoDoc, t.OdontologoNroDoc })
                    .OnDelete(DeleteBehavior.Restrict);

                // FK -> Paciente (PK compuesta)
                e.HasOne(t => t.Paciente)
                    .WithMany()
                    .HasForeignKey(t => new { t.PacienteTipoDoc, t.PacienteNroDoc })
                    .OnDelete(DeleteBehavior.Restrict);

                // Autoreferencia turno original (reprogramacion)
                e.HasOne(t => t.TurnoOriginal)
                    .WithMany()
                    .HasForeignKey(t => t.TurnoOriginalCod)
                    .OnDelete(DeleteBehavior.NoAction);

                e.ToTable("Turnos");
            });

            modelBuilder.Entity<TurnoOdontologico>()
                .ToTable("Turnos"); // Misma tabla que Turno (TPH)

            // === COMPROBANTE DE TURNO ===
            modelBuilder.Entity<ComprobanteDeTurno>(e =>
            {
                e.HasKey(c => c.NroComprobante);
                // FK solo por CodTurno, ya que es la PK de Turno
                e.HasOne(c => c.Turno)
                    .WithOne(t => t.Comprobante)
                    .HasForeignKey<ComprobanteDeTurno>(c => c.CodTurno)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // === INSUMO ===
            modelBuilder.Entity<Insumo>(e =>
            {
                e.HasKey(i => i.CodInsumo);
                e.Property(i => i.Nombre).IsRequired().HasMaxLength(150);
                e.Property(i => i.CostoUnitario).HasPrecision(18, 2);
            });

            // === ATENCION ODONTOLOGICA ===
            modelBuilder.Entity<AtencionOdontologica>(e =>
            {
                e.HasKey(a => a.CodAtencion);
                e.Property(a => a.Observaciones).HasMaxLength(500);

                // FK -> Turno (solo por CodTurno, PK de Turno)
                e.HasOne(a => a.Turno)
                    .WithOne(t => t.Atencion)
                    .HasForeignKey<AtencionOdontologica>(a => a.CodTurno)
                    .OnDelete(DeleteBehavior.Restrict);

                // FK -> Historia Clinica (por NroHC, PK de HistoriaClinica)
                e.HasOne(a => a.HistoriaClinica)
                    .WithMany()
                    .HasForeignKey(a => a.NroHC)
                    .OnDelete(DeleteBehavior.Restrict);

                // PacienteTipoDoc y PacienteNroDoc son datos de trazabilidad (no FK)
                e.Property(a => a.PacienteTipoDoc).HasMaxLength(15);

                // Ignore computed properties
                e.Ignore(a => a.DuracionReal);
                e.Ignore(a => a.MontoTotal);

                e.ToTable("AtencionesMedicas");
            });

            modelBuilder.Entity<Consulta>()
                .ToTable("AtencionesMedicas"); // TPH misma tabla

            // === DETALLE INSUMO UTILIZADO ===
            modelBuilder.Entity<DetalleInsumoUtilizado>(e =>
            {
                e.HasKey(d => new { d.CodInsumo, d.CodAtencion });
                e.Property(d => d.CostoUnitarioAlMomento).HasPrecision(18, 2);
                e.HasOne(d => d.Insumo)
                    .WithMany()
                    .HasForeignKey(d => d.CodInsumo)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(d => d.Atencion)
                    .WithMany(a => a.DetallesInsumos)
                    .HasForeignKey(d => d.CodAtencion)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // === VALORACION ===
            modelBuilder.Entity<Valoracion>(e =>
            {
                e.HasKey(v => v.CodValoracion);
                e.Property(v => v.Observaciones).HasMaxLength(300);
                e.HasOne(v => v.Atencion)
                    .WithOne(a => a.Valoracion)
                    .HasForeignKey<Valoracion>(v => v.CodAtencion)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // === PAGO ===
            modelBuilder.Entity<Pago>(e =>
            {
                e.HasKey(p => p.CodPago);
                e.Property(p => p.Monto).HasPrecision(18, 2);
                e.Property(p => p.AportePaciente).HasPrecision(18, 2);
                e.Property(p => p.AporteObraSocial).HasPrecision(18, 2);
                e.Property(p => p.TipoMetodoPago).IsRequired().HasMaxLength(30);
                e.Ignore(p => p.ResponsablePago); // derived
                e.HasOne(p => p.Atencion)
                    .WithOne(a => a.Pago)
                    .HasForeignKey<Pago>(p => p.CodAtencion)
                    .OnDelete(DeleteBehavior.Restrict);
                e.ToTable("Pagos");
            });

            modelBuilder.Entity<Factura>()
                .ToTable("Pagos"); // TPH misma tabla

            // === SEED DATA ===
            SeedDatabase(modelBuilder);
        }

        private static void SeedDatabase(ModelBuilder modelBuilder)
        {
            // Obras Sociales
            modelBuilder.Entity<ObraSocial>().HasData(
                new ObraSocial(1, "OSDE", "Plan 210 / 310", 18000m, "ACTIVA"),
                new ObraSocial(2, "Swiss Medical", "Black / Gold", 22000m, "ACTIVA"),
                new ObraSocial(3, "IOMA", "Afiliados Obligatorios", 12000m, "ACTIVA"),
                new ObraSocial(4, "Particular", "Sin cobertura", 0m, "ACTIVA")
            );

            // Especialidades
            modelBuilder.Entity<Especialidad>().HasData(
                new Especialidad(1, "Odontología General", 15000m),
                new Especialidad(2, "Endodoncia", 35000m),
                new Especialidad(3, "Ortodoncia", 50000m),
                new Especialidad(4, "Cirugía e Implantes", 80000m),
                new Especialidad(5, "Odontopediatría", 12000m)
            );

            // Disponibilidades Horarias
            modelBuilder.Entity<DisponibilidadHoraria>().HasData(
                new DisponibilidadHoraria(1, "Lunes", new TimeOnly(8, 0), new TimeOnly(16, 0)),
                new DisponibilidadHoraria(2, "Martes", new TimeOnly(9, 0), new TimeOnly(17, 0)),
                new DisponibilidadHoraria(3, "Miércoles", new TimeOnly(10, 0), new TimeOnly(18, 0)),
                new DisponibilidadHoraria(4, "Jueves", new TimeOnly(8, 0), new TimeOnly(14, 0)),
                new DisponibilidadHoraria(5, "Viernes", new TimeOnly(9, 0), new TimeOnly(13, 0))
            );

            // Insumos
            modelBuilder.Entity<Insumo>().HasData(
                new Insumo(1, "Kit de Anestesia Local (Mepivacaína)", 2500m, 120),
                new Insumo(2, "Resina Compuesta Fotocurable", 6800m, 45),
                new Insumo(3, "Película Radiográfica Periapical", 1500m, 200),
                new Insumo(4, "Guantes de Látex Descartables (Par)", 400m, 500),
                new Insumo(5, "Babero y Eyector Descartable", 300m, 350),
                new Insumo(6, "Pasta para Profilaxis Dental", 1200m, 60),
                new Insumo(7, "Conos de Gutapercha Endodoncia", 4500m, 30)
            );

            // Usuarios (para autenticación)
            modelBuilder.Entity<Usuario>().HasData(
                new Usuario(1, "admin", "admin123", "Admin", "Administrador Principal", "admin@turnomolar.com", true),
                new Usuario(2, "recepcion", "recepcion123", "Recepcionista", "María López", "recepcion@turnomolar.com", true),
                new Usuario(3, "doctor1", "doc123", "Odontologo", "Dr. Martín Gómez", "mgomez@turnomolar.com", true),
                new Usuario(4, "doctor2", "doc123", "Odontologo", "Dra. Laura Rossi", "lrossi@turnomolar.com", true),
                new Usuario(5, "paciente1", "paciente123", "Paciente", "Juan Pérez", "juan.perez@gmail.com", true)
            );
        }
    }
}
