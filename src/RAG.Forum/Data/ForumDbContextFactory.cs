namespace RAG.Forum.Data;

public class ForumDbContextFactory : IDesignTimeDbContextFactory<ForumDbContext>
{
    public ForumDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ForumDbContext>();
        var connectionString = "Host=localhost;Database=rag-suite;Username=postgres;Password=postgres";
        optionsBuilder.UseNpgsql(connectionString);

        return new ForumDbContext(optionsBuilder.Options);
    }
}

