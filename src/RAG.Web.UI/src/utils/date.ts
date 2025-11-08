import { LanguageCode } from '@/shared/types/i18n'

/**
 * Format date with time based on language preference
 */
export function formatDateTime(date: Date | string, _language: LanguageCode): string {
  const dateObj = typeof date === 'string' ? new Date(date) : date
  
  // Check if date is valid
  if (isNaN(dateObj.getTime())) {
    console.warn('Invalid date provided to formatDateTime')
    // Return a fallback format for invalid dates
    return new Date().toISOString().replace('T', ' ').substring(0, 19)
  }
  
  try {
    // Format: YYYY-MM-DD HH:mm:ss (ISO style but more readable)
    const year = dateObj.getFullYear()
    const month = String(dateObj.getMonth() + 1).padStart(2, '0')
    const day = String(dateObj.getDate()).padStart(2, '0')
    const hours = String(dateObj.getHours()).padStart(2, '0')
    const minutes = String(dateObj.getMinutes()).padStart(2, '0')
    const seconds = String(dateObj.getSeconds()).padStart(2, '0')
    
    return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`
  } catch (error) {
    console.warn('Failed to format date, using fallback:', error)
    // Fallback to ISO format
    return dateObj.toISOString().replace('T', ' ').substring(0, 19)
  }
}

/**
 * Format date only (without time) based on language preference
 */
export function formatDate(date: Date | string, language: LanguageCode): string {
  const dateObj = typeof date === 'string' ? new Date(date) : date
  
  // Check if date is valid
  if (isNaN(dateObj.getTime())) {
    console.warn('Invalid date provided to formatDate')
    // Return a fallback format for invalid dates
    return new Date().toISOString().substring(0, 10)
  }
  
  const localeMap: Record<LanguageCode, string> = {
    'en': 'en-US',
    'pl': 'pl-PL', 
    'ro': 'ro-RO',
    'hu': 'hu-HU',
    'nl': 'nl-NL'
  }
  
  const locale = localeMap[language] || 'en-US'
  
  try {
    return dateObj.toLocaleDateString(locale, {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit'
    })
  } catch (error) {
    console.warn('Failed to format date with locale, using fallback:', error)
    return dateObj.toISOString().substring(0, 10)
  }
}

/**
 * Format relative time (e.g., "2 hours ago") based on language preference
 */
export function formatRelativeTime(date: Date | string, language: LanguageCode): string {
  const dateObj = typeof date === 'string' ? new Date(date) : date
  const now = new Date()
  const diffMs = now.getTime() - dateObj.getTime()
  const diffMinutes = Math.floor(diffMs / (1000 * 60))
  const diffHours = Math.floor(diffMinutes / 60)
  const diffDays = Math.floor(diffHours / 24)
  
  // Translations for relative time
  const translations: Record<LanguageCode, {
    now: string
    minutesAgo: string
    hoursAgo: string
    daysAgo: string
    yesterday: string
    today: string
  }> = {
    'en': {
      now: 'now',
      minutesAgo: 'min ago',
      hoursAgo: 'h ago', 
      daysAgo: 'd ago',
      yesterday: 'yesterday',
      today: 'today'
    },
    'pl': {
      now: 'teraz',
      minutesAgo: 'min temu',
      hoursAgo: 'godz temu',
      daysAgo: 'dni temu', 
      yesterday: 'wczoraj',
      today: 'dzisiaj'
    },
    'ro': {
      now: 'acum',
      minutesAgo: 'min în urmă',
      hoursAgo: 'h în urmă',
      daysAgo: 'zile în urmă',
      yesterday: 'ieri', 
      today: 'azi'
    },
    'hu': {
      now: 'most',
      minutesAgo: 'perce',
      hoursAgo: 'órája',
      daysAgo: 'napja',
      yesterday: 'tegnap',
      today: 'ma'
    },
    'nl': {
      now: 'nu',
      minutesAgo: 'min geleden',
      hoursAgo: 'u geleden', 
      daysAgo: 'd geleden',
      yesterday: 'gisteren',
      today: 'vandaag'
    }
  }
  
  const t = translations[language] || translations['en']
  
  if (diffMinutes < 1) return t.now
  if (diffMinutes < 60) return `${diffMinutes} ${t.minutesAgo}`
  if (diffHours < 24) return `${diffHours} ${t.hoursAgo}`
  if (diffDays === 1) return t.yesterday
  if (diffDays < 7) return `${diffDays} ${t.daysAgo}`
  
  // For older dates, show formatted date
  return formatDate(dateObj, language)
}
