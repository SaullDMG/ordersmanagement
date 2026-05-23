using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrdersManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaInicio",
                table: "OrdenesServicio",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaInicio",
                table: "OrdenesServicio");
        }
    }
}
