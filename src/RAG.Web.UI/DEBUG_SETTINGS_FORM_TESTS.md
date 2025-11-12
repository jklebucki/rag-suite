# Polecenia do Debugowania Testów SettingsForm

## 1. Uruchomienie testów z szczegółowym outputem

```bash
cd /Users/jklebucki/Projects/rag-suite/src/RAG.Web.UI

# Uruchom tylko testy SettingsForm z verbose output
npm test -- --run src/features/settings/components/SettingsForm.test.tsx --reporter=verbose

# Uruchom z wyświetlaniem wszystkich console.log
npm test -- --run src/features/settings/components/SettingsForm.test.tsx --reporter=verbose --no-coverage

# Uruchom jeden konkretny test
npm test -- --run src/features/settings/components/SettingsForm.test.tsx -t "should use useActionState for form submission"

# Uruchom drugi test
npm test -- --run src/features/settings/components/SettingsForm.test.tsx -t "should display field errors from useActionState"
```

## 2. Uruchomienie z debuggerem (Node.js Inspector)

```bash
# Uruchom testy z debuggerem
node --inspect-brk node_modules/.bin/vitest --run src/features/settings/components/SettingsForm.test.tsx

# Następnie otwórz Chrome i przejdź do: chrome://inspect
# Kliknij "inspect" przy procesie Node.js
```

## 3. Użycie Vitest UI (interaktywne debugowanie)

```bash
# Uruchom Vitest UI
npm test -- --ui

# Następnie:
# 1. Otwórz przeglądarkę na adresie pokazany w terminalu (zwykle http://localhost:51204/__vitest__/)
# 2. Znajdź test SettingsForm
# 3. Kliknij na test aby zobaczyć szczegóły
# 4. Użyj "Inspect" aby zobaczyć co się dzieje
```

## 4. Dodanie console.log do testów (tymczasowe)

Dodaj do testów następujące logi:

```typescript
// W teście "should use useActionState for form submission"
console.log('Before submit - mockValidateLlmSettings calls:', mockValidateLlmSettings.mock.calls.length)
console.log('Before submit - mockUpdateLlmSettings calls:', mockUpdateLlmSettings.mock.calls.length)
fireEvent.submit(form)
console.log('After submit - mockValidateLlmSettings calls:', mockValidateLlmSettings.mock.calls.length)
console.log('After submit - mockUpdateLlmSettings calls:', mockUpdateLlmSettings.mock.calls.length)
```

## 5. Sprawdzenie co jest w FormData

Dodaj do testu:

```typescript
// Przed submitem
const formData = new FormData(form)
console.log('FormData entries:')
for (const [key, value] of formData.entries()) {
  console.log(`  ${key}: ${value}`)
}
```

## 6. Sprawdzenie stanu komponentu

Dodaj do testu:

```typescript
// Po submitie, sprawdź co jest w DOM
console.log('Form HTML:', form.innerHTML)
console.log('All inputs:', Array.from(form.querySelectorAll('input')).map(i => ({
  name: i.name,
  value: i.value,
  type: i.type,
  checked: (i as HTMLInputElement).checked
})))
```

## 7. Uruchomienie z większym timeoutem i szczegółami

```bash
# Uruchom z większym timeoutem (30s)
npm test -- --run src/features/settings/components/SettingsForm.test.tsx --test-timeout=30000 --reporter=verbose

# Uruchom z wyświetlaniem stack trace
npm test -- --run src/features/settings/components/SettingsForm.test.tsx --reporter=verbose --bail=1
```

## 8. Sprawdzenie mocków

Dodaj do beforeEach:

```typescript
beforeEach(() => {
  vi.clearAllMocks()
  
  // Dodaj logi do mocków
  mockGetLlmSettings.mockImplementation(async () => {
    console.log('mockGetLlmSettings called')
    return { url: 'http://localhost:11434', ... }
  })
  
  mockValidateLlmSettings.mockImplementation((settings) => {
    console.log('mockValidateLlmSettings called with:', settings)
    return { isValid: true, errors: {} }
  })
  
  mockUpdateLlmSettings.mockImplementation(async (settings) => {
    console.log('mockUpdateLlmSettings called with:', settings)
    return {}
  })
})
```

## 9. Sprawdzenie czy formAction jest wywoływany

Dodaj do SettingsForm.tsx (tymczasowo):

```typescript
const [state, formAction] = useActionState(
  async (prevState: FormState | null, formData: FormData): Promise<FormState> => {
    console.log('formAction called with FormData:', Array.from(formData.entries()))
    // ... reszta kodu
  },
  null
)
```

## 10. Uruchomienie z watch mode (do iteracyjnego debugowania)

```bash
# Uruchom w trybie watch - testy będą się uruchamiać po każdej zmianie
npm test -- src/features/settings/components/SettingsForm.test.tsx

# Następnie edytuj plik testowy i zapisz - testy uruchomią się automatycznie
```

## 11. Sprawdzenie czy useActionState działa poprawnie

Dodaj do testu:

```typescript
// Po renderze, sprawdź czy formAction istnieje
const form = document.querySelector('form') as HTMLFormElement
console.log('Form action:', form.action)
console.log('Form has onsubmit:', typeof form.onsubmit)
```

## 12. Porównanie z działającym testem

Uruchom działający test dla porównania:

```bash
# Uruchom działający test LoginForm dla porównania
npm test -- --run src/features/auth/components/LoginForm.test.tsx -t "should handle form submission with useActionState" --reporter=verbose
```

## 13. Sprawdzenie czy wszystkie mocki są poprawnie skonfigurowane

Dodaj na początku testu:

```typescript
console.log('Mock status:')
console.log('  mockGetLlmSettings:', typeof mockGetLlmSettings)
console.log('  mockValidateLlmSettings:', typeof mockValidateLlmSettings)
console.log('  mockUpdateLlmSettings:', typeof mockUpdateLlmSettings)
console.log('  mockGetAvailableLlmModelsFromUrl:', typeof mockGetAvailableLlmModelsFromUrl)
```

## 14. Uruchomienie z wyświetlaniem błędów React

```bash
# Uruchom z wyświetlaniem wszystkich warningów React
NODE_ENV=development npm test -- --run src/features/settings/components/SettingsForm.test.tsx --reporter=verbose
```

## 15. Sprawdzenie czy fireEvent.submit działa

Dodaj do testu:

```typescript
// Sprawdź czy event jest wywoływany
form.addEventListener('submit', (e) => {
  console.log('Form submit event fired!', e)
  console.log('Event defaultPrevented:', e.defaultPrevented)
})

fireEvent.submit(form)
```

## Najlepsze podejście do debugowania:

1. **Zacznij od prostego**: Uruchom test z `--reporter=verbose` aby zobaczyć co się dzieje
2. **Dodaj console.log**: Dodaj logi do mocków i testów aby zobaczyć kolejność wywołań
3. **Użyj Vitest UI**: Najlepsze narzędzie do interaktywnego debugowania
4. **Sprawdź FormData**: Upewnij się że FormData zawiera wszystkie potrzebne wartości
5. **Porównaj z działającym testem**: Zobacz jak LoginForm test działa i porównaj

## Szybkie polecenie do rozpoczęcia:

```bash
cd /Users/jklebucki/Projects/rag-suite/src/RAG.Web.UI
npm test -- --ui
```

Następnie w przeglądarce znajdź testy SettingsForm i użyj funkcji "Inspect" aby zobaczyć szczegóły wykonania.

