using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RAG.Orchestrator.Api.Data;

namespace RAG.Orchestrator.Api.Data;

public class ChatDbContextFactory : IDesignTimeDbContextFactory<ChatDbContext>
{
    public ChatDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ChatDbContext>();

        // Use default connection string for migrations
        var connectionString = "Host=localhost;Database=rag-suite;Username=postgres;Password=postgres";
        optionsBuilder.UseNpgsql(connectionString);

        return new ChatDbContext(optionsBuilder.Options);
    }
}
