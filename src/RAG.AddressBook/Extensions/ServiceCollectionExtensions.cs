using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RAG.AddressBook.Data;
using RAG.AddressBook.Features.CreateContact;
using RAG.AddressBook.Features.DeleteContact;
using RAG.AddressBook.Features.GetContact;
using RAG.AddressBook.Features.GetProposal;
using RAG.AddressBook.Features.ImportContacts;
using RAG.AddressBook.Features.ListContacts;
using RAG.AddressBook.Features.ListProposals;
using RAG.AddressBook.Features.ProposeChange;
using RAG.AddressBook.Features.ReviewProposal;
using RAG.AddressBook.Features.SearchContacts;
using RAG.AddressBook.Features.UpdateContact;
using RAG.AddressBook.Services;

namespace RAG.AddressBook.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAddressBook(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SecurityDatabase")
            ?? "Host=localhost;Database=rag-suite;Username=postgres;Password=postgres";

        services.AddDbContext<AddressBookDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        // Register core services
        services.AddScoped<IAddressBookService, AddressBookService>();
        services.AddScoped<IAddressBookAuthorizationService, AddressBookAuthorizationService>();

        // Register contact feature handlers and services
        services.AddScoped<CreateContactHandler>();
        services.AddScoped<GetContactService>();
        services.AddScoped<ListContactsService>();
        services.AddScoped<UpdateContactHandler>();
        services.AddScoped<DeleteContactHandler>();
        services.AddScoped<SearchContactsService>();
        services.AddScoped<ImportContactsHandler>();

        // Register proposal feature handlers and services
        services.AddScoped<ProposeChangeHandler>();
        services.AddScoped<ListProposalsService>();
        services.AddScoped<GetProposalService>();
        services.AddScoped<ReviewProposalHandler>();

        // Register validators
        services.AddScoped<IValidator<CreateContactRequest>, CreateContactValidator>();
        services.AddScoped<IValidator<UpdateContactRequest>, UpdateContactValidator>();
        services.AddScoped<IValidator<ImportContactsRequest>, ImportContactsValidator>();
        services.AddScoped<IValidator<ProposeContactChangeRequest>, ProposeChangeValidator>();
        services.AddScoped<IValidator<ReviewProposalRequest>, ReviewProposalValidator>();

        // Add authorization policies
        services.AddAuthorizationBuilder()
            .AddPolicy("AdminOrPowerUser", policy =>
                policy.RequireRole("Admin", "PowerUser"));

        return services;
    }

    public static async Task EnsureAddressBookDatabaseCreatedAsync(this IServiceProvider services)
    {
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AddressBookDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AddressBookDbContext>>();

            logger.LogInformation("Attempting to ensure AddressBook database is created and migrated...");

            await db.Database.MigrateAsync();

            logger.LogInformation("AddressBook database migration successful");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"AddressBook database initialization failed: {ex.Message}", ex);
        }
    }
}
