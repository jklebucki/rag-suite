using Microsoft.EntityFrameworkCore;
using RAG.AddressBook.Data;
using RAG.AddressBook.Domain;
using RAG.Security.Services;
using System.Text;

namespace RAG.AddressBook.Features.ImportContacts;

/// <summary>
/// Handler for importing contacts from CSV file (KsiazkaAdresowa.csv format)
/// </summary>
public class ImportContactsHandler
{
    private readonly AddressBookDbContext _context;
    private readonly IUserContextService _userContext;

    public ImportContactsHandler(AddressBookDbContext context, IUserContextService userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    public async Task<ImportContactsResponse> HandleAsync(
        ImportContactsRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _userContext.GetCurrentUserId() ?? "system";
        var response = new ImportContactsResponse();
        var errors = new List<string>();
        var importedContacts = new List<ImportedContactDto>();
        var existingEmails = new HashSet<string>();

        if (request.SkipDuplicates)
        {
            // Load existing emails for duplicate detection
            existingEmails = (await _context.Contacts
                .Where(c => c.Email != null)
                .Select(c => c.Email!)
                .ToListAsync(cancellationToken))
                .Select(e => e.ToLower())
                .ToHashSet();
        }

        var lines = request.CsvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0)
        {
            errors.Add("CSV file is empty");
            return new ImportContactsResponse
            {
                TotalRows = 0,
                ErrorCount = 1,
                Errors = errors
            };
        }

        // Skip header row
        var dataLines = lines.Skip(1).ToList();
        var totalRows = dataLines.Count;
        var successCount = 0;
        var skippedCount = 0;
        var errorCount = 0;

        foreach (var line in dataLines)
        {
            try
            {
                var contact = ParseCsvLine(line, userId);

                if (contact == null)
                {
                    skippedCount++;
                    continue;
                }

                // Check for duplicates by email
                if (request.SkipDuplicates &&
                    !string.IsNullOrWhiteSpace(contact.Email) &&
                    existingEmails.Contains(contact.Email.ToLower()))
                {
                    skippedCount++;
                    continue;
                }

                _context.Contacts.Add(contact);
                successCount++;

                importedContacts.Add(new ImportedContactDto
                {
                    Id = contact.Id,
                    FirstName = contact.FirstName,
                    LastName = contact.LastName,
                    Email = contact.Email,
                    Department = contact.Department
                });

                // Add email to tracking set
                if (!string.IsNullOrWhiteSpace(contact.Email))
                {
                    existingEmails.Add(contact.Email.ToLower());
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                errors.Add($"Error parsing line: {ex.Message}");
            }
        }

        if (successCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new ImportContactsResponse
        {
            TotalRows = totalRows,
            SuccessCount = successCount,
            SkippedCount = skippedCount,
            ErrorCount = errorCount,
            Errors = errors,
            ImportedContacts = importedContacts
        };
    }

    /// <summary>
    /// Parse CSV line with format: "Imię";"Nazwisko";"Dział";"Telefon służbowy";"Telefon komórkowy";"Adres e-mail";"Nazwa wyświetlana";"Stanowisko";"Lokalizacja"
    /// </summary>
    private Contact? ParseCsvLine(string line, string userId)
    {
        var fields = ParseCsvFields(line);

        if (fields.Count < 9)
            return null;

        var firstName = CleanField(fields[0]);
        var lastName = CleanField(fields[1]);

        // Skip rows with empty first name or last name
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            return null;

        var department = CleanField(fields[2]);
        var workPhone = CleanField(fields[3]);
        var mobilePhone = CleanField(fields[4]);
        var email = CleanField(fields[5]);
        var displayName = CleanField(fields[6]);
        var position = CleanField(fields[7]);
        var location = CleanField(fields[8]);

        // Extract company from display name (format: "Name - Company")
        string? company = null;
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            var parts = displayName.Split(" - ", 2);
            if (parts.Length == 2)
            {
                company = parts[1].Trim();
            }
        }

        return new Contact
        {
            FirstName = firstName,
            LastName = lastName,
            DisplayName = displayName,
            Department = department,
            Position = position,
            Location = location,
            Company = company,
            WorkPhone = workPhone,
            MobilePhone = mobilePhone,
            Email = email,
            IsActive = true,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Parse CSV fields respecting semicolon delimiter and quoted values
    /// </summary>
    private List<string> ParseCsvFields(string line)
    {
        var fields = new List<string>();
        var currentField = new StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ';' && !inQuotes)
            {
                fields.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }

        // Add the last field
        fields.Add(currentField.ToString());

        return fields;
    }

    /// <summary>
    /// Clean field value (trim, remove quotes, handle encoding issues)
    /// </summary>
    private string? CleanField(string field)
    {
        if (string.IsNullOrWhiteSpace(field))
            return null;

        field = field.Trim();

        // Remove surrounding quotes
        if (field.StartsWith("\"") && field.EndsWith("\""))
        {
            field = field[1..^1];
        }

        field = field.Trim();

        return string.IsNullOrWhiteSpace(field) ? null : field;
    }
}
