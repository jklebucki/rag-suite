# RAG.AddressBook

Address book module for managing contacts within the RAG Suite system.

## Architecture

This module follows **Vertical Slice Architecture** with feature-based organization:

```
RAG.AddressBook/
├── Domain/              # Domain entities
│   ├── Contact.cs
│   └── ContactTag.cs
├── Data/                # DbContext and migrations
│   ├── AddressBookDbContext.cs
│   └── AddressBookDbContextFactory.cs
├── Features/            # Feature slices
│   ├── CreateContact/
│   ├── GetContact/
│   ├── ListContacts/
│   ├── UpdateContact/
│   ├── DeleteContact/
│   └── SearchContacts/
├── Services/            # Core services
│   ├── IAddressBookService.cs
│   └── AddressBookService.cs
├── Endpoints/           # Endpoint registration
│   └── AddressBookEndpoints.cs
└── Extensions/          # Service registration
    └── ServiceCollectionExtensions.cs
```

## Domain Model

### Contact Entity

Represents a person in the address book with the following properties:

**Personal Information:**
- FirstName (required)
- LastName (required)
- DisplayName (optional)

**Work Information:**
- Department
- Position
- Location
- Company

**Contact Information:**
- WorkPhone
- MobilePhone
- Email

**Additional Fields:**
- Notes
- IsActive (default: true)
- PhotoUrl
- Tags (collection of ContactTag)

**Audit Fields:**
- CreatedAt
- UpdatedAt
- CreatedByUserId
- UpdatedByUserId

### ContactTag Entity

Tags for categorizing contacts:
- TagName (required)
- Color (optional hex color)

## Features

### CreateContact
**Endpoint:** `POST /api/addressbook`
- Creates a new contact
- Validates required fields (FirstName, LastName)
- Supports tags
- **Requires Admin or PowerUser role**

### GetContact
**Endpoint:** `GET /api/addressbook/{id}`
- Retrieves a single contact by ID
- Includes tags
- Returns 404 if not found
- Available to all authenticated users

### ListContacts
**Endpoint:** `GET /api/addressbook`
- Lists all contacts
- Optional filters: `IncludeInactive`, `Department`, `Location`
- Returns contacts ordered by LastName, FirstName
- Available to all authenticated users

### UpdateContact
**Endpoint:** `PUT /api/addressbook/{id}`
- Updates an existing contact
- Validates required fields
- Updates audit fields automatically
- Returns 404 if not found
- **Requires Admin or PowerUser role**

### DeleteContact
**Endpoint:** `DELETE /api/addressbook/{id}`
- Soft or hard delete (cascade deletes tags)
- Returns 404 if not found
- **Requires Admin or PowerUser role**

### SearchContacts
**Endpoint:** `GET /api/addressbook/search?searchTerm={term}`
- Searches across FirstName, LastName, Email, Department, Position, Location
- Only returns active contacts
- Case-insensitive search
- Available to all authenticated users

### ImportContacts
**Endpoint:** `POST /api/addressbook/import`
- Imports contacts from CSV file (KsiazkaAdresowa.csv format)
- Request body: `{ "csvContent": "csv-content-here", "skipDuplicates": true }`
- Supports duplicate detection by email
- Returns summary with success/error counts
- Batch imports in single transaction
- **Requires Admin or PowerUser role**
- See [Import Guide](Features/ImportContacts/IMPORT_GUIDE.md) for detailed instructions

## Change Proposal System

Regular users (without Admin/PowerUser role) can propose changes to contacts. Proposals must be reviewed and approved by administrators.

### ProposeContactChange
**Endpoint:** `POST /api/addressbook/proposals`
- Allows regular users to propose Create/Update/Delete operations
- Request includes: `proposalType` (1=Create, 2=Update, 3=Delete), `proposedData`, `reason`
- Returns proposal ID and pending status
- **Available to all authenticated users**
- **Admin/PowerUser users should use direct modification endpoints**

### ListProposals
**Endpoint:** `GET /api/addressbook/proposals`
- Lists change proposals
- Regular users see only their own proposals
- Admin/PowerUser see all proposals
- Optional filters: `status`, `proposalType`, `proposedByUserId`
- **Available to all authenticated users**

### GetProposal
**Endpoint:** `GET /api/addressbook/proposals/{id}`
- Gets detailed information about a specific proposal
- Includes proposed data and review status
- Regular users can only view their own proposals
- Admin/PowerUser can view all proposals
- **Available to all authenticated users**

### ReviewProposal
**Endpoint:** `POST /api/addressbook/proposals/{id}/review`
- Approves or rejects a pending proposal
- Request includes: `decision` (2=Approved, 3=Rejected), `reviewComment`
- Approved proposals are automatically applied to contacts
- **Requires Admin or PowerUser role**

### Proposal States
- **Pending** (1): Waiting for review
- **Approved** (2): Approved but not yet applied
- **Rejected** (3): Rejected by reviewer
- **Applied** (4): Approved and changes applied to contact

## Database

### Tables
- `Contacts` - Main contact information
- `ContactTags` - Tags associated with contacts

### Indexes
- Email (for faster lookups)
- FirstName + LastName (for sorting)
- ContactId + TagName (unique, for ContactTags)

### Relationships
- Contact → ContactTags (one-to-many, cascade delete)

## Registration

The module is registered in `RAG.Orchestrator.Api/Program.cs`:

```csharp
// Service registration
builder.Services.AddAddressBook(builder.Configuration);

// Database initialization
await app.Services.EnsureAddressBookDatabaseCreatedAsync();

// Endpoint mapping
app.MapAddressBookEndpoints();
```

## Data Migration from CSV

To import data from `KsiazkaAdresowa.csv`:

1. The CSV columns map to Contact properties as follows:
   - "Imię" → FirstName
   - "Nazwisko" → LastName
   - "Dział" → Department
   - "Telefon służbowy" → WorkPhone
   - "Telefon komórkowy" → MobilePhone
   - "Adres e-mail" → Email
   - "Nazwa wyświetlana" → DisplayName
   - "Stanowisko" → Position
   - "Lokalizacja" → Location

2. Additional parsing could extract Company from DisplayName field

## Configuration

The module uses the `SecurityDatabase` connection string from appsettings.json:

```json
{
  "ConnectionStrings": {
    "SecurityDatabase": "Host=localhost;Database=rag-suite;Username=postgres;Password=postgres"
  }
}
```

## Security

All endpoints require authentication via JWT bearer token.

## Validation

- FirstName and LastName are required (max 100 chars)
- Email must be valid format when provided
- Phone numbers max 50 chars
- All string lengths are enforced at both validation and database level
