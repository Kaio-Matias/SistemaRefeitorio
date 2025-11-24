using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiRefeicoes.Migrations
{
    /// <inheritdoc />
    public partial class BiommmetriaTamplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BiometriaHash",
                table: "Colaboradores",
                newName: "BiometriaTemplate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BiometriaTemplate",
                table: "Colaboradores",
                newName: "BiometriaHash");
        }
    }
}
