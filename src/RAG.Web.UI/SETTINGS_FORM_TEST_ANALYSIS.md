# Dogłębna Analiza Testów SettingsForm

## Problem
Dwa testy timeoutują:
1. `should use useActionState for form submission`
2. `should display field errors from useActionState`

## Analiza Kodu

### 1. Implementacja SettingsForm
- Używa `useActionState` z `formAction`
- Formularz ładuje dane asynchronicznie w `useEffect` przez `loadSettings()`
- `formAction` czyta dane z `FormData` (nie ze state React)
- Checkbox `isOllama` używa `checked={value}` w LlmFormField

### 2. Problem z FormData
W `formAction` (linia 113):
```typescript
isOllama: formData.get('isOllama') === 'on'
```

**Problem**: Checkbox w HTML zwraca `'on'` gdy jest checked, ale jeśli checkbox nie jest w DOM lub nie jest poprawnie zrenderowany, `formData.get('isOllama')` zwróci `null`.

### 3. Problem z asynchronicznym ładowaniem
Test czeka na:
1. `mockGetLlmSettings` aby zostać wywołany
2. Wartość inputa `url` aby być `'http://localhost:11434'`

Ale może być problem z:
- `loadAvailableModels` jest wywoływany po załadowaniu settings (linia 87-91)
- To może powodować dodatkowe renderowania
- Formularz może nie być w pełni gotowy gdy klikamy submit

### 4. Porównanie z innymi testami
**ContactForm.test.tsx** (działa):
- Używa `user.type()` przed submitem
- Nie czeka na asynchroniczne ładowanie danych

**LoginForm.test.tsx** (działa):
- Używa `user.type()` przed submitem
- Formularz nie ładuje danych asynchronicznie

**SettingsForm.test.tsx** (nie działa):
- Czeka na asynchroniczne ładowanie danych
- Nie używa `user.type()` - zakłada że wartości są już w formularzu

## Główne Problemy

### Problem 1: Timing Issue
Formularz może nie być w pełni zrenderowany gdy klikamy submit. `loadAvailableModels` może być jeszcze w trakcie wykonywania.

### Problem 2: FormData może być niepełne
Jeśli checkbox `isOllama` nie jest poprawnie zrenderowany lub nie ma wartości w FormData, walidacja może nie działać poprawnie.

### Problem 3: Mock może nie być wywoływany
Jeśli `formAction` nie jest wywoływany lub nie może odczytać FormData, `validateLlmSettings` i `updateLlmSettings` nie będą wywołane.

## Rozwiązanie

1. **Dodać więcej debugowania** - sprawdzić czy formAction jest wywoływany
2. **Upewnić się że formularz jest w pełni załadowany** - czekać na zakończenie wszystkich efektów
3. **Sprawdzić czy FormData zawiera wszystkie wartości** - szczególnie checkbox
4. **Użyć bardziej realistycznego podejścia** - może użyć `fireEvent.submit` zamiast `user.click`

