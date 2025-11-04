export type LanguageCode = 'en' | 'pl' | 'ro' | 'hu' | 'nl';

export interface Language {
  code: LanguageCode;
  name: string;
  nativeName: string;
  flag: string;
  countryCode: string;
}

export const SUPPORTED_LANGUAGES: Language[] = [
  { code: 'en', name: 'English', nativeName: 'English', flag: '🇺🇸', countryCode: 'US' },
  { code: 'pl', name: 'Polish', nativeName: 'Polski', flag: '🇵🇱', countryCode: 'PL' },
  { code: 'ro', name: 'Romanian', nativeName: 'Română', flag: '🇷🇴', countryCode: 'RO' },
  { code: 'hu', name: 'Hungarian', nativeName: 'Magyar', flag: '🇭🇺', countryCode: 'HU' },
  { code: 'nl', name: 'Dutch', nativeName: 'Nederlands', flag: '🇳🇱', countryCode: 'NL' },
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
  'nav.addressBook': string;
  'nav.user_guide': string;
  'nav.app_info': string;
  'nav.cyberpanel': string;

  // User Guide
  'userguide.description': string;
  'userguide.browser_not_supported': string;
  'userguide.download_pdf': string;

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
  'chat.delete_session_title': string;
  'chat.delete_session_confirm': string;
  'chat.loading': string;
  'chat.error': string;
  'chat.language_detected': string;
  'chat.translated_from': string;
  'chat.processing_time': string;
  'chat.sources': string;
  'chat.sources.title': string;
  'chat.sources.summary': string;
  'chat.useDocumentSearch': string;

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

  // Authentication
  'auth.login.title': string;
  'auth.login.subtitle': string;
  'auth.login.sign_in': string;
  'auth.logout': string;
  'auth.login.signing_in': string;
  'auth.login.remember_me': string;
  'auth.login.forgot_password': string;
  'auth.login.no_account': string;
  'auth.login.sign_up': string;
  'auth.login.success_title': string;
  'auth.login.success_message': string;

  'auth.register.title': string;
  'auth.register.subtitle': string;
  'auth.register.sign_up': string;
  'auth.register.signing_up': string;
  'auth.register.accept_terms': string;
  'auth.register.have_account': string;
  'auth.register.sign_in': string;
  'auth.register.success_title': string;
  'auth.register.success_message': string;

  'auth.reset.title': string;
  'auth.reset.subtitle': string;
  'auth.reset.send_instructions': string;
  'auth.reset.sending': string;
  'auth.reset.back_to_login': string;
  'auth.reset.success_title': string;
  'auth.reset.success_message': string;

  'auth.reset_confirm.title': string;
  'auth.reset_confirm.subtitle': string;
  'auth.reset_confirm.reset_password': string;
  'auth.reset_confirm.resetting': string;
  'auth.reset_confirm.back_to_login': string;
  'auth.reset_confirm.success_title': string;
  'auth.reset_confirm.success_message': string;
  'auth.reset_confirm.redirect_message': string;

  'auth.change_password.title': string;
  'auth.change_password.subtitle': string;
  'auth.change_password.change': string;
  'auth.change_password.changing': string;
  'auth.change_password.success_title': string;
  'auth.change_password.success_message': string;

  'auth.fields.email': string;
  'auth.fields.password': string;
  'auth.fields.confirm_password': string;
  'auth.fields.current_password': string;
  'auth.fields.new_password': string;
  'auth.fields.first_name': string;
  'auth.fields.last_name': string;
  'auth.fields.username': string;

  'auth.placeholders.email': string;
  'auth.placeholders.password': string;
  'auth.placeholders.confirm_password': string;
  'auth.placeholders.current_password': string;
  'auth.placeholders.new_password': string;
  'auth.placeholders.first_name': string;
  'auth.placeholders.last_name': string;
  'auth.placeholders.username': string;

  'auth.validation.email_required': string;
  'auth.validation.email_invalid': string;
  'auth.validation.password_required': string;
  'auth.validation.password_min_length': string;
  'auth.validation.password_require_digit': string;
  'auth.validation.password_require_uppercase': string;
  'auth.validation.password_require_lowercase': string;
  'auth.validation.password_require_special': string;
  'auth.validation.password_mismatch': string;
  'auth.validation.confirm_password_required': string;
  'auth.validation.passwords_do_not_match': string;
  'auth.validation.first_name_required': string;
  'auth.validation.last_name_required': string;
  'auth.validation.username_required': string;
  'auth.validation.username_min_length': string;
  'auth.validation.terms_required': string;

  'auth.logout.title': string;
  'auth.logout.confirm': string;
  'auth.logout.signing_out': string;
  'auth.logout.success': string;

  // Session expired
  'session.expired.title': string;
  'session.expired.message': string;
  'session.expired.try_again': string;
  'session.expired.logout': string;

  // Account Management
  'account.title': string;
  'account.profile_tab': string;
  'account.security_tab': string;
  'account.firstName': string;
  'account.lastName': string;
  'account.username': string;
  'account.email': string;
  'account.roles': string;
  'account.created_at': string;
  'account.last_login': string;
  'account.update_profile': string;
  'account.manage_account': string;
  'account.danger_zone': string;
  'account.delete_warning': string;
  'account.delete_account': string;
  'account.logout_all_devices': string;
  'account.logout_all_devices_description': string;
  'account.logout_all_devices_confirm': string;

  // Cyber Panel
  'cyberpanel.quizzes': string;
  'cyberpanel.builder': string;
  'cyberpanel.results': string;
  'cyberpanel.preview': string;
  'cyberpanel.closePreview': string;
  'cyberpanel.untitledQuiz': string;
  'cyberpanel.noQuestionText': string;
  'cyberpanel.points': string;
  'cyberpanel.correctAnswer': string;
  'cyberpanel.editQuiz': string;
  'cyberpanel.createQuiz': string;
  'cyberpanel.validationErrors': string;
  'cyberpanel.quizDetails': string;
  'cyberpanel.title': string;
  'cyberpanel.titlePlaceholder': string;
  'cyberpanel.description': string;
  'cyberpanel.descriptionPlaceholder': string;
  'cyberpanel.quizImage': string;
  'cyberpanel.uploadImage': string;
  'cyberpanel.changeImage': string;
  'cyberpanel.questions': string;
  'cyberpanel.addQuestion': string;
  'cyberpanel.questionText': string;
  'cyberpanel.questionTextPlaceholder': string;
  'cyberpanel.questionPoints': string;
  'cyberpanel.questionImage': string;
  'cyberpanel.options': string;
  'cyberpanel.addOption': string;
  'cyberpanel.optionText': string;
  'cyberpanel.optionImage': string;
  'cyberpanel.removeQuestion': string;
  'cyberpanel.removeOption': string;
  'cyberpanel.moveUp': string;
  'cyberpanel.moveDown': string;
  'cyberpanel.cancel': string;
  'cyberpanel.save': string;
  'cyberpanel.saving': string;
  'cyberpanel.quizCreated': string;
  'cyberpanel.quizUpdated': string;
  'cyberpanel.errorCreating': string;
  'cyberpanel.errorUpdating': string;
  'cyberpanel.errorLoading': string;
  'cyberpanel.imageUploadError': string;
  'cyberpanel.imageTooLarge': string;
  'cyberpanel.mustHaveCorrectAnswer': string;
  'cyberpanel.duplicateOptions': string;
  'cyberpanel.publishQuiz': string;
  'cyberpanel.noQuestions': string;
  'cyberpanel.question': string;
  'cyberpanel.option': string;
  'cyberpanel.exportQuiz': string;
  'cyberpanel.importQuiz': string;
  'cyberpanel.exportSuccess': string;
  'cyberpanel.importSuccess': string;
  'cyberpanel.importError': string;
  'cyberpanel.selectJsonFile': string;
  'cyberpanel.newQuiz': string;
  'cyberpanel.edit': string;
  'cyberpanel.delete': string;
  'cyberpanel.confirmDelete': string;
  'cyberpanel.deleteSuccess': string;
  'cyberpanel.deleteError': string;
  'cyberpanel.questionsCount': string;
  'cyberpanel.published': string;
  'cyberpanel.draft': string;
  'cyberpanel.actions': string;
  'cyberpanel.noQuizzesYet': string;
  'cyberpanel.createFirstQuiz': string;
  'cyberpanel.takeQuiz': string;
  'cyberpanel.startQuiz': string;
  'cyberpanel.submitAnswers': string;
  'cyberpanel.backToQuizzes': string;
  'cyberpanel.quizResults': string;
  'cyberpanel.yourScore': string;
  'cyberpanel.correctAnswers': string;
  'cyberpanel.incorrectAnswers': string;
  'cyberpanel.totalPoints': string;
  'cyberpanel.earnedPoints': string;
  'cyberpanel.percentage': string;
  'cyberpanel.questionNumber': string;
  'cyberpanel.selectAnswers': string;
  'cyberpanel.correct': string;
  'cyberpanel.incorrect': string;
  'cyberpanel.notAnswered': string;
  'cyberpanel.answerDetails': string;
  'cyberpanel.yourAnswers': string;
  'cyberpanel.correctAnswerWas': string;
  'cyberpanel.pleaseSelectAnswer': string;
  'cyberpanel.submitting': string;
  'cyberpanel.loadingQuiz': string;
  'cyberpanel.quizNotFound': string;
  'cyberpanel.retakeQuiz': string;
  'cyberpanel.noResultsYet': string;
  'cyberpanel.takeFirstQuiz': string;
  'cyberpanel.browseQuizzes': string;
  'cyberpanel.viewQuiz': string;
  'cyberpanel.score': string;
  'cyberpanel.userName': string;
  'cyberpanel.submittedAt': string;
  'cyberpanel.backToResults': string;
  'cyberpanel.yourAnswer': string;
  'cyberpanel.yourCorrectAnswer': string;
  'cyberpanel.viewDetails': string;

  // Address Book
  'addressBook.title': string;
  'addressBook.subtitle': string;
  'addressBook.tabs.contacts': string;
  'addressBook.tabs.import': string;
  'addressBook.tabs.proposals': string;
  'addressBook.addContact': string;
  'addressBook.search': string;
  'addressBook.noContacts': string;
  'addressBook.loading': string;
  'addressBook.permissions.admin': string;
  'addressBook.permissions.user': string;
  'addressBook.permissions.guest': string;
  'addressBook.table.details': string;
  'addressBook.table.firstName': string;
  'addressBook.table.lastName': string;
  'addressBook.table.email': string;
  'addressBook.table.mobilePhone': string;
  'addressBook.table.department': string;
  'addressBook.table.position': string;
  'addressBook.table.location': string;
  'addressBook.table.phone': string;
  'addressBook.table.actions': string;
  'addressBook.table.noResults': string;
  'addressBook.table.rowsPerPage': string;
  'addressBook.table.of': string;
  'addressBook.table.configureColumns': string;
  'addressBook.table.showColumn': string;
  'addressBook.table.hideColumn': string;
  'addressBook.table.searchIn': string;
  'addressBook.table.displayName': string;
  'addressBook.table.mobile': string;
  'addressBook.table.status': string;
  'addressBook.table.active': string;
  'addressBook.table.inactive': string;
  'addressBook.table.totalContacts': string;
  'addressBook.actions.edit': string;
  'addressBook.actions.delete': string;
  'addressBook.actions.propose': string;
  'addressBook.actions.view': string;
  'addressBook.deleteConfirm': string;
  'addressBook.form.title.create': string;
  'addressBook.form.title.edit': string;
  'addressBook.form.title.propose': string;
  'addressBook.form.firstName': string;
  'addressBook.form.lastName': string;
  'addressBook.form.displayName': string;
  'addressBook.form.email': string;
  'addressBook.form.department': string;
  'addressBook.form.position': string;
  'addressBook.form.location': string;
  'addressBook.form.company': string;
  'addressBook.form.workPhone': string;
  'addressBook.form.mobilePhone': string;
  'addressBook.form.notes': string;
  'addressBook.form.tags': string;
  'addressBook.form.isActive': string;
  'addressBook.form.required': string;
  'addressBook.form.invalidEmail': string;
  'addressBook.form.submit': string;
  'addressBook.form.reason': string;
  'addressBook.form.reasonPlaceholder': string;
  'addressBook.import.title': string;
  'addressBook.import.format': string;
  'addressBook.import.formatDesc': string;
  'addressBook.import.required': string;
  'addressBook.import.optional': string;
  'addressBook.import.uploadFile': string;
  'addressBook.import.clickOrDrag': string;
  'addressBook.import.csvOnly': string;
  'addressBook.import.skipDuplicates': string;
  'addressBook.import.encoding': string;
  'addressBook.import.upload': string;
  'addressBook.import.uploading': string;
  'addressBook.import.selectFile': string;
  'addressBook.import.csvFileOnly': string;
  'addressBook.import.complete': string;
  'addressBook.import.totalRows': string;
  'addressBook.import.imported': string;
  'addressBook.import.skipped': string;
  'addressBook.import.errors': string;
  'addressBook.proposals.title': string;
  'addressBook.proposals.noProposals': string;
  'addressBook.proposals.type.create': string;
  'addressBook.proposals.type.update': string;
  'addressBook.proposals.type.delete': string;
  'addressBook.proposals.status.pending': string;
  'addressBook.proposals.status.approved': string;
  'addressBook.proposals.status.rejected': string;
  'addressBook.proposals.status.applied': string;
  'addressBook.proposals.proposedBy': string;
  'addressBook.proposals.proposedAt': string;
  'addressBook.proposals.reason': string;
  'addressBook.proposals.reviewedBy': string;
  'addressBook.proposals.approve': string;
  'addressBook.proposals.approveAndApply': string;
  'addressBook.proposals.reject': string;
  'addressBook.proposals.comment': string;
  'addressBook.proposals.commentPlaceholder': string;
  'addressBook.proposals.cancel': string;
  'addressBook.proposals.reviewProposal': string;
  'addressBook.proposals.processing': string;
  'addressBook.proposals.loading': string;
  'addressBook.proposals.noProposalsFound': string;
  'addressBook.proposals.newContact': string;
  'addressBook.proposals.failedToReview': string;

  // Messages and alerts
  'addressBook.messages.failedToLoad': string;
  'addressBook.messages.failedToLoadProposals': string;
  'addressBook.messages.proposalSubmitted': string;
  'addressBook.messages.deleteConfirmTitle': string;
  'addressBook.messages.deletionProposalSubmitted': string;
  'addressBook.messages.failedToDelete': string;
  'addressBook.messages.unknownError': string;
  'addressBook.messages.failedToReview': string;
  'addressBook.messages.importFailed': string;
  'addressBook.messages.submitting': string;
  'addressBook.messages.update': string;
  'addressBook.messages.create': string;
  'addressBook.messages.proposeChange': string;
  'addressBook.messages.newContact': string;
  'addressBook.messages.contactUpdateProposal': string;
  'addressBook.messages.contactDeletionProposal': string;
  'addressBook.messages.newContactProposal': string;
}

export type Translations = Record<LanguageCode, Partial<TranslationKeys>>;

