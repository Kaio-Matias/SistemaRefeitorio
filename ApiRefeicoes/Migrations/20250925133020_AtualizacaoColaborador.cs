using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiRefeicoes.Migrations
{
    /// <inheritdoc />
    public partial class AtualizacaoColaborador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RegistrosRefeicoes_Colaboradores_ColaboradorId",
                table: "RegistrosRefeicoes");

            migrationBuilder.DropIndex(
                name: "IX_Dispositivos_UsuarioId_DeviceIdentifier",
                table: "Dispositivos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RegistrosRefeicoes",
                table: "RegistrosRefeicoes");

            migrationBuilder.RenameTable(
                name: "RegistrosRefeicoes",
                newName: "RegistroRefeicoes");

            migrationBuilder.RenameColumn(
                name: "AzureId",
                table: "Colaboradores",
                newName: "FotoBase64");

            migrationBuilder.RenameIndex(
                name: "IX_RegistrosRefeicoes_ColaboradorId",
                table: "RegistroRefeicoes",
                newName: "IX_RegistroRefeicoes_ColaboradorId");

            migrationBuilder.AlterColumn<string>(
                name: "DeviceIdentifier",
                table: "Dispositivos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<Guid>(
                name: "AzurePersonId",
                table: "Colaboradores",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RegistroRefeicoes",
                table: "RegistroRefeicoes",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Cardapios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NomeArquivo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CaminhoArquivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataUpload = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cardapios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dispositivos_UsuarioId",
                table: "Dispositivos",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_RegistroRefeicoes_Colaboradores_ColaboradorId",
                table: "RegistroRefeicoes",
                column: "ColaboradorId",
                principalTable: "Colaboradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RegistroRefeicoes_Colaboradores_ColaboradorId",
                table: "RegistroRefeicoes");

            migrationBuilder.DropTable(
                name: "Cardapios");

            migrationBuilder.DropIndex(
                name: "IX_Dispositivos_UsuarioId",
                table: "Dispositivos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RegistroRefeicoes",
                table: "RegistroRefeicoes");

            migrationBuilder.DropColumn(
                name: "AzurePersonId",
                table: "Colaboradores");

            migrationBuilder.RenameTable(
                name: "RegistroRefeicoes",
                newName: "RegistrosRefeicoes");

            migrationBuilder.RenameColumn(
                name: "FotoBase64",
                table: "Colaboradores",
                newName: "AzureId");

            migrationBuilder.RenameIndex(
                name: "IX_RegistroRefeicoes_ColaboradorId",
                table: "RegistrosRefeicoes",
                newName: "IX_RegistrosRefeicoes_ColaboradorId");

            migrationBuilder.AlterColumn<string>(
                name: "DeviceIdentifier",
                table: "Dispositivos",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RegistrosRefeicoes",
                table: "RegistrosRefeicoes",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Dispositivos_UsuarioId_DeviceIdentifier",
                table: "Dispositivos",
                columns: new[] { "UsuarioId", "DeviceIdentifier" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RegistrosRefeicoes_Colaboradores_ColaboradorId",
                table: "RegistrosRefeicoes",
                column: "ColaboradorId",
                principalTable: "Colaboradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
