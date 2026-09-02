using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public class TurnoMolarDbContext : DbContext
    {
        public TurnoMolarDbContext(DbContextOptions<TurnoMolarDbContext> options) : base(options) { }

        // === 15 DbSets Oficiales según MDF v1.01 ===
        public DbSet<Especialidad> Especialidades => Set<Especialidad>();
        public DbSet<DisponibilidadHoraria> DisponibilidadesHorarias => Set<DisponibilidadHoraria>();
        public DbSet<Odontologo> Odontologos => Set<Odontologo>();
        public DbSet<ResponsableClinica> ResponsablesClinica => Set<ResponsableClinica>();
        public DbSet<ObraSocial> ObrasSociales => Set<ObraSocial>();
        public DbSet<Convenio> Convenios => Set<Convenio>();
        public DbSet<ComprobanteDeTurno> ComprobantesTurnos => Set<ComprobanteDeTurno>();
        public DbSet<Turno> Turnos => Set<Turno>();
        public DbSet<AtencionOdontologica> AtencionesOdontologicas => Set<AtencionOdontologica>();
        public DbSet<Pago> Pagos => Set<Pago>();
        public DbSet<DetalleInsumoUtilizado> DetallesInsumosUtilizados => Set<DetalleInsumoUtilizado>();
        public DbSet<Insumo> Insumos => Set<Insumo>();
        public DbSet<Valoracion> Valoraciones => Set<Valoracion>();
        public DbSet<Paciente> Pacientes => Set<Paciente>();
        public DbSet<HistoriaClinica> HistoriasClinicas => Set<HistoriaClinica>();

        // DbSets adicionales / utilitarios
        public DbSet<Usuario> Usuarios => Set<Usuario>();

        // Propiedades de compatibilidad
        public DbSet<ComprobanteDeTurno> Comprobantes => ComprobantesTurnos;
        public DbSet<AtencionOdontologica> Atenciones => AtencionesOdontologicas;
        public DbSet<DetalleInsumoUtilizado> DetallesInsumos => DetallesInsumosUtilizados;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. ESPECIALIDADES
            modelBuilder.Entity<Especialidad>(e =>
            {
                e.ToTable("Especialidades");
                e.HasKey(es => es.IdEspecialidad);
                e.Property(es => es.IdEspecialidad).ValueGeneratedOnAdd();
                e.Property(es => es.Nombre).IsRequired().HasMaxLength(100);
                e.HasIndex(es => es.Nombre).IsUnique();
                e.Property(es => es.ArancelParticular).HasPrecision(12, 2);
            });

            // 2. DISPONIBILIDADES HORARIAS (PK Compuesta)
            modelBuilder.Entity<DisponibilidadHoraria>(e =>
            {
                e.ToTable("DisponibilidadesHorarias");
                e.HasKey(d => new { d.TipoDocumentoOdontologo, d.NroDocumentoOdontologo, d.DiaSemana });
                e.Property(d => d.TipoDocumentoOdontologo).HasMaxLength(20);
                e.Property(d => d.NroDocumentoOdontologo).HasMaxLength(20);
                e.Property(d => d.DiaSemana).HasMaxLength(20);

                e.HasOne(d => d.Odontologo)
                    .WithMany(o => o.DisponibilidadesHorarias)
                    .HasForeignKey(d => new { d.TipoDocumentoOdontologo, d.NroDocumentoOdontologo })
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(d => d.Especialidad)
                    .WithMany(es => es.DisponibilidadesHorarias)
                    .HasForeignKey(d => d.IdEspecialidad)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 3. ODONTOLOGOS (PK Compuesta)
            modelBuilder.Entity<Odontologo>(e =>
            {
                e.ToTable("Odontologos");
                e.HasKey(o => new { o.TipoDocumento, o.NroDocumento });
                e.Property(o => o.TipoDocumento).HasMaxLength(20);
                e.Property(o => o.NroDocumento).HasMaxLength(20);
                e.Property(o => o.Matricula).IsRequired().HasMaxLength(20);
                e.HasIndex(o => o.Matricula).IsUnique();
                e.Property(o => o.Nombre).IsRequired().HasMaxLength(100);
                e.Property(o => o.Apellido).IsRequired().HasMaxLength(100);
                e.Property(o => o.FechaNacimiento).HasColumnType("date");
                e.Property(o => o.Telefono).HasMaxLength(20);
                e.Property(o => o.Email).HasMaxLength(150);
                e.Property(o => o.Domicilio).HasMaxLength(150);
                e.Property(o => o.EstadoOdontologo).HasMaxLength(20).HasDefaultValue("ACTIVO");
                e.Property(o => o.Clave).HasMaxLength(255);
                e.Property(o => o.SaltClave).HasMaxLength(100);
                e.Property(o => o.FechaAlta).HasColumnType("datetime2(0)");
                e.Property(o => o.Rol).HasMaxLength(20);
            });

            // 4. RESPONSABLES DE CLINICA (PK Compuesta)
            modelBuilder.Entity<ResponsableClinica>(e =>
            {
                e.ToTable("ResponsablesClinica");
                e.HasKey(r => new { r.TipoDocumento, r.NroDocumento });
                e.Property(r => r.TipoDocumento).HasMaxLength(20);
                e.Property(r => r.NroDocumento).HasMaxLength(20);
                e.Property(r => r.Nombre).IsRequired().HasMaxLength(100);
                e.Property(r => r.Apellido).IsRequired().HasMaxLength(100);
                e.Property(r => r.FechaNacimiento).HasColumnType("date");
                e.Property(r => r.Telefono).HasMaxLength(20);
                e.Property(r => r.Email).HasMaxLength(150);
                e.Property(r => r.Domicilio).HasMaxLength(150);
                e.Property(r => r.Clave).HasMaxLength(255);
                e.Property(r => r.SaltClave).HasMaxLength(100);
                e.Property(r => r.FechaAlta).HasColumnType("datetime2(0)");
                e.Property(r => r.Rol).HasMaxLength(20);
            });

            // 5. OBRAS SOCIALES
            modelBuilder.Entity<ObraSocial>(e =>
            {
                e.ToTable("ObrasSociales");
                e.HasKey(o => o.IdentificadorOS);
                e.Property(o => o.IdentificadorOS).HasMaxLength(20);
                e.Property(o => o.NombreOS).IsRequired().HasMaxLength(100);
                e.Property(o => o.PlanCobertura).HasMaxLength(100);
                e.Property(o => o.EstadoOS).HasMaxLength(20).HasDefaultValue("ACTIVA");
            });

            // 6. CONVENIOS (PK Compuesta)
            modelBuilder.Entity<Convenio>(e =>
            {
                e.ToTable("Convenios");
                e.HasKey(c => new { c.IdentificadorOS, c.IdEspecialidad });
                e.Property(c => c.IdentificadorOS).HasMaxLength(20);
                e.Property(c => c.ArancelConvenio).HasPrecision(12, 2);

                e.HasOne(c => c.ObraSocial)
                    .WithMany(o => o.Convenios)
                    .HasForeignKey(c => c.IdentificadorOS)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(c => c.Especialidad)
                    .WithMany(es => es.Convenios)
                    .HasForeignKey(c => c.IdEspecialidad)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 7. COMPROBANTES DE TURNOS
            modelBuilder.Entity<ComprobanteDeTurno>(e =>
            {
                e.ToTable("ComprobantesTurnos");
                e.HasKey(c => c.NroComprobante);
                e.Property(c => c.NroComprobante).ValueGeneratedOnAdd();
                e.Property(c => c.FechaHoraEmision).HasColumnType("datetime2(0)");

                e.HasOne(c => c.Turno)
                    .WithOne(t => t.Comprobante)
                    .HasForeignKey<ComprobanteDeTurno>(c => c.NroTurno)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 8. TURNOS
            modelBuilder.Entity<Turno>(e =>
            {
                e.ToTable("Turnos");
                e.HasKey(t => t.NroTurno);
                e.Property(t => t.NroTurno).ValueGeneratedOnAdd();
                e.Property(t => t.ModalidadPagoElegida).IsRequired().HasMaxLength(20);
                e.Property(t => t.FechaHoraTurno).HasColumnType("datetime2(0)");
                e.Property(t => t.FechaHoraReprogramacion).HasColumnType("datetime2(0)");
                e.Property(t => t.FechaHoraCancelacion).HasColumnType("datetime2(0)");
                e.Property(t => t.MotivoCancelacion).HasMaxLength(300);
                e.Property(t => t.EstadoTurno).IsRequired().HasMaxLength(20);
                e.Property(t => t.DescripcionMaterial).HasMaxLength(300);
                e.Property(t => t.ArancelPenalizacionAplicado).HasPrecision(12, 2);

                e.Property(t => t.TipoDocumentoPaciente).HasMaxLength(20);
                e.Property(t => t.NroDocumentoPaciente).HasMaxLength(20);
                e.Property(t => t.TipoDocumentoOdontologo).HasMaxLength(20);
                e.Property(t => t.NroDocumentoOdontologo).HasMaxLength(20);

                e.HasOne(t => t.Paciente)
                    .WithMany(p => p.Turnos)
                    .HasForeignKey(t => new { t.TipoDocumentoPaciente, t.NroDocumentoPaciente })
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(t => t.Odontologo)
                    .WithMany(o => o.Turnos)
                    .HasForeignKey(t => new { t.TipoDocumentoOdontologo, t.NroDocumentoOdontologo })
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(t => t.Especialidad)
                    .WithMany(es => es.Turnos)
                    .HasForeignKey(t => t.IdEspecialidad)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(t => t.TurnoOriginal)
                    .WithMany()
                    .HasForeignKey(t => t.NroTurnoOriginal)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // 9. ATENCIONES ODONTOLOGICAS
            modelBuilder.Entity<AtencionOdontologica>(e =>
            {
                e.ToTable("AtencionesOdontologicas");
                e.HasKey(a => a.IdAtencion);
                e.Property(a => a.IdAtencion).ValueGeneratedOnAdd();
                e.Property(a => a.FechaHoraAtencionInicio).HasColumnType("datetime2(0)");
                e.Property(a => a.FechaHoraAtencionFin).HasColumnType("datetime2(0)");
                e.Property(a => a.Observaciones).HasMaxLength(1000);
                e.Property(a => a.ArancelAplicado).HasPrecision(12, 2);

                e.HasOne(a => a.Turno)
                    .WithOne(t => t.Atencion)
                    .HasForeignKey<AtencionOdontologica>(a => a.NroTurno)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(a => a.HistoriaClinica)
                    .WithMany(h => h.Atenciones)
                    .HasForeignKey(a => a.NroHC)
                    .OnDelete(DeleteBehavior.Restrict);

                e.Ignore(a => a.DuracionReal);
                e.Ignore(a => a.MontoTotal);
            });

            // 10. PAGOS
            modelBuilder.Entity<Pago>(e =>
            {
                e.ToTable("Pagos");
                e.HasKey(p => p.IdPago);
                e.Property(p => p.IdPago).ValueGeneratedOnAdd();
                e.Property(p => p.FechaHoraPago).HasColumnType("datetime2(0)");
                e.Property(p => p.Monto).HasPrecision(12, 2);
                e.Property(p => p.MetodoPago).IsRequired().HasMaxLength(30);
                e.Property(p => p.AportePaciente).HasPrecision(12, 2);
                e.Property(p => p.AporteObraSocial).HasPrecision(12, 2);
                e.Property(p => p.IdentificadorOS).HasMaxLength(20);

                e.HasOne(p => p.Turno)
                    .WithOne(t => t.Pago)
                    .HasForeignKey<Pago>(p => p.NroTurno)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(p => p.ObraSocial)
                    .WithMany(o => o.Pagos)
                    .HasForeignKey(p => p.IdentificadorOS)
                    .OnDelete(DeleteBehavior.SetNull);

                e.Ignore(p => p.ResponsablePago);
            });

            // 11. DETALLES INSUMOS UTILIZADOS (PK Compuesta)
            modelBuilder.Entity<DetalleInsumoUtilizado>(e =>
            {
                e.ToTable("DetallesInsumosUtilizados");
                e.HasKey(d => new { d.IdAtencion, d.IdInsumo });
                e.Property(d => d.CostoUnitarioAlMomento).HasPrecision(12, 2);

                e.HasOne(d => d.Atencion)
                    .WithMany(a => a.DetallesInsumos)
                    .HasForeignKey(d => d.IdAtencion)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(d => d.Insumo)
                    .WithMany(i => i.DetallesInsumos)
                    .HasForeignKey(d => d.IdInsumo)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 12. INSUMOS
            modelBuilder.Entity<Insumo>(e =>
            {
                e.ToTable("Insumos");
                e.HasKey(i => i.IdInsumo);
                e.Property(i => i.IdInsumo).ValueGeneratedOnAdd();
                e.Property(i => i.Nombre).IsRequired().HasMaxLength(150);
                e.HasIndex(i => i.Nombre).IsUnique();
                e.Property(i => i.CostoUnitario).HasPrecision(12, 2);
            });

            // 13. VALORACIONES
            modelBuilder.Entity<Valoracion>(e =>
            {
                e.ToTable("Valoraciones");
                e.HasKey(v => v.IdValoracion);
                e.Property(v => v.IdValoracion).ValueGeneratedOnAdd();
                e.Property(v => v.Observaciones).HasMaxLength(500);

                e.HasOne(v => v.Atencion)
                    .WithOne(a => a.Valoracion)
                    .HasForeignKey<Valoracion>(v => v.IdAtencion)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 14. PACIENTES (PK Compuesta)
            modelBuilder.Entity<Paciente>(e =>
            {
                e.ToTable("Pacientes");
                e.HasKey(p => new { p.TipoDocumento, p.NroDocumento });
                e.Property(p => p.TipoDocumento).HasMaxLength(20);
                e.Property(p => p.NroDocumento).HasMaxLength(20);
                e.Property(p => p.Nombre).IsRequired().HasMaxLength(100);
                e.Property(p => p.Apellido).IsRequired().HasMaxLength(100);
                e.Property(p => p.FechaNacimiento).HasColumnType("date");
                e.Property(p => p.Telefono).HasMaxLength(20);
                e.Property(p => p.Email).HasMaxLength(150);
                e.Property(p => p.Domicilio).HasMaxLength(150);
                e.Property(p => p.EstadoPaciente).HasMaxLength(20).HasDefaultValue("HABILITADO");
                e.Property(p => p.Clave).HasMaxLength(255);
                e.Property(p => p.SaltClave).HasMaxLength(100);
                e.Property(p => p.FechaAlta).HasColumnType("datetime2(0)");
                e.Property(p => p.Rol).HasMaxLength(20);
                e.Property(p => p.IdentificadorOS).HasMaxLength(20);
                e.Property(p => p.MontoAdeudado).HasPrecision(12, 2);

                e.HasOne(p => p.ObraSocial)
                    .WithMany(o => o.Pacientes)
                    .HasForeignKey(p => p.IdentificadorOS)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // 15. HISTORIAS CLINICAS
            modelBuilder.Entity<HistoriaClinica>(e =>
            {
                e.ToTable("HistoriasClinicas");
                e.HasKey(h => h.NroHC);
                e.Property(h => h.NroHC).ValueGeneratedOnAdd();
                e.Property(h => h.FechaCreacion).HasColumnType("date");
                e.Property(h => h.TipoDocumentoPaciente).HasMaxLength(20);
                e.Property(h => h.NroDocumentoPaciente).HasMaxLength(20);
                e.Property(h => h.AntecedentesMedicos).HasMaxLength(500);
                e.Property(h => h.Alergias).HasMaxLength(300);
                e.Property(h => h.ObservacionesGeneral).HasMaxLength(500);

                e.HasOne(h => h.Paciente)
                    .WithOne(p => p.HistoriaClinica)
                    .HasForeignKey<HistoriaClinica>(h => new { h.TipoDocumentoPaciente, h.NroDocumentoPaciente })
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // USUARIOS (Para autenticación y administración general)
            modelBuilder.Entity<Usuario>(e =>
            {
                e.ToTable("Usuarios");
                e.HasKey(u => u.Id);
                e.Property(u => u.Username).IsRequired().HasMaxLength(50);
                e.Property(u => u.Rol).IsRequired().HasMaxLength(30);
                e.HasIndex(u => u.Username).IsUnique();
            });
        }
    }
}
