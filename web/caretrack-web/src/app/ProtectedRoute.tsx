import { Navigate, Outlet } from 'react-router-dom'
import { authStore } from '@/lib/auth-store'
import { getRoleRedirect } from '@/lib/utils'

export function ProtectedRoute({ roles }: { roles?: string[] }) {
  const auth = authStore.get()

  if (!auth || !authStore.isAuthenticated()) {
    return <Navigate to="/login" replace />
  }

  if (roles && !roles.includes(auth.role)) {
    return <Navigate to={getRoleRedirect(auth.role)} replace />
  }

  return <Outlet />
}
