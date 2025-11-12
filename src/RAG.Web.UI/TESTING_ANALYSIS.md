# Analiza Testów - RAG.Web.UI

## 📊 Obecny Stan Testów

### ✅ Co jest dobrze zorganizowane:

1. **Konfiguracja testów:**
   - ✅ Vitest jako framework testowy
   - ✅ React Testing Library do testowania komponentów
   - ✅ jsdom jako środowisko testowe
   - ✅ Setup file z mockami (window.matchMedia, IntersectionObserver, etc.)
   - ✅ Test utilities z providerami (AllTheProviders)

2. **Struktura testów:**
   - ✅ Testy są blisko komponentów (np. `LoginForm.test.tsx` obok `LoginForm.tsx`)
   - ✅ Osobny folder `__tests__` dla RouteGuards
   - ✅ Testy dla utility functions (date, validation, cn)

3. **Jakość istniejących testów:**
   - ✅ Testy używają dobrych praktyk (Arrange-Act-Assert)
   - ✅ Mockowanie kontekstów jest poprawne
   - ✅ Testy pokrywają podstawowe scenariusze

### ❌ Problemy i Braki:

#### 1. **Testy nie są zaktualizowane do React 19**

**LoginForm.test.tsx:**
- ❌ Nie testuje `useActionState` - testy używają starych mocków
- ❌ Nie testuje `SubmitButton` z `useFormStatus`
- ❌ Testy sprawdzają `mockLogin.mockResolvedValue`, ale faktyczny kod używa `formAction`
- ❌ Test "should show loading state" sprawdza `loading: true` z AuthContext, ale faktyczny kod używa `useFormStatus` w SubmitButton

**Przykład problemu:**
```typescript
// Test sprawdza:
expect(mockLogin).toHaveBeenCalledWith({...})

// Ale faktyczny kod używa:
<form action={formAction}>
  <SubmitButton>...</SubmitButton>
</form>
```

#### 2. **Brak testów dla nowych komponentów React 19**

- ❌ **SubmitButton** - brak testów dla `useFormStatus`
- ❌ **MessageItem** - brak testów dla `React.memo`
- ❌ **ThreadItem** - brak testów dla `React.memo`
- ❌ **SearchResultItem** - brak testów dla `React.memo`
- ❌ **PostCard** - brak testów dla `React.memo`

#### 3. **Brak testów dla nowych hooków**

- ❌ **useOptimisticMutation** - brak testów
- ❌ **useDeferredSearch** - brak testów
- ❌ **useAsyncData** - brak testów
- ❌ **useAsyncComponent** - brak testów

#### 4. **Brak testów dla zintegrowanych funkcji React 19**

- ❌ **useOptimistic** w ChatInterface - brak testów
- ❌ **useOptimistic** w ForumPage - brak testów
- ❌ **useOptimistic** w AddressBook - brak testów
- ❌ **useDeferredValue** w wyszukiwarkach - brak testów

#### 5. **Brak testów dla zmigrowanych formularzy**

- ❌ **RegisterForm** - brak testów (używa SubmitButton)
- ❌ **ContactForm** - brak testów (używa useActionState)
- ❌ **SettingsForm** - brak testów (używa useActionState)

#### 6. **Problemy z konfiguracją**

- ⚠️ Vitest config nie ma specjalnej konfiguracji dla React 19
- ⚠️ Brak testów dla Suspense boundaries (potrzebne dla `use()` hook)
- ⚠️ Brak testów dla Error Boundaries

## 🔧 Co trzeba naprawić:

### Priorytet 1: Zaktualizować LoginForm.test.tsx

**Problemy:**
1. Testy nie uwzględniają `useActionState`
2. Testy nie sprawdzają `SubmitButton` z `useFormStatus`
3. Testy używają starych mocków zamiast testować faktyczne zachowanie

**Rozwiązanie:**
```typescript
// Przykład poprawnego testu:
it('should disable submit button during form submission', async () => {
  const user = userEvent.setup()
  mockLogin.mockImplementation(() => new Promise(resolve => setTimeout(() => resolve(true), 100)))
  
  await renderLoginForm()
  
  const submitButton = screen.getByRole('button', { name: /sign in/i })
  await user.click(submitButton)
  
  // SubmitButton używa useFormStatus, więc powinien być disabled
  expect(submitButton).toBeDisabled()
  expect(screen.getByText(/signing in/i)).toBeInTheDocument()
})
```

