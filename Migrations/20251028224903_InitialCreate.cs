using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CrudCloud.api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // La tabla AuditLogs no necesita cambios
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Accion = table.Column<string>(type: "text", nullable: false),
                    Entidad = table.Column<string>(type: "text", nullable: false),
                    Detalle = table.Column<string>(type: "text", nullable: true),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            // --- SECCIÓN CORREGIDA ---
            migrationBuilder.CreateTable(
                name: "DatabaseInstances", // El nombre de la tabla ya era correcto
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Motor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false), // Mejora: Longitud definida
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    
                    // ✅ CORRECCIÓN 1: El nombre de la columna ahora es 'UserId'
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    
                    UsuarioDb = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Contraseña = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Puerto = table.Column<int>(type: "integer", nullable: false),
                    Host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false), // Host añadido que faltaba
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseInstances", x => x.Id);
                    
                    // ✅ CORRECCIÓN 2: Se añade la definición de la Clave Externa (ForeignKey)
                    // Esto le dice a la BD que 'UserId' en esta tabla se conecta con 'Id' en la tabla 'Users'.
                    table.ForeignKey(
                        name: "FK_DatabaseInstances_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users", // Asegúrate de que tu tabla de usuarios se llame 'Users'
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade); // Cascade significa que si borras un usuario, sus instancias se borran también.
                });

            // ✅ CORRECCIÓN 3: Añadimos un índice a la clave externa para mejorar el rendimiento de las búsquedas.
            migrationBuilder.CreateIndex(
                name: "IX_DatabaseInstances_UserId",
                table: "DatabaseInstances",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "DatabaseInstances");
        }
    }
}