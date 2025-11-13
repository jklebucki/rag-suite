# Plan Refaktoryzacji z wykorzystaniem `use()` Hook - React 19

## Przegląd
Projekt RAG.Web.UI używa React 19, który wprowadza hook `use()` do bezpośredniego unwrapowania Promise i Context. Ten plan opisuje kompleksową refaktoryzację projektu, aby wykorzystać pełny potencjał tego hooka.

## Analiza Obecnego Stanu

### 1. Lazy Loading Komponentów
**Obecny stan:** Wszystkie route'y używają `React.lazy()` w `AppRoutes.tsx`
- 20+ komponentów używają `lazy()`
- Wszystkie są opakowane w `Suspense` przez `RouteSuspense`

**Możliwość refaktoryzacji:** ✅ TAK - można zastąpić przez `useAsyncComponent` z `use()`

### 2. Ładowanie Danych z useState + useEffect
**Obecny stan:** Wiele komponentów używa wzorca useState + useEffect:
- `AddressBook.tsx` - loadContacts(), loadProposals()
- `ConfigurationContext.tsx` - fetchConfiguration()
- Różne komponenty z manualnym ładowaniem danych

**Możliwość refaktoryzacji:** ✅ TAK - można zastąpić przez `use()` hook z Suspense

### 3. React Query (useQuery)
**Obecny stan:** Projekt używa @tanstack/react-query:
- `useDashboard.ts` - wiele useQuery
- `useSearch.ts` - useQuery z enabled: false
- `useForumQueries.ts` - wiele useQuery
- Inne hooki używające React Query

**Możliwość refaktoryzacji:** ⚠️ CZĘŚCIOWO - React Query ma cache, refetch, staleTime, które są trudne do zastąpienia. Jednak proste przypadki można refaktoryzować.

### 4. Contexty z async loading
**Obecny stan:**
- `ConfigurationContext.tsx` - używa useReducer + useEffect
- `AuthContext.tsx` - używa useReducer + useEffect
- Przykład `ConfigurationContextWithUse.example.tsx` pokazuje jak można użyć `use()`

**Możliwość refaktoryzacji:** ⚠️ CZĘŚCIOWO - Contexty z manual refresh są trudniejsze, ale można użyć `use()` dla initial load

## Plan Refaktoryzacji

### Faza 1: Lazy Loading Komponentów (Wysoki Priorytet)
**Cel:** Zastąpienie wszystkich `React.lazy()` przez `use()` hook

**Pliki do modyfikacji:**
1. `src/app/AppRoutes.tsx` - zastąpienie wszystkich `lazy()` przez `useAsyncComponent`
2. `src/features/search/components/SearchResults.tsx` - PDFViewerModal
3. `src/features/chat/components/MessageSources.tsx` - PDFViewerModal

**Korzyści:**
- Lepsza integracja z React 19
- Spójność w całym projekcie
- Możliwość lepszego error handling

**Szacowany czas:** 2-3 godziny

### Faza 2: Proste Komponenty z useState + useEffect (Średni Priorytet)
**Cel:** Refaktoryzacja komponentów, które ładują dane przy mount

**Pliki do modyfikacji:**
1. `src/features/address-book/components/AddressBook.tsx`
   - `loadContacts()` → użyć `use()` z Suspense
   - `loadProposals()` → użyć `use()` z Suspense (conditional)
   
2. Komponenty z prostym ładowaniem danych przy mount

**Korzyści:**
- Eliminacja useState/useEffect boilerplate
- Automatyczne Suspense boundaries
- Lepsze error handling przez Error Boundaries

**Szacowany czas:** 4-5 godzin

### Faza 3: Contexty z Async Loading (Niski Priorytet)
**Cel:** Refaktoryzacja contextów, które ładują dane asynchronicznie

**Pliki do modyfikacji:**
1. `src/shared/contexts/ConfigurationContext.tsx`
   - Rozważyć użycie `use()` dla initial load
   - Zachować manual refresh functionality

**Uwaga:** Contexty z manual refresh mogą wymagać hybrydowego podejścia

**Szacowany czas:** 3-4 godziny

### Faza 4: React Query - Proste Przypadki (Opcjonalnie)
**Cel:** Refaktoryzacja prostych przypadków useQuery, które nie wymagają cache/refetch

**Pliki do rozważenia:**
1. `src/features/search/hooks/useSearch.ts` - useQuery z enabled: false
2. Proste przypadki bez refetchInterval

**Uwaga:** Większość przypadków React Query powinna pozostać, ponieważ oferuje cache, staleTime, refetchInterval, które są trudne do zastąpienia.

**Szacowany czas:** 2-3 godziny (jeśli w ogóle)

## Szczegółowy Plan Implementacji

### Krok 1: Rozszerzenie useAsyncComponent
- Upewnić się, że `useAsyncComponent` jest w pełni funkcjonalny
- Dodać error handling
- Dodać testy

### Krok 2: Refaktoryzacja AppRoutes.tsx
- Zastąpić wszystkie `lazy()` przez `useAsyncComponent`
- Upewnić się, że Suspense boundaries działają poprawnie
- Przetestować wszystkie route'y

