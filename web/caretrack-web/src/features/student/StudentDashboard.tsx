import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import {
  Bell,
  BookOpen,
  Calendar,
  Clock,
  LayoutDashboard,
  Lock,
  Play,
  Stethoscope,
} from 'lucide-react'
import { api } from '@/lib/api-client'
import { authStore } from '@/lib/auth-store'
import { useDashboardAnimation } from '@/animations/useDashboardAnimation'
import { ProgressRing } from '@/components/dashboard/ProgressRing'
import { DashboardShell, Panel } from '@/components/layout/DashboardShell'
import { Button } from '@/components/ui/button'
import { ProgressBar } from '@/components/ui/label'

export function StudentDashboard() {
  const animRef = useDashboardAnimation()
  const auth = authStore.get()!

  const { data } = useQuery({
    queryKey: ['student-dashboard'],
    queryFn: async () => (await api.get('/students/me/dashboard')).data,
  })

  const { data: schedule } = useQuery({
    queryKey: ['student-schedule'],
    queryFn: async () => (await api.get('/calendar')).data,
  })

  const progress = data?.overallProgressPercent ?? 0
  const modules = data?.modules ?? []
  const continueModule = modules.find((m: { isLocked: boolean; isCompleted: boolean }) => !m.isLocked && !m.isCompleted)

  return (
    <DashboardShell
      accent="emerald"
      roleLabel="Student Portal"
      portalTitle="Today's Learning"
      userName={data?.studentName ?? auth.fullName}
      tenantLabel={data?.cohortName ?? 'Your cohort'}
      navItems={[
        { label: 'Dashboard', href: '/dashboard', icon: LayoutDashboard },
        { label: 'My Courses', href: '/dashboard', icon: BookOpen },
        { label: 'Clinical', href: '/dashboard', icon: Stethoscope },
      ]}
    >
      <div ref={animRef} className="space-y-8">
        <div className="grid gap-6 lg:grid-cols-3">
          <Panel title="Overall progress" className="flex flex-col items-center justify-center">
            <ProgressRing value={progress} sublabel="Year 1 · Semester 1" />
          </Panel>

          <Panel title="Today's schedule" className="lg:col-span-2">
            <div className="space-y-3">
              {(schedule?.length ? schedule : [
                { time: '09:00', title: 'Live class — Cardiovascular intro', type: 'live', active: true },
              ]).map((item: { startAt?: string; title: string; eventType?: string; canJoin?: boolean; time?: string; type?: string; active?: boolean }) => {
                const time = item.startAt ? new Date(item.startAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : item.time
                const isLive = item.eventType === 'LiveClass' || item.type === 'live'
                const canJoin = item.canJoin ?? item.active
                return (
                <div
                  key={item.title}
                  data-animate-card
                  className="flex items-center gap-4 rounded-xl border border-slate-100 bg-slate-50/80 p-4 transition hover:border-emerald-200"
                >
                  <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-white shadow-sm">
                    {isLive ? <Play className="h-5 w-5 text-emerald-600" /> : <Calendar className="h-5 w-5 text-slate-500" />}
                  </div>
                  <div className="flex-1">
                    <p className="text-xs font-medium text-emerald-600">{time}</p>
                    <p className="font-medium text-slate-900">{item.title}</p>
                  </div>
                  {canJoin && (
                    <Button size="sm">Join</Button>
                  )}
                </div>
              )})}
            </div>
          </Panel>
        </div>

        <div className="grid gap-6 lg:grid-cols-2">
          <Panel
            title="Continue learning"
            action={
              continueModule && (
                <Button size="sm" asChild>
                  <Link to={`/learn/modules/${continueModule.id}`}>Resume</Link>
                </Button>
              )
            }
          >
            {continueModule ? (
              <div className="rounded-xl bg-gradient-to-r from-emerald-500/10 to-teal-500/10 p-5">
                <div className="flex items-center gap-3">
                  <div className="rounded-full bg-emerald-600 p-3 text-white">
                    <Play className="h-5 w-5" />
                  </div>
                  <div>
                    <p className="font-semibold text-slate-900">{continueModule.title}</p>
                    <p className="text-sm text-slate-500">{continueModule.progressPercent}% complete · pick up where you left off</p>
                  </div>
                </div>
                <ProgressBar value={continueModule.progressPercent} className="mt-4" />
              </div>
            ) : (
              <p className="text-sm text-slate-500">All caught up — great work!</p>
            )}
          </Panel>

          <Panel title="Pending actions">
            <ul className="space-y-3">
              <li className="flex items-center gap-3 text-sm">
                <Bell className="h-4 w-4 text-amber-500" />
                <span>Complete Module 1 quiz before semester end</span>
              </li>
              <li className="flex items-center gap-3 text-sm">
                <Clock className="h-4 w-4 text-slate-400" />
                <span>Clinical rotations unlock in Year 2</span>
              </li>
            </ul>
          </Panel>
        </div>

        <Panel title="My modules">
          <div className="grid gap-4 md:grid-cols-2">
            {modules.map((m: { id: string; title: string; progressPercent: number; isLocked: boolean; lockReason?: string; isCompleted: boolean }) => (
              <div
                key={m.id}
                data-animate-card
                className={`rounded-xl border p-5 transition ${m.isLocked ? 'border-dashed bg-slate-50 opacity-70' : 'border-slate-200 bg-white hover:shadow-md'}`}
              >
                <div className="mb-3 flex items-start justify-between">
                  <div className="flex items-center gap-2">
                    {m.isLocked ? <Lock className="h-4 w-4 text-slate-400" /> : <BookOpen className="h-4 w-4 text-emerald-600" />}
                    <p className="font-semibold text-slate-900">{m.title}</p>
                  </div>
                  {m.isCompleted && <span className="rounded-full bg-emerald-100 px-2 py-0.5 text-xs text-emerald-700">Done</span>}
                </div>
                {m.isLocked ? (
                  <p className="text-sm text-slate-500">{m.lockReason ?? 'Locked'}</p>
                ) : (
                  <>
                    <ProgressBar value={m.progressPercent} className="mb-3" />
                    <Button size="sm" variant="outline" asChild>
                      <Link to={`/learn/modules/${m.id}`}>Open module</Link>
                    </Button>
                  </>
                )}
              </div>
            ))}
          </div>
        </Panel>

        {data?.notices?.length > 0 && (
          <Panel title="Notices">
            <ul className="space-y-2">
              {data.notices.map((n: string, i: number) => (
                <li key={i} className="flex items-start gap-2 text-sm text-slate-600">
                  <Bell className="mt-0.5 h-4 w-4 shrink-0 text-emerald-500" />
                  {n}
                </li>
              ))}
            </ul>
          </Panel>
        )}
      </div>
    </DashboardShell>
  )
}
