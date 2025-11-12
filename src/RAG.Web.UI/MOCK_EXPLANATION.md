# Wyjaśnienie Mocków w SettingsForm.test.tsx

## Czy mock wymaga działającego serwera?

**NIE** - mock nie wymaga działającego serwera. `http://localhost:11434` to tylko **wartość tekstowa** w mocku, nie prawdziwe połączenie HTTP.

## Jak działa mock:

### 1. Mockowanie serwisu

```typescript
// Linia 42-48: Mock całego serwisu llm.service
vi.mock('@/features/settings/services/llm.service', () => ({
  default: {
    getLlmSettings: mockGetLlmSettings,  // ← Mock funkcji
    updateLlmSettings: mockUpdateLlmSettings,
    getAvailableLlmModelsFromUrl: mockGetAvailableLlmModelsFromUrl,
  },
}))
```

### 2. Co zwraca mock

```typescript
// Linia 64-73: Mock zwraca obiekt z wartościami
mockGetLlmSettings.mockResolvedValue({
  url: 'http://localhost:11434',  // ← To jest tylko STRING, nie prawdziwe połączenie!
  maxTokens: 3000,
  temperature: 0.7,
  model: 'llama2',
  isOllama: true,
  timeoutMinutes: 15,
  chatEndpoint: '/api/chat',
  generateEndpoint: '/api/generate',
})
```

### 3. Co się dzieje w teście:

1. **SettingsForm** wywołuje `llmService.getLlmSettings()`
2. **Zamiast prawdziwego HTTP request** → wywoływany jest **mock** (`mockGetLlmSettings`)
3. **Mock zwraca obiekt** z wartościami (w tym `url: 'http://localhost:11434'`)
4. **Brak prawdziwego połączenia HTTP** - wszystko dzieje się w pamięci

## Potencjalny problem:

### `loadAvailableModels` może próbować wywołać prawdziwy HTTP request

W `SettingsForm.tsx` (linia 87-91):
```typescript
useEffect(() => {
  if (settings.url.trim()) {
    loadAvailableModels()  // ← To wywołuje getAvailableLlmModelsFromUrl
  }
}, [loadAvailableModels, settings.url])
```

**Ale:** Mock `mockGetAvailableLlmModelsFromUrl` jest skonfigurowany (linia 75):
```typescript
mockGetAvailableLlmModelsFromUrl.mockResolvedValue({ models: ['llama2', 'mistral'] })
```

Więc to też jest zamockowane i **nie wymaga działającego serwera**.

## Podsumowanie:

✅ **Mock nie wymaga działającego serwera**
✅ **`http://localhost:11434` to tylko wartość tekstowa**
✅ **Wszystkie wywołania HTTP są zamockowane**
✅ **Test działa całkowicie w izolacji**

## Jeśli test nie działa, problem może być w:

1. **Timing** - mock może nie być wywoływany w odpowiednim momencie
2. **FormData** - wartości mogą nie być poprawnie odczytywane z formularza
3. **useActionState** - React 19 może mieć problemy z aktualizacją state w testach
4. **Mock nie jest wywoływany** - sprawdź czy `mockGetLlmSettings` jest faktycznie wywoływany

## Jak sprawdzić czy mock działa:

Dodaj do testu:
```typescript
console.log('mockGetLlmSettings calls:', mockGetLlmSettings.mock.calls.length)
console.log('mockGetLlmSettings results:', mockGetLlmSettings.mock.results)
```

Lub użyj wersji debugowej:
```bash
npm test -- --run src/features/settings/components/SettingsForm.test.debug.tsx
```

