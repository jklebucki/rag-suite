import type {
  DocumentAuditAction,
  DocumentCategory,
  DocumentDownloadLog,
  DocumentsPageData,
  EmployeeDocument,
} from '../types/documentsTypes'

const categories: DocumentCategory[] = [
  {
    id: 'hr',
    nameKey: 'employeeDashboard.documents.category.hr',
    descriptionKey: 'employeeDashboard.documents.category.hr.description',
  },
  {
    id: 'tax',
    nameKey: 'employeeDashboard.documents.category.tax',
    descriptionKey: 'employeeDashboard.documents.category.tax.description',
  },
  {
    id: 'company',
    nameKey: 'employeeDashboard.documents.category.company',
    descriptionKey: 'employeeDashboard.documents.category.company.description',
  },
]

const documents: EmployeeDocument[] = [
  {
    id: 'doc-hr-contract-2024',
    name: 'Umowa o prace',
    categoryId: 'hr',
    addedAt: '2024-09-02',
    version: '1.0',
    status: 'available',
    description: 'Aktualna umowa o prace zawarta z pracownikiem wraz z podstawowymi warunkami zatrudnienia.',
    owner: 'Dzial HR',
    fileName: 'umowa-o-prace-2024.pdf',
    accessLevel: 'restricted',
  },
  {
    id: 'doc-hr-annex-remote',
    name: 'Aneks do umowy - praca hybrydowa',
    categoryId: 'hr',
    addedAt: '2026-03-18',
    version: '2.1',
    status: 'updated',
    description: 'Aneks regulujacy zasady wykonywania pracy w modelu hybrydowym oraz dostepnosc pracownika.',
    owner: 'Dzial HR',
    fileName: 'aneks-praca-hybrydowa.pdf',
    accessLevel: 'restricted',
  },
  {
    id: 'doc-hr-responsibilities',
    name: 'Zakres obowiazkow',
    categoryId: 'hr',
    addedAt: '2025-11-14',
    version: '1.3',
    status: 'available',
    description: 'Zakres obowiazkow dla stanowiska wraz z odpowiedzialnosciami procesowymi i raportowaniem.',
    owner: 'Dzial HR',
    fileName: 'zakres-obowiazkow.pdf',
    accessLevel: 'employee',
  },
  {
    id: 'doc-tax-pit11-2025',
    name: 'PIT-11 za 2025',
    categoryId: 'tax',
    addedAt: '2026-02-12',
    version: '1.0',
    status: 'new',
    description: 'Informacja o dochodach oraz pobranych zaliczkach na podatek dochodowy za rok 2025.',
    owner: 'Dzial Plac',
    fileName: 'pit-11-2025.pdf',
    accessLevel: 'restricted',
  },
  {
    id: 'doc-tax-pit2',
    name: 'PIT-2',
    categoryId: 'tax',
    addedAt: '2025-01-08',
    version: '1.2',
    status: 'available',
    description: 'Oswiadczenie pracownika dla celow obliczania miesiecznych zaliczek na podatek dochodowy.',
    owner: 'Dzial Plac',
    fileName: 'pit-2.pdf',
    accessLevel: 'restricted',
  },
  {
    id: 'doc-company-work-regulations',
    name: 'Regulamin pracy',
    categoryId: 'company',
    addedAt: '2026-01-15',
    version: '4.0',
    status: 'updated',
    description: 'Aktualny regulamin pracy okreslajacy organizacje pracy, prawa i obowiazki pracownikow.',
    owner: 'Biuro Zarzadu',
    fileName: 'regulamin-pracy.pdf',
    accessLevel: 'company',
  },
  {
    id: 'doc-company-gdpr',
    name: 'Polityka RODO',
    categoryId: 'company',
    addedAt: '2025-10-03',
    version: '3.2',
    status: 'available',
    description: 'Polityka ochrony danych osobowych oraz zasady przetwarzania informacji w organizacji.',
    owner: 'Inspektor Ochrony Danych',
    fileName: 'polityka-rodo.pdf',
    accessLevel: 'company',
  },
  {
    id: 'doc-company-instructions',
    name: 'Instrukcje bezpieczenstwa informacji',
    categoryId: 'company',
    addedAt: '2026-05-20',
    version: '1.1',
    status: 'new',
    description: 'Zestaw instrukcji dotyczacych bezpiecznej pracy z dokumentami, systemami i danymi firmowymi.',
    owner: 'IT Security',
    fileName: 'instrukcje-bezpieczenstwa-informacji.pdf',
    accessLevel: 'company',
  },
  {
    id: 'doc-company-board-instructions',
    name: 'Instrukcje zarzadu',
    categoryId: 'company',
    addedAt: '2024-06-11',
    version: '2.0',
    status: 'archived',
    description: 'Archiwalny pakiet instrukcji zarzadu dotyczacy obiegu dokumentow i akceptacji kosztow.',
    owner: 'Biuro Zarzadu',
    fileName: 'instrukcje-zarzadu-archiwum.pdf',
    accessLevel: 'company',
  },
]

const downloadLogs: DocumentDownloadLog[] = [
  {
    id: 'log-001',
    downloadedAt: '2026-06-27T09:18:00',
    documentName: 'PIT-11 za 2025',
    categoryId: 'tax',
    user: 'Kamil Kozlowski',
    action: 'download',
  },
  {
    id: 'log-002',
    downloadedAt: '2026-06-26T15:42:00',
    documentName: 'Regulamin pracy',
    categoryId: 'company',
    user: 'Kamil Kozlowski',
    action: 'preview',
  },
  {
    id: 'log-003',
    downloadedAt: '2026-06-21T11:04:00',
    documentName: 'Umowa o prace',
    categoryId: 'hr',
    user: 'Kamil Kozlowski',
    action: 'preview',
  },
  {
    id: 'log-004',
    downloadedAt: '2026-06-18T08:37:00',
    documentName: 'Polityka RODO',
    categoryId: 'company',
    user: 'Kamil Kozlowski',
    action: 'download',
  },
]

const documentsPageData: DocumentsPageData = {
  categories,
  documents,
  downloadLogs,
  canViewDocuments: true,
  canDownloadDocuments: true,
}

export async function getDocumentsPageData(_userId: string): Promise<DocumentsPageData> {
  await new Promise((resolve) => setTimeout(resolve, 350))
  return structuredClone(documentsPageData)
}

export async function downloadDocumentFile(
  _userId: string,
  _documentId: string
): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, 250))
}

export async function saveDocumentAuditLog(
  _userId: string,
  document: EmployeeDocument,
  category: DocumentCategory,
  action: DocumentAuditAction,
  userName: string
): Promise<DocumentDownloadLog> {
  await new Promise((resolve) => setTimeout(resolve, 120))

  return {
    id: `log-${Date.now()}`,
    downloadedAt: new Date().toISOString(),
    documentName: document.name,
    categoryId: category.id,
    user: userName,
    action,
  }
}
