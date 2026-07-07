export interface AuthUser {
  token: string
  email: string
  fullName: string
  role: string
  universityId?: string
  universityLogoUrl?: string
  cohortId?: string
  expiresAt: string
}

const STORAGE_KEY = 'caretrack_auth'

export const authStore = {
  get(): AuthUser | null {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    try {
      return JSON.parse(raw) as AuthUser
    } catch {
      return null
    }
  },
  set(user: AuthUser) {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(user))
  },
  clear() {
    localStorage.removeItem(STORAGE_KEY)
  },
  isAuthenticated() {
    const user = this.get()
    if (!user) return false
    return new Date(user.expiresAt) > new Date()
  },
}
