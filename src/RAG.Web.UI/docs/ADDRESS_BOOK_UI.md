# RAG.AddressBook UI Implementation

## Overview

Complete React implementation of the Address Book module with diacritics-insensitive search, role-based access control, and change proposal system.

## Features

### ✅ Smart Search with Diacritics Support
- **TanStack Table**: Headless table library with full control over UI
- **remove-accents**: Normalizes diacritics for search (e.g., "Kłębucki" found by searching "Klebucki")
- **Client-side filtering**: All contacts loaded once, then filtered/paginated in browser
- **Global search**: Searches across all columns (name, email, department, position, location, phone)
- **Column sorting**: Click headers to sort ascending/descending

### ✅ Role-Based Access Control
- **Guest users**: Can browse and search contacts (read-only)
- **Authenticated users**: Can propose changes (create/update/delete)
- **Admin/PowerUser**: Can directly create, update, delete contacts + review proposals + import CSV

### ✅ Change Proposal System
- Regular users cannot modify contacts directly
- Changes submitted as proposals with optional reason
- Admin/PowerUser reviews proposals and approves/rejects with comments
- Approved proposals automatically applied to contacts
- Proposals tracked with status: Pending → Approved/Rejected → Applied

### ✅ CSV Import
- Upload CSV files with Polish column names (semicolon-delimited)
- Expected format: `Imię;Nazwisko;Stanowisko;Telefon służbowy;Telefon komórkowy;Email;Lokalizacja;Wyświetlana nazwa;Notatki`
- Skip duplicates option (by email)
- Import results with success/skipped/error counts
- Admin/PowerUser only

## Components

### AddressBook.tsx (Main Component)
- Tab navigation: Contacts | Import | Proposals
- Loads contacts on mount
- Manages form/modal states
- Handles CRUD operations with role checks
- Integrates all sub-components

### ContactsTable.tsx
- TanStack Table with diacritics-insensitive global filter
- Client-side pagination (10/20/50/100 per page)
- Sortable columns
- Action buttons based on role:
  - Admin/PowerUser: Edit, Delete
  - Regular user: Propose Change
  - Guest: No actions

### ContactForm.tsx
- Modal form for create/edit
- All fields with validation
- Tags management
- Role-based submit:
  - Admin/PowerUser: Direct create/update
  - Regular user: Submit proposal

### ContactImport.tsx
- File upload with drag & drop
- CSV format instructions
- Import results display
- Skip duplicates option

### ProposalsList.tsx
- Lists pending proposals
- Shows proposal type (Create/Update/Delete)
- Review interface for Admin/PowerUser
- Approve/Reject with comments

## API Service

**File**: `src/services/addressBookService.ts`

Methods:
- `listContacts()` - Get all contacts
- `getContact(id)` - Get single contact
- `searchContacts(query)` - Search contacts
- `createContact(data)` - Create new contact (Admin/PowerUser)
- `updateContact(id, data)` - Update contact (Admin/PowerUser)
- `deleteContact(id)` - Delete contact (Admin/PowerUser)
- `proposeChange(request)` - Submit proposal (Regular user)
- `listProposals(filters)` - List proposals
- `getProposal(id)` - Get proposal details
- `reviewProposal(id, decision)` - Approve/Reject (Admin/PowerUser)
- `importContacts(csvContent)` - Import from CSV (Admin/PowerUser)
- `importContactsFromFile(file)` - Helper to read and import CSV

## Types

**File**: `src/types/addressbook.ts`

Key types:
- `Contact` - Full contact entity
- `ContactListItem` - Contact for table display
- `ContactData` - Contact fields for forms
- `CreateContactRequest` - Create payload
- `UpdateContactRequest` - Update payload (includes `isActive`)
- `ContactChangeProposal` - Proposal entity
- `ChangeProposalType` - enum: Create | Update | Delete
- `ProposalStatus` - enum: Pending | Approved | Rejected | Applied
- `ImportContactsRequest` - CSV import payload
- `ImportContactsResponse` - Import results

## Routing & Navigation

- **Route**: `/address-book` (public access in App.tsx)
- **Navigation**: Added to sidebar in `useLayout.ts` hook
- **Icon**: Users icon from lucide-react
- **i18n key**: `nav.addressBook` (already translated in all locales)

## Installation

Required packages (already installed):
```bash
npm install @tanstack/react-table remove-accents
```

## Usage

1. **As Guest**: Browse and search contacts
2. **As Regular User**: Login → Browse/Search → Click "Propose Change" → Submit proposal
3. **As Admin/PowerUser**: 
   - Direct CRUD: Click "+ Add Contact", "Edit", "Delete"
   - Review proposals: Go to "Proposals" tab → Click "Review Proposal" → Approve/Reject
   - Import CSV: Go to "Import" tab → Upload CSV file

## Technical Implementation Details

### Diacritics-Insensitive Search
```typescript
import removeAccents from 'remove-accents'

const globalDiacriticsFilter: FilterFn<ContactListItem> = (row, columnId, filterValue) => {
  const searchableValues = [
    row.original.firstName,
    row.original.lastName,
    // ... other fields
  ].filter(Boolean).join(' ')
  
  const normalizedSearchable = removeAccents(searchableValues).toLowerCase()
  const normalizedFilter = removeAccents(String(filterValue)).toLowerCase()
  
  return normalizedSearchable.includes(normalizedFilter)
}
```

### Role Detection
```typescript
const canModify = !!(isAuthenticated && 
  (user?.roles?.includes('Admin') || user?.roles?.includes('PowerUser')))
```

### Proposal Flow
1. Regular user edits contact → `handleUpdateContact()` → `proposeChange()`
2. Admin opens Proposals tab → `loadProposals()` filters by `Pending`
3. Admin reviews → `handleReviewProposal()` with `Approved`/`Rejected`
4. Backend automatically applies approved changes (status → `Applied`)

## Testing Checklist

- [ ] Guest can view contacts (no login required)
- [ ] Guest can search with diacritics (e.g., "Klebucki" finds "Kłębucki")
- [ ] Guest cannot see action buttons
- [ ] Regular user sees "Propose Change" button
- [ ] Regular user can propose create/update/delete
- [ ] Admin/PowerUser sees "Edit" and "Delete" buttons
- [ ] Admin/PowerUser can create contacts directly
- [ ] Admin/PowerUser can import CSV
- [ ] Admin/PowerUser sees pending proposals count badge
- [ ] Admin/PowerUser can approve/reject proposals
- [ ] Approved proposals automatically apply changes
- [ ] Table pagination works (10/20/50/100 per page)
- [ ] Column sorting works (click headers)
- [ ] Search updates result count dynamically

## Future Enhancements

- [ ] Email notifications when proposals reviewed
- [ ] Audit log for approved changes
- [ ] Export contacts to CSV
- [ ] Bulk operations (multi-select + bulk delete/export)
- [ ] Advanced filters (department dropdown, location dropdown, active/inactive toggle)
- [ ] Contact photos/avatars display
- [ ] Phone number formatting by locale
- [ ] VCard export/import
