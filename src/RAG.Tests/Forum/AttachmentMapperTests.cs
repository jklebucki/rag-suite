using FluentAssertions;
using RAG.Forum.Features.Shared;

namespace RAG.Tests.Forum;

public class AttachmentMapperTests
{
    [Fact]
    public void TryCreateAttachments_WithValidUploads_ReturnsAttachments()
    {
        // Arrange
        var threadId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var createdAt = new DateTime(2025, 11, 10, 12, 0, 0, DateTimeKind.Utc);
        var data = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });

        var uploads = new[]
        {
            new ForumAttachmentUpload(" Document.pdf ", " application/pdf ", data)
        };

        // Act
        var result = AttachmentMapper.TryCreateAttachments(
            uploads,
            threadId,
            postId,
            createdAt,
            maxAttachmentCount: 5,
            maxAttachmentSizeBytes: 10,
            out var attachments,
            out var errors);

        // Assert
        result.Should().BeTrue();
        errors.Should().BeEmpty();
        attachments.Should().HaveCount(1);

        var attachment = attachments.Single();
        attachment.ThreadId.Should().Be(threadId);
        attachment.PostId.Should().Be(postId);
        attachment.CreatedAt.Should().Be(createdAt);
        attachment.FileName.Should().Be("Document.pdf");
        attachment.ContentType.Should().Be("application/pdf");
        attachment.Size.Should().Be(4);
        attachment.Data.Should().Equal(new byte[] { 1, 2, 3, 4 });
    }

    [Fact]
    public void TryCreateAttachments_WithInvalidBase64_ReturnsError()
    {
        // Arrange
        var uploads = new[]
        {
            new ForumAttachmentUpload("Invalid.txt", "text/plain", "not-base64")
        };

        // Act
        var result = AttachmentMapper.TryCreateAttachments(
            uploads,
            Guid.NewGuid(),
            postId: null,
            createdAt: DateTime.UtcNow,
            maxAttachmentCount: 5,
            maxAttachmentSizeBytes: 512,
            out var attachments,
            out var errors);

        // Assert
        result.Should().BeFalse();
        attachments.Should().BeEmpty();
        errors.Should().ContainKey("attachments");
        errors["attachments"].Should().ContainSingle()
            .Which.Should().Be("Attachment 'Invalid.txt' is not a valid Base64 string.");
    }

    [Fact]
    public void TryCreateAttachments_WhenAttachmentExceedsSize_ReturnsFormattedError()
    {
        // Arrange
        var oversizeData = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6 });
        var uploads = new[]
        {
            new ForumAttachmentUpload("Large.bin", "application/octet-stream", oversizeData)
        };

        // Act
        var result = AttachmentMapper.TryCreateAttachments(
            uploads,
            Guid.NewGuid(),
            postId: null,
            createdAt: DateTime.UtcNow,
            maxAttachmentCount: 5,
            maxAttachmentSizeBytes: 5,
            out var attachments,
            out var errors);

        // Assert
        result.Should().BeFalse();
        attachments.Should().BeEmpty();
        errors.Should().ContainKey("attachments");
        errors["attachments"].Should().ContainSingle()
            .Which.Should().Be("Attachment 'Large.bin' exceeds the maximum size of 5 B.");
    }

    [Fact]
    public void TryCreateAttachments_WhenAttachmentCountExceedsLimit_ReturnsError()
    {
        // Arrange
        var uploads = new[]
        {
            new ForumAttachmentUpload("1.txt", "text/plain", Convert.ToBase64String(new byte[] { 1 })),
            new ForumAttachmentUpload("2.txt", "text/plain", Convert.ToBase64String(new byte[] { 2 })),
            new ForumAttachmentUpload("3.txt", "text/plain", Convert.ToBase64String(new byte[] { 3 }))
        };

        // Act
        var result = AttachmentMapper.TryCreateAttachments(
            uploads,
            Guid.NewGuid(),
            postId: null,
            createdAt: DateTime.UtcNow,
            maxAttachmentCount: 2,
            maxAttachmentSizeBytes: 10,
            out var attachments,
            out var errors);

        // Assert
        result.Should().BeFalse();
        attachments.Should().BeEmpty();
        errors.Should().ContainKey("attachments");
        errors["attachments"].Should().ContainSingle()
            .Which.Should().Be("You can upload up to 2 attachments per message.");
    }
}

