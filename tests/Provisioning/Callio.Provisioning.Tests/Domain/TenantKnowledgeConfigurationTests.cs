using Callio.Knowledge.Domain;
using FluentAssertions;

namespace Callio.Provisioning.Tests.Domain;

public class TenantKnowledgeConfigurationTests
{
    [Fact]
    public void Constructor_NormalizesAllowedFileTypesAndInitializesAuditFields()
    {
        var now = new DateTime(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc);

        var configuration = new TenantKnowledgeConfiguration(
            42,
            "  System prompt  ",
            "  Assistant instructions  ",
            800,
            120,
            8,
            6,
            0.73456m,
            [".PDF", " docx ", "pdf"],
            10 * 1024 * 1024,
            autoProcessOnUpload: true,
            manualApprovalRequiredBeforeIndexing: false,
            versioningEnabled: true,
            isActive: true,
            now);

        configuration.SystemPrompt.Should().Be("System prompt");
        configuration.AssistantInstructionPrompt.Should().Be("Assistant instructions");
        configuration.AllowedFileTypes.Should().BeEquivalentTo([".docx", ".pdf"], options => options.WithStrictOrdering());
        configuration.MinimumSimilarityThreshold.Should().Be(0.7346m);
        configuration.IsActive.Should().BeTrue();
        configuration.CreatedAtUtc.Should().Be(now);
        configuration.UpdatedAtUtc.Should().Be(now);
    }

    [Fact]
    public void Update_ThrowsWhenChunkOverlapIsNotSmallerThanChunkSize()
    {
        var configuration = CreateValidConfiguration();

        var act = () => configuration.Update(
            configuration.SystemPrompt,
            configuration.AssistantInstructionPrompt,
            chunkSize: 300,
            chunkOverlap: 300,
            topKRetrievalCount: 8,
            maximumChunksInFinalContext: 6,
            minimumSimilarityThreshold: 0.7m,
            allowedFileTypes: [".pdf"],
            maximumFileSizeBytes: 1024,
            autoProcessOnUpload: true,
            manualApprovalRequiredBeforeIndexing: false,
            versioningEnabled: true,
            DateTime.UtcNow);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Chunk overlap must be smaller than chunk size.*");
    }

    [Fact]
    public void Update_ThrowsWhenMaximumChunksInFinalContextExceedsTopK()
    {
        var configuration = CreateValidConfiguration();

        var act = () => configuration.Update(
            configuration.SystemPrompt,
            configuration.AssistantInstructionPrompt,
            chunkSize: 600,
            chunkOverlap: 100,
            topKRetrievalCount: 4,
            maximumChunksInFinalContext: 5,
            minimumSimilarityThreshold: 0.7m,
            allowedFileTypes: [".pdf"],
            maximumFileSizeBytes: 1024,
            autoProcessOnUpload: true,
            manualApprovalRequiredBeforeIndexing: false,
            versioningEnabled: true,
            DateTime.UtcNow);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Maximum chunks in final context cannot exceed the top K retrieval count.*");
    }

    [Fact]
    public void DeactivateAndActivate_UpdateStateAndTimestamp()
    {
        var configuration = CreateValidConfiguration();
        var deactivatedAt = new DateTime(2026, 4, 6, 13, 0, 0, DateTimeKind.Utc);
        var activatedAt = deactivatedAt.AddMinutes(5);

        configuration.Deactivate(deactivatedAt);
        configuration.IsActive.Should().BeFalse();
        configuration.UpdatedAtUtc.Should().Be(deactivatedAt);

        configuration.Activate(activatedAt);
        configuration.IsActive.Should().BeTrue();
        configuration.UpdatedAtUtc.Should().Be(activatedAt);
    }

    private static TenantKnowledgeConfiguration CreateValidConfiguration()
        => new(
            42,
            "System prompt",
            "Assistant instructions",
            800,
            120,
            8,
            6,
            0.7m,
            [".pdf", ".docx"],
            10 * 1024 * 1024,
            autoProcessOnUpload: true,
            manualApprovalRequiredBeforeIndexing: false,
            versioningEnabled: true,
            isActive: true,
            new DateTime(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc));
}
