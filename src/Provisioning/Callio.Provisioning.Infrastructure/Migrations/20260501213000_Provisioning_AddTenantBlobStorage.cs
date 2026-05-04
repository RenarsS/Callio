using Callio.Provisioning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Callio.Provisioning.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ProvisioningDbContext))]
    [Migration("20260501213000_Provisioning_AddTenantBlobStorage")]
    public partial class Provisioning_AddTenantBlobStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlobContainerName",
                schema: "provisioning",
                table: "TenantInfrastructureProvisionings",
                type: "nvarchar(63)",
                maxLength: 63,
                nullable: false,
                defaultValue: "tenant-knowledge");

            migrationBuilder.Sql("""
UPDATE provisioning.TenantInfrastructureProvisionings
SET BlobContainerName = CONCAT('tenant-knowledge-', TenantId)
WHERE BlobContainerName = 'tenant-knowledge';

INSERT INTO provisioning.TenantInfrastructureProvisioningSteps
    (TenantInfrastructureProvisioningId, Name, [Order], Status, AttemptCount, CreatedAtUtc, UpdatedAtUtc)
SELECT
    p.Id,
    'blob-storage',
    3,
    'Pending',
    0,
    SYSUTCDATETIME(),
    SYSUTCDATETIME()
FROM provisioning.TenantInfrastructureProvisionings p
WHERE NOT EXISTS (
    SELECT 1
    FROM provisioning.TenantInfrastructureProvisioningSteps s
    WHERE s.TenantInfrastructureProvisioningId = p.Id
      AND s.Name = 'blob-storage'
);
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DELETE FROM provisioning.TenantInfrastructureProvisioningSteps
WHERE Name = 'blob-storage';
""");

            migrationBuilder.DropColumn(
                name: "BlobContainerName",
                schema: "provisioning",
                table: "TenantInfrastructureProvisionings");
        }
    }
}
