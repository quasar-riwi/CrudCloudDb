using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrudCloud.api.Migrations
{
    /// <inheritdoc />
    public partial class RenameUsuarioIdToUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DatabaseInstances_Users_UsuarioId",
                table: "DatabaseInstances");

            migrationBuilder.RenameColumn(
                name: "UsuarioId",
                table: "DatabaseInstances",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_DatabaseInstances_UsuarioId",
                table: "DatabaseInstances",
                newName: "IX_DatabaseInstances_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DatabaseInstances_Users_UserId",
                table: "DatabaseInstances",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DatabaseInstances_Users_UserId",
                table: "DatabaseInstances");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "DatabaseInstances",
                newName: "UsuarioId");

            migrationBuilder.RenameIndex(
                name: "IX_DatabaseInstances_UserId",
                table: "DatabaseInstances",
                newName: "IX_DatabaseInstances_UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_DatabaseInstances_Users_UsuarioId",
                table: "DatabaseInstances",
                column: "UsuarioId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
