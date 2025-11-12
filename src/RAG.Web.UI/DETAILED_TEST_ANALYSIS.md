# Szczegółowa Analiza Testu "should display field errors from useActionState"

## Co widzimy w logach:

### ✅ Co działa:
1. **Mock jest wywoływany:**
   ```
   💥 mockUpdateLlmSettings IMPLEMENTATION called with: {...}
   💥 About to throw error...
   ```

2. **FormData jest poprawnie wypełnione:**
   ```
   url: http://localhost:11434
   model: llama2
   isOllama: on
   ...
   ```

3. **Submit działa:**
   - `fireEvent.submit(form)` jest wywoływany
   - `formAction` z `useActionState` jest wywoływany
   - `mockValidateLlmSettings` jest wywoływany (1 call)
   - `mockUpdateLlmSettings` jest wywoływany (1 call)

### ❌ Problem:

**Mock rzuca błąd, ale:**
```
Result type: return  ← Powinno być "throw"!
```

## Analiza kodu formAction:

```typescript
// SettingsForm.tsx linia 147
await llmService.updateLlmSettings(request)  // ← To powinno rzucić błąd

// Linia 164-175
catch (error) {
  logger.error('Failed to update LLM settings:', error)
  addToast({...})
  return {
    success: false,
    error: t('settings.llm.messages.update_error'),  // ← To powinno być w state.error
    fieldErrors: {}
  }
}
```

## Możliwe przyczyny:

### 1. Mock nie rzuca błędu poprawnie
- Mock jest wywoływany
- Mock próbuje rzucić błąd (`throw errorToThrow`)
- Ale Vitest może nie propagować błędu poprawnie

### 2. useActionState nie aktualizuje state w testach
- `formAction` zwraca błąd w `catch` bloku
- Ale `useActionState` może nie aktualizować `state` w środowisku testowym
- React 19 może mieć problemy z aktualizacją state w testach

### 3. Timing issue
- Błąd jest rzucany
- `catch` blok jest wykonywany
- `state.error` jest ustawiony
- Ale komponent nie re-renderuje się w testach

## Rozwiązanie:

### Sprawdź czy błąd jest faktycznie łapany:

Dodaj logi do SettingsForm.tsx (tymczasowo):

```typescript
catch (error) {
  console.log('🔴 CATCH BLOCK - Error caught:', error)
  logger.error('Failed to update LLM settings:', error)
  // ...
  const errorState = {
    success: false,
    error: t('settings.llm.messages.update_error'),
    fieldErrors: {}
  }
  console.log('🔴 Returning error state:', errorState)
  return errorState
}
```

### Lub sprawdź w teście czy promise jest rejected:

```typescript
// Po submitie
const promise = mockUpdateLlmSettings.mock.results[0]?.value
if (promise) {
  try {
    await promise
    console.log('❌ Promise resolved (should have rejected!)')
  } catch (error) {
    console.log('✅ Promise rejected as expected:', error)
  }
}
```

## Rekomendacja:

Najlepsze podejście - sprawdź czy `formAction` faktycznie zwraca błąd:

1. Dodaj logi do `formAction` w SettingsForm.tsx
2. Sprawdź czy `catch` blok jest wykonywany
3. Sprawdź czy `state.error` jest ustawiony
4. Sprawdź czy komponent re-renderuje się z błędem

