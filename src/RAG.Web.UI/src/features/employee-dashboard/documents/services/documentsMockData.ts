import type { Pit11Document, Pit11DownloadFile } from '../types/documentsTypes'

const documents: Pit11Document[] = [
  {
    id: 'pit11-2025',
    name: 'PIT-11 za 2025',
    taxYear: 2025,
    generatedAt: '2026-02-15',
    fileName: 'pit-11-2025.pdf',
  },
  {
    id: 'pit11-2024',
    name: 'PIT-11 za 2024',
    taxYear: 2024,
    generatedAt: '2025-02-14',
    fileName: 'pit-11-2024.pdf',
  },
  {
    id: 'pit11-2023',
    name: 'PIT-11 za 2023',
    taxYear: 2023,
    generatedAt: '2024-02-15',
    fileName: 'pit-11-2023.pdf',
  },
]

function createMockPdf(document: Pit11Document): Blob {
  const content = `BT /F1 18 Tf 72 720 Td (${document.name}) Tj 0 -30 Td /F1 11 Tf (Rok podatkowy: ${document.taxYear}) Tj 0 -20 Td (Dokument demonstracyjny.) Tj ET`
  const objects = [
    '<< /Type /Catalog /Pages 2 0 R >>',
    '<< /Type /Pages /Kids [3 0 R] /Count 1 >>',
    '<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>',
    `<< /Length ${content.length} >>\nstream\n${content}\nendstream`,
    '<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>',
  ]

  let pdf = '%PDF-1.4\n'
  const offsets = [0]

  objects.forEach((object, index) => {
    offsets.push(pdf.length)
    pdf += `${index + 1} 0 obj\n${object}\nendobj\n`
  })

  const xrefOffset = pdf.length
  pdf += `xref\n0 ${objects.length + 1}\n`
  pdf += '0000000000 65535 f \n'
  offsets.slice(1).forEach((offset) => {
    pdf += `${String(offset).padStart(10, '0')} 00000 n \n`
  })
  pdf += `trailer\n<< /Size ${objects.length + 1} /Root 1 0 R >>\nstartxref\n${xrefOffset}\n%%EOF`

  return new Blob([pdf], { type: 'application/pdf' })
}

export async function getPit11Documents(_userId: string): Promise<Pit11Document[]> {
  await new Promise((resolve) => setTimeout(resolve, 350))
  return structuredClone(documents).sort((a, b) => b.taxYear - a.taxYear)
}

export async function downloadPit11File(
  _userId: string,
  documentId: string
): Promise<Pit11DownloadFile> {
  await new Promise((resolve) => setTimeout(resolve, 250))

  const document = documents.find((item) => item.id === documentId)
  if (!document) {
    throw new Error(`PIT-11 document not found: ${documentId}`)
  }

  return {
    blob: createMockPdf(document),
    fileName: document.fileName,
  }
}
