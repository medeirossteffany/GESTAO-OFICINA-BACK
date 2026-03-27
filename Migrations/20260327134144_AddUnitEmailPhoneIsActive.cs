using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoOficina.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitEmailPhoneIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceOrderTimelines_AspNetUsers_UserId",
                table: "ServiceOrderTimelines");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "UserUnits",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Units",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Units",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Units",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ServiceOrderTimelines",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ServiceOrderParts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "CustomerUnits",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceOrderTimelines_AspNetUsers_UserId",
                table: "ServiceOrderTimelines",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceOrderTimelines_AspNetUsers_UserId",
                table: "ServiceOrderTimelines");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "UserUnits");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ServiceOrderTimelines");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ServiceOrderParts");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "CustomerUnits");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceOrderTimelines_AspNetUsers_UserId",
                table: "ServiceOrderTimelines",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
