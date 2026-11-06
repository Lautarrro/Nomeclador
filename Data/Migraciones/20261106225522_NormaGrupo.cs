using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nomenclador.Web.Data.Migraciones
{
    /// <inheritdoc />
    public partial class NormaGrupo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nota",
                table: "Grupos",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nota",
                table: "Grupos");
        }
    }
}
