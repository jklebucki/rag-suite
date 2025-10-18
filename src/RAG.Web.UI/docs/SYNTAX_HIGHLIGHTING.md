# Syntax Highlighting Features

## Overview
The chat component now includes full syntax highlighting for code blocks using `react-syntax-highlighter` with Prism.js engine.

## Features

### 1. Automatic Language Detection
The component automatically detects the programming language from the Markdown code fence:

````markdown
```typescript
const greeting: string = "Hello, World!";
```
````

### 2. VS Code Dark+ Theme
Uses the familiar VS Code Dark+ color scheme for consistent, professional code highlighting across all supported languages.

### 3. Line Numbers
Automatically displays line numbers for code blocks with more than 5 lines:

````markdown
```python
def fibonacci(n):
    if n <= 1:
        return n
    a, b = 0, 1
    for _ in range(n - 1):
        a, b = b, a + b
    return b
```
````

### 4. Copy to Clipboard
Each code block includes a "Copy" button that copies the entire code to the clipboard with one click.

**Features:**
- Appears in the language label header
- Hover effect for better UX
- Uses native `navigator.clipboard` API
- Works with all code blocks

### 5. Language Label
Shows the programming language name in uppercase in the header bar:

```
┌─────────────────────────────────┐
│ TYPESCRIPT              [Copy]  │ ← Language label with copy button
├─────────────────────────────────┤
│ 1  const x = 42;               │
│ 2  console.log(x);             │
└─────────────────────────────────┘
```

## Supported Languages

### Web Development
- JavaScript, TypeScript
- HTML, CSS, SCSS, SASS, Less
- JSX, TSX
- JSON, YAML
- GraphQL

### Backend Languages
- Python
- Java, Kotlin, Scala
- C#, F#, VB.NET
- PHP
- Ruby
- Go (Golang)
- Rust
- Swift
- Elixir, Erlang

### Systems Programming
- C, C++
- Assembly

### Scripting & Shell
- Bash, Shell
- PowerShell
- Batch

### Database
- SQL (PostgreSQL, MySQL, SQLite)
- PL/SQL
- MongoDB Query Language

### Markup & Data
- Markdown
- XML
- CSV
- INI, TOML

### Configuration
- Dockerfile
- Nginx config
- Apache config
- Git config

### Other
- R
- Matlab
- LaTeX
- Diff
- Log files
- Plain text

## Implementation Details

### Component Structure

```typescript
<div className="my-3 rounded-md overflow-hidden">
  {/* Language Header with Copy Button */}
  <div className="language-header">
    <span>LANGUAGE_NAME</span>
    <button onClick={copyToClipboard}>Copy</button>
  </div>
  
  {/* Syntax Highlighted Code */}
  <SyntaxHighlighter
    language={detectedLanguage}
    style={vscDarkPlus}
    showLineNumbers={lines > 5}
  >
    {codeString}
  </SyntaxHighlighter>
</div>
```

### Style Configuration

```typescript
customStyle={{
  margin: 0,
  borderRadius: language ? '0 0 0.375rem 0.375rem' : '0.375rem',
  fontSize: '0.75rem',
  lineHeight: '1.5',
}}
```

### Line Number Threshold
Line numbers are shown when the code block has more than 5 lines:

```typescript
showLineNumbers={codeString.split('\n').length > 5}
```

## Usage Examples

### Simple Code Block
````markdown
```javascript
console.log("Hello, World!");
```
````

**Result:** Single line code without line numbers, with Copy button.

### Complex Code Block
````markdown
```typescript
interface User {
  id: number;
  name: string;
  email: string;
  createdAt: Date;
}

async function fetchUsers(): Promise<User[]> {
  const response = await fetch('/api/users');
  return response.json();
}
```
````

**Result:** Multi-line code with line numbers, syntax highlighting, and Copy button.

### No Language Specified
````markdown
```
Plain text without syntax highlighting
But still with Copy button
```
````

**Result:** Displays as plain text with dark background, still includes Copy button.

## Browser Compatibility

The syntax highlighting works in all modern browsers:
- ✅ Chrome/Edge (Chromium) 90+
- ✅ Firefox 88+
- ✅ Safari 14+
- ✅ Opera 76+

The `navigator.clipboard` API requires:
- HTTPS context (or localhost)
- User permission (automatically granted in modern browsers)

## Performance Considerations

### Bundle Size
- `react-syntax-highlighter` uses code splitting
- Only loads the Prism engine and VS Code Dark+ theme
- Lazy loads other themes if needed

### Rendering Performance
- Efficiently handles code blocks up to 1000+ lines
- Virtual scrolling for very long code blocks
- No performance impact on messages without code

### Memory Usage
- Minimal memory footprint
- Proper cleanup on component unmount
- No memory leaks from clipboard API

## Customization Options

### Change Theme
To use a different syntax highlighting theme:

```typescript
import { oneDark, oneLight } from 'react-syntax-highlighter/dist/esm/styles/prism'

// Then in the component:
style={theme === 'dark' ? oneDark : oneLight}
```

### Disable Line Numbers
To always hide line numbers:

```typescript
showLineNumbers={false}
```

### Custom Font Size
Adjust the fontSize in customStyle:

```typescript
customStyle={{
  fontSize: '0.875rem', // 14px instead of 12px
  lineHeight: '1.6',
}}
```

## Accessibility

- ✅ Keyboard accessible Copy button
- ✅ Proper ARIA labels
- ✅ Screen reader friendly
- ✅ High contrast for code readability
- ✅ Supports browser zoom

## Testing

Test the syntax highlighting with various languages:

1. **TypeScript/JavaScript**
   - Variables, functions, classes
   - Async/await syntax
   - Template literals

2. **Python**
   - Function definitions
   - List comprehensions
   - Decorators

3. **SQL**
   - SELECT queries
   - JOIN statements
   - Subqueries

4. **JSON**
   - Objects and arrays
   - Proper key highlighting

5. **Bash**
   - Commands and pipes
   - Variables and conditionals

## Troubleshooting

### Copy Button Not Working
- Ensure the app is running on HTTPS or localhost
- Check browser console for clipboard permission errors
- Verify `navigator.clipboard` API is available

### Wrong Language Detection
- Ensure the language name in the code fence is correct
- Check supported languages list
- Fall back to `text` for unsupported languages

### Styling Issues
- Verify Tailwind CSS is properly configured
- Check for CSS conflicts with global styles
- Ensure proper z-index for overlays

## Migration Notes

### From Previous Version
If upgrading from the plain text version:

1. Install dependencies:
   ```bash
   npm install react-syntax-highlighter
   npm install --save-dev @types/react-syntax-highlighter
   ```

2. No changes needed to existing Markdown content
3. All existing code blocks automatically get syntax highlighting
4. Backward compatible with all existing features

## Future Improvements

Planned enhancements:
1. **Theme switcher** - User preference for light/dark themes
2. **Copy feedback** - Visual confirmation when code is copied
3. **Language selector** - Manual language override option
4. **Download option** - Save code blocks as files
5. **Search in code** - Find text within code blocks
6. **Line highlighting** - Emphasize specific lines
