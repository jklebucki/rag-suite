# Strategia Testowania - RAG.Web.UI

## Przegląd

Ten dokument opisuje kompleksową strategię testowania dla aplikacji RAG.Web.UI. Projekt używa React, TypeScript, Vite i wymaga solidnego pokrycia testami.

## Narzędzia Testowe

### Główne narzędzia:
- **Vitest** - framework testowy (kompatybilny z Vite)
- **React Testing Library** - testowanie komponentów React
- **@testing-library/jest-dom** - dodatkowe matchery dla DOM
- **@testing-library/user-event** - symulacja interakcji użytkownika
- **MSW (Mock Service Worker)** - mockowanie API calls
- **@vitest/ui** - interfejs graficzny do testów

## Struktura Testów (Co-located - Best Practice)

```
src/
├── components/
│   └── auth/
│       ├── LoginForm.tsx
│       └── LoginForm.test.tsx      # Test obok pliku źródłowego
├── hooks/
│   ├── useLayout.ts
│   └── useLayout.test.ts            # Test obok pliku źródłowego
├── services/
│   ├── api.ts
│   └── api.test.ts                  # Test obok pliku źródłowego
├── utils/
│   ├── validation.ts
│   └── validation.test.ts           # Test obok pliku źródłowego
└── contexts/
    ├── AuthContext.tsx
    └── AuthContext.test.tsx         # Test obok pliku źródłowego
```

**Zalety co-located tests:**
- ✅ Testy są łatwe do znalezienia (obok pliku źródłowego)
- ✅ Zgodne z Clean Code (bliskość kodu i testów)
- ✅ Łatwiejsze utrzymanie i refactoring
- ✅ Standard w React/TypeScript community

## Priorytety Testowania

### 🔴 Wysoki Priorytet (Krytyczne)

#### 1. **Utils - Funkcje Walidacyjne** (`utils/validation.ts`)
- ✅ `validateEmail()` - różne formaty emaili
- ✅ `validatePassword()` - siła hasła, długość
- ✅ `validatePasswordMatch()` - zgodność haseł
- ✅ `validateRequired()` - wymagane pola
- ✅ `validateLength()` - długość stringów
- ✅ `validateUsername()` - format username
- ✅ `combineValidations()` - kombinowanie walidacji

**Lokalizacja:** `src/utils/validation.test.ts` (co-located)

#### 2. **Utils - Funkcje Daty** (`utils/date.ts`)
- ✅ `formatDateTime()` - formatowanie daty z czasem
- ✅ `formatDate()` - formatowanie daty
- ✅ `formatRelativeTime()` - względny czas (wszystkie języki)

**Lokalizacja:** `src/utils/date.test.ts` (co-located)

#### 3. **Services - API Client** (`services/api.ts`)
- ✅ Mockowanie HTTP requests
- ✅ Obsługa błędów
- ✅ Timeout handling
- ✅ Retry logic
- ✅ Wszystkie endpointy API

**Lokalizacja:** `src/services/api.test.ts` (co-located)

#### 4. **Auth Context** (`contexts/AuthContext.tsx`)
- ✅ Login flow
- ✅ Logout flow
- ✅ Token refresh
- ✅ Error handling
- ✅ State management

**Lokalizacja:** `src/contexts/AuthContext.test.tsx` (co-located)

#### 5. **Komponenty Autoryzacji**
- ✅ `LoginForm` - walidacja, submit, błędy
- ✅ `RegisterForm` - walidacja, rejestracja
- ✅ `ProtectedRoute` - redirect logic
- ✅ `AdminProtectedRoute` - kontrola dostępu
- ✅ `RoleProtectedRoute` - kontrola ról

**Lokalizacja:** `src/components/auth/*.test.tsx` (co-located)

### 🟡 Średni Priorytet (Ważne)

#### 6. **Hooks**
- ✅ `useAuthStorage` - localStorage operations
- ✅ `useTokenRefresh` - token refresh logic
- ✅ `useSearch` - search functionality
- ✅ `useQuizzes` - quiz operations
- ✅ `useQuizBuilder` - quiz building
- ✅ `useDashboard` - dashboard data
- ✅ `useErrorHandler` - error handling
- ✅ `useLayout` - navigation logic

**Lokalizacja:** `src/hooks/*.test.ts` (co-located)

#### 7. **Komponenty UI**
- ✅ `Button` - różne warianty, stany
- ✅ `Input` - walidacja, stany
- ✅ `Modal` - otwieranie/zamykanie
- ✅ `Toast` - wyświetlanie komunikatów
- ✅ `Card` - renderowanie

