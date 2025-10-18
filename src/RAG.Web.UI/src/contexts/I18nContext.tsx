import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { LanguageCode, TranslationKeys, SUPPORTED_LANGUAGES } from '@/types/i18n';
import { translations } from '@/locales';
import { getSavedLanguage, saveLanguage, isLanguageAutoDetected } from '@/utils/language';

interface I18nContextType {
  language: LanguageCode;
  setLanguage: (language: LanguageCode) => void;
  t: (key: keyof TranslationKeys, params?: Record<string, string> | string[]) => string;
  languages: typeof SUPPORTED_LANGUAGES;
  isAutoDetected: boolean;
}

const I18nContext = createContext<I18nContextType | undefined>(undefined);

interface I18nProviderProps {
  children: ReactNode;
}

export function I18nProvider({ children }: I18nProviderProps) {
  const [language, setLanguageState] = useState<LanguageCode>(() => getSavedLanguage());
  const [isAutoDetected, setIsAutoDetected] = useState(() => isLanguageAutoDetected());

  const setLanguage = (newLanguage: LanguageCode) => {
    setLanguageState(newLanguage);
    saveLanguage(newLanguage);
    setIsAutoDetected(false);
  };

  const t = (key: keyof TranslationKeys, params?: Record<string, string> | string[]): string => {
    const translation = translations[language]?.[key] || translations.en[key] || key;
    
    if (!params) {
      return translation;
    }

    // Handle named parameters (object with key-value pairs)
    if (!Array.isArray(params)) {
      return Object.entries(params).reduce((str, [paramKey, paramValue]) => {
        return str.replace(new RegExp(`\\{${paramKey}\\}`, 'g'), paramValue);
      }, translation);
    }

    // Handle indexed parameters (array with positional values)
    return params.reduce((str, arg, index) => {
      return str.replace(new RegExp(`\\{${index}\\}`, 'g'), arg);
    }, translation);
  };

  useEffect(() => {
    // Update document lang attribute
    document.documentElement.lang = language;
  }, [language]);

  const value: I18nContextType = {
    language,
    setLanguage,
    t,
    languages: SUPPORTED_LANGUAGES,
    isAutoDetected,
  };

  return (
    <I18nContext.Provider value={value}>
      {children}
    </I18nContext.Provider>
  );
}

export function useI18n(): I18nContextType {
  const context = useContext(I18nContext);
  if (context === undefined) {
    throw new Error('useI18n must be used within an I18nProvider');
  }
  return context;
}
