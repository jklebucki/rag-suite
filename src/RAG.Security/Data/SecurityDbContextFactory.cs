using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RAG.Security.Data;

public class SecurityDbContextFactory : IDesignTimeDbContextFactory<SecurityDbContext>
{
    public SecurityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SecurityDbContext>();

        // Use default connection string for migrations
        var connectionString = "Host=localhost;Database=rag-suite;Username=postgres;Password=postgres";
        optionsBuilder.UseNpgsql(connectionString);

        return new SecurityDbContext(optionsBuilder.Options);
    }
}