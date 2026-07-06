import { QueryClient } from '@tanstack/react-query'
import { authStore } from '@/lib/auth-store'

export const queryClient = new QueryClient({
  defaultOptions: { queries: { staleTime: 30_000, retry: 1 } },
})

export function studentQueryKey(...parts: unknown[]) {
  return ['student', authStore.get()?.email ?? 'anonymous', ...parts] as const
}

export function clearStudentCache() {
  queryClient.clear()
}
