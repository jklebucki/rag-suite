import type { Address } from '../../types/personalData'

export function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('pl-PL', {
    day: '2-digit',
    month: 'long',
    year: 'numeric',
  })
}

export function formatAddress(addr: Address): string {
  const apt = addr.apartmentNumber ? `/${addr.apartmentNumber}` : ''
  return `${addr.street} ${addr.buildingNumber}${apt}, ${addr.postalCode} ${addr.city}`
}

export function computeSeniority(hireDateIso: string): string {
  const hire = new Date(hireDateIso)
  const now = new Date()
  let years = now.getFullYear() - hire.getFullYear()
  let months = now.getMonth() - hire.getMonth()
  if (months < 0) {
    years -= 1
    months += 12
  }
  const yearsPart = years > 0 ? `${years} ${years === 1 ? 'rok' : years < 5 ? 'lata' : 'lat'}` : ''
  const monthsPart = months > 0 ? `${months} ${months === 1 ? 'miesiac' : months < 5 ? 'miesiace' : 'miesiecy'}` : ''
  return [yearsPart, monthsPart].filter(Boolean).join(' ') || '< 1 miesiac'
}
