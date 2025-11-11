using FluentAssertions;
using RAG.Forum.Domain;
using RAG.Forum.Features.Shared;
using RAG.Forum.Features.Threads;

namespace RAG.Tests.Forum;

public class ThreadDtoMapperTests
{
    [Fact]
    public void ToDetailDto_OrdersAttachmentsAndPostsCorrectly()
    {
        // Arrange
        var category = new ForumCategory { Id = Guid.NewGuid(), Name = "Announcements" };

        var thread = new ForumThread
        {
            Id = Guid.NewGuid(),
            Category = category,
            CategoryId = category.Id,
            Title = "Update",
            AuthorId = "author-1",
            AuthorEmail = "author@example.com",
            Content = "Thread content",
            CreatedAt = new DateTime(2025, 11, 10, 11, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 11, 10, 12, 0, 0, DateTimeKind.Utc),
            LastPostAt = new DateTime(2025, 11, 10, 13, 0, 0, DateTimeKind.Utc),
            ViewCount = 42
        };

        thread.Attachments = new List<ForumAttachment>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ThreadId = thread.Id,
                FileName = "b-notes.txt",
                ContentType = "text/plain",
                Size = 150,
                CreatedAt = new DateTime(2025, 11, 10, 12, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = Guid.NewGuid(),
                ThreadId = thread.Id,
                FileName = "a-diagram.png",
                ContentType = "image/png",
                Size = 1024,
                CreatedAt = new DateTime(2025, 11, 9, 16, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = Guid.NewGuid(),
                ThreadId = thread.Id,
                FileName = "a-notes.txt",
                ContentType = "text/plain",
                Size = 200,
                CreatedAt = new DateTime(2025, 11, 10, 12, 0, 0, DateTimeKind.Utc)
            }
        };

        var post1Id = Guid.NewGuid();
        var post2Id = Guid.NewGuid();

        var postWithAttachments = new ForumPost
        {
            Id = post1Id,
            ThreadId = thread.Id,
            AuthorId = "author-2",
            AuthorEmail = "reply@example.com",
            Content = "Reply with files",
            CreatedAt = new DateTime(2025, 11, 10, 13, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 11, 10, 13, 30, 0, DateTimeKind.Utc),
            Attachments = new List<ForumAttachment>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ThreadId = thread.Id,
                    PostId = post1Id,
                    FileName = "z-image.png",
                    ContentType = "image/png",
                    Size = 500,
                    CreatedAt = new DateTime(2025, 11, 10, 13, 5, 0, DateTimeKind.Utc)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ThreadId = thread.Id,
                    PostId = post1Id,
                    FileName = "a-image.png",
                    ContentType = "image/png",
                    Size = 400,
                    CreatedAt = new DateTime(2025, 11, 10, 13, 5, 0, DateTimeKind.Utc)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ThreadId = thread.Id,
                    PostId = post1Id,
                    FileName = "older-image.png",
                    ContentType = "image/png",
                    Size = 300,
                    CreatedAt = new DateTime(2025, 11, 10, 13, 0, 0, DateTimeKind.Utc)
                }
            }
        };

        var earlierPost = new ForumPost
        {
            Id = post2Id,
            ThreadId = thread.Id,
            AuthorId = "author-3",
            AuthorEmail = "another@example.com",
            Content = "Earlier reply",
            CreatedAt = new DateTime(2025, 11, 10, 12, 45, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 11, 10, 12, 50, 0, DateTimeKind.Utc)
        };

        thread.Posts = new List<ForumPost> { postWithAttachments, earlierPost };

        // Act
        var dto = ThreadDtoMapper.ToDetailDto(thread);

        // Assert
        dto.Attachments.Select(a => a.FileName).Should().Equal(
            "a-diagram.png",
            "a-notes.txt",
            "b-notes.txt");

        dto.Posts.Select(p => p.Id).Should().Equal(
            post2Id,
            post1Id);

        var firstPostAttachments = dto.Posts.Last().Attachments;
        firstPostAttachments.Select(a => a.FileName).Should().Equal(
            "older-image.png",
            "a-image.png",
            "z-image.png");

        dto.Should().BeEquivalentTo(new
        {
            thread.Id,
            thread.CategoryId,
            CategoryName = category.Name,
            thread.Title,
            thread.AuthorId,
            thread.AuthorEmail,
            thread.Content,
            thread.CreatedAt,
            thread.UpdatedAt,
            thread.LastPostAt,
            thread.IsLocked,
            thread.ViewCount
        });
    }
}

