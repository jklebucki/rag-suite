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
        var errors = new List<string>();
        var importedContacts = new List<ImportedContactDto>();
        var existingEmails = new HashSet<string>();
        var existingPhoneNumbers = new HashSet<string>();

        var existingContacts = await _context.Contacts
            .AsNoTracking()
            .Select(c => new
            {
                c.Email,
                c.WorkPhone,
                c.MobilePhone
            })
            .ToListAsync(cancellationToken);

        foreach (var existingContact in existingContacts)
        {
            AddToSet(existingEmails, NormalizeEmail(existingContact.Email));
            AddToSet(existingPhoneNumbers, NormalizePhone(existingContact.WorkPhone));
            AddToSet(existingPhoneNumbers, NormalizePhone(existingContact.MobilePhone));
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

        for (var index = 0; index < dataLines.Count; index++)
        {
            var line = dataLines[index];
            var rowNumber = index + 2; // +1 for zero-based, +1 for header

            try
            {
                var contact = ParseCsvLine(line, userId);

                if (contact == null)
                {
                    skippedCount++;
                    continue;
                }

                var duplicateFields = GetDuplicateFields(contact, existingEmails, existingPhoneNumbers);
                if (duplicateFields.Count > 0)
                {
                    if (request.SkipDuplicates)
                    {
                        skippedCount++;
                    }
                    else
                    {
                        errorCount++;
                        errors.Add($"Row {rowNumber}: Duplicate {string.Join(", ", duplicateFields)}");
                    }

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

                AddToSet(existingEmails, NormalizeEmail(contact.Email));
                AddToSet(existingPhoneNumbers, NormalizePhone(contact.WorkPhone));
                AddToSet(existingPhoneNumbers, NormalizePhone(contact.MobilePhone));
            }
            catch (Exception ex)
            {
                errorCount++;
                errors.Add($"Row {rowNumber}: Error parsing line: {ex.Message}");
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

    private static List<string> GetDuplicateFields(
        Contact contact,
        HashSet<string> existingEmails,
        HashSet<string> existingPhoneNumbers)
    {
        var duplicateFields = new List<string>();

        var normalizedEmail = NormalizeEmail(contact.Email);
        if (normalizedEmail != null && existingEmails.Contains(normalizedEmail))
        {
            duplicateFields.Add("email");
        }

        var normalizedWorkPhone = NormalizePhone(contact.WorkPhone);
        if (normalizedWorkPhone != null && existingPhoneNumbers.Contains(normalizedWorkPhone))
        {
            duplicateFields.Add("work phone");
        }

        var normalizedMobilePhone = NormalizePhone(contact.MobilePhone);
        if (normalizedMobilePhone != null && existingPhoneNumbers.Contains(normalizedMobilePhone))
        {
            duplicateFields.Add("mobile phone");
        }

        return duplicateFields;
    }

    private static string? NormalizeEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim().ToLowerInvariant();
    }

    private static string? NormalizePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        var digitsOnly = new string(trimmed.Where(char.IsDigit).ToArray());

        return string.IsNullOrWhiteSpace(digitsOnly)
            ? trimmed.ToLowerInvariant()
            : digitsOnly;
    }

    private static void AddToSet(HashSet<string> set, string? value)
    {
        if (value != null)
        {
            set.Add(value);
        }
    }
}
