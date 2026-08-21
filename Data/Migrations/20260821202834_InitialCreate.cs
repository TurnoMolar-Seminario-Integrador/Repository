using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DisponibilidadesHorarias",
                columns: table => new
                {
                    CodDisponibilidad = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiaSemana = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time", nullable: false),
                    HoraFin = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisponibilidadesHorarias", x => x.CodDisponibilidad);
                });

            migrationBuilder.CreateTable(
                name: "Especialidades",
                columns: table => new
                {
                    CodEspecialidad = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ArancelParticular = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Especialidades", x => x.CodEspecialidad);
                });

            migrationBuilder.CreateTable(
                name: "Insumos",
                columns: table => new
                {
                    CodInsumo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StockDisponible = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Insumos", x => x.CodInsumo);
                });

            migrationBuilder.CreateTable(
                name: "Multas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Monto = table.Column<float>(type: "real", nullable: false),
                    EstadoPago = table.Column<bool>(type: "bit", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Multas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ObrasSociales",
                columns: table => new
                {
                    IdentificadorOS = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreOS = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PlanCobertura = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ArancelOS = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EstadoOS = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "ACTIVA")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObrasSociales", x => x.IdentificadorOS);
                });

            migrationBuilder.CreateTable(
                name: "Personas",
                columns: table => new
                {
                    TipoDocumento = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    NroDocumento = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaNacimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Domicilio = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personas", x => new { x.TipoDocumento, x.NroDocumento });
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NombreCompleto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    EntidadId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Odontologos",
                columns: table => new
                {
                    TipoDocumento = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    NroDocumento = table.Column<int>(type: "int", nullable: false),
                    Matricula = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EstadoOdontologo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "ACTIVO"),
                    CodDisponibilidad = table.Column<int>(type: "int", nullable: true),
                    CodEspecialidad = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Odontologos", x => new { x.TipoDocumento, x.NroDocumento });
                    table.ForeignKey(
                        name: "FK_Odontologos_DisponibilidadesHorarias_CodDisponibilidad",
                        column: x => x.CodDisponibilidad,
                        principalTable: "DisponibilidadesHorarias",
                        principalColumn: "CodDisponibilidad",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Odontologos_Especialidades_CodEspecialidad",
                        column: x => x.CodEspecialidad,
                        principalTable: "Especialidades",
                        principalColumn: "CodEspecialidad",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Odontologos_Personas_TipoDocumento_NroDocumento",
                        columns: x => new { x.TipoDocumento, x.NroDocumento },
                        principalTable: "Personas",
                        principalColumns: new[] { "TipoDocumento", "NroDocumento" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pacientes",
                columns: table => new
                {
                    TipoDocumento = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    NroDocumento = table.Column<int>(type: "int", nullable: false),
                    EstadoPaciente = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "HABILITADO"),
                    MontoAdeudado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IdentificadorOS = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pacientes", x => new { x.TipoDocumento, x.NroDocumento });
                    table.ForeignKey(
                        name: "FK_Pacientes_ObrasSociales_IdentificadorOS",
                        column: x => x.IdentificadorOS,
                        principalTable: "ObrasSociales",
                        principalColumn: "IdentificadorOS",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Pacientes_Personas_TipoDocumento_NroDocumento",
                        columns: x => new { x.TipoDocumento, x.NroDocumento },
                        principalTable: "Personas",
                        principalColumns: new[] { "TipoDocumento", "NroDocumento" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistoriasClinicas",
                columns: table => new
                {
                    NroHC = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PacienteTipoDoc = table.Column<string>(type: "nvarchar(15)", nullable: false),
                    PacienteNroDoc = table.Column<int>(type: "int", nullable: false),
                    AntecedentesMedicos = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Alergias = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ObservacionesGeneral = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoriasClinicas", x => x.NroHC);
                    table.ForeignKey(
                        name: "FK_HistoriasClinicas_Pacientes_PacienteTipoDoc_PacienteNroDoc",
                        columns: x => new { x.PacienteTipoDoc, x.PacienteNroDoc },
                        principalTable: "Pacientes",
                        principalColumns: new[] { "TipoDocumento", "NroDocumento" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Turnos",
                columns: table => new
                {
                    CodTurno = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaYHoraReserva = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModalidadPagoElegida = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaYHoraSolicitudReprogramacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaYHoraCancelacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoCancelacion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CodEspecialidad = table.Column<int>(type: "int", nullable: false),
                    OdontologoTipoDoc = table.Column<string>(type: "nvarchar(15)", nullable: false),
                    OdontologoNroDoc = table.Column<int>(type: "int", nullable: false),
                    PacienteTipoDoc = table.Column<string>(type: "nvarchar(15)", nullable: false),
                    PacienteNroDoc = table.Column<int>(type: "int", nullable: false),
                    TurnoOriginalCod = table.Column<int>(type: "int", nullable: true),
                    TurnoOriginalFecha = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Discriminator = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turnos", x => x.CodTurno);
                    table.ForeignKey(
                        name: "FK_Turnos_Especialidades_CodEspecialidad",
                        column: x => x.CodEspecialidad,
                        principalTable: "Especialidades",
                        principalColumn: "CodEspecialidad",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Turnos_Odontologos_OdontologoTipoDoc_OdontologoNroDoc",
                        columns: x => new { x.OdontologoTipoDoc, x.OdontologoNroDoc },
                        principalTable: "Odontologos",
                        principalColumns: new[] { "TipoDocumento", "NroDocumento" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Turnos_Pacientes_PacienteTipoDoc_PacienteNroDoc",
                        columns: x => new { x.PacienteTipoDoc, x.PacienteNroDoc },
                        principalTable: "Pacientes",
                        principalColumns: new[] { "TipoDocumento", "NroDocumento" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Turnos_Turnos_TurnoOriginalCod",
                        column: x => x.TurnoOriginalCod,
                        principalTable: "Turnos",
                        principalColumn: "CodTurno");
                });

            migrationBuilder.CreateTable(
                name: "AtencionesMedicas",
                columns: table => new
                {
                    CodAtencion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaYHoraAtencionInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaYHoraAtencionFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CodTurno = table.Column<int>(type: "int", nullable: false),
                    FechaYHoraReserva = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NroHC = table.Column<int>(type: "int", nullable: false),
                    PacienteTipoDoc = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    PacienteNroDoc = table.Column<int>(type: "int", nullable: false),
                    Discriminator = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtencionesMedicas", x => x.CodAtencion);
                    table.ForeignKey(
                        name: "FK_AtencionesMedicas_HistoriasClinicas_NroHC",
                        column: x => x.NroHC,
                        principalTable: "HistoriasClinicas",
                        principalColumn: "NroHC",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AtencionesMedicas_Turnos_CodTurno",
                        column: x => x.CodTurno,
                        principalTable: "Turnos",
                        principalColumn: "CodTurno",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Comprobantes",
                columns: table => new
                {
                    NroComprobante = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodTurno = table.Column<int>(type: "int", nullable: false),
                    FechaYHoraReserva = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaYHoraEmision = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comprobantes", x => x.NroComprobante);
                    table.ForeignKey(
                        name: "FK_Comprobantes_Turnos_CodTurno",
                        column: x => x.CodTurno,
                        principalTable: "Turnos",
                        principalColumn: "CodTurno",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DetallesInsumos",
                columns: table => new
                {
                    CodInsumo = table.Column<int>(type: "int", nullable: false),
                    CodAtencion = table.Column<int>(type: "int", nullable: false),
                    CantidadUtilizada = table.Column<int>(type: "int", nullable: false),
                    CostoUnitarioAlMomento = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesInsumos", x => new { x.CodInsumo, x.CodAtencion });
                    table.ForeignKey(
                        name: "FK_DetallesInsumos_AtencionesMedicas_CodAtencion",
                        column: x => x.CodAtencion,
                        principalTable: "AtencionesMedicas",
                        principalColumn: "CodAtencion",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesInsumos_Insumos_CodInsumo",
                        column: x => x.CodInsumo,
                        principalTable: "Insumos",
                        principalColumn: "CodInsumo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    CodPago = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodAtencion = table.Column<int>(type: "int", nullable: false),
                    FechaYHoraPago = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TipoMetodoPago = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AportePaciente = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AporteObraSocial = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Discriminator = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.CodPago);
                    table.ForeignKey(
                        name: "FK_Pagos_AtencionesMedicas_CodAtencion",
                        column: x => x.CodAtencion,
                        principalTable: "AtencionesMedicas",
                        principalColumn: "CodAtencion",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Valoraciones",
                columns: table => new
                {
                    CodValoracion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Calificacion = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CodAtencion = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Valoraciones", x => x.CodValoracion);
                    table.ForeignKey(
                        name: "FK_Valoraciones_AtencionesMedicas_CodAtencion",
                        column: x => x.CodAtencion,
                        principalTable: "AtencionesMedicas",
                        principalColumn: "CodAtencion",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DisponibilidadesHorarias",
                columns: new[] { "CodDisponibilidad", "DiaSemana", "HoraFin", "HoraInicio" },
                values: new object[,]
                {
                    { 1, "Lunes", new TimeOnly(16, 0, 0), new TimeOnly(8, 0, 0) },
                    { 2, "Martes", new TimeOnly(17, 0, 0), new TimeOnly(9, 0, 0) },
                    { 3, "Miércoles", new TimeOnly(18, 0, 0), new TimeOnly(10, 0, 0) },
                    { 4, "Jueves", new TimeOnly(14, 0, 0), new TimeOnly(8, 0, 0) },
                    { 5, "Viernes", new TimeOnly(13, 0, 0), new TimeOnly(9, 0, 0) }
                });

            migrationBuilder.InsertData(
                table: "Especialidades",
                columns: new[] { "CodEspecialidad", "ArancelParticular", "Nombre" },
                values: new object[,]
                {
                    { 1, 15000m, "Odontología General" },
                    { 2, 35000m, "Endodoncia" },
                    { 3, 50000m, "Ortodoncia" },
                    { 4, 80000m, "Cirugía e Implantes" },
                    { 5, 12000m, "Odontopediatría" }
                });

            migrationBuilder.InsertData(
                table: "Insumos",
                columns: new[] { "CodInsumo", "CostoUnitario", "Nombre", "StockDisponible" },
                values: new object[,]
                {
                    { 1, 2500m, "Kit de Anestesia Local (Mepivacaína)", 120 },
                    { 2, 6800m, "Resina Compuesta Fotocurable", 45 },
                    { 3, 1500m, "Película Radiográfica Periapical", 200 },
                    { 4, 400m, "Guantes de Látex Descartables (Par)", 500 },
                    { 5, 300m, "Babero y Eyector Descartable", 350 },
                    { 6, 1200m, "Pasta para Profilaxis Dental", 60 },
                    { 7, 4500m, "Conos de Gutapercha Endodoncia", 30 }
                });

            migrationBuilder.InsertData(
                table: "ObrasSociales",
                columns: new[] { "IdentificadorOS", "ArancelOS", "EstadoOS", "NombreOS", "PlanCobertura" },
                values: new object[,]
                {
                    { 1, 18000m, "ACTIVA", "OSDE", "Plan 210 / 310" },
                    { 2, 22000m, "ACTIVA", "Swiss Medical", "Black / Gold" },
                    { 3, 12000m, "ACTIVA", "IOMA", "Afiliados Obligatorios" },
                    { 4, 0m, "ACTIVA", "Particular", "Sin cobertura" }
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "Activo", "Email", "EntidadId", "NombreCompleto", "PasswordHash", "Rol", "Username" },
                values: new object[,]
                {
                    { 1, true, "admin@turnomolar.com", null, "Administrador Principal", "admin123", "Admin", "admin" },
                    { 2, true, "recepcion@turnomolar.com", null, "María López", "recepcion123", "Recepcionista", "recepcion" },
                    { 3, true, "mgomez@turnomolar.com", null, "Dr. Martín Gómez", "doc123", "Odontologo", "doctor1" },
                    { 4, true, "lrossi@turnomolar.com", null, "Dra. Laura Rossi", "doc123", "Odontologo", "doctor2" },
                    { 5, true, "juan.perez@gmail.com", null, "Juan Pérez", "paciente123", "Paciente", "paciente1" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AtencionesMedicas_CodTurno",
                table: "AtencionesMedicas",
                column: "CodTurno",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AtencionesMedicas_NroHC",
                table: "AtencionesMedicas",
                column: "NroHC");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_CodTurno",
                table: "Comprobantes",
                column: "CodTurno",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetallesInsumos_CodAtencion",
                table: "DetallesInsumos",
                column: "CodAtencion");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriasClinicas_PacienteTipoDoc_PacienteNroDoc",
                table: "HistoriasClinicas",
                columns: new[] { "PacienteTipoDoc", "PacienteNroDoc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Odontologos_CodDisponibilidad",
                table: "Odontologos",
                column: "CodDisponibilidad");

            migrationBuilder.CreateIndex(
                name: "IX_Odontologos_CodEspecialidad",
                table: "Odontologos",
                column: "CodEspecialidad");

            migrationBuilder.CreateIndex(
                name: "IX_Pacientes_IdentificadorOS",
                table: "Pacientes",
                column: "IdentificadorOS");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_CodAtencion",
                table: "Pagos",
                column: "CodAtencion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_CodEspecialidad",
                table: "Turnos",
                column: "CodEspecialidad");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_OdontologoTipoDoc_OdontologoNroDoc",
                table: "Turnos",
                columns: new[] { "OdontologoTipoDoc", "OdontologoNroDoc" });

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_PacienteTipoDoc_PacienteNroDoc",
                table: "Turnos",
                columns: new[] { "PacienteTipoDoc", "PacienteNroDoc" });

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_TurnoOriginalCod",
                table: "Turnos",
                column: "TurnoOriginalCod");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Username",
                table: "Usuarios",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Valoraciones_CodAtencion",
                table: "Valoraciones",
                column: "CodAtencion",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Comprobantes");

            migrationBuilder.DropTable(
                name: "DetallesInsumos");

            migrationBuilder.DropTable(
                name: "Multas");

            migrationBuilder.DropTable(
                name: "Pagos");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Valoraciones");

            migrationBuilder.DropTable(
                name: "Insumos");

            migrationBuilder.DropTable(
                name: "AtencionesMedicas");

            migrationBuilder.DropTable(
                name: "HistoriasClinicas");

            migrationBuilder.DropTable(
                name: "Turnos");

            migrationBuilder.DropTable(
                name: "Odontologos");

            migrationBuilder.DropTable(
                name: "Pacientes");

            migrationBuilder.DropTable(
                name: "DisponibilidadesHorarias");

            migrationBuilder.DropTable(
                name: "Especialidades");

            migrationBuilder.DropTable(
                name: "ObrasSociales");

            migrationBuilder.DropTable(
                name: "Personas");
        }
    }
}
