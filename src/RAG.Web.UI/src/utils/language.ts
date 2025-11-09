import { SUPPORTED_LANGUAGES, DEFAULT_LANGUAGE, LanguageCode } from '@/shared/types/i18n';
import { createScopedLogger } from '@/utils/logger';
const log = createScopedLogger('language');

const LANGUAGE_STORAGE_KEY = 'rag-suite-language';

/**
 * Get browser's preferred language
 */
export function getBrowserLanguage(): LanguageCode {
  // Check navigator.languages for better accuracy
  const browserLanguages = navigator.languages || [navigator.language];
  
  for (const browserLang of browserLanguages) {
    // Extract language code (e.g., 'en-US' -> 'en')
    const langCode = browserLang.split('-')[0].toLowerCase();
    
    // Check if we support this language
    if (SUPPORTED_LANGUAGES.some(lang => lang.code === langCode)) {
      return langCode as LanguageCode;
    }
  }
  
  return DEFAULT_LANGUAGE as LanguageCode;
}

/**
 * Get saved language from localStorage or browser default
 */
export function getSavedLanguage(): LanguageCode {
  try {
    const saved = localStorage.getItem(LANGUAGE_STORAGE_KEY);
    if (saved && SUPPORTED_LANGUAGES.some(lang => lang.code === saved)) {
      return saved as LanguageCode;
    }
  } catch (error) {
    log.warn('Failed to read language from localStorage:', error);
  }
  
  // Fallback to browser language
  return getBrowserLanguage();
}

/**
 * Save language preference to localStorage
 */
export function saveLanguage(languageCode: LanguageCode): void {
  try {
    localStorage.setItem(LANGUAGE_STORAGE_KEY, languageCode);
  } catch (error) {
    log.warn('Failed to save language to localStorage:', error);
  }
}

/**
 * Clear saved language preference
 */
export function clearSavedLanguage(): void {
  try {
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  } catch (error) {
    log.warn('Failed to clear language from localStorage:', error);
  }
}

/**
 * Check if language was auto-detected from browser
 */
export function isLanguageAutoDetected(): boolean {
  try {
    return !localStorage.getItem(LANGUAGE_STORAGE_KEY);
  } catch {
    return true;
  }
}
