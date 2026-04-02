using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoOficina.Migrations
{
    /// <inheritdoc />
    public partial class AddMechanicsFieldsToServiceOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MechanicsDescription",
                table: "ServiceOrders",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "MechanicsValue",
                table: "ServiceOrders",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MechanicsDescription",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "MechanicsValue",
                table: "ServiceOrders");
        }
    }
}
