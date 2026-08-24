using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class ModelDERUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pagos_AtencionesMedicas_CodAtencion",
                table: "Pagos");

            migrationBuilder.DropIndex(
                name: "IX_Pagos_CodAtencion",
                table: "Pagos");

            migrationBuilder.AlterColumn<int>(
                name: "CodAtencion",
                table: "Pagos",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "IdentificadorOS",
                table: "Pagos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NroTurno",
                table: "Pagos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CodEspecialidad",
                table: "DisponibilidadesHorarias",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OdontologoNroDoc",
                table: "DisponibilidadesHorarias",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OdontologoTipoDoc",
                table: "DisponibilidadesHorarias",
                type: "nvarchar(15)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ArancelAplicado",
                table: "AtencionesMedicas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "DisponibilidadesHorarias",
                keyColumn: "CodDisponibilidad",
                keyValue: 1,
                columns: new[] { "CodEspecialidad", "OdontologoNroDoc", "OdontologoTipoDoc" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "DisponibilidadesHorarias",
                keyColumn: "CodDisponibilidad",
                keyValue: 2,
                columns: new[] { "CodEspecialidad", "OdontologoNroDoc", "OdontologoTipoDoc" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "DisponibilidadesHorarias",
                keyColumn: "CodDisponibilidad",
                keyValue: 3,
                columns: new[] { "CodEspecialidad", "OdontologoNroDoc", "OdontologoTipoDoc" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "DisponibilidadesHorarias",
                keyColumn: "CodDisponibilidad",
                keyValue: 4,
                columns: new[] { "CodEspecialidad", "OdontologoNroDoc", "OdontologoTipoDoc" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "DisponibilidadesHorarias",
                keyColumn: "CodDisponibilidad",
                keyValue: 5,
                columns: new[] { "CodEspecialidad", "OdontologoNroDoc", "OdontologoTipoDoc" },
                values: new object[] { null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_CodAtencion",
                table: "Pagos",
                column: "CodAtencion",
                unique: true,
                filter: "[CodAtencion] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_IdentificadorOS",
                table: "Pagos",
                column: "IdentificadorOS");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_NroTurno",
                table: "Pagos",
                column: "NroTurno");

            migrationBuilder.CreateIndex(
                name: "IX_DisponibilidadesHorarias_CodEspecialidad",
                table: "DisponibilidadesHorarias",
                column: "CodEspecialidad");

            migrationBuilder.CreateIndex(
                name: "IX_DisponibilidadesHorarias_OdontologoTipoDoc_OdontologoNroDoc",
                table: "DisponibilidadesHorarias",
                columns: new[] { "OdontologoTipoDoc", "OdontologoNroDoc" });

            migrationBuilder.AddForeignKey(
                name: "FK_DisponibilidadesHorarias_Especialidades_CodEspecialidad",
                table: "DisponibilidadesHorarias",
                column: "CodEspecialidad",
                principalTable: "Especialidades",
                principalColumn: "CodEspecialidad",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DisponibilidadesHorarias_Odontologos_OdontologoTipoDoc_OdontologoNroDoc",
                table: "DisponibilidadesHorarias",
                columns: new[] { "OdontologoTipoDoc", "OdontologoNroDoc" },
                principalTable: "Odontologos",
                principalColumns: new[] { "TipoDocumento", "NroDocumento" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pagos_AtencionesMedicas_CodAtencion",
                table: "Pagos",
                column: "CodAtencion",
                principalTable: "AtencionesMedicas",
                principalColumn: "CodAtencion",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Pagos_ObrasSociales_IdentificadorOS",
                table: "Pagos",
                column: "IdentificadorOS",
                principalTable: "ObrasSociales",
                principalColumn: "IdentificadorOS",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Pagos_Turnos_NroTurno",
                table: "Pagos",
                column: "NroTurno",
                principalTable: "Turnos",
                principalColumn: "CodTurno",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DisponibilidadesHorarias_Especialidades_CodEspecialidad",
                table: "DisponibilidadesHorarias");

            migrationBuilder.DropForeignKey(
                name: "FK_DisponibilidadesHorarias_Odontologos_OdontologoTipoDoc_OdontologoNroDoc",
                table: "DisponibilidadesHorarias");

            migrationBuilder.DropForeignKey(
                name: "FK_Pagos_AtencionesMedicas_CodAtencion",
                table: "Pagos");

            migrationBuilder.DropForeignKey(
                name: "FK_Pagos_ObrasSociales_IdentificadorOS",
                table: "Pagos");

            migrationBuilder.DropForeignKey(
                name: "FK_Pagos_Turnos_NroTurno",
                table: "Pagos");

            migrationBuilder.DropIndex(
                name: "IX_Pagos_CodAtencion",
                table: "Pagos");

            migrationBuilder.DropIndex(
                name: "IX_Pagos_IdentificadorOS",
                table: "Pagos");

            migrationBuilder.DropIndex(
                name: "IX_Pagos_NroTurno",
                table: "Pagos");

            migrationBuilder.DropIndex(
                name: "IX_DisponibilidadesHorarias_CodEspecialidad",
                table: "DisponibilidadesHorarias");

            migrationBuilder.DropIndex(
                name: "IX_DisponibilidadesHorarias_OdontologoTipoDoc_OdontologoNroDoc",
                table: "DisponibilidadesHorarias");

            migrationBuilder.DropColumn(
                name: "IdentificadorOS",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "NroTurno",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "CodEspecialidad",
                table: "DisponibilidadesHorarias");

            migrationBuilder.DropColumn(
                name: "OdontologoNroDoc",
                table: "DisponibilidadesHorarias");

            migrationBuilder.DropColumn(
                name: "OdontologoTipoDoc",
                table: "DisponibilidadesHorarias");

            migrationBuilder.DropColumn(
                name: "ArancelAplicado",
                table: "AtencionesMedicas");

            migrationBuilder.AlterColumn<int>(
                name: "CodAtencion",
                table: "Pagos",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_CodAtencion",
                table: "Pagos",
                column: "CodAtencion",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Pagos_AtencionesMedicas_CodAtencion",
                table: "Pagos",
                column: "CodAtencion",
                principalTable: "AtencionesMedicas",
                principalColumn: "CodAtencion",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
