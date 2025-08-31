# Multilingual System Documentation

## Overview

The RAG Suite Web UI now supports 5 languages with automatic browser detection and localStorage persistence:

- 🇺🇸 English (en) - Default
- 🇵🇱 Polish (pl)
- 🇷🇴 Romanian (ro)
- 🇭🇺 Hungarian (hu)
- 🇳🇱 Dutch (nl)

## Features

### Language Detection
- Automatically detects browser's preferred language on first visit
- Falls back to English if browser language is not supported
- Uses `navigator.languages` for better accuracy

### Language Persistence
- User's language choice is saved in localStorage
- Persists across browser sessions
- Only clears when user manually clears browser cache
- Key: `rag-suite-language`

### Language Selector Component
- Located in the top navigation bar
- Shows current language flag and code
- Dropdown with all available languages
- Indicates when language was auto-detected
- Smooth hover effects and proper ARIA attributes

## Implementation

### Core Files

1. **Types**: `/src/types/i18n.ts`
   - Language definitions and supported codes
   - Translation keys interface
   - TypeScript types for type safety

2. **Translations**: `/src/locales/`
   - One file per language (en.ts, pl.ts, ro.ts, hu.ts, nl.ts)
   - Comprehensive translations for all UI elements
   - Organized by feature areas (nav, dashboard, chat, search, etc.)

3. **Context**: `/src/contexts/I18nContext.tsx`
   - React Context for global language state
   - Translation function with placeholder support
   - Language switching functionality

4. **Utilities**: `/src/utils/language.ts`
   - Browser language detection
   - localStorage management
   - Auto-detection status tracking

5. **Components**: `/src/components/ui/LanguageSelector.tsx`
   - Interactive language dropdown
   - Accessibility compliant
   - Visual feedback for current selection

### Integration

The system is integrated at the App level:

```tsx
<I18nProvider>
  <ToastProvider>
    <Layout>
      {/* Routes */}
    </Layout>
  </ToastProvider>
</I18nProvider>
```

### Usage in Components

```tsx
import { useI18n } from '@/contexts/I18nContext'

function MyComponent() {
  const { t, language, setLanguage } = useI18n()
  
  return (
    <h1>{t('dashboard.title')}</h1>
  )
}
```

### Translation Function

The `t()` function supports:
- Simple key lookup: `t('common.loading')`
- Placeholder interpolation: `t('common.translated_from', sourceLanguage, targetLanguage)`

## Browser Behavior

1. **First Visit**: 
   - Detects browser language
   - Shows auto-detection indicator
   - Uses detected language if supported, otherwise English

2. **Language Change**:
   - User selects language from dropdown
   - Choice saved to localStorage
   - Auto-detection indicator disappears
   - Selected language used on future visits

3. **Cache Clear**:
   - Language preference is lost
   - System returns to auto-detection mode
   - Browser language detected again

## Testing

You can test the language system by:

1. Opening `http://localhost:3000`
2. Checking the language selector in the top-right
3. Switching between languages
4. Refreshing the page to verify persistence
5. Clearing localStorage to test auto-detection

## API Integration

The frontend language system is designed to work with the multilingual API:
- Language preference can be sent to `/api/chat/sessions/{id}/messages/multilingual`
- `preferredLanguage` parameter uses same language codes
- Consistent language handling between frontend and backend

## Adding New Languages

To add a new language:

1. Add language definition to `SUPPORTED_LANGUAGES` in `/src/types/i18n.ts`
2. Create new translation file in `/src/locales/` (e.g., `de.ts`)
3. Export from `/src/locales/index.ts`
4. Add to translations object

Example for German:
```typescript
// In types/i18n.ts
{ code: 'de', name: 'German', nativeName: 'Deutsch', flag: '🇩🇪' }

// Create locales/de.ts with all translations
// Export from locales/index.ts
```

## Accessibility

- Proper ARIA attributes on language selector
- Screen reader friendly labels
- Keyboard navigation support
- Language attribute set on document element

## Performance

- Translations loaded synchronously (small bundle size)
- No dynamic imports or code splitting needed
- Minimal runtime overhead
- localStorage operations wrapped in try-catch for robustness
