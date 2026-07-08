import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

/** Works on HTTP (non-secure contexts) where crypto.randomUUID is unavailable. */
export function createId(): string {
  const c = globalThis.crypto as Crypto | undefined
  if (c && typeof c.randomUUID === 'function') {
    return c.randomUUID()
  }
  if (c && typeof c.getRandomValues === 'function') {
    const bytes = new Uint8Array(16)
    c.getRandomValues(bytes)
    bytes[6] = (bytes[6] & 0x0f) | 0x40
    bytes[8] = (bytes[8] & 0x3f) | 0x80
    const hex = Array.from(bytes, (b) => b.toString(16).padStart(2, '0')).join('')
    return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`
  }
  return `id-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 11)}`
}

export function getRoleRedirect(role: string): string {
  switch (role) {
    case 'ApolloAdmin':
    case 'ApolloFaculty':
      return '/console'
    case 'UniversityAdmin':
      return '/admin'
    case 'Student':
      return '/dashboard'
    case 'Supervisor':
      return '/signoffs'
    default:
      return '/login'
  }
}

const domainIdpMap: Record<string, { name: string; type: 'sso' | 'entra' }> = {
  'meridian.edu': { name: 'Meridian University SSO', type: 'sso' },
  'apollohospitals.com': { name: 'Apollo Entra ID', type: 'entra' },
}

export function resolveIdpFromEmail(email: string) {
  const domain = email.split('@')[1]?.toLowerCase()
  if (!domain) return null
  return domainIdpMap[domain] ?? null
}
