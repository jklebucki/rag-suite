using Microsoft.EntityFrameworkCore;
using RAG.Orchestrator.Api.Models;

namespace RAG.Orchestrator.Api.Data;

public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
    {
    }

    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<GlobalSetting> GlobalSettings { get; set; }
    public DbSet<Feedback> FeedbackEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure PostgreSQL naming convention (lowercase with underscores)
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(entity.GetTableName()?.ToSnakeCase());

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.GetColumnName().ToSnakeCase());
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName(key.GetName()?.ToSnakeCase());
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                foreignKey.SetConstraintName(foreignKey.GetConstraintName()?.ToSnakeCase());
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(index.GetDatabaseName()?.ToSnakeCase());
            }
        }

        // Chat Session configuration
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.ToTable("chat_sessions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.UserId)
                .HasMaxLength(450) // Standard ASP.NET Identity user ID length
                .IsRequired();

            entity.Property(e => e.Title)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired();

            // Index on UserId for fast user session lookup
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("ix_chat_sessions_user_id");

            // Index on UpdatedAt for ordering
            entity.HasIndex(e => e.UpdatedAt)
                .HasDatabaseName("ix_chat_sessions_updated_at");
        });

        // Chat Message configuration
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("chat_messages");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.SessionId)
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.Content)
                .IsRequired();

            entity.Property(e => e.Timestamp)
                .IsRequired();

            // JSON column for sources
            entity.Property(e => e.SourcesJson)
                .HasColumnType("jsonb")
                .HasColumnName("sources");

            // JSON column for metadata
            entity.Property(e => e.MetadataJson)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");

            // Foreign key to chat session
            entity.HasOne(e => e.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index on SessionId for fast message lookup
            entity.HasIndex(e => e.SessionId)
                .HasDatabaseName("ix_chat_messages_session_id");

            // Index on Timestamp for ordering
            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("ix_chat_messages_timestamp");

            // Composite index for session + timestamp (most common query)
            entity.HasIndex(e => new { e.SessionId, e.Timestamp })
                .HasDatabaseName("ix_chat_messages_session_timestamp");
        });

        // Global Setting configuration
        modelBuilder.Entity<GlobalSetting>(entity =>
        {
            entity.ToTable("global_settings");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Key)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Value)
                .IsRequired();

            // Unique index on Key for fast lookup
            entity.HasIndex(e => e.Key)
                .IsUnique()
                .HasDatabaseName("ix_global_settings_key");
        });

        // Feedback configuration
        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.ToTable("feedback");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.UserId)
                .HasMaxLength(450)
                .IsRequired();

            entity.Property(e => e.UserEmail)
                .HasMaxLength(320);

            entity.Property(e => e.Subject)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Message)
                .HasMaxLength(4000)
                .IsRequired();

            entity.Property(e => e.Response)
                .HasMaxLength(4000);

            entity.Property(e => e.ResponseAuthorEmail)
                .HasMaxLength(320);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired();

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("ix_feedback_user_id");

            entity.HasIndex(e => e.Subject)
                .HasDatabaseName("ix_feedback_subject");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("ix_feedback_created_at");
        });
    }
}

// Extension method for snake_case conversion
public static class StringExtensions
{
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        return string.Concat(input.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString())).ToLower();
    }
}
