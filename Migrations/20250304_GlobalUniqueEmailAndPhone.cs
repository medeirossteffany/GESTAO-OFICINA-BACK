using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoOficina.Migrations
{
    /// <inheritdoc />
    public partial class GlobalUniqueEmailAndPhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_TenantId_Email",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Email",
                table: "AspNetUsers",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PhoneNumber",
                table: "AspNetUsers",
                column: "PhoneNumber",
                unique: true,
                filter: "[PhoneNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Email",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PhoneNumber",
                table: "AspNetUsers");

            // Restaura o índice antigo
            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_TenantId_Email",
                table: "AspNetUsers",
                columns: new[] { "TenantId", "Email" },
                unique: true);
        }
    }
}
