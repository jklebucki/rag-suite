# Import CSV Script

This script demonstrates how to import the KsiazkaAdresowa.csv file using the AddressBook API.

## Using the API

### Option 1: Direct API Call with CSV Content

```bash
# Read CSV file and import
CSV_CONTENT=$(cat KsiazkaAdresowa.csv)
TOKEN="your-jwt-token"

curl -X POST "https://localhost:5001/api/addressbook/import" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"csvContent\": \"$(echo "$CSV_CONTENT" | sed 's/"/\\"/g' | sed ':a;N;$!ba;s/\n/\\n/g')\", \"skipDuplicates\": true}"
```

### Option 2: Using C# Script

```csharp
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var csvFilePath = "KsiazkaAdresowa.csv";
var apiUrl = "https://localhost:5001/api/addressbook/import";
var token = "your-jwt-token";

// Read CSV file
var csvContent = await File.ReadAllTextAsync(csvFilePath);

// Prepare request
var request = new
{
    csvContent = csvContent,
    skipDuplicates = true
};

var json = JsonSerializer.Serialize(request);
var content = new StringContent(json, Encoding.UTF8, "application/json");

// Send request
using var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

var response = await client.PostAsync(apiUrl, content);
var responseContent = await response.Content.ReadAsStringAsync();

Console.WriteLine($"Status: {response.StatusCode}");
Console.WriteLine($"Response: {responseContent}");
```

### Option 3: Using PowerShell

```powershell
$csvContent = Get-Content -Path "KsiazkaAdresowa.csv" -Raw
$token = "your-jwt-token"
$apiUrl = "https://localhost:5001/api/addressbook/import"

$body = @{
    csvContent = $csvContent
    skipDuplicates = $true
} | ConvertTo-Json

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

$response = Invoke-RestMethod -Uri $apiUrl -Method Post -Headers $headers -Body $body
$response | ConvertTo-Json -Depth 10
```

## Response Format

The API returns:

```json
{
  "totalRows": 150,
  "successCount": 145,
  "skippedCount": 5,
  "errorCount": 0,
  "errors": [],
  "importedContacts": [
    {
      "id": "guid",
      "firstName": "Jan",
      "lastName": "Kowalski",
      "email": "jan.kowalski@example.com",
      "department": "IT"
    }
  ]
}
```

## Features

- **Duplicate Detection**: By default, contacts with duplicate emails are skipped
- **Error Handling**: Invalid rows are logged but don't stop the import
- **Batch Processing**: All valid contacts are imported in a single database transaction
- **Audit Trail**: Each imported contact has `CreatedByUserId` and `CreatedAt` set automatically

## CSV Format

The expected CSV format (semicolon-delimited):

```
"Imię";"Nazwisko";"Dział";"Telefon służbowy";"Telefon komórkowy";"Adres e-mail";"Nazwa wyświetlana";"Stanowisko";"Lokalizacja"
"Jan";"Kowalski";"IT";"";"+ 48600123456";"jan.kowalski@example.com";"Jan Kowalski - ACME Corp";"Software Developer";"Warsaw"
```

## Field Mapping

- "Imię" → FirstName
- "Nazwisko" → LastName
- "Dział" → Department
- "Telefon służbowy" → WorkPhone
- "Telefon komórkowy" → MobilePhone
- "Adres e-mail" → Email
- "Nazwa wyświetlana" → DisplayName
- "Stanowisko" → Position
- "Lokalizacja" → Location
- Company extracted from DisplayName (text after " - ")
