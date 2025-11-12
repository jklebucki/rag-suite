# React 19 use() Hook - Implementation Guide

## Overview

React 19 introduces the `use()` hook, which allows you to read the value of a Promise or Context directly in components. This simplifies async data loading and eliminates the need for `useState` + `useEffect` patterns in many cases.

## Key Features

- **Direct Promise unwrapping**: Read Promise values directly without `useState`/`useEffect`
- **Automatic Suspense**: Works seamlessly with Suspense boundaries
- **Simpler code**: Reduces boilerplate for async operations
- **Better error handling**: Integrates with Error Boundaries

## Implementation

### 1. Custom Hooks Created

#### `useAsyncComponent.ts`
Hook for lazy loading components using `use()` hook:

```typescript
import { useAsyncComponent } from '@/shared/hooks/useAsyncComponent'

// In a component
const HeavyComponent = useAsyncComponent(
  import('./HeavyComponent').then(m => ({ default: m.HeavyComponent }))
)
```

#### `useAsyncData.ts`
Hook for async data loading using `use()` hook:

```typescript
import { useAsyncData } from '@/shared/hooks/useAsyncData'

function UserProfile({ userId }: { userId: string }) {
  const user = useAsyncData(fetchUser(userId))
  return <div>{user.name}</div>
}
```

### 2. Example Implementation

See `ConfigurationContextWithUse.example.tsx` for an example of how `ConfigurationContext` could be refactored to use `use()` hook.

## When to Use `use()` Hook

### ✅ Good Use Cases

1. **Component-level data loading**
   - When loading data specific to a component
   - When you don't need manual refresh functionality

2. **Lazy loading components**
   - Alternative to `React.lazy()` for dynamic imports
   - When you need more control over loading

3. **Simple Promise unwrapping**
   - When you have a Promise and just need its value
   - When error handling is handled by Error Boundaries

### ❌ Not Recommended For

1. **Contexts with manual refresh**
   - `ConfigurationContext` needs `refreshConfiguration()` method
   - Traditional `useState` + `useEffect` is better here

2. **Complex error handling**
   - When you need custom error states
   - When you need to retry failed requests

3. **Loading state management**
   - When you need to show loading indicators
   - `use()` works with Suspense, which handles loading differently

## Comparison: Traditional vs use() Hook

### Traditional Pattern (useState + useEffect)

```typescript
function UserProfile({ userId }: { userId: string }) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<Error | null>(null)

  useEffect(() => {
    fetchUser(userId)
      .then(setUser)
      .catch(setError)
      .finally(() => setLoading(false))
  }, [userId])

  if (loading) return <div>Loading...</div>
  if (error) return <div>Error: {error.message}</div>
  return <div>{user.name}</div>
}
```

### use() Hook Pattern

```typescript
function UserProfile({ userId }: { userId: string }) {
  const user = use(fetchUser(userId))
  return <div>{user.name}</div>
}

// Wrap in Suspense boundary
<Suspense fallback={<div>Loading...</div>}>
  <UserProfile userId="123" />
</Suspense>
```

## Benefits

1. **Less boilerplate**: No need for `useState` and `useEffect`
2. **Automatic Suspense**: Loading states handled by Suspense
3. **Better error handling**: Errors propagate to Error Boundaries
4. **Simpler code**: More declarative and easier to read

## Limitations

1. **Requires Suspense**: Must wrap components in Suspense boundary
2. **No manual refresh**: Harder to implement manual refresh functionality
3. **Error handling**: Errors must be caught by Error Boundaries or try/catch
4. **Loading states**: Loading is handled by Suspense, less control

## Files Created

- `src/shared/hooks/useAsyncComponent.ts` - Hook for lazy loading components
- `src/shared/hooks/useAsyncData.ts` - Hook for async data loading
- `src/shared/contexts/ConfigurationContextWithUse.example.tsx` - Example implementation

## Next Steps

1. **Evaluate use cases**: Identify components that could benefit from `use()` hook
2. **Gradual adoption**: Start with simple cases, then expand
3. **Error boundaries**: Ensure Error Boundaries are in place
4. **Suspense boundaries**: Wrap components using `use()` in Suspense

## References

- [React 19 use() Hook Documentation](https://react.dev/reference/react/use)
- [React 19 Suspense Documentation](https://react.dev/reference/react/Suspense)

