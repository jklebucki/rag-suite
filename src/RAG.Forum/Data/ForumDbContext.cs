using RAG.Forum.Domain;

namespace RAG.Forum.Data;

public class ForumDbContext : DbContext
{
    public ForumDbContext(DbContextOptions<ForumDbContext> options)
        : base(options)
    {
    }

    public DbSet<ForumCategory> Categories => Set<ForumCategory>();

    public DbSet<ForumThread> Threads => Set<ForumThread>();

    public DbSet<ForumPost> Posts => Set<ForumPost>();

    public DbSet<ForumAttachment> Attachments => Set<ForumAttachment>();

    public DbSet<ThreadSubscription> Subscriptions => Set<ThreadSubscription>();

    public DbSet<ThreadBadge> Badges => Set<ThreadBadge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("forum");

        ConfigureCategory(modelBuilder);
        ConfigureThread(modelBuilder);
        ConfigurePost(modelBuilder);
        ConfigureAttachment(modelBuilder);
        ConfigureSubscription(modelBuilder);
        ConfigureBadge(modelBuilder);
    }

    private static void ConfigureCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ForumCategory>(entity =>
        {
            entity.ToTable("categories");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Slug)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired();

            entity.HasIndex(e => e.Slug)
                .IsUnique()
                .HasDatabaseName("ix_categories_slug");

            entity.HasIndex(e => e.Order)
                .HasDatabaseName("ix_categories_order");
        });
    }

    private static void ConfigureThread(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ForumThread>(entity =>
        {
            entity.ToTable("threads");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Content)
                .IsRequired();

            entity.Property(e => e.AuthorId)
                .HasMaxLength(450)
                .IsRequired();

            entity.Property(e => e.AuthorEmail)
                .HasMaxLength(320)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired();

            entity.Property(e => e.LastPostAt)
                .IsRequired();

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Threads)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.CategoryId)
                .HasDatabaseName("ix_threads_category_id");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("ix_threads_created_at");

            entity.HasIndex(e => e.LastPostAt)
                .HasDatabaseName("ix_threads_last_post_at");
        });
    }

    private static void ConfigurePost(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ForumPost>(entity =>
        {
            entity.ToTable("posts");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Content)
                .IsRequired();

            entity.Property(e => e.AuthorId)
                .HasMaxLength(450)
                .IsRequired();

            entity.Property(e => e.AuthorEmail)
                .HasMaxLength(320)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired();

            entity.HasOne(e => e.Thread)
                .WithMany(t => t.Posts)
                .HasForeignKey(e => e.ThreadId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ThreadId)
                .HasDatabaseName("ix_posts_thread_id");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("ix_posts_created_at");
        });
    }

    private static void ConfigureAttachment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ForumAttachment>(entity =>
        {
            entity.ToTable("attachments");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.ContentType)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Size)
                .IsRequired();

            entity.Property(e => e.Data)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasOne(e => e.Thread)
                .WithMany(t => t.Attachments)
                .HasForeignKey(e => e.ThreadId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Post)
                .WithMany(p => p.Attachments)
                .HasForeignKey(e => e.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ThreadId)
                .HasDatabaseName("ix_attachments_thread_id");

            entity.HasIndex(e => e.PostId)
                .HasDatabaseName("ix_attachments_post_id");
        });
    }

    private static void ConfigureSubscription(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ThreadSubscription>(entity =>
        {
            entity.ToTable("subscriptions");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.UserId)
                .HasMaxLength(450)
                .IsRequired();

            entity.Property(e => e.Email)
                .HasMaxLength(320)
                .IsRequired();

            entity.Property(e => e.SubscribedAt)
                .IsRequired();

            entity.HasOne(e => e.Thread)
                .WithMany(t => t.Subscriptions)
                .HasForeignKey(e => e.ThreadId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.ThreadId, e.UserId })
                .IsUnique()
                .HasDatabaseName("ix_subscriptions_thread_user");
        });
    }

    private static void ConfigureBadge(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ThreadBadge>(entity =>
        {
            entity.ToTable("badges");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.UserId)
                .HasMaxLength(450)
                .IsRequired();

            entity.Property(e => e.HasUnreadReplies)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired();

            entity.HasOne(e => e.Thread)
                .WithMany(t => t.Badges)
                .HasForeignKey(e => e.ThreadId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.LastSeenPost)
                .WithMany()
                .HasForeignKey(e => e.LastSeenPostId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.LastSeenPostId)
                .HasDatabaseName("ix_badges_last_seen_post_id");

            entity.HasIndex(e => new { e.ThreadId, e.UserId })
                .IsUnique()
                .HasDatabaseName("ix_badges_thread_user");
        });
    }
}

