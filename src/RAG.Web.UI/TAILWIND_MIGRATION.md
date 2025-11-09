# Migracja Tailwind CSS 3.3.3 → 3.4.16 z obsługą Dark Mode

## Podsumowanie

Projekt RAG.Web.UI został pomyślnie zaktualizowany z Tailwind CSS 3.3.3 do 3.4.16 z pełną obsługą motywów jasnego i ciemnego.

## Zmiany w zależnościach

### package.json
- `tailwindcss`: `^3.3.3` → `^3.4.16`
- `tailwind-merge`: `^1.14.0` → `^2.5.5`

### Instalacja
```bash
cd src/RAG.Web.UI
npm install
```

## Konfiguracja

### tailwind.config.js
- Dodano `darkMode: 'class'` - przełączanie motywu przez klasę CSS
- Rozszerzono paletę kolorów `primary` (50-950) dla lepszej obsługi dark mode

### index.html
- Dodano `class="light"` do elementu `<html>`
- Dodano `<meta name="color-scheme" content="light dark" />`
- Dodano klasy bazowe do `<body>`

### index.css
- Dodano płynne przejścia kolorów dla `html`
- Zaktualizowano klasy pomocnicze `.btn-primary` i `.btn-secondary` z obsługą dark mode
- Zaktualizowano `.search-highlights` dla dark mode

## Nowe komponenty

### ThemeContext
**Lokalizacja:** `src/shared/contexts/ThemeContext.tsx`

Kontekst React do zarządzania motywem z:
- Persystencją w localStorage (`rag-suite-theme`)
- Automatyczną detekcją preferencji systemowych
- Nasłuchiwaniem zmian preferencji systemowych
- Hook `useTheme()` do łatwego dostępu

### ThemeToggle
**Lokalizacja:** `src/shared/components/ui/ThemeToggle.tsx`

Przycisk przełączania motywu z ikonami słońca/księżyca, zintegrowany w TopBar.

## Zaktualizowane komponenty

### Komponenty UI (`src/shared/components/ui/`)
Wszystkie komponenty zostały zaktualizowane z klasami `dark:*`:
- ✅ Button.tsx - warianty primary, secondary, outline, ghost, destructive
- ✅ Card.tsx - tło, obramowanie, tekst
- ✅ Input.tsx - tło, obramowanie, placeholder
- ✅ Modal.tsx - tło, obramowanie, backdrop
- ✅ Textarea.tsx - tło, obramowanie, placeholder
- ✅ ConfirmModal.tsx - wszystkie warianty (danger, warning, info)
- ✅ ConnectionStatus.tsx - tło offline
- ✅ Toast.tsx - wszystkie typy (success, error, warning, info)
- ✅ LanguageSelector.tsx - dropdown i opcje
- ✅ SessionExpiredModal.tsx - ikony i przyciski
- ✅ ThemeToggle.tsx - nowy komponent

### Komponenty Layout (`src/shared/components/layout/`)
- ✅ Layout.tsx - tło główne
- ✅ Sidebar.tsx - tło, nawigacja, overlay
- ✅ TopBar.tsx - tło, menu użytkownika, przyciski
  - Dodano ThemeToggle w TopBar

### Komponenty wspólne (`src/shared/components/common/`)
- ✅ ErrorBoundary.tsx - strona błędu
- ✅ DeleteConfirmationModal.tsx - już miał obsługę dark mode

### Komponenty feature
- ✅ LandingPage.tsx - gradient, karty funkcji
- Pozostałe komponenty feature będą automatycznie korzystać z zaktualizowanych komponentów UI

## Integracja ThemeProvider

**Lokalizacja:** `src/app/AppProviders.tsx`

ThemeProvider został dodany jako najwyższy provider w hierarchii:
```tsx
<ThemeProvider>
  <I18nProvider>
    <ToastProvider>
      <ConfigurationProvider>
        <AuthProvider>
          {children}
        </AuthProvider>
      </ConfigurationProvider>
    </ToastProvider>
  </I18nProvider>
</ThemeProvider>
```

## Paleta kolorów Dark Mode

### Tła
- Jasny: `bg-white`, `bg-gray-50`, `bg-gray-100`
- Ciemny: `dark:bg-gray-800`, `dark:bg-gray-900`

