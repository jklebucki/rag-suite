import { describe, it, expect } from 'vitest'
import {
  ADDRESS_BOOK_CSV_HEADERS,
  buildAddressBookCsv,
  encodeCsvContent,
  getExportFileName,
} from './csvExport'
import type { ContactListItem } from '@/features/address-book/types/addressbook'

const contact = (overrides: Partial<ContactListItem> = {}): ContactListItem => ({
  id: '1',
  firstName: 'Jan',
  lastName: 'Kowalski',
  displayName: 'Jan Kowalski - ACME',
  department: 'IT',
  position: 'Developer',
  location: 'Warszawa',
  workPhone: '+48123456789',
  mobilePhone: '+48987654321',
  email: 'jan.kowalski@example.com',
  isActive: true,
  ...overrides,
})

describe('buildAddressBookCsv', () => {
  it('uses the same 9-column header as the import', () => {
    expect([...ADDRESS_BOOK_CSV_HEADERS]).toEqual([
      'Imię',
      'Nazwisko',
      'Dział',
      'Telefon służbowy',
      'Telefon komórkowy',
      'Adres e-mail',
      'Nazwa wyświetlana',
      'Stanowisko',
      'Lokalizacja',
    ])
  })

  it('maps contact fields in import column order with quoting and CRLF', () => {
    const csv = buildAddressBookCsv([contact()])
    const lines = csv.split('\r\n')
    expect(lines).toHaveLength(2)
    expect(lines[1]).toBe(
      '"Jan";"Kowalski";"IT";"+48123456789";"+48987654321";"jan.kowalski@example.com";"Jan Kowalski - ACME";"Developer";"Warszawa"',
    )
  })

  it('renders empty values as empty quotes and escapes embedded quotes', () => {
    const csv = buildAddressBookCsv([
      contact({ department: null, workPhone: null, displayName: 'Jan "Jasiu" Kowalski' }),
    ])
    const line = csv.split('\r\n')[1]
    expect(line).toBe(
      '"Jan";"Kowalski";"";"";"+48987654321";"jan.kowalski@example.com";"Jan ""Jasiu"" Kowalski";"Developer";"Warszawa"',
    )
  })

  it('returns only the header when there are no contacts', () => {
    const csv = buildAddressBookCsv([])
    expect(csv).not.toContain('\r\n')
    expect(csv.split(';')).toHaveLength(9)
  })
})

describe('encodeCsvContent', () => {
  const text = 'Zażółć gęślą jaźń;Łódź;ąęść'

  it('encodes UTF-8 with BOM', () => {
    const bytes = encodeCsvContent('Aą', 'UTF-8')
    expect(bytes[0]).toBe(0xef)
    expect(bytes[1]).toBe(0xbb)
    expect(bytes[2]).toBe(0xbf)
    expect(new TextDecoder('utf-8').decode(bytes)).toBe('Aą')
  })

  it.each([
    ['windows-1250', 'windows-1250'],
    ['ISO-8859-2', 'iso-8859-2'],
    ['windows-1252', 'windows-1252'],
  ] as const)('round-trips Polish text through %s identically to TextDecoder', (encoding, label) => {
    // windows-1252 cannot hold Polish ł/ż/ź/ć — use a sample it can
    // represent (missing characters are covered by the '?' test below).
    const sample = encoding === 'windows-1252' ? 'Élise àü;óÓ' : text
    const bytes = encodeCsvContent(sample, encoding)
    expect(new TextDecoder(label).decode(bytes)).toBe(sample)
  })

  it('encodes windows-1250 bytes for Polish diacritics', () => {
    // ł -> 0xB3, ą -> 0xB9, ę -> 0xEA, Ś -> 0x8C, ż -> 0xBF, € -> 0x80
    const bytes = encodeCsvContent('łąęŚż€', 'windows-1250')
    expect([...bytes]).toEqual([0xb3, 0xb9, 0xea, 0x8c, 0xbf, 0x80])
  })

  it('encodes ISO-8859-2 bytes for Polish diacritics', () => {
    // ą -> 0xB1, ł -> 0xB3 in Latin-2
    const bytes = encodeCsvContent('ął', 'ISO-8859-2')
    expect([...bytes]).toEqual([0xb1, 0xb3])
  })

  it('replaces characters missing from the target encoding with ?', () => {
    // ł (U+0142) does not exist in windows-1252
    const bytes = encodeCsvContent('ł', 'windows-1252')
    expect([...bytes]).toEqual([0x3f])
    // € (U+20AC) does not exist in ISO-8859-2
    expect([...encodeCsvContent('€', 'ISO-8859-2')]).toEqual([0x3f])
  })

  it('keeps ASCII untouched in single-byte encodings', () => {
    const bytes = encodeCsvContent('ABC;";\r\n', 'windows-1250')
    expect([...bytes]).toEqual([0x41, 0x42, 0x43, 0x3b, 0x22, 0x3b, 0x0d, 0x0a])
  })
})

describe('getExportFileName', () => {
  it('includes the date and csv extension', () => {
    expect(getExportFileName(new Date(2026, 8, 9))).toBe('address-book-export-20260909.csv')
  })
})
