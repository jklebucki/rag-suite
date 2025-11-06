# Organizacja Testów - Best Practices

## 🔍 Analiza Obecnej Struktury

### Obecna struktura (nieoptymalna):
```
src/
├── components/
│   └── auth/
│       ├── __tests__/
│       │   └── LoginForm.test.tsx
│       └── LoginForm.tsx
├── utils/
│   └── __tests__/
│       └── validation.test.ts
└── hooks/
    └── __tests__/
        └── useLayout.test.ts
```

### Problemy:
1. ❌ Folder `__tests__` rozprasza testy
2. ❌ Trudniej znaleźć test dla danego pliku
3. ❌ Niezgodne z Clean Code (testy powinny być blisko kodu)
4. ❌ Mniej czytelne dla nowych deweloperów

## ✅ Rekomendowana Struktura (Co-located Tests)

### Opcja 1: Co-located (Zalecane dla React/TypeScript)
```
src/
├── components/
│   └── auth/
│       ├── LoginForm.tsx
│       └── LoginForm.test.tsx      # Test obok pliku
├── utils/
│   ├── validation.ts
│   └── validation.test.ts          # Test obok pliku
└── hooks/
    ├── useLayout.ts
    └── useLayout.test.ts            # Test obok pliku
```

**Zalety:**
- ✅ Testy są łatwe do znalezienia
- ✅ Zgodne z Clean Code (bliskość kodu i testów)
- ✅ Łatwiejsze utrzymanie
- ✅ Standard w React/TypeScript community
- ✅ Lepsze dla refactoringu

### Opcja 2: Osobny folder `tests/` (Alternatywa)
```
src/
├── components/
│   └── auth/
│       └── LoginForm.tsx
└── tests/
    ├── components/
    │   └── auth/
    │       └── LoginForm.test.tsx
    ├── utils/
    │   └── validation.test.ts
    └── hooks/
        └── useLayout.test.ts
```

**Zalety:**
- ✅ Pełna separacja testów od kodu produkcyjnego
- ✅ Łatwiejsze wykluczenie z buildów
- ✅ Lepsze dla bardzo dużych projektów

## 🎯 Rekomendacja: Co-located Tests

Dla projektu RAG.Web.UI rekomendujemy **Opcję 1 (Co-located)** ponieważ:
1. Projekt ma umiarkowaną wielkość
2. React/TypeScript community preferuje co-located
3. Lepsze dla Clean Code
4. Łatwiejsze utrzymanie

## 📋 Plan Migracji

1. Przenieść testy z `__tests__/` do lokalizacji obok plików źródłowych
2. Zaktualizować konfigurację Vitest
3. Zaktualizować dokumentację
4. Usunąć puste foldery `__tests__/`

## 🔧 Konfiguracja Vitest

Vitest automatycznie znajdzie testy z rozszerzeniem `.test.ts` lub `.test.tsx` niezależnie od lokalizacji.

