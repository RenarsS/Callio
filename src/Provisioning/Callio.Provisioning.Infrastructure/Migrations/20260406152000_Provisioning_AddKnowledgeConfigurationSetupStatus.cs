using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Callio.Provisioning.Infrastructure.Migrations
{
    public partial class Provisioning_AddKnowledgeConfigurationSetupStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantKnowledgeConfigurationSetups",
                schema: "provisioning",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    ActiveConfigurationId = table.Column<int>(type: "int", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastStartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastCompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantKnowledgeConfigurationSetups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantKnowledgeConfigurationSetups_TenantId",
                schema: "provisioning",
                table: "TenantKnowledgeConfigurationSetups",
                column: "TenantId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantKnowledgeConfigurationSetups",
                schema: "provisioning");
        }
    }
}
