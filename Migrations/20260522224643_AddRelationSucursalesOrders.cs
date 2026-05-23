using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrdersManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationSucursalesOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Presupuesto",
                table: "OrdenesServicio",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCierre",
                table: "OrdenesServicio",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "OrdenesServicio",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesServicio_SucursalId",
                table: "OrdenesServicio",
                column: "SucursalId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenesServicio_Sucursales_SucursalId",
                table: "OrdenesServicio",
                column: "SucursalId",
                principalTable: "Sucursales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrdenesServicio_Sucursales_SucursalId",
                table: "OrdenesServicio");

            migrationBuilder.DropIndex(
                name: "IX_OrdenesServicio_SucursalId",
                table: "OrdenesServicio");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "OrdenesServicio");

            migrationBuilder.AlterColumn<decimal>(
                name: "Presupuesto",
                table: "OrdenesServicio",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCierre",
                table: "OrdenesServicio",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);
        }
    }
}
