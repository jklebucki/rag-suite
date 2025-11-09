# Przewodnik po Dark Mode w RAG.Web.UI

## Szybki start

### Używanie przełącznika motywu

1. **Znajdź przełącznik** - Ikona słońca/księżyca znajduje się w prawym górnym rogu TopBar, obok selectora języka
2. **Kliknij ikonę** - Motyw przełączy się natychmiastowo
3. **Automatyczny zapis** - Twój wybór zostanie zapisany w localStorage

### Dla użytkowników

- 🌙 **Ciemny motyw** - Ikona księżyca, ciemne tło
- ☀️ **Jasny motyw** - Ikona słońca, jasne tło
- 💾 **Automatyczne zapisywanie** - Wybór jest zapamiętywany między sesjami
- 🔄 **Detekcja systemowa** - Przy pierwszym użyciu aplikacja wykryje preferencje systemowe

## Dla programistów

### Dodawanie dark mode do nowych komponentów

#### 1. Podstawowe zasady

```tsx
// ✅ Dobrze - używaj klas dark:
<div className="bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100">
  Zawartość
</div>

// ❌ Źle - nie używaj tylko jasnych kolorów
<div className="bg-white text-gray-900">
  Zawartość
</div>
```

#### 2. Standardowe pary kolorów

```tsx
// Tła
bg-white → dark:bg-gray-800
bg-gray-50 → dark:bg-gray-900
bg-gray-100 → dark:bg-gray-800

// Tekst
text-gray-900 → dark:text-gray-100
text-gray-700 → dark:text-gray-300
text-gray-600 → dark:text-gray-300

// Obramowania
border-gray-200 → dark:border-gray-700
border-gray-300 → dark:border-gray-600

// Hover
hover:bg-gray-100 → dark:hover:bg-gray-700
hover:bg-gray-50 → dark:hover:bg-gray-800
```

#### 3. Przykład kompletnego komponentu

```tsx
export function MyComponent() {
  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md">
      {/* Header */}
      <div className="border-b border-gray-200 dark:border-gray-700 p-4">
        <h2 className="text-xl font-bold text-gray-900 dark:text-gray-100">
          Tytuł
        </h2>
      </div>
      
      {/* Content */}
      <div className="p-4">
        <p className="text-gray-600 dark:text-gray-300">
          Treść komponentu
        </p>
        
        <button className="mt-4 bg-blue-600 hover:bg-blue-700 dark:bg-blue-500 dark:hover:bg-blue-600 text-white px-4 py-2 rounded">
          Akcja
        </button>
      </div>
    </div>
  )
}
```

#### 4. Używanie ThemeContext

```tsx
import { useTheme } from '@/shared/contexts/ThemeContext'

export function MyComponent() {
  const { theme, setTheme, toggleTheme } = useTheme()
  
  return (
    <div>
      <p>Aktualny motyw: {theme}</p>
      <button onClick={toggleTheme}>Przełącz motyw</button>
      <button onClick={() => setTheme('dark')}>Ustaw ciemny</button>
      <button onClick={() => setTheme('light')}>Ustaw jasny</button>
    </div>
  )
}
```

### Komponenty z pełną obsługą dark mode

Następujące komponenty można bezpośrednio używać - mają już pełną obsługę:

#### UI Components
- `Button` - wszystkie warianty
- `Card`, `CardHeader`, `CardTitle`, `CardContent`
- `Input`
- `Textarea`
- `Modal`
- `ConfirmModal`
- `Toast`
- `ThemeToggle`
- `LanguageSelector`
- `SessionExpiredModal`
- `ConnectionStatus`

#### Layout Components
- `Layout`
- `Sidebar`
- `TopBar`

#### Common Components
- `ErrorBoundary`
- `DeleteConfirmationModal`

### Testowanie dark mode

```bash
# Uruchom aplikację
npm run dev

# Sprawdź:
# 1. Przełącznik w TopBar działa
# 2. Wszystkie komponenty wyglądają dobrze w obu motywach
# 3. Persystencja działa (odśwież stronę)
# 4. Zmień preferencje systemowe (sprawdź detekcję)
```

### Debugowanie

#### Problem: Komponent nie zmienia się z motywem

```tsx
// Sprawdź czy używasz klas dark:
// ❌ Źle
<div className="bg-white">

// ✅ Dobrze
<div className="bg-white dark:bg-gray-800">
```

#### Problem: Błąd "useTheme must be used within ThemeProvider"

Upewnij się że komponent jest wewnątrz ThemeProvider (powinien być automatycznie przez AppProviders)

```tsx
// src/app/AppProviders.tsx
<ThemeProvider>
  <YourAppHere />
</ThemeProvider>
```

#### Problem: Migotanie przy ładowaniu

ThemeContext automatycznie stosuje motyw z localStorage przed renderowaniem, więc migotanie powinno być minimalne. Jeśli występuje:

1. Sprawdź czy `index.html` ma `class="light"` na `<html>`
2. Sprawdź czy ThemeProvider jest na górze hierarchii providerów

### Najlepsze praktyki

1. **Zawsze dodawaj dark mode do nowych komponentów** - to łatwiejsze niż dodawanie później
2. **Używaj standardowych par kolorów** - zachowaj spójność z resztą aplikacji
3. **Testuj w obu motywach** - przed commitowaniem kodu
4. **Używaj gradientów ostrożnie** - mogą wyglądać źle w dark mode
5. **Dodawaj płynne przejścia** - `transition-colors duration-200`

### Dostępne klasy pomocnicze

```css
/* src/index.css */

/* Przyciski */
.btn-primary /* Ma już obsługę dark mode */
.btn-secondary /* Ma już obsługę dark mode */

/* Podświetlenia wyszukiwania */
.search-highlights em /* Ma już obsługę dark mode */
```

### Customizacja kolorów

W `tailwind.config.js` możesz dostosować kolory:

```js
theme: {
  extend: {
    colors: {
      primary: {
        // Pełna paleta 50-950
        500: '#3b82f6', // Używane w jasnym motywie
        600: '#2563eb', // Używane w ciemnym motywie
      },
    },
  },
}
```

## FAQ

**Q: Czy mogę dodać więcej motywów (np. sepia)?**
A: Tak! Rozszerz ThemeContext o nowe typy i dodaj odpowiednie klasy w Tailwind.

**Q: Czy motyw jest synchronizowany między zakładkami?**
A: Nie automatycznie, ale możesz dodać `storage` event listener w ThemeContext.

**Q: Czy mogę wykrywać preferencje systemowe na bieżąco?**
A: Tak! ThemeContext już ma listener dla `prefers-color-scheme` media query.

**Q: Jak dodać obrazy różne dla każdego motywu?**
A: Użyj conditional rendering z `useTheme()`:
```tsx
const { theme } = useTheme()
return <img src={theme === 'dark' ? '/logo-dark.png' : '/logo-light.png'} />
```

## Wsparcie

W razie problemów:
1. Sprawdź `TAILWIND_MIGRATION.md` - szczegółowa dokumentacja techniczna
2. Sprawdź browser console - błędy TypeScript/React
3. Użyj React DevTools - sprawdź czy ThemeContext ma poprawny stan

## Zasoby

- [Tailwind CSS Dark Mode](https://tailwindcss.com/docs/dark-mode)
- [React Context API](https://react.dev/reference/react/useContext)
- [localStorage API](https://developer.mozilla.org/en-US/docs/Web/API/Window/localStorage)
- [prefers-color-scheme](https://developer.mozilla.org/en-US/docs/Web/CSS/@media/prefers-color-scheme)

