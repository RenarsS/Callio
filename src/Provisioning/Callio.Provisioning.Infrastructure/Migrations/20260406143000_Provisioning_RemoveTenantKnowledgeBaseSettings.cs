using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Callio.Provisioning.Infrastructure.Migrations
{
    public partial class Provisioning_RemoveTenantKnowledgeBaseSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantKnowledgeBaseSettings",
                schema: "provisioning");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantKnowledgeBaseSettings",
                schema: "provisioning",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChunkOverlap = table.Column<int>(type: "int", nullable: false),
                    ChunkSize = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DatabaseSchema = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EmbeddingModel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EmbeddingProvider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IngestionEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RetrievalEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RetrievalTopK = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VectorStoreNamespace = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantKnowledgeBaseSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantKnowledgeBaseSettings_TenantId",
                schema: "provisioning",
                table: "TenantKnowledgeBaseSettings",
                column: "TenantId",
                unique: true);
        }
    }
}
