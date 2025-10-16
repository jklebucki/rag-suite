using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RAG.CyberPanel.Data;

/// <summary>
/// Design-time factory for creating CyberPanelDbContext instances during migrations.
/// </summary>
public class CyberPanelDbContextFactory : IDesignTimeDbContextFactory<CyberPanelDbContext>
{
    public CyberPanelDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CyberPanelDbContext>();
        
        // Use a default connection string for design-time operations
        var connectionString = "Host=localhost;Database=rag-suite;Username=postgres;Password=postgres";
        optionsBuilder.UseNpgsql(connectionString);

        return new CyberPanelDbContext(optionsBuilder.Options);
    }
}