### Priorytet 2: Dodać testy dla SubmitButton

**Brakujące testy:**
- Test `useFormStatus` integration
- Test disabled state podczas pending
- Test loadingText display
- Test showSpinner prop

### Priorytet 3: Dodać testy dla React.memo komponentów

**Brakujące testy:**
- MessageItem - test że nie re-renderuje się niepotrzebnie
- ThreadItem - test że nie re-renderuje się niepotrzebnie
- SearchResultItem - test że nie re-renderuje się niepotrzebnie
- PostCard - test że nie re-renderuje się niepotrzebnie

### Priorytet 4: Dodać testy dla useOptimistic

**Brakujące testy:**
- ChatInterface - test optymistycznych wiadomości
- ForumPage - test optymistycznych wątków
- AddressBook - test optymistycznych kontaktów
- Test rollback przy błędzie

### Priorytet 5: Dodać testy dla useDeferredValue

**Brakujące testy:**
- ForumPage - test deferred search
- useMultilingualSearch - test deferred query
- Test że UI pozostaje responsywne podczas wpisywania

## 📋 Plan Naprawy

### Faza 1: Naprawa istniejących testów (2-3 dni)
1. ✅ Zaktualizować `LoginForm.test.tsx` do React 19
2. ✅ Dodać testy dla `SubmitButton`
3. ✅ Zaktualizować testy aby używały faktycznych komponentów

### Faza 2: Testy dla nowych komponentów (2-3 dni)
1. ✅ Dodać testy dla `MessageItem`
2. ✅ Dodać testy dla `ThreadItem`
3. ✅ Dodać testy dla `SearchResultItem`
4. ✅ Dodać testy dla `PostCard`

### Faza 3: Testy dla hooków (2-3 dni)
1. ✅ Dodać testy dla `useOptimisticMutation`
2. ✅ Dodać testy dla `useDeferredSearch`
3. ✅ Dodać testy dla `useAsyncData`
4. ✅ Dodać testy dla `useAsyncComponent`

### Faza 4: Testy integracyjne React 19 (2-3 dni)
1. ✅ Testy dla `useOptimistic` w komponentach
2. ✅ Testy dla `useDeferredValue` w wyszukiwarkach
3. ✅ Testy dla `useActionState` w formularzach
4. ✅ Testy dla Suspense boundaries

### Faza 5: Testy dla pozostałych formularzy (1-2 dni)
1. ✅ Dodać testy dla `RegisterForm`
2. ✅ Dodać testy dla `ContactForm`
3. ✅ Dodać testy dla `SettingsForm`

## 🎯 Oczekiwane Rezultaty

Po naprawie:
- ✅ Wszystkie testy przechodzą z React 19
- ✅ Testy pokrywają nowe funkcjonalności React 19
- ✅ Testy są aktualne i odzwierciedlają faktyczny kod
- ✅ Pokrycie testami > 70% dla nowych komponentów
- ✅ Testy są szybkie i niezawodne

## 📝 Uwagi Techniczne

### Mockowanie React 19 hooks:

```typescript
// useFormStatus wymaga form context
vi.mock('react-dom', async () => {
  const actual = await vi.importActual('react-dom')
  return {
    ...actual,
    useFormStatus: () => ({ pending: false }),
  }
})

// useActionState wymaga specjalnego mockowania
// Najlepiej testować przez faktyczne renderowanie formularza
```

### Testowanie Suspense:

```typescript
import { Suspense } from 'react'

it('should handle Suspense boundary', async () => {
  render(
    <Suspense fallback={<div>Loading...</div>}>
      <ComponentUsingUseHook />
    </Suspense>
  )
  
  await waitFor(() => {
    expect(screen.getByText('Loaded')).toBeInTheDocument()
  })
})
```

## 🔗 Przydatne Linki

- [Vitest React Testing](https://vitest.dev/guide/testing-react.html)
- [React Testing Library](https://testing-library.com/docs/react-testing-library/intro/)
- [Testing React 19 Features](https://react.dev/reference/react/useActionState#testing)

