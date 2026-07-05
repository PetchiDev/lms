import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { ArrowLeft, Download, AlertTriangle } from 'lucide-react'
import { api } from '@/lib/api-client'
import { authStore } from '@/lib/auth-store'
import { usePageTransition } from '@/animations/usePageTransition'
import { AppLayout, PageSection } from '@/components/layout/AppLayout'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { ProgressBar } from '@/components/ui/label'

export function UniversityReportsPage() {
  const ref = usePageTransition()
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

  return (
    <div ref={ref}>
      <AppLayout
        title="University Reports"
        subtitle={data?.cohortName}
        nav={
          <div className="flex gap-2">
            <Button variant="outline" onClick={() => exportReport('excel')}><Download className="mr-2 h-4 w-4" />Excel</Button>
            <Button variant="outline" asChild><Link to="/admin"><ArrowLeft className="mr-2 h-4 w-4" />Back</Link></Button>
          </div>
        }
      >
        <div className="mb-6 grid gap-4 md:grid-cols-4">
          <Card><CardContent className="pt-6"><p className="text-2xl font-bold">{data?.totalStudents ?? 0}</p><p className="text-sm text-slate-500">Total Students</p></CardContent></Card>
          <Card><CardContent className="pt-6"><p className="text-2xl font-bold">{Math.round(data?.averageProgress ?? 0)}%</p><p className="text-sm text-slate-500">Avg Progress</p></CardContent></Card>
          <Card><CardContent className="pt-6"><p className="text-2xl font-bold text-amber-600">{data?.atRiskCount ?? 0}</p><p className="text-sm text-slate-500">At Risk</p></CardContent></Card>
        </div>

        <PageSection title="Student Progress">
          <div className="overflow-hidden rounded-xl border">
            <table className="w-full text-sm">
              <thead className="bg-slate-50 text-left">
                <tr>
                  <th className="p-3">Student</th>
                  <th className="p-3">Cohort</th>
                  <th className="p-3">Progress</th>
                  <th className="p-3">Status</th>
                </tr>
              </thead>
              <tbody>
                {data?.students?.map((s: { studentId: string; fullName: string; cohortName: string; progressPercent: number; isAtRisk: boolean }) => (
                  <tr key={s.studentId} className="border-t">
                    <td className="p-3">{s.fullName}</td>
                    <td className="p-3">{s.cohortName}</td>
                    <td className="p-3"><ProgressBar value={s.progressPercent} className="max-w-[120px]" /></td>
                    <td className="p-3">
                      {s.isAtRisk ? (
                        <span className="inline-flex items-center gap-1 text-amber-600"><AlertTriangle className="h-3 w-3" />At Risk</span>
                      ) : 'On Track'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </PageSection>
      </AppLayout>
    </div>
  )
}

export function ApolloReportsPage() {
  const ref = usePageTransition()
  const auth = authStore.get()!

  const universities = useQuery({
    queryKey: ['apollo-universities'],
    queryFn: async () => (await api.get('/reports/apollo/universities')).data,
  })

  const content = useQuery({
    queryKey: ['content-performance'],
    queryFn: async () => (await api.get('/reports/apollo/content-performance')).data,
  })

  return (
    <div ref={ref}>
      <AppLayout title="Apollo Analytics" subtitle={auth.fullName} nav={<Button variant="outline" asChild><Link to="/console"><ArrowLeft className="mr-2 h-4 w-4" />Back</Link></Button>}>
        <PageSection title="University Comparison">
          <div className="grid gap-4 md:grid-cols-2">
            {universities.data?.map((u: { universityId: string; universityName: string; totalStudents: number; averageProgress: number; atRiskCount: number }) => (
              <Card key={u.universityId}>
                <CardContent className="pt-6">
                  <p className="font-semibold">{u.universityName}</p>
                  <p className="text-sm text-slate-500">{u.totalStudents} students · {Math.round(u.averageProgress)}% avg</p>
                  <p className="text-sm text-amber-600">{u.atRiskCount} at risk</p>
                </CardContent>
              </Card>
            ))}
          </div>
        </PageSection>

        <PageSection title="Content Performance">
          <div className="overflow-hidden rounded-xl border">
            <table className="w-full text-sm">
              <thead className="bg-slate-50 text-left">
                <tr>
                  <th className="p-3">Module</th>
                  <th className="p-3">Programme</th>
                  <th className="p-3">Completion Rate</th>
                </tr>
              </thead>
              <tbody>
                {content.data?.slice(0, 10).map((c: { moduleId: string; moduleTitle: string; programmeName: string; completionRate: number }) => (
                  <tr key={c.moduleId} className="border-t">
                    <td className="p-3">{c.moduleTitle}</td>
                    <td className="p-3">{c.programmeName}</td>
                    <td className="p-3">{Math.round(c.completionRate)}%</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </PageSection>
      </AppLayout>
    </div>
  )
}
