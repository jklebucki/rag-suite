import { useI18n } from '@/shared/contexts/I18nContext'
import type { LanguageCode } from '@/shared/types/i18n'

export type HrAbsenceTranslationKey =
  | 'title'
  | 'subtitle'
  | 'tabs.dashboard'
  | 'tabs.allRequests'
  | 'dashboard.all'
  | 'dashboard.allDesc'
  | 'dashboard.pending'
  | 'dashboard.pendingDesc'
  | 'dashboard.payrollClosure'
  | 'dashboard.payrollClosureDesc'
  | 'dashboard.overdue'
  | 'dashboard.overdueDesc'
  | 'requests.title'
  | 'requests.summary'
  | 'requests.onlyPayrollClosure'
  | 'requests.onlyOverdue'
  | 'requests.col.employee'
  | 'requests.col.leaveType'
  | 'requests.col.dateFrom'
  | 'requests.col.dateTo'
  | 'requests.col.days'
  | 'requests.col.submittedAt'
  | 'requests.col.manager'
  | 'requests.col.status'
  | 'requests.col.action'
  | 'requests.submittedShort'
  | 'table.searchIn'
  | 'table.noResults'
  | 'status.pending'
  | 'status.approved'
  | 'status.rejected'
  | 'leaveType.annual'
  | 'leaveType.onDemand'
  | 'leaveType.occasional'
  | 'leaveType.childCare'
  | 'action.payrollClosure'
  | 'action.overdue'
  | 'action.none'
  | 'common.days'
  | 'error.loadFailed'

const en: Record<HrAbsenceTranslationKey, string> = {
  title: 'HR absence management',
  subtitle: 'Leave requests for the selected company',
  'tabs.dashboard': 'Dashboard',
  'tabs.allRequests': 'All requests',
  'dashboard.all': 'All requests',
  'dashboard.allDesc': 'Requests in the selected company',
  'dashboard.pending': 'Pending',
  'dashboard.pendingDesc': 'Awaiting decision',
  'dashboard.payrollClosure': 'To close before payroll',
  'dashboard.payrollClosureDesc': 'Require action in the payroll period',
  'dashboard.overdue': 'Overdue',
  'dashboard.overdueDesc': 'Pending after the absence end date',
  'requests.title': 'All leave requests',
  'requests.summary': '{visible} displayed out of {total}',
  'requests.onlyPayrollClosure': 'Payroll closure only',
  'requests.onlyOverdue': 'Overdue only',
  'requests.col.employee': 'Employee',
  'requests.col.leaveType': 'Leave type',
  'requests.col.dateFrom': 'From',
  'requests.col.dateTo': 'To',
  'requests.col.days': 'Days',
  'requests.col.submittedAt': 'Submitted at',
  'requests.col.manager': 'Manager',
  'requests.col.status': 'Status',
  'requests.col.action': 'Alert / required action',
  'requests.submittedShort': 'Submitted',
  'table.searchIn': 'Search in',
  'table.noResults': 'No requests match the selected filters',
  'status.pending': 'Pending',
  'status.approved': 'Approved',
  'status.rejected': 'Rejected',
  'leaveType.annual': 'Annual leave',
  'leaveType.onDemand': 'On-demand leave',
  'leaveType.occasional': 'Occasional leave',
  'leaveType.childCare': 'Child care',
  'action.payrollClosure': 'Close before payroll',
  'action.overdue': 'Decision overdue',
  'action.none': 'No action required',
  'common.days': 'days',
  'error.loadFailed': 'Could not load absence requests.',
}

