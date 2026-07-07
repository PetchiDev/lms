import { Link, useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { BookOpen, CheckCircle2, ClipboardCheck, Lock, Play, Video } from 'lucide-react'
import { api } from '@/lib/api-client'
import { notify } from '@/lib/notify'
import { authStore } from '@/lib/auth-store'
import { studentQueryKey } from '@/lib/query-client'
import { STUDENT_NAV } from '@/lib/student-nav'
import { useDashboardAnimation } from '@/animations/useDashboardAnimation'
import { StudentShell } from '@/components/layout/StudentShell'
import { Button } from '@/components/ui/button'
import { ProgressBar } from '@/components/ui/label'

export function CurriculumPage() {
  const animRef = useDashboardAnimation()
  const auth = authStore.get()!
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const { data } = useQuery({
    queryKey: studentQueryKey('dashboard'),
    queryFn: async () => (await api.get('/students/me/dashboard')).data,
  })

  const markAll = useMutation({
    mutationFn: async () => (await api.post('/students/me/curriculum/complete-all')).data,
    onSuccess: (result: { allCurriculumComplete: boolean }) => {
      queryClient.invalidateQueries({ queryKey: studentQueryKey('dashboard') })
      if (result.allCurriculumComplete) {
        notify.success('Curriculum complete!')
        navigate('/dashboard/assessments')
      } else {
        notify.success('Lessons marked complete.')
      }
    },
    onError: (err) => notify.error(err),
  })

  const modules = data?.modules ?? []
  const allComplete = modules.length > 0 && modules.every((m: { progressPercent: number }) => m.progressPercent >= 100)

  return (
    <StudentShell userName={data?.studentName ?? auth.fullName} tenantLabel="Meridian × Apollo" yearLabel="Curriculum" navItems={STUDENT_NAV}>
      <div ref={animRef} className="mx-auto max-w-5xl">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <h1 className="font-display text-3xl font-bold text-[#1a1d1f]">Curriculum</h1>
            <p className="mt-2 text-slate-600">
              {data ? (
                <>Year <strong>{data.currentYear}</strong> · Semester <strong>{data.currentSemester}</strong> — complete all modules and pass assessments to unlock the next semester.</>
              ) : (
                'Your modules and learning path for this semester.'
              )}
            </p>
          </div>
          <Button
            variant="outline"
            className="border-[#2d5f5a] text-[#2d5f5a] hover:bg-[#2d5f5a]/5"
            onClick={() => markAll.mutate()}
            disabled={markAll.isPending || allComplete}
          >
            <CheckCircle2 className="mr-2 h-4 w-4" />
            {markAll.isPending ? 'Marking…' : 'Mark all as done'}
          </Button>
        </div>
        <div className="mt-8 grid gap-4 md:grid-cols-2">
          {modules.map((m: { id: string; title: string; progressPercent: number; isLocked: boolean; lockReason?: string; isCompleted: boolean }) => (
            <div key={m.id} data-animate-card className="rounded-2xl border border-[#e0e4d8] bg-white p-6 shadow-sm transition hover:shadow-md">
              <div className="flex items-start gap-3">
                {m.isLocked ? <Lock className="h-5 w-5 text-slate-300" /> : <BookOpen className="h-5 w-5 text-[#2d5f5a]" />}
                <div className="flex-1">
                  <p className="font-display text-lg font-semibold">{m.title}</p>
                  {m.isCompleted && <span className="text-xs text-emerald-600">Completed ✓</span>}
                  {!m.isLocked && (
                    <>
                      <ProgressBar value={m.progressPercent} className="mt-3" />
                      <Button size="sm" className="mt-3 bg-[#2d5f5a]" asChild>
                        <Link to={`/learn/modules/${m.id}`}>Open module</Link>
                      </Button>
                    </>
                  )}
                  {m.isLocked && <p className="mt-2 text-sm text-slate-400">{m.lockReason}</p>}
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </StudentShell>
  )
}

export function LiveClassesPage() {
  const animRef = useDashboardAnimation()
  const auth = authStore.get()!
  const { data: schedule } = useQuery({
    queryKey: studentQueryKey('schedule'),
    queryFn: async () => (await api.get('/calendar')).data,
  })

  return (
    <StudentShell userName={auth.fullName} tenantLabel="Meridian × Apollo" yearLabel="Live Classes" navItems={STUDENT_NAV}>
      <div ref={animRef} className="mx-auto max-w-3xl">
        <h1 className="font-display text-3xl font-bold text-[#1a1d1f]">Live Classes</h1>
        <p className="mt-2 text-slate-600">Join sessions 15 minutes before start time.</p>
        <div className="mt-8 space-y-4">
          {(schedule ?? []).map((item: { id?: string; title: string; startAt: string; canJoin?: boolean; joinUrl?: string }, i: number) => (
            <div key={item.id ?? i} data-animate-card className="flex items-center justify-between rounded-2xl border border-[#e0e4d8] bg-white p-5 shadow-sm">
              <div className="flex items-center gap-4">
                <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-[#2d5f5a]/10">
                  <Video className="h-5 w-5 text-[#2d5f5a]" />
                </div>
                <div>
                  <p className="font-medium">{item.title}</p>
                  <p className="text-sm text-slate-500">{new Date(item.startAt).toLocaleString()}</p>
                </div>
              </div>
              {item.canJoin && (
                <Button className="gap-1 bg-[#2d5f5a]"><Play className="h-4 w-4" /> Join</Button>
              )}
            </div>
          ))}
          {!schedule?.length && <p className="text-slate-500">No live classes scheduled this week.</p>}
        </div>
      </div>
    </StudentShell>
  )
}

export function AssessmentsPage() {
  const animRef = useDashboardAnimation()
  const auth = authStore.get()!
  const { data } = useQuery({
    queryKey: studentQueryKey('dashboard'),
    queryFn: async () => (await api.get('/students/me/dashboard')).data,
  })
  const modules = data?.modules?.filter((m: { isLocked: boolean }) => !m.isLocked) ?? []

  return (
    <StudentShell userName={data?.studentName ?? auth.fullName} tenantLabel="Meridian × Apollo" yearLabel="Assessments" navItems={STUDENT_NAV}>
      <div ref={animRef} className="mx-auto max-w-3xl">
        <h1 className="font-display text-3xl font-bold text-[#1a1d1f]">Assessments</h1>
        <p className="mt-2 text-slate-600">Quizzes unlock when you complete all lessons in a module.</p>
        <div className="mt-8 space-y-4">
          {modules.map((m: { id: string; title: string; progressPercent: number; quizPassed: boolean; bestQuizScorePercent?: number }) => (
            <div key={m.id} data-animate-card className="flex items-center justify-between rounded-2xl border border-[#e0e4d8] bg-white p-5 shadow-sm">
              <div className="flex items-center gap-4">
                <ClipboardCheck className="h-8 w-8 text-[#2d5f5a]" />
                <div>
                  <p className="font-medium">{m.title} — Quiz</p>
                  <p className="text-sm text-slate-500">
                    Your module progress: {m.progressPercent}%
                    {m.bestQuizScorePercent != null && ` · Best score: ${m.bestQuizScorePercent}%`}
                  </p>
                </div>
              </div>
              {m.quizPassed ? (
                <span className="rounded-full bg-emerald-100 px-3 py-1 text-sm font-medium text-emerald-700">Passed ✓</span>
              ) : m.progressPercent >= 100 ? (
                <Button className="bg-[#2d5f5a]" asChild>
                  <Link to={`/learn/modules/${m.id}/quiz`}>Take Quiz</Link>
                </Button>
              ) : (
                <Button className="bg-[#2d5f5a]" disabled>Locked</Button>
              )}
            </div>
          ))}
        </div>
      </div>
    </StudentShell>
  )
}
