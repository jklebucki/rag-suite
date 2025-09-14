using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RAG.Security.Models;

namespace RAG.Security.Data;

public class SecurityDbContext : IdentityDbContext<User, Role, string, UserClaim, UserRole, IdentityUserLogin<string>, RoleClaim, IdentityUserToken<string>>
{
    public SecurityDbContext(DbContextOptions<SecurityDbContext> options) : base(options)
    {
    }

    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure PostgreSQL naming convention (lowercase with underscores)
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            // Convert table names to lowercase with underscores
            entity.SetTableName(entity.GetTableName()?.ToSnakeCase());

            // Convert column names to lowercase with underscores
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.GetColumnName().ToSnakeCase());
            }

            // Convert key names to lowercase with underscores
            foreach (var key in entity.GetKeys())
            {
                key.SetName(key.GetName()?.ToSnakeCase());
            }

            // Convert foreign key names to lowercase with underscores
            foreach (var foreignKey in entity.GetForeignKeys())
            {
                foreignKey.SetConstraintName(foreignKey.GetConstraintName()?.ToSnakeCase());
            }

            // Convert index names to lowercase with underscores
            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(index.GetDatabaseName()?.ToSnakeCase());
            }
        }

        // Configure table names explicitly for clarity
        builder.Entity<User>().ToTable("users");
        builder.Entity<Role>().ToTable("roles");
        builder.Entity<UserRole>().ToTable("user_roles");
        builder.Entity<UserClaim>().ToTable("user_claims");
        builder.Entity<RoleClaim>().ToTable("role_claims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<string>>().ToTable("user_tokens");

        // Configure User entity
        builder.Entity<User>(entity =>
        {
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.UserName).IsUnique();
        });

        // Configure Role entity
        builder.Entity<Role>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure UserRole relationship
        builder.Entity<UserRole>(entity =>
        {
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });

            entity.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure UserClaim relationship
        builder.Entity<UserClaim>(entity =>
        {
            entity.HasOne(uc => uc.User)
                .WithMany(u => u.Claims)
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure RoleClaim relationship
        builder.Entity<RoleClaim>(entity =>
        {
            entity.HasOne(rc => rc.Role)
                .WithMany(r => r.RoleClaims)
                .HasForeignKey(rc => rc.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure PasswordResetToken relationship
        builder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(prt => prt.Id);
            entity.HasOne(prt => prt.User)
                .WithMany()
                .HasForeignKey(prt => prt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(prt => prt.Token).IsRequired();
            entity.Property(prt => prt.CreatedAt).IsRequired();
            entity.Property(prt => prt.ExpiresAt).IsRequired();
        });

        // Note: Role seeding is now handled during application startup for consistency across environments
    }
}

public static class StringExtensions
{
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new System.Text.StringBuilder();
        result.Append(char.ToLowerInvariant(input[0]));

        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(input[i]));
            }
            else
            {
                result.Append(input[i]);
            }
        }

        return result.ToString();
    }
}
