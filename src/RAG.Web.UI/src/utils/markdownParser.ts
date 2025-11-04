/**
 * Markdown parsing utilities for About page
 * Centralized functions for extracting sections, titles, and content from markdown
 */

/**
 * Escapes special regex characters in a string
 */
function escapeRegExp(str: string): string {
  return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
}

/**
 * Get section by ID from markdown
 * Pattern: ## Section {#id} or ### Section {#id}
 */
export function getSectionById(md: string, id: string): string {
  const pattern = new RegExp(`(^|\\n)##?#?\\s+[^{\\n]*\\{#${escapeRegExp(id)}\\}\\s*\\n([\\s\\S]*?)(?=\\n##\\s+|$)`, 'i')
  const match = md.match(pattern)
  return match ? match[2].trim() : ''
}

/**
 * Get section content by ID, removing subtitle lines
 */
export function getSectionContentById(md: string, id: string): string {
  let content = getSectionById(md, id)
  if (!content) return ''
  
  // Remove subtitle lines (### Text {#subtitle-xxx})
  content = content.replace(/^###\s+[^{]*\{#subtitle-[^}]+\}\s*$/gm, '')
  
  // Remove empty lines at the beginning
  content = content.replace(/^\s*\n+/, '')
  
  return content.trim()
}

/**
 * Extract section title by ID
 */
export function getSectionTitleById(md: string, id: string): string {
  const pattern = new RegExp(`(^|\\n)##\\s+([^{\\n]+?)\\s*\\{#${escapeRegExp(id)}\\}`, 'i')
  const match = md.match(pattern)
  return match ? match[2].trim() : ''
}

/**
 * Extract subtitle by dedicated ID - looks for ### Subtitle {#subtitle-xxx}
 */
export function getSubtitleById(md: string, id: string): string {
  const pattern = new RegExp(`(^|\\n)###\\s+([^{\\n]+?)\\s*\\{#${escapeRegExp(id)}\\}`, 'i')
  const match = md.match(pattern)
  return match ? match[2].trim() : ''
}

/**
 * Get hero subtitle by ID or fallback to tagline extraction
 */
export function getHeroSubtitle(md: string): string {
  const subtitleById = getSubtitleById(md, 'subtitle-hero')
  if (subtitleById) return subtitleById
  
  // Fallback to existing tagline logic
  return md.match(/^\s*_(.+)_\s*$/m)?.[1]?.trim() ?? ''
}

/**
 * Extract subtitle by ID - looks for first paragraph after section header
 */
export function getSectionSubtitleById(md: string, id: string): string {
  const sectionContent = getSectionById(md, id)
  if (!sectionContent) return ''
  
  // Find first paragraph that's not empty
  const lines = sectionContent.split('\n').filter(line => line.trim())
  const firstParagraph = lines.find(line => 
    !line.startsWith('#') && 
    !line.startsWith('-') && 
    !line.startsWith('*') && 
    line.trim().length > 0
  )
  
  return firstParagraph?.trim() || ''
}

/**
 * Get section by title
 */
export function getSectionByTitle(md: string, title: string): string {
  const pattern = new RegExp(`(^|\\n)##\\s+${escapeRegExp(title)}\\s*(?:\\{#[^}]+\\})?\\s*\\n([\\s\\S]*?)(?=\\n##\\s+|$)`, 'i')
  const match = md.match(pattern)
  return match ? match[2].trim() : ''
}

/**
 * Get section by identifier (tries ID first, then falls back to title)
 */
export function getSection(md: string, identifier: string): string {
  // Try ID first, then fallback to title
  const byId = getSectionById(md, identifier.toLowerCase().replace(/\s+/g, '-'))
  if (byId) return byId
  
  const byTitle = getSectionByTitle(md, identifier)
  return byTitle
}

/**
 * Get top section (H1 title + first paragraph as description)
 */
export function getTop(md: string): { h1: string; tagline: string } {
  // Title (H1) + first paragraph as description
  const h1 = md.match(/^\s*#\s+(.+)\s*$/m)?.[1]?.trim() ?? 'App Info'
  // Tagline in quotes or > blockquote or first paragraph
  const tagline =
    md.match(/^\s*>\s+(.+)\s*$/m)?.[1]?.trim() ??
    md.match(/^\s*_(.+)_\s*$/m)?.[1]?.trim() ??
    md.match(/^\s*(?:[^#>\-\n][^\n]+)\s*$/m)?.[0]?.trim() ??
    ''
  return { h1, tagline }
}

/**
 * Get subsections from a markdown section
 * Splits section by ### Title {#id} or ### Title
 */
export function getSubsections(mdSection: string): Array<{ title: string; body: string; id: string }> {
  const blocks = mdSection.split(/\n(?=###\s+)/g).filter(Boolean)
  return blocks.map(b => {
    const titleMatch = b.match(/^\s*###\s+([^{]+?)(?:\s*\{#([^}]+)\})?\s*$/m)
    const title = titleMatch?.[1]?.trim() ?? ''
    const id = titleMatch?.[2]?.trim() ?? ''
    const body = b.replace(/^\s*###\s+.+\s*$/m, '').trim()
    return { title, body, id }
  }).filter(s => s.title || s.body)
}

