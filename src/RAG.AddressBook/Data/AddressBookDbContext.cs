using Microsoft.EntityFrameworkCore;
using RAG.AddressBook.Domain;

namespace RAG.AddressBook.Data;

public class AddressBookDbContext : DbContext
{
    public AddressBookDbContext(DbContextOptions<AddressBookDbContext> options) : base(options)
    {
    }

    public DbSet<Contact> Contacts { get; set; } = null!;
    public DbSet<ContactTag> ContactTags { get; set; } = null!;
    public DbSet<ContactChangeProposal> ContactChangeProposals { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Contact>(b =>
        {
            b.HasKey(c => c.Id);
            b.HasIndex(c => c.Email);
            b.HasIndex(c => new { c.FirstName, c.LastName });
            b.HasMany(c => c.Tags)
                .WithOne(t => t.Contact)
                .HasForeignKey(t => t.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
            b.Property(c => c.LastName).HasMaxLength(100).IsRequired();
            b.Property(c => c.DisplayName).HasMaxLength(200);
            b.Property(c => c.Department).HasMaxLength(100);
            b.Property(c => c.Position).HasMaxLength(150);
            b.Property(c => c.Location).HasMaxLength(200);
            b.Property(c => c.Company).HasMaxLength(200);
            b.Property(c => c.WorkPhone).HasMaxLength(50);
            b.Property(c => c.MobilePhone).HasMaxLength(50);
            b.Property(c => c.Email).HasMaxLength(255);
            b.Property(c => c.PhotoUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<ContactTag>(b =>
        {
            b.HasKey(t => t.Id);
            b.HasIndex(t => new { t.ContactId, t.TagName }).IsUnique();
            b.Property(t => t.TagName).HasMaxLength(50).IsRequired();
            b.Property(t => t.Color).HasMaxLength(20);
        });

        modelBuilder.Entity<ContactChangeProposal>(b =>
        {
            b.HasKey(p => p.Id);
            b.HasIndex(p => p.Status);
            b.HasIndex(p => p.ProposedByUserId);
            b.HasIndex(p => new { p.ContactId, p.Status });

            b.Property(p => p.ProposalType).IsRequired();
            b.Property(p => p.ProposedData).IsRequired();
            b.Property(p => p.Status).IsRequired();
            b.Property(p => p.ProposedByUserId).HasMaxLength(450).IsRequired();
            b.Property(p => p.ProposedByUserName).HasMaxLength(256);
            b.Property(p => p.ReviewedByUserId).HasMaxLength(450);
            b.Property(p => p.ReviewedByUserName).HasMaxLength(256);
            b.Property(p => p.Reason).HasMaxLength(1000);
            b.Property(p => p.ReviewComment).HasMaxLength(1000);

            b.HasOne(p => p.Contact)
                .WithMany()
                .HasForeignKey(p => p.ContactId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
