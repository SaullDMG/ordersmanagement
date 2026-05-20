using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrdersManagement.Migrations
{
    /// <inheritdoc />
    public partial class ModelUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrdenesServicio_Tecnicos_TecnicoId",
                table: "OrdenesServicio");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Tecnicos_TecnicoId",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "Tecnicos");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_TecnicoId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TecnicoId",
                table: "Usuarios");

            migrationBuilder.RenameColumn(
                name: "correo",
                table: "Usuarios",
                newName: "Correo");

            migrationBuilder.RenameColumn(
                name: "TecnicoId",
                table: "OrdenesServicio",
                newName: "UsuarioId");

            migrationBuilder.RenameIndex(
                name: "IX_OrdenesServicio_TecnicoId",
                table: "OrdenesServicio",
                newName: "IX_OrdenesServicio_UsuarioId");

            migrationBuilder.AlterColumn<string>(
                name: "Contraseña",
                table: "Usuarios",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Especialidad",
                table: "Usuarios",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "Usuarios",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenesServicio_Usuarios_UsuarioId",
                table: "OrdenesServicio",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "UsuarioId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrdenesServicio_Usuarios_UsuarioId",
                table: "OrdenesServicio");

            migrationBuilder.DropColumn(
                name: "Especialidad",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "Usuarios");

            migrationBuilder.RenameColumn(
                name: "Correo",
                table: "Usuarios",
                newName: "correo");

            migrationBuilder.RenameColumn(
                name: "UsuarioId",
                table: "OrdenesServicio",
                newName: "TecnicoId");

            migrationBuilder.RenameIndex(
                name: "IX_OrdenesServicio_UsuarioId",
                table: "OrdenesServicio",
                newName: "IX_OrdenesServicio_TecnicoId");

            migrationBuilder.AlterColumn<string>(
                name: "Contraseña",
                table: "Usuarios",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "TecnicoId",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Tecnicos",
                columns: table => new
                {
                    TecnicoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Especialidad = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telefono = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tecnicos", x => x.TecnicoId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_TecnicoId",
                table: "Usuarios",
                column: "TecnicoId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenesServicio_Tecnicos_TecnicoId",
                table: "OrdenesServicio",
                column: "TecnicoId",
                principalTable: "Tecnicos",
                principalColumn: "TecnicoId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Tecnicos_TecnicoId",
                table: "Usuarios",
                column: "TecnicoId",
                principalTable: "Tecnicos",
                principalColumn: "TecnicoId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
