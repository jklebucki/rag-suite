# Analiza Wyników Testu SettingsForm

## Co działa ✅

1. **FormData jest poprawnie wypełnione:**
   ```
   url: http://localhost:11434
   model: llama2
   maxTokens: 3000
   temperature: 0.7
   isOllama: on
   timeoutMinutes: 15
   chatEndpoint: /api/chat
   generateEndpoint: /api/generate
   ```

2. **Submit działa poprawnie:**
   - `fireEvent.submit(form)` jest wywoływany
   - `formAction` z `useActionState` jest wywoływany

3. **Mocki są wywoływane:**
   - `mockValidateLlmSettings` - ✅ wywołany (1 call)
   - `mockUpdateLlmSettings` - ✅ wywołany (1 call)

## Problem ❌

### Główny problem: Mock nie rzuca błędu

```
Result type: return  ← To powinno być "throw"!
```

**Co się dzieje:**
- Test konfiguruje: `mockUpdateLlmSettings.mockRejectedValue(new Error('Validation failed'))`
- Ale mock zwraca wartość zamiast rzucać błąd
- `formAction` w `useActionState` nie trafia do bloku `catch`
- `state.error` nie jest ustawiony
- Błąd nie jest wyświetlony w UI

### Dlaczego mock nie rzuca błędu?

**Możliwe przyczyny:**

1. **Mock jest resetowany w `beforeEach`:**
   ```typescript
   beforeEach(() => {
     mockUpdateLlmSettings.mockResolvedValue({})  // ← To nadpisuje mockRejectedValue!
   })
   ```

2. **Kolejność wywołań:**
   - `beforeEach` ustawia `mockResolvedValue({})`
   - Test ustawia `mockRejectedValue(new Error(...))`
   - Ale może być problem z timingiem

3. **Mock może być wywoływany asynchronicznie:**
   - `formAction` jest async
   - Mock może być wywoływany przed ustawieniem `mockRejectedValue`

## Rozwiązanie

### Opcja 1: Ustaw mock przed renderowaniem

```typescript
it('should display field errors from useActionState', async () => {
  // Ustaw mock PRZED renderowaniem
  mockUpdateLlmSettings.mockRejectedValue(new Error('Validation failed'))
  mockValidateLlmSettings.mockReturnValue({ isValid: true, errors: {} })

  render(<SettingsForm />)
  // ...
})
```

### Opcja 2: Użyj `mockImplementation` zamiast `mockRejectedValue`

```typescript
mockUpdateLlmSettings.mockImplementation(async () => {
  throw new Error('Validation failed')
})
```

### Opcja 3: Wyczyść mock przed ustawieniem nowego

```typescript
beforeEach(() => {
  vi.clearAllMocks()
  // NIE ustawiaj mockResolvedValue tutaj dla tego testu
})

it('should display field errors from useActionState', async () => {
  // Wyczyść i ustaw nowy mock
  mockUpdateLlmSettings.mockReset()
  mockUpdateLlmSettings.mockRejectedValue(new Error('Validation failed'))
  // ...
})
```

## Rekomendacja

Najlepsze rozwiązanie: **Ustaw mock PRZED renderowaniem i użyj `mockImplementation` dla większej kontroli:**

```typescript
it('should display field errors from useActionState', async () => {
  // Ustaw mock PRZED renderowaniem
  mockUpdateLlmSettings.mockReset()
  mockUpdateLlmSettings.mockImplementation(async () => {
    console.log('Mock throwing error...')
    throw new Error('Validation failed')
  })
  mockValidateLlmSettings.mockReturnValue({ isValid: true, errors: {} })

  render(<SettingsForm />)
  // ...
})
```