**Lokalizacja:** `src/components/ui/*.test.tsx` (co-located)

#### 8. **Komponenty Funkcjonalne**
- ✅ `ChatInterface` - wysyłanie wiadomości, renderowanie
- ✅ `SearchInterface` - wyszukiwanie, wyniki
- ✅ `Dashboard` - wyświetlanie statystyk
- ✅ `Settings` - konfiguracja
- ✅ `AddressBook` - CRUD operacje

**Lokalizacja:** `src/components/**/*.test.tsx` (co-located)

#### 9. **I18n Context** (`contexts/I18nContext.tsx`)
- ✅ Zmiana języka
- ✅ Tłumaczenia
- ✅ Fallback handling

**Lokalizacja:** `src/contexts/I18nContext.test.tsx` (co-located)

### 🟢 Niski Priorytet (Nice to Have)

#### 10. **Integracje**
- ✅ Routing (React Router)
- ✅ React Query integration
- ✅ Form handling (react-hook-form)

#### 11. **E2E Testy** (opcjonalnie z Playwright/Cypress)
- ✅ Pełny flow logowania
- ✅ Flow wyszukiwania
- ✅ Flow chat
- ✅ Quiz flow

## Przykłady Testów

### Test Utility Function

```typescript
// src/utils/validation.test.ts
import { describe, it, expect } from 'vitest'
import { validateEmail, validatePassword } from './validation'

describe('validateEmail', () => {
  it('should validate correct email', () => {
    expect(validateEmail('test@example.com')).toBe(true)
  })
  
  it('should reject invalid email', () => {
    expect(validateEmail('invalid')).toBe(false)
  })
})
```

### Test Hook

```typescript
// src/hooks/useSearch.test.ts
import { renderHook, waitFor } from '@testing-library/react'
import { describe, it, expect, vi } from 'vitest'
import { useSearch } from './useSearch'

describe('useSearch', () => {
  it('should perform search', async () => {
    const { result } = renderHook(() => useSearch())
    // test implementation
  })
})
```

### Test Component

```typescript
// src/components/auth/LoginForm.test.tsx
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { describe, it, expect, vi } from 'vitest'
import { LoginForm } from './LoginForm'

describe('LoginForm', () => {
  it('should render login form', () => {
    render(<LoginForm />)
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument()
  })
  
  it('should validate email on submit', async () => {
    render(<LoginForm />)
    const emailInput = screen.getByLabelText(/email/i)
    fireEvent.change(emailInput, { target: { value: 'invalid' } })
    fireEvent.click(screen.getByRole('button', { name: /login/i }))
    
    await waitFor(() => {
      expect(screen.getByText(/invalid email/i)).toBeInTheDocument()
    })
  })
})
```

## Konfiguracja

### Vitest Config (`vitest.config.ts`)
- Konfiguracja środowiska testowego
- Path aliases (@/)
- Coverage settings
- Mock setup

### Test Utilities (`src/test-utils/`)
- `renderWithProviders` - wrapper z providerami
- `createMockUser` - mock user data
- `createMockApiResponse` - mock API responses

## Coverage Goals

- **Utils:** 100% coverage
- **Services:** 90%+ coverage
- **Hooks:** 85%+ coverage
- **Components:** 80%+ coverage
- **Contexts:** 90%+ coverage
- **Overall:** 80%+ coverage

## CI/CD Integration

Testy powinny być uruchamiane:
- Przed każdym commit (pre-commit hook)
- W CI/CD pipeline
- Przed merge do main branch

## Uruchamianie Testów

```bash
# Wszystkie testy
npm test

# Watch mode
npm test -- --watch

# Coverage
npm test -- --coverage

# UI mode
npm test -- --ui

# Konkretny plik
npm test validation.test.ts
```

## Best Practices

1. **Testowanie zachowania, nie implementacji**
2. **Używanie user-centric queries** (getByRole, getByLabelText)
3. **Mockowanie zewnętrznych zależności**
4. **Czytelne nazwy testów** (should do X when Y)
5. **Arrange-Act-Assert pattern**
6. **Unikanie testowania szczegółów implementacji**
7. **Testowanie edge cases**
8. **Utrzymywanie testów szybkich i izolowanych**

## Następne Kroki

1. ✅ Skonfigurować Vitest
2. ✅ Dodać zależności testowe
3. ✅ Utworzyć przykładowe testy dla utils
4. ✅ Utworzyć test utilities
5. ⏳ Dodać testy dla hooks
6. ⏳ Dodać testy dla komponentów
7. ⏳ Dodać testy dla services
8. ⏳ Skonfigurować coverage reporting
9. ⏳ Dodać do CI/CD

