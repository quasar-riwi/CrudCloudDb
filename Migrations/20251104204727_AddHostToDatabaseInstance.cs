using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrudCloud.api.Migrations
{
    /// <inheritdoc />
    public partial class AddHostToDatabaseInstance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Host",
                table: "DatabaseInstances",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Host",
                table: "DatabaseInstances");
        }
    }
}