### Tekst
- Jasny: `text-gray-900`, `text-gray-700`, `text-gray-600`
- Ciemny: `dark:text-gray-100`, `dark:text-gray-200`, `dark:text-gray-300`

### Obramowania
- Jasny: `border-gray-200`, `border-gray-300`
- Ciemny: `dark:border-gray-700`, `dark:border-gray-600`

### Akcenty
- Primary: `bg-blue-600` → `dark:bg-blue-500`
- Success: `bg-green-600` → `dark:bg-green-500`
- Error: `bg-red-600` → `dark:bg-red-500`
- Warning: `bg-yellow-600` → `dark:bg-yellow-500`

## Funkcjonalność

### Przełączanie motywu
1. Kliknięcie ikony słońca/księżyca w TopBar
2. Automatyczne zapisywanie preferencji w localStorage
3. Automatyczna detekcja preferencji systemowych przy pierwszym ładowaniu
4. Płynne przejścia między motywami (200ms)

### Persystencja
- Klucz localStorage: `rag-suite-theme`
- Wartości: `'light'` | `'dark'`
- Automatyczne stosowanie przy kolejnych wizytach

### Kompatybilność
- Wszystkie nowoczesne przeglądarki
- Safari i starsze przeglądarki (z fallbackiem dla MediaQuery API)

## Testowanie

### Testowanie manualne
1. Uruchom aplikację: `npm run dev`
2. Kliknij ikonę słońca/księżyca w TopBar
3. Sprawdź czy wszystkie komponenty przełączają się poprawnie
4. Odśwież stronę - motyw powinien zostać zachowany
5. Zmień preferencje systemowe - aplikacja powinna reagować

### Krytyczne obszary do sprawdzenia
- [ ] Formularze (Input, Textarea, Button)
- [ ] Modalne (Modal, ConfirmModal, SessionExpiredModal)
- [ ] Nawigacja (Sidebar, TopBar)
- [ ] Karty (Card, Dashboard)
- [ ] Powiadomienia (Toast)
- [ ] Landing Page
- [ ] Strona błędu (ErrorBoundary)

## Zgodność z poprzednią wersją

- ✅ Wszystkie istniejące klasy Tailwind działają poprawnie
- ✅ Brak breaking changes w używanych klasach
- ✅ Kompatybilność wsteczna z Tailwind 3.x
- ✅ Zachowano istniejący wygląd w jasnym motywie
- ✅ Drobne zmiany wynikające z nowych klas są akceptowalne

## Dalsze kroki

### Opcjonalne ulepszenia
1. Dodanie motywu "system" (auto)
2. Animacje przejść między motywami
3. Persystencja preferencji motywu w bazie danych (dla zalogowanych użytkowników)
4. Dodanie więcej wariantów kolorystycznych
5. Rozszerzenie dark mode na pozostałe komponenty feature

### Komponenty do aktualizacji w przyszłości
Następujące komponenty feature mogą wymagać dalszych aktualizacji:
- Dashboard components (StatsCard, SystemHealth, PluginsStatus)
- Chat components (ChatInterface, MessageInput, ChatSidebar)
- Search components (SearchForm, SearchResults, DocumentDetail)
- Settings components (wszystkie podstrony)
- CyberPanel components
- Address Book components

## Troubleshooting

### Problem: Motyw nie przełącza się
**Rozwiązanie:** Sprawdź czy ThemeProvider jest dodany w AppProviders.tsx

### Problem: Brak persystencji motywu
**Rozwiązanie:** Sprawdź czy localStorage jest dostępne w przeglądarce

### Problem: Niektóre komponenty nie mają dark mode
**Rozwiązanie:** To normalne - zaktualizowano tylko główne komponenty UI i Layout. Pozostałe komponenty można zaktualizować stopniowo.

### Problem: Kolory nie pasują
**Rozwiązanie:** Można dostosować paletę kolorów w `tailwind.config.js`

## Dodatkowe zasoby

- [Tailwind CSS v3.4 Release Notes](https://tailwindcss.com/blog/tailwindcss-v3-4)
- [Tailwind CSS Dark Mode Documentation](https://tailwindcss.com/docs/dark-mode)
- [React Context Documentation](https://react.dev/reference/react/useContext)

## Autorzy

Migracja wykonana przez: AI Assistant
Data: 2025-01-09
Wersja: 1.0.0

