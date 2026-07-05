import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import {
  BarChart3,
  BookOpen,
  Building2,
  FileStack,
  Globe,
  LayoutDashboard,
  Users,
} from 'lucide-react'
import { api } from '@/lib/api-client'
import { authStore } from '@/lib/auth-store'
import { useDashboardAnimation } from '@/animations/useDashboardAnimation'
import { StatCard } from '@/components/dashboard/StatCard'
import { Sparkline } from '@/components/dashboard/Sparkline'
import { DashboardShell, Panel } from '@/components/layout/DashboardShell'
import { Button } from '@/components/ui/button'

export function ApolloDashboard() {
  const animRef = useDashboardAnimation()
  const auth = authStore.get()!

  const universities = useQuery({
    queryKey: ['universities'],
    queryFn: async () => (await api.get('/universities')).data,
  })

  const programmes = useQuery({
    queryKey: ['programmes'],
    queryFn: async () => (await api.get('/programmes')).data,
  })

  const uniCount = universities.data?.items?.length ?? 0
  const programmeCount = programmes.data?.length ?? 0

  return (
    <DashboardShell
      accent="indigo"
      roleLabel="Apollo Console"
      portalTitle="Platform Overview"
      userName={auth.fullName}
      tenantLabel="Cross-tenant · Content Owner"
      navItems={[
        { label: 'Overview', href: '/console', icon: LayoutDashboard },
        { label: 'Content Library', href: '/apollo/content', icon: BookOpen },
        { label: 'Universities', href: '/console', icon: Building2 },
        { label: 'Analytics', href: '/apollo/reports', icon: BarChart3 },
      ]}
    >
      <div ref={animRef} className="space-y-8">
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <StatCard title="Universities" value={uniCount} change="Active tenants" icon={Globe} accent="violet" />
          <StatCard title="Programmes" value={programmeCount} change="B.Sc Allied Health +" icon={FileStack} accent="blue" />
          <StatCard title="Published Modules" value="—" change="Content pipeline" icon={BookOpen} accent="teal" trend="up" />
          <StatCard title="Learners (all)" value="—" change="Across tenants" icon={Users} accent="amber" />
        </div>

        <div className="grid gap-6 xl:grid-cols-3">
          <Panel title="Engagement trend" className="xl:col-span-2">
            <div className="flex items-end justify-between gap-6">
              <div>
                <p className="text-3xl font-bold text-slate-900">Content performance</p>
                <p className="text-sm text-slate-500">Completion rates across published modules</p>
              </div>
              <Sparkline data={[42, 48, 45, 58, 62, 59, 71, 68, 74]} className="h-16 w-32" />
            </div>
            <div className="mt-6 grid gap-3 sm:grid-cols-3">
              {['Cardiovascular', 'Respiratory', 'Clinical Skills'].map((m, i) => (
                <div key={m} className="rounded-xl bg-slate-50 p-4">
                  <p className="text-xs text-slate-500">Module</p>
                  <p className="font-semibold text-slate-800">{m}</p>
                  <div className="mt-2 h-2 overflow-hidden rounded-full bg-slate-200">
                    <div className="h-full rounded-full bg-indigo-500" style={{ width: `${55 + i * 12}%` }} />
                  </div>
                </div>
              ))}
            </div>
          </Panel>

          <Panel title="Quick actions">
            <div className="space-y-3">
              <Button className="w-full justify-start" asChild>
                <Link to="/apollo/content"><BookOpen className="mr-2 h-4 w-4" />Upload & publish content</Link>
              </Button>
              <Button variant="outline" className="w-full justify-start" asChild>
                <Link to="/apollo/reports"><BarChart3 className="mr-2 h-4 w-4" />View analytics</Link>
              </Button>
            </div>
            <p className="mt-4 text-xs leading-relaxed text-slate-500">
              Publish once → all or selected universities consume. Version audit trail on every release.
            </p>
          </Panel>
        </div>

        <Panel title="Partner universities">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {universities.data?.items?.map((u: { id: string; name: string; domain: string; isActive: boolean }) => (
              <div
                key={u.id}
                data-animate-card
                className="group rounded-xl border border-slate-100 bg-gradient-to-br from-white to-slate-50 p-5 transition hover:border-indigo-200 hover:shadow-md"
              >
                <div className="flex items-start gap-3">
                  <div className="rounded-lg bg-indigo-100 p-2 text-indigo-700">
                    <Building2 className="h-5 w-5" />
                  </div>
                  <div>
                    <p className="font-semibold text-slate-900">{u.name}</p>
                    <p className="text-sm text-slate-500">{u.domain}</p>
                    <span className={`mt-2 inline-block rounded-full px-2 py-0.5 text-xs ${u.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100'}`}>
                      {u.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </Panel>
      </div>
    </DashboardShell>
  )
}
