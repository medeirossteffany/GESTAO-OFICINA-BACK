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
            // Remove o índice antigo (TenantId, Email)
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_TenantId_Email",
                table: "AspNetUsers");

            // Adiciona índice único para Email globalmente
            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Email",
                table: "AspNetUsers",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            // Adiciona índice único para PhoneNumber globalmente
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
            // Remove os índices novos
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
