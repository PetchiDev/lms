import { Outlet, useLocation } from 'react-router-dom'
import { authStore } from '@/lib/auth-store'
import { UNIVERSITY_NAV } from '@/lib/university-nav'
import { getUniversityPageMeta } from '@/lib/university-page-meta'
import { UniversityShell } from '@/components/layout/UniversityShell'

export function UniversityAdminLayout() {
  const { pathname } = useLocation()
  const { title, subtitle } = getUniversityPageMeta(pathname)
  const auth = authStore.get()!

  return (
    <UniversityShell
      portalTitle={title}
      portalSubtitle={subtitle}
      userName={auth.fullName}
      tenantLabel="Your college · Tenant-scoped access"
      navItems={UNIVERSITY_NAV}
    >
      <Outlet />
    </UniversityShell>
  )
}
