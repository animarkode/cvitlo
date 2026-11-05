using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faber.Modules.Auth.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingAccountErasures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pending_account_erasures",
                schema: "auth",
                columns: table => new
                {
                    subject = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pending_account_erasures", x => x.subject);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pending_account_erasures_requested_at",
                schema: "auth",
                table: "pending_account_erasures",
                column: "requested_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pending_account_erasures",
                schema: "auth");
        }
    }
}
