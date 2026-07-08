import axios from 'axios'
import { authStore } from './auth-store'
import { clearStudentCache } from './query-client'

function joinUrl(base: string, path: string) {
  const b = base.replace(/\/+$/, '')
  const p = path.replace(/^\/+/, '')
  return `${b}/${p}`
}

const apiBase = import.meta.env.VITE_API_URL
  ? joinUrl(import.meta.env.VITE_API_URL, 'api')
  : '/api'

export const api = axios.create({
  baseURL: apiBase,
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use((config) => {
  const auth = authStore.get()
  if (auth?.token) {
    config.headers.Authorization = `Bearer ${auth.token}`
  }
  return config
})

api.interceptors.response.use(
  (r) => r,
  (error) => {
    const isLoginRequest = error.config?.url?.includes('/auth/login')
    if (error.response?.status === 401 && !isLoginRequest) {
      authStore.clear()
      clearStudentCache()
      if (!window.location.hash.startsWith('#/login')) {
        window.location.replace('/#/login')
      }
    }
    return Promise.reject(error)
  }
)

export function getErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    return error.response?.data?.detail ?? error.response?.data?.title ?? error.message
  }
  return 'An unexpected error occurred'
}
