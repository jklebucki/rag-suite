// All code comments must be written in English, regardless of the conversation language.

/**
 * Username helpers that mirror the ASP.NET Identity `User.AllowedUserNameCharacters`
 * default set. Keeping the client rules in sync with Identity prevents a request
 * that looks valid in the UI from failing server-side with a generic 400.
 */
export const ALLOWED_USER_NAME_CHARACTERS = '-._@+'

const ALLOWED_USER_NAME_PATTERN = /^[a-zA-Z0-9\-._@+]+$/

/** Combining diacritical marks left over after NFD normalization. */
const COMBINING_MARKS = new RegExp('[\\u0300-\\u036f]', 'g')

/**
 * Latin characters that survive NFD normalization and therefore need an explicit
 * transliteration (e.g. Polish 'l with stroke' has no combining-mark decomposition).
 */
const NON_DECOMPOSABLE_CHARACTERS: Record<string, string> = {
  'ł': 'l', // ł
  'Ł': 'L', // Ł
  'ø': 'o', // ø
  'Ø': 'O', // Ø
  'đ': 'd', // đ
  'Đ': 'D', // Đ
  'ð': 'd', // ð
  'Ð': 'D', // Ð
  'ß': 'ss', // ß
  'æ': 'ae', // æ
  'Æ': 'AE', // Æ
  'œ': 'oe', // œ
  'Œ': 'OE', // Œ
  'þ': 'th', // þ
  'Þ': 'TH', // Þ
}

/**
 * Replaces diacritics with their plain ASCII counterparts
 * (s-acute -> s, c-acute -> c, l-stroke -> l, o-double-acute -> o, s-comma -> s).
 */
export function removeDiacritics(value: string): string {
  if (!value) {
    return ''
  }

  const transliterated = Array.from(value)
    .map((char) => NON_DECOMPOSABLE_CHARACTERS[char] ?? char)
    .join('')

  return transliterated.normalize('NFD').replace(COMBINING_MARKS, '')
}

/**
 * Splits a compound name into ASCII-only parts. Handles multiple given names and
 * multi-part surnames written either with a hyphen or with spaces
 * ('Klebucki-Pasisz' and 'Klebucki Pasisz' both yield ['Klebucki', 'Pasisz']).
 */
function splitNameParts(value: string): string[] {
  return removeDiacritics(value ?? '')
    .split(/[\s\-_]+/)
    .map((part) => part.replace(/[^a-zA-Z0-9]/g, ''))
    .filter((part) => part.length > 0)
}

/**
 * Builds a username proposal from the first and last name: the initial of every
 * given name followed by the full surname, hyphenated when multi-part.
 *
 * 'Jaroslaw Piotr' + 'Klebucki-Pasisz' -> 'jpklebucki-pasisz'
 * 'Paulina' + 'Helbrecht'              -> 'phelbrecht'
 */
export function suggestUserName(firstName: string, lastName: string): string {
  const initials = splitNameParts(firstName)
    .map((part) => part.charAt(0))
    .join('')
  const surname = splitNameParts(lastName).join('-')

  return `${initials}${surname}`.toLowerCase()
}

/**
 * Validates a username against the character set accepted by ASP.NET Identity.
 * Returns the raw offending characters so the caller can render a localized,
 * user-facing message pointing at the real problem.
 */
export function validateUserNameCharacters(userName: string): {
  isValid: boolean
  invalidCharacters: string[]
} {
  if (!userName) {
    return { isValid: false, invalidCharacters: [] }
  }

  if (ALLOWED_USER_NAME_PATTERN.test(userName)) {
    return { isValid: true, invalidCharacters: [] }
  }

  const invalidCharacters = Array.from(
    new Set(Array.from(userName).filter((char) => !ALLOWED_USER_NAME_PATTERN.test(char)))
  )

  return { isValid: false, invalidCharacters }
}