const pl: Record<HrAbsenceTranslationKey, string> = {
  title: 'Zarządzanie nieobecnościami HR',
  subtitle: 'Wnioski urlopowe dla wybranej firmy',
  'tabs.dashboard': 'Dashboard',
  'tabs.allRequests': 'Wszystkie wnioski',
  'dashboard.all': 'Wszystkie wnioski',
  'dashboard.allDesc': 'Wnioski w wybranej firmie',
  'dashboard.pending': 'Oczekujące',
  'dashboard.pendingDesc': 'Nierozstrzygnięte wnioski',
  'dashboard.payrollClosure': 'Do zamknięcia przed listą płac',
  'dashboard.payrollClosureDesc': 'Wymagają działania w okresie płacowym',
  'dashboard.overdue': 'Po terminie',
  'dashboard.overdueDesc': 'Nierozstrzygnięte po dacie nieobecności',
  'requests.title': 'Wszystkie wnioski urlopowe',
  'requests.summary': 'Wyświetlono {visible} z {total}',
  'requests.onlyPayrollClosure': 'Tylko do zamknięcia przed listą płac',
  'requests.onlyOverdue': 'Tylko po terminie',
  'requests.col.employee': 'Pracownik',
  'requests.col.leaveType': 'Typ urlopu',
  'requests.col.dateFrom': 'Od',
  'requests.col.dateTo': 'Do',
  'requests.col.days': 'Liczba dni',
  'requests.col.submittedAt': 'Data złożenia',
  'requests.col.manager': 'Przełożony',
  'requests.col.status': 'Status',
  'requests.col.action': 'Alert / wymagane działanie',
  'requests.submittedShort': 'Złożono',
  'table.searchIn': 'Szukaj w',
  'table.noResults': 'Brak wniosków spełniających wybrane filtry',
  'status.pending': 'Oczekujący',
  'status.approved': 'Zaakceptowany',
  'status.rejected': 'Odrzucony',
  'leaveType.annual': 'Urlop wypoczynkowy',
  'leaveType.onDemand': 'Urlop na żądanie',
  'leaveType.occasional': 'Urlop okolicznościowy',
  'leaveType.childCare': 'Opieka nad dzieckiem',
  'action.payrollClosure': 'Zamknij przed listą płac',
  'action.overdue': 'Termin decyzji minął',
  'action.none': 'Brak wymaganego działania',
  'common.days': 'dni',
  'error.loadFailed': 'Nie udało się pobrać wniosków urlopowych.',
}

const ro: Record<HrAbsenceTranslationKey, string> = {
  title: 'Gestionarea absențelor HR',
  subtitle: 'Cereri de concediu pentru compania selectată',
  'tabs.dashboard': 'Dashboard',
  'tabs.allRequests': 'Toate cererile',
  'dashboard.all': 'Toate cererile',
  'dashboard.allDesc': 'Cereri în compania selectată',
  'dashboard.pending': 'În așteptare',
  'dashboard.pendingDesc': 'Așteaptă o decizie',
  'dashboard.payrollClosure': 'De închis înainte de salarizare',
  'dashboard.payrollClosureDesc': 'Necesită acțiune în perioada de salarizare',
  'dashboard.overdue': 'Termen depășit',
  'dashboard.overdueDesc': 'Nerezolvate după data absenței',
  'requests.title': 'Toate cererile de concediu',
  'requests.summary': '{visible} afișate din {total}',
  'requests.onlyPayrollClosure': 'Doar pentru închiderea salarizării',
  'requests.onlyOverdue': 'Doar cu termen depășit',
  'requests.col.employee': 'Angajat',
  'requests.col.leaveType': 'Tip concediu',
  'requests.col.dateFrom': 'De la',
  'requests.col.dateTo': 'Până la',
  'requests.col.days': 'Număr de zile',
  'requests.col.submittedAt': 'Data depunerii',
  'requests.col.manager': 'Manager',
  'requests.col.status': 'Status',
  'requests.col.action': 'Alertă / acțiune necesară',
  'requests.submittedShort': 'Depusă',
  'table.searchIn': 'Caută în',
  'table.noResults': 'Nicio cerere nu corespunde filtrelor selectate',
  'status.pending': 'În așteptare',
  'status.approved': 'Aprobată',
  'status.rejected': 'Respinsă',
  'leaveType.annual': 'Concediu anual',
  'leaveType.onDemand': 'Concediu la cerere',
  'leaveType.occasional': 'Concediu ocazional',
  'leaveType.childCare': 'Îngrijire copil',
  'action.payrollClosure': 'Închide înainte de salarizare',
  'action.overdue': 'Termenul deciziei a trecut',
  'action.none': 'Nicio acțiune necesară',
  'common.days': 'zile',
  'error.loadFailed': 'Cererile de concediu nu au putut fi încărcate.',
}

