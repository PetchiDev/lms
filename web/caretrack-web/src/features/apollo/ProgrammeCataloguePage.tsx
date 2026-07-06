import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import {
  BookOpen,
  Building2,
  CheckCircle2,
  ChevronDown,
  ClipboardList,
  AlertCircle,
  Layers,
  Search,
  XCircle,
} from 'lucide-react'
import { api } from '@/lib/api-client'
import { authStore } from '@/lib/auth-store'
import { getApolloNavItems } from '@/lib/apollo-nav'
import { useDashboardAnimation } from '@/animations/useDashboardAnimation'
import { DashboardShell, Panel } from '@/components/layout/DashboardShell'
import { StatCard } from '@/components/dashboard/StatCard'
import { cn } from '@/lib/utils'

interface ModuleRow {
  id: string
  title: string
  lessonCount: number
  publishedLessonCount: number
  lessonsWithAssets: number
  hasAssessment: boolean
  assessmentQuestionCount: number
  isContentReady: boolean
  isAssessmentReady: boolean
}

interface SemesterRow {
  id: string
  semesterNumber: number
  name: string
  moduleCount: number
  modulesWithContent: number
  modulesWithAssessment: number
  modules: ModuleRow[]
}

interface YearRow {
  yearNumber: number
  name: string
  semesters: SemesterRow[]
}

interface ProgrammeRow {
  id: string
  name: string
  code: string
  durationYears: number
  universities: { id: string; name: string; domain: string }[]
  years: YearRow[]
}

function StatusPill({ ok, label }: { ok: boolean; label: string }) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-[10px] font-semibold uppercase tracking-wide',
        ok ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-800',
      )}
    >
      {ok ? <CheckCircle2 className="h-3 w-3" /> : <AlertCircle className="h-3 w-3" />}
      {label}
    </span>
  )
}

function ReadinessBar({ ready, total }: { ready: number; total: number }) {
  const pct = total === 0 ? 0 : Math.round((ready / total) * 100)
  return (
    <div className="flex items-center gap-2">
      <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-slate-200">
        <div
          className="h-full rounded-full bg-gradient-to-r from-[#2081A1] to-emerald-500 transition-all duration-700"
          style={{ width: `${pct}%` }}
        />
      </div>
      <span className="text-xs font-medium text-slate-500">{ready}/{total}</span>
    </div>
  )
}

