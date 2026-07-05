import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
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
