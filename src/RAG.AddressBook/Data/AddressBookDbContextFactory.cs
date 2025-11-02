using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RAG.AddressBook.Data;

/// <summary>
/// Design-time factory for creating AddressBookDbContext instances during migrations.
/// </summary>
public class AddressBookDbContextFactory : IDesignTimeDbContextFactory<AddressBookDbContext>
{
    public AddressBookDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AddressBookDbContext>();

        // Use a default connection string for design-time operations
        var connectionString = "Host=localhost;Database=rag-suite;Username=postgres;Password=postgres";
        optionsBuilder.UseNpgsql(connectionString);

        return new AddressBookDbContext(optionsBuilder.Options);
    }
}