### Krok 3: Refaktoryzacja komponentów z useState + useEffect
- Dla każdego komponentu:
  - Utworzyć async data loader component
  - Opakować w Suspense
  - Użyć `use()` hook do unwrapowania Promise
  - Dodać Error Boundary

### Krok 4: Refaktoryzacja Contextów
- Rozważyć hybrydowe podejście
- Użyć `use()` dla initial load
- Zachować manual refresh przez Promise recreation

### Krok 5: Testy i Dokumentacja
- Przetestować wszystkie zmiany
- Zaktualizować dokumentację
- Dodać przykłady użycia

## Wzorce do Implementacji

### Wzorzec 1: Lazy Loading Komponentu
```tsx
// Przed
const Dashboard = lazy(() => import('@/features/dashboard/components/Dashboard'))

// Po
const DashboardPromise = import('@/features/dashboard/components/Dashboard').then(m => ({ default: m.Dashboard }))
function DashboardLoader() {
  const Dashboard = useAsyncComponent(DashboardPromise)
  return <Dashboard />
}
```

### Wzorzec 2: Async Data Loading
```tsx
// Przed
const [data, setData] = useState(null)
useEffect(() => {
  loadData().then(setData)
}, [])

// Po
function DataLoader() {
  const data = use(loadData())
  return <Component data={data} />
}

<Suspense fallback={<Loading />}>
  <DataLoader />
</Suspense>
```

### Wzorzec 3: Conditional Loading
```tsx
// Przed
useEffect(() => {
  if (condition) {
    loadData().then(setData)
  }
}, [condition])

// Po
function ConditionalDataLoader({ condition }: { condition: boolean }) {
  if (!condition) return null
  const data = use(loadData())
  return <Component data={data} />
}
```

## Ryzyka i Uwagi

1. **Error Handling:** `use()` rzuca błędy, które muszą być obsłużone przez Error Boundary
2. **Manual Refresh:** Trudniejsze z `use()` - wymaga recreacji Promise
3. **React Query:** Nie wszystkie przypadki powinny być refaktoryzowane - React Query oferuje cache i inne funkcje
4. **Testing:** Wymaga aktualizacji testów dla nowych wzorców

## Metryki Sukcesu

- ✅ Wszystkie `React.lazy()` zastąpione przez `use()`
- ✅ Komponenty z prostym ładowaniem używają `use()`
- ✅ Wszystkie testy przechodzą
- ✅ Brak regresji funkcjonalności
- ✅ Lepsze error handling przez Error Boundaries

## Harmonogram

1. **Dzień 1:** Faza 1 (Lazy Loading) - 2-3h ✅ **ZAKOŃCZONA**
2. **Dzień 2:** Faza 2 (useState + useEffect) - 4-5h ⚠️ **CZĘŚCIOWO** - AddressBook wymaga manual refresh
3. **Dzień 3:** Faza 3 (Contexty) - 3-4h ⚠️ **CZĘŚCIOWO** - ConfigurationContext wymaga manual refresh
4. **Dzień 4:** Testy i dokumentacja - 2-3h

**Całkowity czas:** ~12-15 godzin

## Status Implementacji

### ✅ ZAKOŃCZONE

#### Faza 1: Lazy Loading Komponentów
- ✅ `AppRoutes.tsx` - wszystkie 20+ komponentów zastąpione przez `useAsyncComponent`
- ✅ `SearchResults.tsx` - PDFViewerModal używa `use()` hook
- ✅ `MessageSources.tsx` - PDFViewerModal używa `use()` hook

**Wynik:** Wszystkie `React.lazy()` zostały zastąpione przez `use()` hook z React 19.

### ⚠️ CZĘŚCIOWO ZREALIZOWANE

#### Faza 2: useState + useEffect
- ⚠️ `AddressBook.tsx` - wymaga manual refresh po operacjach CRUD, więc refaktoryzacja byłaby skomplikowana
- 💡 **Rekomendacja:** Pozostawić obecny wzorzec dla komponentów wymagających manual refresh

#### Faza 3: Contexty
- ⚠️ `ConfigurationContext.tsx` - wymaga manual refresh (`refreshConfiguration()`)
- 💡 **Rekomendacja:** Pozostawić obecny wzorzec dla contextów z manual refresh
- ✅ Przykład `ConfigurationContextWithUse.example.tsx` pokazuje jak można użyć `use()` dla prostych przypadków

### 📝 UWAGI

1. **Manual Refresh:** Komponenty i contexty wymagające manual refresh (np. po mutacjach) są lepiej obsługiwane przez tradycyjne wzorce (useState + useEffect lub React Query).

2. **React Query:** Większość przypadków użycia React Query powinna pozostać bez zmian, ponieważ oferuje cache, staleTime, refetchInterval, które są trudne do zastąpienia przez `use()` hook.

3. **use() Hook jest najlepszy dla:**
   - Lazy loading komponentów ✅ (zrealizowane)
   - Proste ładowanie danych przy mount (bez manual refresh)
   - Komponenty, które nie wymagają cache/refetch

4. **Tradycyjne wzorce są lepsze dla:**
   - Komponenty z manual refresh
   - Contexty z manual refresh
   - Dane wymagające cache i automatycznego refetch (React Query)

