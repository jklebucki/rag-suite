# Markdown Rendering Examples for RAG Suite Chat

This document contains various Markdown examples to demonstrate the rendering capabilities of the chat component.

## Text Formatting

This is **bold text** and this is *italic text*. You can also use ***bold and italic*** together.

Here's some ~~strikethrough text~~ (if supported by remark-gfm).

## Headings

# Heading 1
## Heading 2
### Heading 3
#### Heading 4
##### Heading 5
###### Heading 6

## Lists

### Unordered List
- Item 1
- Item 2
  - Nested item 2.1
  - Nested item 2.2
- Item 3

### Ordered List
1. First item
2. Second item
3. Third item
   1. Nested numbered item
   2. Another nested item
4. Fourth item

### Task List (GitHub Flavored)
- [x] Completed task
- [ ] Pending task
- [ ] Another pending task

## Code Examples

### Inline Code
Use `console.log()` to print to the console.

### Code Blocks

#### JavaScript
```javascript
function greet(name) {
  return `Hello, ${name}!`;
}

console.log(greet('World'));
```

#### TypeScript
```typescript
interface User {
  id: number;
  name: string;
  email: string;
}

function createUser(data: Omit<User, 'id'>): User {
  return {
    id: Date.now(),
    ...data
  };
}
```

#### Python
```python
def fibonacci(n):
    if n <= 1:
        return n
    return fibonacci(n-1) + fibonacci(n-2)

print([fibonacci(i) for i in range(10)])
```

#### SQL
```sql
SELECT 
  users.name,
  COUNT(orders.id) as order_count
FROM users
LEFT JOIN orders ON users.id = orders.user_id
GROUP BY users.id, users.name
HAVING COUNT(orders.id) > 5
ORDER BY order_count DESC;
```

#### JSON
```json
{
  "name": "RAG Suite",
  "version": "1.0.0",
  "features": [
    "Markdown rendering",
    "Code highlighting",
    "Multi-language support"
  ],
  "active": true
}
```

#### Bash
```bash
#!/bin/bash
echo "Building application..."
npm install
npm run build
echo "Build complete!"
```

## Links

[Visit GitHub](https://github.com)

[RAG Suite Documentation](https://example.com/docs)

Auto-link: https://github.com

## Blockquotes

> This is a blockquote.
> It can span multiple lines.
>
> > And even be nested!

> **Note:** This is an important message.

## Tables

| Feature | Status | Priority |
|---------|--------|----------|
| Markdown Rendering | ✅ Complete | High |
| Syntax Highlighting | 🚧 Planned | Medium |
| LaTeX Support | 📋 Todo | Low |
| Mermaid Diagrams | 📋 Todo | Low |

### Complex Table

| Column 1 | Column 2 | Column 3 | Column 4 |
|----------|----------|----------|----------|
| Data 1.1 | Data 1.2 | Data 1.3 | Data 1.4 |
| Data 2.1 | Data 2.2 | Data 2.3 | Data 2.4 |
| Data 3.1 | Data 3.2 | Data 3.3 | Data 3.4 |

## Horizontal Rules

Content above

---

Content in the middle

***

Content below

## Mixed Content Example

Here's a real-world example combining multiple elements:

### API Response Handler

```typescript
interface ApiResponse<T> {
  data: T;
  status: number;
  message: string;
}

async function fetchData<T>(url: string): Promise<T> {
  try {
    const response = await fetch(url);
    const json: ApiResponse<T> = await response.json();
    
    if (json.status !== 200) {
      throw new Error(json.message);
    }
    
    return json.data;
  } catch (error) {
    console.error('Fetch failed:', error);
    throw error;
  }
}
```

**Usage:**

1. Import the function
2. Call with proper types
3. Handle errors appropriately

```typescript
const users = await fetchData<User[]>('/api/users');
console.log(users);
```

> **Warning:** Always handle network errors in production!

## Mathematical Expressions (Plain Text)

Since LaTeX support is not yet implemented, here's how to write formulas:

- Quadratic formula: x = (-b ± √(b² - 4ac)) / 2a
- Pythagorean theorem: a² + b² = c²
- E = mc²

## Emoji Support

You can use emojis in your markdown:

✅ Checkmark
❌ Cross
⚠️ Warning
📝 Note
🚀 Rocket
💡 Idea
🔧 Tool
📊 Chart

## Special Characters

&copy; Copyright
&reg; Registered
&trade; Trademark
&lt; Less than
&gt; Greater than
&amp; Ampersand

## Nested Elements

1. First level
   - Bullet point
   - Another bullet
     ```typescript
     const nested = true;
     ```
   - Back to bullets
2. Second level
   > A quote inside a list
   >
   > With multiple lines

## Long Content Test

Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris.

**Key Points:**
- Point 1: Very important information
- Point 2: Another crucial detail
- Point 3: Final consideration

```python
# This is a longer code example to test scrolling
def complex_function(param1, param2, param3):
    result = []
    for i in range(param1):
        if i % 2 == 0:
            result.append(param2 * i)
        else:
            result.append(param3 * i)
    return result
```

---

## Conclusion

This demonstrates the comprehensive Markdown rendering capabilities implemented in the RAG Suite chat component. The component handles:

- ✅ Text formatting (bold, italic, etc.)
- ✅ All heading levels
- ✅ Lists (ordered, unordered, nested)
- ✅ Code blocks with language detection
- ✅ Links
- ✅ Blockquotes
- ✅ Tables
- ✅ Horizontal rules
- ✅ Mixed content

For future enhancements, see the [MARKDOWN_RENDERING.md](./MARKDOWN_RENDERING.md) documentation.
