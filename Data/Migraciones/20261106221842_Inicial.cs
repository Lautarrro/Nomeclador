using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nomenclador.Web.Data.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JuegosValores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PorDefecto = table.Column<bool>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    ActualizadoUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JuegosValores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Secciones",
                columns: table => new
                {
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    CoefHonorario = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CoefGasto = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Secciones", x => x.Codigo);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NombreUsuario = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    NombreCompleto = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ClaveHash = table.Column<string>(type: "TEXT", nullable: false),
                    Rol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    DebeCambiarClave = table.Column<bool>(type: "INTEGER", nullable: false),
                    SelloSeguridad = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreadoUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UltimoIngresoUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ValoresCoeficiente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JuegoValoresId = table.Column<int>(type: "INTEGER", nullable: false),
                    Clave = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Valor = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValoresCoeficiente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValoresCoeficiente_JuegosValores_JuegoValoresId",
                        column: x => x.JuegoValoresId,
                        principalTable: "JuegosValores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Grupos",
                columns: table => new
                {
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    SeccionCodigo = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grupos", x => x.Codigo);
                    table.ForeignKey(
                        name: "FK_Grupos_Secciones_SeccionCodigo",
                        column: x => x.SeccionCodigo,
                        principalTable: "Secciones",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Temas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Cuerpo = table.Column<string>(type: "TEXT", maxLength: 20000, nullable: false),
                    Categoria = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CodigoPractica = table.Column<string>(type: "TEXT", maxLength: 12, nullable: true),
                    Fijado = table.Column<bool>(type: "INTEGER", nullable: false),
                    Cerrado = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutorId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreadoUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UltimaActividadUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Temas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Temas_Usuarios_AutorId",
                        column: x => x.AutorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Practicas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false),
                    GrupoCodigo = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 1, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    UEspecialista = table.Column<decimal>(type: "TEXT", nullable: true),
                    AyudantesCantidad = table.Column<int>(type: "INTEGER", nullable: true),
                    UAyudante = table.Column<decimal>(type: "TEXT", nullable: true),
                    UAnestesista = table.Column<decimal>(type: "TEXT", nullable: true),
                    UGasto = table.Column<decimal>(type: "TEXT", nullable: true),
                    CoefGastoOverride = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Nota = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    TextoBusqueda = table.Column<string>(type: "TEXT", maxLength: 6000, nullable: false),
                    EditadaPorAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    ActualizadoUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Practicas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Practicas_Grupos_GrupoCodigo",
                        column: x => x.GrupoCodigo,
                        principalTable: "Grupos",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Respuestas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TemaForoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Cuerpo = table.Column<string>(type: "TEXT", maxLength: 20000, nullable: false),
                    AutorId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreadoUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Respuestas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Respuestas_Temas_TemaForoId",
                        column: x => x.TemaForoId,
                        principalTable: "Temas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Respuestas_Usuarios_AutorId",
                        column: x => x.AutorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Grupos_SeccionCodigo",
                table: "Grupos",
                column: "SeccionCodigo");

            migrationBuilder.CreateIndex(
                name: "IX_Practicas_Codigo",
                table: "Practicas",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Practicas_GrupoCodigo",
                table: "Practicas",
                column: "GrupoCodigo");

            migrationBuilder.CreateIndex(
                name: "IX_Respuestas_AutorId",
                table: "Respuestas",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Respuestas_TemaForoId",
                table: "Respuestas",
                column: "TemaForoId");

            migrationBuilder.CreateIndex(
                name: "IX_Temas_AutorId",
                table: "Temas",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Temas_UltimaActividadUtc",
                table: "Temas",
                column: "UltimaActividadUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_NombreUsuario",
                table: "Usuarios",
                column: "NombreUsuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValoresCoeficiente_JuegoValoresId_Clave",
                table: "ValoresCoeficiente",
                columns: new[] { "JuegoValoresId", "Clave" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Practicas");

            migrationBuilder.DropTable(
                name: "Respuestas");

            migrationBuilder.DropTable(
                name: "ValoresCoeficiente");

            migrationBuilder.DropTable(
                name: "Grupos");

            migrationBuilder.DropTable(
                name: "Temas");

            migrationBuilder.DropTable(
                name: "JuegosValores");

            migrationBuilder.DropTable(
                name: "Secciones");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
