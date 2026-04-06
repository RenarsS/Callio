using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Callio.Provisioning.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Provisioning_InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "provisioning");

            migrationBuilder.CreateTable(
                name: "TenantInfrastructureProvisionings",
                schema: "provisioning",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    TenantRequestId = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    DatabaseSchema = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    VectorStoreNamespace = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FailedStep = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastStartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastCompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantInfrastructureProvisionings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantKnowledgeBaseSettings",
                schema: "provisioning",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    DatabaseSchema = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    VectorStoreNamespace = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EmbeddingProvider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmbeddingModel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ChunkSize = table.Column<int>(type: "int", nullable: false),
                    ChunkOverlap = table.Column<int>(type: "int", nullable: false),
                    RetrievalTopK = table.Column<int>(type: "int", nullable: false),
                    IngestionEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RetrievalEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantKnowledgeBaseSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantInfrastructureProvisioningSteps",
                schema: "provisioning",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantInfrastructureProvisioningId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastStartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastCompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantInfrastructureProvisioningSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantInfrastructureProvisioningSteps_TenantInfrastructureProvisionings_TenantInfrastructureProvisioningId",
                        column: x => x.TenantInfrastructureProvisioningId,
                        principalSchema: "provisioning",
                        principalTable: "TenantInfrastructureProvisionings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantInfrastructureProvisionings_TenantId",
                schema: "provisioning",
                table: "TenantInfrastructureProvisionings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantInfrastructureProvisioningSteps_TenantInfrastructureProvisioningId_Name",
                schema: "provisioning",
                table: "TenantInfrastructureProvisioningSteps",
                columns: new[] { "TenantInfrastructureProvisioningId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantKnowledgeBaseSettings_TenantId",
                schema: "provisioning",
                table: "TenantKnowledgeBaseSettings",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantInfrastructureProvisioningSteps",
                schema: "provisioning");

            migrationBuilder.DropTable(
                name: "TenantKnowledgeBaseSettings",
                schema: "provisioning");

            migrationBuilder.DropTable(
                name: "TenantInfrastructureProvisionings",
                schema: "provisioning");
        }
    }
}
