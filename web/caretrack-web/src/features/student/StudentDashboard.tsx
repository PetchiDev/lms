import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { ArrowRight, Bell, BookOpen, Calendar, Clock, Play, Sparkles } from 'lucide-react'
import { api } from '@/lib/api-client'
import { authStore } from '@/lib/auth-store'
import { STUDENT_NAV } from '@/lib/student-nav'
import { cn } from '@/lib/utils'
import { useDashboardAnimation } from '@/animations/useDashboardAnimation'
import { StudentShell } from '@/components/layout/StudentShell'
import { Button } from '@/components/ui/button'
import { ProgressBar } from '@/components/ui/label'

function getGreeting() {
  const h = new Date().getHours()
  if (h < 12) return 'Good morning'
  if (h < 17) return 'Good afternoon'
  return 'Good evening'
}

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

  const modules = data?.modules ?? []
  const continueModule = modules.find((m: { isLocked: boolean; isCompleted: boolean }) => !m.isLocked && !m.isCompleted)
  const firstName = (data?.studentName ?? auth.fullName).split(' ')[0]
  const resumeHref = continueModule ? `/learn/modules/${continueModule.id}` : undefined

  const scheduleItems = schedule?.length
    ? schedule
    : [
        { startAt: new Date().setHours(9, 0), endAt: new Date().setHours(10, 0), title: 'Live class — Cardiovascular intro', eventType: 'LiveClass', canJoin: true },
        { startAt: new Date().setHours(14, 0), title: 'Self-paced — Lesson 3', eventType: 'Deadline' },
        { startAt: new Date().setHours(17, 0), title: 'Quiz deadline — Module 1', eventType: 'Exam' },
      ]

  return (
    <StudentShell
      userName={data?.studentName ?? auth.fullName}
      tenantLabel={data?.cohortName ? `${data.cohortName} · Apollo` : 'Meridian × Apollo'}
      yearLabel={data ? `Year ${data.currentYear} · Semester ${data.currentSemester}` : 'Dashboard'}
      navItems={STUDENT_NAV}
      resumeHref={resumeHref}
    >
      <div ref={animRef} className="mx-auto max-w-6xl space-y-8">
        {/* Greeting */}
        <div data-animate-card>
          <h1 className="font-display text-3xl font-bold tracking-tight text-[#1a1d1f] lg:text-4xl">
            {getGreeting()}, {firstName}
          </h1>
          <p className="mt-2 max-w-xl text-slate-600">
            You have <span className="font-semibold text-[#2d5f5a]">{scheduleItems.length} sessions</span> today
            {continueModule && <> and <span className="font-semibold text-[#2d5f5a]">{continueModule.title}</span> is waiting for you</>}.
          </p>
        </div>

        {/* Hero overview card */}
        <div
          data-animate-card
          className="overflow-hidden rounded-2xl border border-[#e0e4d8] bg-white shadow-[0_4px_24px_-8px_rgba(26,29,31,0.08)]"
        >
          <div className="grid lg:grid-cols-5">
            <div className="p-6 lg:col-span-3 lg:p-8">
              <span className="inline-flex items-center gap-1.5 rounded-full bg-[#c8e6d9]/60 px-3 py-1 text-xs font-semibold text-[#2d5f5a]">
                <Sparkles className="h-3 w-3" /> Year 1, Semester 1
              </span>
              <h2 className="font-display mt-4 text-2xl font-bold text-[#1a1d1f] lg:text-3xl">
                {continueModule?.title ?? 'Clinical Foundations'}
              </h2>
              <p className="mt-3 text-sm leading-relaxed text-slate-600">
                Build core competencies in cardiovascular assessment, patient communication, and clinical reasoning
                before your hospital rotations begin.
              </p>
              <Button className="mt-6 gap-2 bg-[#2d5f5a] hover:bg-[#234a46]" asChild>
                <Link to={resumeHref ?? '/dashboard/curriculum'}>
                  {continueModule ? 'Resume learning' : 'View curriculum'} <ArrowRight className="h-4 w-4" />
                </Link>
              </Button>
            </div>
            <div className="flex flex-col justify-center border-t border-[#e8ebe0] bg-[#fafbf7] p-6 lg:col-span-2 lg:border-l lg:border-t-0">
              <p className="text-xs font-semibold uppercase tracking-widest text-slate-400">Your progress</p>
              <p className="font-display mt-2 text-5xl font-bold text-[#2d5f5a]">{data?.overallProgressPercent ?? 0}%</p>
              <ProgressBar value={data?.overallProgressPercent ?? 0} className="mt-4 h-2" />
              <div className="mt-4 flex gap-4 text-xs text-slate-500">
                <span>Orientation ✓</span>
                <span className="font-semibold text-[#2d5f5a]">Current</span>
                <span>Mid-term</span>
                <span>Exam</span>
              </div>
            </div>
          </div>
        </div>

        {/* Three columns */}
        <div className="grid gap-6 lg:grid-cols-3">
          {/* Schedule */}
          <section data-animate-card className="rounded-2xl border border-[#e0e4d8] bg-white p-6 shadow-sm">
            <div className="mb-4 flex items-center gap-2">
              <Calendar className="h-4 w-4 text-[#2d5f5a]" />
              <h3 className="font-semibold text-slate-900">Today&apos;s Schedule</h3>
            </div>
            <div className="space-y-3">
              {scheduleItems.slice(0, 4).map((item: { startAt?: string | number; title: string; eventType?: string; canJoin?: boolean }, i: number) => {
                const start = item.startAt ? new Date(item.startAt) : null
                const timeStr = start
                  ? `${start.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`
                  : '—'
                const isLive = item.eventType === 'LiveClass' && (item.canJoin || i === 0)
                return (
                  <div
                    key={item.title + i}
                    className={cn(
                      'rounded-xl border p-3.5 transition',
                      isLive ? 'border-[#2d5f5a]/30 bg-[#2d5f5a]/5' : 'border-slate-100 bg-[#fafbf7]'
                    )}
                  >
                    <div className="flex items-start justify-between gap-2">
                      <div>
                        {isLive && (
                          <span className="mb-1 inline-block rounded bg-red-500 px-1.5 py-0.5 text-[9px] font-bold uppercase tracking-wider text-white">
                            Live now
                          </span>
                        )}
                        <p className="text-xs text-slate-500">{timeStr}</p>
                        <p className="mt-0.5 text-sm font-medium text-slate-900">{item.title}</p>
                      </div>
                    </div>
                    {isLive && (
                      <Button size="sm" className="mt-2 w-full bg-[#2d5f5a] hover:bg-[#234a46]">
                        <Play className="mr-1 h-3 w-3" /> Join Live Class
                      </Button>
                    )}
                  </div>
                )
              })}
            </div>
          </section>

          {/* Continue learning */}
          <section data-animate-card className="rounded-2xl border border-[#e0e4d8] bg-white p-6 shadow-sm">
            <div className="mb-4 flex items-center gap-2">
              <BookOpen className="h-4 w-4 text-[#2d5f5a]" />
              <h3 className="font-semibold text-slate-900">Continue Learning</h3>
            </div>
            {continueModule ? (
              <>
                <div className="relative mb-4 overflow-hidden rounded-xl bg-gradient-to-br from-[#2d5f5a]/20 to-[#3d7a70]/10 aspect-video flex items-center justify-center">
                  <div className="text-center">
                    <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-white/90 shadow-lg">
                      <Play className="h-6 w-6 text-[#2d5f5a] ml-0.5" />
                    </div>
                    <p className="mt-2 text-xs font-medium text-[#2d5f5a]">Module in progress</p>
                  </div>
                </div>
                <p className="font-display text-lg font-semibold text-slate-900">{continueModule.title}</p>
                <ProgressBar value={continueModule.progressPercent} className="mt-3" />
                <p className="mt-1 text-xs text-slate-500">{continueModule.progressPercent}% complete</p>
                <Button variant="outline" className="mt-4 w-full border-[#2d5f5a] text-[#2d5f5a] hover:bg-[#2d5f5a]/5" asChild>
                  <Link to={`/learn/modules/${continueModule.id}`}>Resume lesson →</Link>
                </Button>
              </>
            ) : (
              <p className="text-sm text-slate-500">All caught up — brilliant work!</p>
            )}
          </section>

          {/* Pending actions */}
          <section data-animate-card className="rounded-2xl border border-[#e0e4d8] bg-white p-6 shadow-sm">
            <div className="mb-4 flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Bell className="h-4 w-4 text-amber-500" />
                <h3 className="font-semibold text-slate-900">Pending Actions</h3>
              </div>
              <span className="flex h-5 w-5 items-center justify-center rounded-full bg-red-500 text-[10px] font-bold text-white">2</span>
            </div>
            <ul className="space-y-2.5">
              <li className="rounded-xl border border-amber-100 bg-amber-50/80 px-3 py-2.5 text-sm text-amber-900">
                <Clock className="mb-1 inline h-3.5 w-3.5" /> Complete Module 1 quiz before semester end
              </li>
              <li className="rounded-xl border border-slate-100 bg-slate-50 px-3 py-2.5 text-sm text-slate-700">
                Clinical rotations unlock in Year 2
              </li>
              {data?.notices?.map((n: string, i: number) => (
                <li key={i} className="rounded-xl border border-emerald-100 bg-emerald-50/80 px-3 py-2.5 text-sm text-emerald-900">
                  {n}
                </li>
              ))}
            </ul>
            <Link to="/dashboard/assessments" className="mt-4 inline-block text-xs font-medium text-[#2d5f5a] hover:underline">
              View all tasks →
            </Link>
          </section>
        </div>
      </div>
    </StudentShell>
  )
}
