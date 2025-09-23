import React, { useState, useRef, useEffect } from 'react';
import { ChevronDown, Check, Globe } from 'lucide-react';
import ReactCountryFlag from 'react-country-flag';
import { useI18n } from '@/contexts/I18nContext';
import { Language } from '@/types/i18n';

/* eslint-disable jsx-a11y/aria-proptypes */

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
          onClick={() => setIsOpen(!isOpen)}
          className="flex items-center gap-2 px-3 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
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
        <div className="absolute right-0 mt-2 w-64 bg-white rounded-lg shadow-lg border border-gray-200 py-2 z-50">
          <div className="px-3 py-2 border-b border-gray-100">
            <p className="text-sm font-medium text-gray-900">{t('language.selector.title')}</p>
            {isAutoDetected && (
              <p className="text-xs text-gray-500 mt-1">
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
                className="w-full flex items-center justify-between px-3 py-2 text-left hover:bg-gray-50 transition-colors"
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
                    <p className="text-sm font-medium text-gray-900">{lang.nativeName}</p>
                    <p className="text-xs text-gray-500">{lang.name}</p>
                  </div>
                </div>
                {language === lang.code && (
                  <Check className="h-4 w-4 text-blue-600" />
                )}
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