export function ProgrammeCataloguePage() {
  const animRef = useDashboardAnimation()
  const auth = authStore.get()!
  const isAdmin = auth.role === 'ApolloAdmin'
  const [search, setSearch] = useState('')
  const [expanded, setExpanded] = useState<string | null>(null)

  const { data, isLoading } = useQuery({
    queryKey: ['programme-catalogue'],
    queryFn: async () => (await api.get('/programmes/catalogue')).data,
  })

  const programmes: ProgrammeRow[] = data?.programmes ?? []

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return programmes
    return programmes.filter(
      (p) =>
        p.name.toLowerCase().includes(q) ||
        p.code.toLowerCase().includes(q) ||
        p.universities.some((u) => u.name.toLowerCase().includes(q)),
    )
  }, [programmes, search])

  return (
    <DashboardShell
      accent="apollo"
      roleLabel="Apollo Console"
      portalTitle="Programme Catalogue"
      userName={auth.fullName}
      tenantLabel="Content · assessments · university mapping"
      navItems={getApolloNavItems(isAdmin)}
    >
      <div ref={animRef} className="space-y-6">
        <div className="relative overflow-hidden rounded-2xl border border-[#2081A1]/20 bg-gradient-to-br from-[#2081A1]/10 via-white to-indigo-50/50 p-6 md:p-8">
          <div className="absolute -right-8 -top-8 h-40 w-40 rounded-full bg-[#2081A1]/10 blur-2xl" />
          <div className="relative">
            <div className="flex items-center gap-2 text-[#2081A1]">
              <Layers className="h-5 w-5" />
              <span className="text-xs font-bold uppercase tracking-widest">Master catalogue</span>
            </div>
            <h1 className="mt-2 font-display text-2xl font-bold text-slate-900 md:text-3xl">
              Programme readiness overview
            </h1>
            <p className="mt-2 max-w-2xl text-sm text-slate-600">
              See every programme, semester, course upload status, assessment setup, and which universities are mapped — all in one place.
            </p>
          </div>
        </div>

        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <StatCard title="Programmes" value={data?.totalProgrammes ?? '—'} icon={Layers} accent="blue" />
          <StatCard title="Semesters" value={data?.totalSemesters ?? '—'} icon={BookOpen} accent="teal" />
          <StatCard title="Courses ready" value={`${data?.modulesWithContent ?? 0}/${data?.totalModules ?? 0}`} change="Published + assets" icon={BookOpen} accent="violet" />
          <StatCard title="Assessments ready" value={`${data?.modulesWithAssessment ?? 0}/${data?.totalModules ?? 0}`} change="Quiz configured" icon={ClipboardList} accent="amber" />
        </div>

        <div className="relative">
          <Search className="absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
          <input
            type="search"
            placeholder="Search programme, code, or university…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="h-12 w-full rounded-2xl border border-slate-200 bg-white pl-11 pr-4 text-sm shadow-sm focus:border-[#2081A1] focus:outline-none focus:ring-2 focus:ring-[#2081A1]/20"
          />
        </div>

        {isLoading ? (
          <Panel title="Loading catalogue…"><p className="text-sm text-slate-500">Fetching programme data…</p></Panel>
        ) : filtered.length === 0 ? (
          <Panel title="No programmes"><p className="text-sm text-slate-500">No programmes match your search.</p></Panel>
        ) : (
          <div className="space-y-4">
            {filtered.map((programme) => {
              const isOpen = expanded === programme.id
              const totalModules = programme.years.flatMap((y) => y.semesters).reduce((s, sem) => s + sem.moduleCount, 0)
              const contentReady = programme.years.flatMap((y) => y.semesters).reduce((s, sem) => s + sem.modulesWithContent, 0)
              const assessReady = programme.years.flatMap((y) => y.semesters).reduce((s, sem) => s + sem.modulesWithAssessment, 0)

              return (
                <div
                  key={programme.id}
                  data-animate-card
                  className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-sm transition hover:shadow-md"
                >
                  <button
                    type="button"
                    onClick={() => setExpanded(isOpen ? null : programme.id)}
                    className="flex w-full items-start gap-4 p-5 text-left md:p-6"
                  >
                    <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-[#2081A1] to-[#1a6d89] text-sm font-bold text-white shadow">
                      {programme.code.slice(0, 3).toUpperCase()}
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="flex flex-wrap items-center gap-2">
                        <h2 className="font-display text-lg font-bold text-slate-900">{programme.name}</h2>
                        <span className="rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600">{programme.code}</span>
                        <span className="text-xs text-slate-400">{programme.durationYears} years</span>
                      </div>
                      <div className="mt-3 grid gap-2 sm:grid-cols-2">
                        <div>
                          <p className="mb-1 text-[10px] font-semibold uppercase tracking-wide text-slate-400">Course upload</p>
                          <ReadinessBar ready={contentReady} total={totalModules} />
                        </div>
                        <div>
                          <p className="mb-1 text-[10px] font-semibold uppercase tracking-wide text-slate-400">Assessments</p>
                          <ReadinessBar ready={assessReady} total={totalModules} />
                        </div>
                      </div>
                      <div className="mt-3 flex flex-wrap gap-1.5">
                        {programme.universities.length === 0 ? (
                          <span className="inline-flex items-center gap-1 rounded-full bg-red-50 px-2.5 py-0.5 text-xs text-red-600">
                            <XCircle className="h-3 w-3" /> No university mapped
                          </span>
                        ) : (
                          programme.universities.map((u) => (
                            <Link
                              key={u.id}
                              to={`/apollo/universities/${u.id}`}
                              onClick={(e) => e.stopPropagation()}
                              className="inline-flex items-center gap-1 rounded-full bg-[#2081A1]/10 px-2.5 py-0.5 text-xs font-medium text-[#2081A1] transition hover:bg-[#2081A1]/20"
                            >
                              <Building2 className="h-3 w-3" />
                              {u.name}
                            </Link>
                          ))
                        )}
                      </div>
                    </div>
                    <ChevronDown className={cn('h-5 w-5 shrink-0 text-slate-400 transition-transform', isOpen && 'rotate-180')} />
                  </button>

                  {isOpen && (
                    <div className="border-t border-slate-100 bg-slate-50/50 px-5 pb-6 pt-2 md:px-6">
                      {programme.years.map((year) => (
                        <div key={year.yearNumber} className="mt-4">
                          <p className="mb-2 text-xs font-bold uppercase tracking-widest text-slate-400">
                            {year.name}
                          </p>
                          <div className="space-y-3">
                            {year.semesters.map((semester) => (
                              <div
                                key={semester.id}
                                className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm"
                              >
                                <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
                                  <div>
                                    <p className="font-semibold text-slate-800">
                                      Semester {semester.semesterNumber} — {semester.name}
                                    </p>
                                    <p className="text-xs text-slate-500">{semester.moduleCount} module(s)</p>
                                  </div>
                                  <div className="flex flex-wrap gap-2">
                                    <StatusPill
                                      ok={semester.modulesWithContent === semester.moduleCount && semester.moduleCount > 0}
                                      label={`Courses ${semester.modulesWithContent}/${semester.moduleCount}`}
                                    />
                                    <StatusPill
                                      ok={semester.modulesWithAssessment === semester.moduleCount && semester.moduleCount > 0}
                                      label={`Quiz ${semester.modulesWithAssessment}/${semester.moduleCount}`}
                                    />
                                  </div>
                                </div>
                                {semester.modules.length === 0 ? (
                                  <p className="text-sm text-slate-400">No modules in this semester.</p>
                                ) : (
                                  <div className="overflow-x-auto">
                                    <table className="w-full min-w-[520px] text-left text-sm">
                                      <thead>
                                        <tr className="border-b border-slate-100 text-[10px] font-semibold uppercase tracking-wide text-slate-400">
                                          <th className="pb-2 pr-4">Module</th>
                                          <th className="pb-2 pr-4">Lessons</th>
                                          <th className="pb-2 pr-4">Published</th>
                                          <th className="pb-2 pr-4">Assets</th>
                                          <th className="pb-2">Assessment</th>
                                        </tr>
                                      </thead>
                                      <tbody>
                                        {semester.modules.map((mod) => (
                                          <tr key={mod.id} className="border-b border-slate-50 last:border-0">
                                            <td className="py-2.5 pr-4 font-medium text-slate-800">{mod.title}</td>
                                            <td className="py-2.5 pr-4 text-slate-600">{mod.lessonCount}</td>
                                            <td className="py-2.5 pr-4">
                                              {mod.isContentReady ? (
                                                <span className="text-emerald-600">{mod.publishedLessonCount} ✓</span>
                                              ) : (
                                                <span className="text-amber-600">{mod.publishedLessonCount}</span>
                                              )}
                                            </td>
                                            <td className="py-2.5 pr-4 text-slate-600">{mod.lessonsWithAssets}</td>
                                            <td className="py-2.5">
                                              {mod.isAssessmentReady ? (
                                                <span className="inline-flex items-center gap-1 text-emerald-600">
                                                  <CheckCircle2 className="h-3.5 w-3.5" />
                                                  {mod.assessmentQuestionCount} Q
                                                </span>
                                              ) : (
                                                <Link
                                                  to="/apollo/assessments"
                                                  className="inline-flex items-center gap-1 text-amber-600 hover:underline"
                                                >
                                                  <AlertCircle className="h-3.5 w-3.5" />
                                                  Add quiz
                                                </Link>
                                              )}
                                            </td>
                                          </tr>
                                        ))}
                                      </tbody>
                                    </table>
                                  </div>
                                )}
                              </div>
                            ))}
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )
            })}
          </div>
        )}

        <Panel title="University ↔ Programme matrix">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[480px] text-left text-sm">
              <thead>
                <tr className="border-b border-slate-200 text-[10px] font-semibold uppercase tracking-wide text-slate-400">
                  <th className="pb-3 pr-4">Programme</th>
                  <th className="pb-3">Mapped universities</th>
                </tr>
              </thead>
              <tbody>
                {programmes.map((p) => (
                  <tr key={p.id} className="border-b border-slate-50">
                    <td className="py-3 pr-4 font-medium text-slate-800">{p.name}</td>
                    <td className="py-3">
                      {p.universities.length === 0 ? (
                        <span className="text-amber-600">Not mapped — link via Universities page</span>
                      ) : (
                        <div className="flex flex-wrap gap-1.5">
                          {p.universities.map((u) => (
                            <Link
                              key={u.id}
                              to={`/apollo/universities/${u.id}`}
                              className="rounded-lg bg-slate-100 px-2 py-1 text-xs text-slate-700 hover:bg-[#2081A1]/10 hover:text-[#2081A1]"
                            >
                              {u.name}
                            </Link>
                          ))}
                        </div>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Panel>
      </div>
    </DashboardShell>
  )
}
