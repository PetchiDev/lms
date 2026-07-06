import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import {
  AlertTriangle,
  BarChart3,
  BookOpen,
  Building2,
  Download,
  Globe,
  LayoutDashboard,
  TrendingUp,
  Users,
} from 'lucide-react'
import { api } from '@/lib/api-client'
import { getApolloNavItems } from '@/lib/apollo-nav'
import { authStore } from '@/lib/auth-store'
import { useDashboardAnimation } from '@/animations/useDashboardAnimation'
import { BarChart, DonutChart, MiniBarChart } from '@/components/dashboard/Charts'
import { Sparkline } from '@/components/dashboard/Sparkline'
import { StatCard } from '@/components/dashboard/StatCard'
import { DashboardShell, Panel } from '@/components/layout/DashboardShell'
import { UniPanel } from '@/components/layout/UniversityShell'
import { Button } from '@/components/ui/button'
import { ProgressBar } from '@/components/ui/label'

const COLORS = ['#2081A1', '#2d5f5a', '#6366f1', '#f59e0b', '#ec4899']

export function UniversityReportsPage() {
  const { data } = useQuery({
    queryKey: ['university-report'],
    queryFn: async () => (await api.get('/reports/university/students')).data,
  })

  async function exportReport(format: string) {
    const res = await api.get(`/reports/university/export?format=${format}`, { responseType: 'blob' })
    const url = window.URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url
    a.download = format === 'pdf' ? 'report.pdf' : 'report.xlsx'
    a.click()
  }

  const students = data?.students ?? []
  const progressBars = students.slice(0, 8).map((s: { fullName: string; progressPercent: number }) => ({
    label: s.fullName.split(' ')[0],
    value: s.progressPercent,
  }))

  return (
    <div className="space-y-8">
        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={() => exportReport('excel')}><Download className="mr-2 h-4 w-4" />Export Excel</Button>
          <Button variant="outline" asChild><Link to="/admin">Back to dashboard</Link></Button>
        </div>
        <div className="grid gap-4 sm:grid-cols-3">
          <StatCard title="Total students" value={data?.totalStudents ?? 0} icon={Users} accent="teal" />
          <StatCard title="Avg progress" value={`${Math.round(data?.averageProgress ?? 0)}%`} icon={TrendingUp} accent="blue" trend="up" />
          <StatCard title="At risk" value={data?.atRiskCount ?? 0} icon={AlertTriangle} accent="amber" />
        </div>
        <UniPanel title="Student progress">
          {progressBars.length > 0 ? <BarChart data={progressBars} /> : <p className="text-sm text-slate-500">No students enrolled.</p>}
        </UniPanel>
        <UniPanel title="Student roster">
          <div className="overflow-hidden rounded-xl border">
            <table className="w-full text-sm">
              <thead className="bg-slate-50 text-left text-xs font-semibold uppercase text-slate-500">
                <tr>
                  <th className="p-3">Student</th>
                  <th className="p-3">Cohort</th>
                  <th className="p-3">Progress</th>
                  <th className="p-3">Status</th>
                </tr>
              </thead>
              <tbody>
                {students.map((s: { studentId: string; fullName: string; cohortName: string; progressPercent: number; isAtRisk: boolean }) => (
                  <tr key={s.studentId} className="border-t hover:bg-slate-50/50">
                    <td className="p-3 font-medium">{s.fullName}</td>
                    <td className="p-3 text-slate-500">{s.cohortName}</td>
                    <td className="p-3"><ProgressBar value={s.progressPercent} className="max-w-[140px]" /></td>
                    <td className="p-3">
                      {s.isAtRisk ? (
                        <span className="inline-flex items-center gap-1 rounded-full bg-amber-100 px-2 py-0.5 text-xs text-amber-700">
                          <AlertTriangle className="h-3 w-3" /> At risk
                        </span>
                      ) : (
                        <span className="rounded-full bg-emerald-100 px-2 py-0.5 text-xs text-emerald-700">On track</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </UniPanel>
    </div>
  )
}

export function ApolloReportsPage() {
  const animRef = useDashboardAnimation()
  const auth = authStore.get()!

  const universities = useQuery({
    queryKey: ['apollo-universities'],
    queryFn: async () => (await api.get('/reports/apollo/universities')).data,
  })

  const content = useQuery({
    queryKey: ['content-performance'],
    queryFn: async () => (await api.get('/reports/apollo/content-performance')).data,
  })

  const uniList = universities.data ?? []
  const contentList = content.data ?? []

  const totalStudents = uniList.reduce((s: number, u: { totalStudents: number }) => s + u.totalStudents, 0)
  const totalAtRisk = uniList.reduce((s: number, u: { atRiskCount: number }) => s + u.atRiskCount, 0)
  const avgProgress = uniList.length
    ? uniList.reduce((s: number, u: { averageProgress: number }) => s + u.averageProgress, 0) / uniList.length
    : 0

  const contentBars = contentList.slice(0, 6).map((c: { moduleTitle: string; completionRate: number }, i: number) => ({
    label: c.moduleTitle.length > 28 ? c.moduleTitle.slice(0, 28) + '…' : c.moduleTitle,
    value: c.completionRate,
    color: COLORS[i % COLORS.length],
  }))

  const uniBars = uniList.map((u: { universityName: string; averageProgress: number }, i: number) => ({
    label: u.universityName,
    value: u.averageProgress,
    color: COLORS[i % COLORS.length],
  }))

  const donutSegments = uniList.map((u: { universityName: string; totalStudents: number }, i: number) => ({
    label: u.universityName,
    value: u.totalStudents,
    color: COLORS[i % COLORS.length],
  }))

  const trendData = contentList.length
    ? contentList.slice(0, 8).map((c: { completionRate: number }) => c.completionRate)
    : [42, 48, 55, 58, 62, 68, 71, 74]

  return (
    <DashboardShell
      accent="apollo"
      roleLabel="Apollo Analytics"
      portalTitle="Platform Insights"
      userName={auth.fullName}
      tenantLabel="Cross-university · Real-time"
      navItems={getApolloNavItems(auth.role === 'ApolloAdmin')}
    >
      <div ref={animRef} className="space-y-8">
        {/* KPI row */}
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <StatCard title="Total learners" value={totalStudents} change="Across all universities" icon={Users} accent="teal" />
          <StatCard title="Partner universities" value={uniList.length} icon={Globe} accent="blue" />
          <StatCard title="Avg completion" value={`${Math.round(avgProgress)}%`} icon={TrendingUp} accent="violet" trend="up" />
          <StatCard title="At-risk students" value={totalAtRisk} change={totalAtRisk > 0 ? 'Needs attention' : 'All on track'} icon={AlertTriangle} accent="amber" trend={totalAtRisk > 0 ? 'down' : 'up'} />
        </div>

        <div className="grid gap-6 xl:grid-cols-3">
          {/* University comparison - bar chart */}
          <Panel title="University progress comparison" className="xl:col-span-2">
            {uniBars.length > 0 ? (
              <BarChart data={uniBars} />
            ) : (
              <p className="py-8 text-center text-sm text-slate-500">No university data yet.</p>
            )}
            <div className="mt-6 grid gap-3 sm:grid-cols-2">
              {uniList.map((u: { universityId: string; universityName: string; totalStudents: number; averageProgress: number; atRiskCount: number }) => (
                <div
                  key={u.universityId}
                  data-animate-card
                  className="flex items-center gap-4 rounded-xl border border-slate-100 bg-gradient-to-br from-white to-slate-50/80 p-4"
                >
                  <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-[#2081A1]/10">
                    <Building2 className="h-5 w-5 text-[#2081A1]" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="truncate font-semibold text-slate-900">{u.universityName}</p>
                    <p className="text-xs text-slate-500">{u.totalStudents} students · {Math.round(u.averageProgress)}% avg</p>
                    {u.atRiskCount > 0 && (
                      <p className="mt-0.5 text-xs font-medium text-amber-600">{u.atRiskCount} at risk</p>
                    )}
                  </div>
                  <div className="text-right">
                    <p className="text-2xl font-bold text-[#2081A1]">{Math.round(u.averageProgress)}%</p>
                  </div>
                </div>
              ))}
            </div>
          </Panel>

          {/* Student distribution donut */}
          <Panel title="Learner distribution">
            {donutSegments.length > 0 && totalStudents > 0 ? (
              <DonutChart segments={donutSegments} />
            ) : (
              <div className="flex flex-col items-center py-8 text-center">
                <Users className="mb-2 h-10 w-10 text-slate-300" />
                <p className="text-sm text-slate-500">Enrol students to see distribution</p>
              </div>
            )}
          </Panel>
        </div>

        {/* Content performance */}
        <Panel title="Content performance">
          <div className="grid gap-8 lg:grid-cols-2">
            <div>
              <p className="mb-4 text-sm text-slate-500">Module completion rates</p>
              {contentBars.length > 0 ? (
                <BarChart data={contentBars} />
              ) : (
                <p className="py-6 text-sm text-slate-400">Publish content to see completion analytics.</p>
              )}
            </div>
            <div>
              <p className="mb-4 text-sm text-slate-500">Completion trend</p>
              <div className="rounded-xl bg-slate-50 p-4">
                <MiniBarChart
                  data={trendData}
                  labels={trendData.map((_: number, i: number) => `M${i + 1}`)}
                  color="#2081A1"
                  height={140}
                />
              </div>
              <div className="mt-4 flex items-end justify-between">
                <div>
                  <p className="text-2xl font-bold text-slate-900">
                    {contentList.length ? Math.round(trendData.reduce((a: number, b: number) => a + b, 0) / trendData.length) : 0}%
                  </p>
                  <p className="text-xs text-slate-500">Average completion</p>
                </div>
                <Sparkline data={trendData} className="h-12 w-28" />
              </div>
            </div>
          </div>

          {contentList.length > 0 && (
            <div className="mt-8 overflow-hidden rounded-xl border border-slate-100">
              <table className="w-full text-sm">
                <thead className="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">
                  <tr>
                    <th className="p-3">Module</th>
                    <th className="p-3">Programme</th>
                    <th className="p-3">Completion</th>
                    <th className="p-3 w-40">Visual</th>
                  </tr>
                </thead>
                <tbody>
                  {contentList.slice(0, 10).map((c: { moduleId: string; moduleTitle: string; programmeName: string; completionRate: number }, i: number) => (
                    <tr key={c.moduleId} className="border-t border-slate-50 hover:bg-slate-50/50">
                      <td className="p-3 font-medium text-slate-900">{c.moduleTitle}</td>
                      <td className="p-3 text-slate-500">{c.programmeName}</td>
                      <td className="p-3 tabular-nums font-semibold text-[#2081A1]">{Math.round(c.completionRate)}%</td>
                      <td className="p-3">
                        <div className="h-2 overflow-hidden rounded-full bg-slate-100">
                          <div
                            className="h-full rounded-full transition-all"
                            style={{ width: `${c.completionRate}%`, backgroundColor: COLORS[i % COLORS.length] }}
                          />
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Panel>
      </div>
    </DashboardShell>
  )
}
