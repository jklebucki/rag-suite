import React, { useState, useRef, useEffect } from 'react';
import { ChevronDown, Check } from 'lucide-react';
import ReactCountryFlag from 'react-country-flag';
import { useI18n } from '@/shared/contexts/I18nContext';
import { Language } from '@/shared/types/i18n';

 

export function LanguageSelector() {
  const { language, setLanguage, languages, isAutoDetected, t } = useI18n();
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  const currentLanguage = languages.find(lang => lang.code === language);

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleLanguageSelect = (selectedLanguage: Language) => {
    setLanguage(selectedLanguage.code);
    setIsOpen(false);
  };

  return (
    <div className="relative" ref={dropdownRef}>
              <button
          data-language-selector-toggle
          onClick={() => setIsOpen(!isOpen)}
          className="hidden md:inline-flex items-center gap-2 px-3 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 dark:bg-gray-800 dark:text-gray-200 dark:border-gray-600 dark:hover:bg-gray-700"
        >
          {currentLanguage && (
            <ReactCountryFlag
              countryCode={currentLanguage.countryCode}
              svg
              style={{
                width: '20px',
                height: '15px',
              }}
            />
          )}
          <span>{currentLanguage?.nativeName}</span>
          <ChevronDown className="w-4 h-4" />
        </button>

      {isOpen && (
        <div className="fixed right-0 top-16 w-64 bg-white rounded-lg shadow-lg border border-gray-200 py-2 z-50 md:absolute md:right-0 md:top-auto md:mt-2 dark:bg-gray-800 dark:border-gray-700">
          <div className="px-3 py-2 border-b border-gray-100 dark:border-gray-700">
            <p className="text-sm font-medium text-gray-900 dark:text-gray-100">{t('language.selector.title')}</p>
            {isAutoDetected && (
              <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                {t('language.auto_detected')}
              </p>
            )}
          </div>
          
          <div className="py-1">
            {languages.map((lang) => (
              <button
                key={lang.code}
                type="button"
                onClick={() => handleLanguageSelect(lang)}
                className="w-full flex items-center justify-between px-3 py-2 text-left hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
              >
                <div className="flex items-center gap-3">
                  <ReactCountryFlag
                    countryCode={lang.countryCode}
                    svg
                    style={{
                      width: '20px',
                      height: '15px',
                    }}
                  />
                  <div>
                    <p className="text-sm font-medium text-gray-900 dark:text-gray-100">{lang.nativeName}</p>
                    <p className="text-xs text-gray-500 dark:text-gray-400">{lang.name}</p>
                  </div>
                </div>
                {language === lang.code && (
                  <Check className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                )}
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
