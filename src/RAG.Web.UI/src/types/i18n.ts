export type LanguageCode = 'en' | 'pl' | 'ro' | 'hu' | 'nl';

export interface Language {
  code: LanguageCode;
  name: string;
  nativeName: string;
  flag: string;
}

export const SUPPORTED_LANGUAGES: Language[] = [
  { code: 'en', name: 'English', nativeName: 'English', flag: '🇺🇸' },
  { code: 'pl', name: 'Polish', nativeName: 'Polski', flag: '🇵🇱' },
  { code: 'ro', name: 'Romanian', nativeName: 'Română', flag: '🇷🇴' },
  { code: 'hu', name: 'Hungarian', nativeName: 'Magyar', flag: '🇭🇺' },
  { code: 'nl', name: 'Dutch', nativeName: 'Nederlands', flag: '🇳🇱' },
];

export const DEFAULT_LANGUAGE: LanguageCode = 'en';

export interface TranslationKeys {
  // Navigation
  'nav.dashboard': string;
  'nav.chat': string;
  'nav.search': string;
  'nav.documents': string;
  'nav.ingestion': string;
  'nav.analytics': string;
  'nav.settings': string;

  // Common keys
  'common.loading': string;
  'common.error': string;
  'common.success': string;
  'common.cancel': string;
  'common.save': string;
  'common.delete': string;
  'common.edit': string;
  'common.back': string;
  'common.next': string;
  'common.previous': string;
  'common.close': string;
  'common.open': string;
  'common.user_menu': string;
  'common.toggle_menu': string;
  'common.confirm': string;
  'common.view_details': string;
  'common.export': string;
  'common.settings': string;

  // Dashboard
  'dashboard.title': string;
  'dashboard.subtitle': string;
  'dashboard.overview': string;
  'dashboard.recent_activity': string;
  'dashboard.metrics': string;
  'dashboard.documents_count': string;
  'dashboard.queries_count': string;
  'dashboard.active_sessions': string;
  'dashboard.features.chat.title': string;
  'dashboard.features.chat.description': string;
  'dashboard.features.search.title': string;
  'dashboard.features.search.description': string;
  'dashboard.features.analytics.title': string;
  'dashboard.features.analytics.description': string;

  // Chat
  'chat.title': string;
  'chat.subtitle': string;
  'chat.new_conversation': string;
  'chat.type_message': string;
  'chat.send': string;
  'chat.clear': string;
  'chat.thinking': string;
  'chat.error_message': string;
  'chat.select_plugin': string;
  'chat.send_message': string;
  'chat.history': string;
  'chat.clear_history': string;
  'chat.no_messages': string;
  'chat.start_conversation': string;
  'chat.input.placeholder': string;
  'chat.new_session': string;
  'chat.sessions': string;
  'chat.no_sessions': string;
  'chat.loading': string;
  'chat.error': string;
  'chat.language_detected': string;
  'chat.translated_from': string;
  'chat.processing_time': string;
  'chat.sources': string;

  // Document database status
  'chat.documents_unavailable': string;
  'chat.documents_unavailable_message': string;

  // Search
  'search.title': string;
  'search.subtitle': string;
  'search.placeholder': string;
  'search.search': string;
  'search.clear': string;
  'search.results': string;
  'search.no_results': string;
  'search.results_count': string;
  'search.enter_query': string;
  'search.filter_by_source': string;
  'search.all_sources': string;
  'search.input.placeholder': string;
  'search.button': string;
  'search.loading': string;
  'search.error': string;
  'search.filters.title': string;
  'search.filters.max_results': string;
  'search.filters.threshold': string;

  // Language
  'language.selector.title': string;
  'language.selector.current': string;
  'language.selector.select': string;
  'language.auto_detect': string;
  'language.change_success': string;
  'language.auto_detected': string;
}

export type Translations = Record<LanguageCode, Partial<TranslationKeys>>;
