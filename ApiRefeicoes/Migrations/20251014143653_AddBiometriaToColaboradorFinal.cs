using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiRefeicoes.Migrations
{
    /// <inheritdoc />
    public partial class AddBiometriaToColaboradorFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cardapios");

            migrationBuilder.AddColumn<byte[]>(
                name: "BiometriaHash",
                table: "Colaboradores",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceIdentifier",
                table: "Colaboradores",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BiometriaHash",
                table: "Colaboradores");

            migrationBuilder.DropColumn(
                name: "DeviceIdentifier",
                table: "Colaboradores");

            migrationBuilder.CreateTable(
                name: "Cardapios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaminhoArquivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataUpload = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NomeArquivo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cardapios", x => x.Id);
                });
        }
    }
}