const hu: Record<HrAbsenceTranslationKey, string> = {
  title: 'HR távollétkezelés',
  subtitle: 'Szabadságkérelmek a kiválasztott vállalathoz',
  'tabs.dashboard': 'Dashboard',
  'tabs.allRequests': 'Összes kérelem',
  'dashboard.all': 'Összes kérelem',
  'dashboard.allDesc': 'Kérelmek a kiválasztott vállalatnál',
  'dashboard.pending': 'Függőben',
  'dashboard.pendingDesc': 'Döntésre vár',
  'dashboard.payrollClosure': 'Bérszámfejtés előtt lezárandó',
  'dashboard.payrollClosureDesc': 'Teendő a bérszámfejtési időszakban',
  'dashboard.overdue': 'Határidőn túl',
  'dashboard.overdueDesc': 'A távollét után is függőben',
  'requests.title': 'Összes szabadságkérelem',
  'requests.summary': '{visible} megjelenítve, összesen {total}',
  'requests.onlyPayrollClosure': 'Csak bérszámfejtés előtt lezárandó',
  'requests.onlyOverdue': 'Csak határidőn túli',
  'requests.col.employee': 'Munkavállaló',
  'requests.col.leaveType': 'Szabadság típusa',
  'requests.col.dateFrom': 'Kezdete',
  'requests.col.dateTo': 'Vége',
  'requests.col.days': 'Napok száma',
  'requests.col.submittedAt': 'Benyújtás dátuma',
  'requests.col.manager': 'Vezető',
  'requests.col.status': 'Állapot',
  'requests.col.action': 'Riasztás / szükséges művelet',
  'requests.submittedShort': 'Benyújtva',
  'table.searchIn': 'Keresés ebben:',
  'table.noResults': 'Nincs a szűrőknek megfelelő kérelem',
  'status.pending': 'Függőben',
  'status.approved': 'Jóváhagyva',
  'status.rejected': 'Elutasítva',
  'leaveType.annual': 'Éves szabadság',
  'leaveType.onDemand': 'Igény szerinti szabadság',
  'leaveType.occasional': 'Rendkívüli szabadság',
  'leaveType.childCare': 'Gyermekgondozás',
  'action.payrollClosure': 'Lezárás bérszámfejtés előtt',
  'action.overdue': 'A döntési határidő lejárt',
  'action.none': 'Nincs szükséges művelet',
  'common.days': 'nap',
  'error.loadFailed': 'A szabadságkérelmek betöltése sikertelen.',
}

const nl: Record<HrAbsenceTranslationKey, string> = {
  title: 'HR-afwezigheidsbeheer',
  subtitle: 'Verlofaanvragen voor het geselecteerde bedrijf',
  'tabs.dashboard': 'Dashboard',
  'tabs.allRequests': 'Alle aanvragen',
  'dashboard.all': 'Alle aanvragen',
  'dashboard.allDesc': 'Aanvragen in het geselecteerde bedrijf',
  'dashboard.pending': 'In behandeling',
  'dashboard.pendingDesc': 'Wachten op een beslissing',
  'dashboard.payrollClosure': 'Af te ronden vóór salarisverwerking',
  'dashboard.payrollClosureDesc': 'Actie vereist in de salarisperiode',
  'dashboard.overdue': 'Te laat',
  'dashboard.overdueDesc': 'Nog open na de einddatum van afwezigheid',
  'requests.title': 'Alle verlofaanvragen',
  'requests.summary': '{visible} weergegeven van {total}',
  'requests.onlyPayrollClosure': 'Alleen vóór salarisverwerking',
  'requests.onlyOverdue': 'Alleen te late aanvragen',
  'requests.col.employee': 'Medewerker',
  'requests.col.leaveType': 'Verloftype',
  'requests.col.dateFrom': 'Van',
  'requests.col.dateTo': 'Tot',
  'requests.col.days': 'Aantal dagen',
  'requests.col.submittedAt': 'Ingediend op',
  'requests.col.manager': 'Manager',
  'requests.col.status': 'Status',
  'requests.col.action': 'Waarschuwing / vereiste actie',
  'requests.submittedShort': 'Ingediend',
  'table.searchIn': 'Zoeken in',
  'table.noResults': 'Geen aanvragen voldoen aan de gekozen filters',
  'status.pending': 'In behandeling',
  'status.approved': 'Goedgekeurd',
  'status.rejected': 'Afgewezen',
  'leaveType.annual': 'Jaarlijks verlof',
  'leaveType.onDemand': 'Verlof op aanvraag',
  'leaveType.occasional': 'Bijzonder verlof',
  'leaveType.childCare': 'Kinderzorg',
  'action.payrollClosure': 'Afronden vóór salarisverwerking',
  'action.overdue': 'Beslistermijn verstreken',
  'action.none': 'Geen actie vereist',
  'common.days': 'dagen',
  'error.loadFailed': 'De verlofaanvragen konden niet worden geladen.',
}

const translations: Record<LanguageCode, Record<HrAbsenceTranslationKey, string>> = {
  en,
  pl,
  ro,
  hu,
  nl,
}

export function useHrAbsenceT() {
  const { language } = useI18n()

  return (key: HrAbsenceTranslationKey, params?: Record<string, string | number>) => {
    const template = translations[language]?.[key] ?? translations.en[key] ?? key
    if (!params) return template

    return Object.entries(params).reduce(
      (value, [paramKey, paramValue]) =>
        value.replace(new RegExp(`\\{${paramKey}\\}`, 'g'), String(paramValue)),
      template
    )
  }
}
