import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import {
  AlertTriangle,
  BarChart3,
  GraduationCap,
  LayoutDashboard,
  Upload,
  UserPlus,
  Users,
} from 'lucide-react'
import { api, getErrorMessage } from '@/lib/api-client'
import { authStore } from '@/lib/auth-store'
import { useDashboardAnimation } from '@/animations/useDashboardAnimation'
import { StatCard } from '@/components/dashboard/StatCard'
import { Sparkline } from '@/components/dashboard/Sparkline'
import { ProgressRing } from '@/components/dashboard/ProgressRing'
import { DashboardShell, Panel } from '@/components/layout/DashboardShell'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Link } from 'react-router-dom'

export function UniversityDashboard() {
  const animRef = useDashboardAnimation()
  const queryClient = useQueryClient()
  const auth = authStore.get()!
  const [email, setEmail] = useState('')
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [cohortId, setCohortId] = useState('')

  const cohorts = useQuery({ queryKey: ['cohorts'], queryFn: async () => (await api.get('/cohorts')).data })
  const students = useQuery({ queryKey: ['students'], queryFn: async () => (await api.get('/enrolments/students')).data })
  const report = useQuery({ queryKey: ['university-report'], queryFn: async () => (await api.get('/reports/university/students')).data })

  const createStudent = useMutation({
    mutationFn: async () => api.post('/enrolments/students', { email, firstName, lastName, cohortId }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['students'] })
      queryClient.invalidateQueries({ queryKey: ['university-report'] })
      setEmail('')
      setFirstName('')
      setLastName('')
    },
  })

  async function importCsv(file: File) {
    if (!cohortId) return
    const form = new FormData()
    form.append('file', file)
    await api.post(`/enrolments/students/import?cohortId=${cohortId}`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    queryClient.invalidateQueries({ queryKey: ['students'] })
  }

  const avgProgress = Math.round(report.data?.averageProgress ?? 0)
  const atRisk = report.data?.atRiskCount ?? 0

  return (
    <DashboardShell
      accent="teal"
      roleLabel="University Admin"
      portalTitle="Enrolment & Compliance"
      userName={auth.fullName}
      tenantLabel="Tenant-scoped · Your students only"
      navItems={[
        { label: 'Dashboard', href: '/admin', icon: LayoutDashboard },
        { label: 'Enrolment', href: '/admin', icon: UserPlus },
        { label: 'Students', href: '/admin', icon: Users },
        { label: 'Reports', href: '/university/reports', icon: BarChart3 },
      ]}
    >
      <div ref={animRef} className="space-y-8">
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <StatCard title="Total students" value={report.data?.totalStudents ?? 0} icon={Users} accent="teal" />
          <StatCard title="Avg progress" value={`${avgProgress}%`} icon={GraduationCap} accent="blue" trend="up" />
          <StatCard title="At risk" value={atRisk} change={atRisk > 0 ? 'Needs attention' : 'All on track'} icon={AlertTriangle} accent="amber" trend={atRisk > 0 ? 'down' : 'up'} />
          <StatCard title="Active cohorts" value={cohorts.data?.length ?? 0} icon={GraduationCap} accent="violet" />
        </div>

        <div className="grid gap-6 xl:grid-cols-3">
          <Panel title="Cohort health" className="xl:col-span-1">
            <div className="flex flex-col items-center py-4">
              <ProgressRing value={avgProgress} label="Avg" sublabel="Semester progress" />
            </div>
            <Sparkline data={[30, 35, 38, 42, 45, 48, avgProgress]} className="mt-4 h-12 w-full" />
          </Panel>

          <Panel title="Enrol new student" className="xl:col-span-2">
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label>Cohort</Label>
                <select className="h-10 w-full rounded-lg border px-3 text-sm" value={cohortId} onChange={(e) => setCohortId(e.target.value)}>
                  <option value="">Select cohort</option>
                  {cohorts.data?.map((c: { id: string; name: string }) => (
                    <option key={c.id} value={c.id}>{c.name}</option>
                  ))}
                </select>
              </div>
              <div className="space-y-2">
                <Label>Email</Label>
                <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label>First name</Label>
                <Input value={firstName} onChange={(e) => setFirstName(e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label>Last name</Label>
                <Input value={lastName} onChange={(e) => setLastName(e.target.value)} />
              </div>
            </div>
            {createStudent.error && <p className="mt-2 text-sm text-red-600">{getErrorMessage(createStudent.error)}</p>}
            <div className="mt-4 flex flex-wrap gap-2">
              <Button onClick={() => createStudent.mutate()} disabled={!email || !cohortId}>
                <UserPlus className="mr-2 h-4 w-4" />Invite student
              </Button>
              <Label className="inline-flex cursor-pointer items-center gap-2 rounded-lg border px-4 py-2 text-sm hover:bg-slate-50">
                <Upload className="h-4 w-4" />CSV import
                <Input type="file" accept=".csv" className="hidden" onChange={(e) => e.target.files?.[0] && importCsv(e.target.files[0])} />
              </Label>
              <Button variant="outline" asChild>
                <Link to="/university/reports">View reports</Link>
              </Button>
            </div>
          </Panel>
        </div>

        <Panel title="Student roster">
          <div className="overflow-hidden rounded-xl border">
            <table className="w-full text-sm">
              <thead className="bg-slate-50 text-left text-slate-500">
                <tr>
                  <th className="p-3 font-medium">Student</th>
                  <th className="p-3 font-medium">Cohort</th>
                  <th className="p-3 font-medium">Status</th>
                </tr>
              </thead>
              <tbody>
                {students.data?.items?.map((s: { id: string; firstName: string; lastName: string; email: string; cohortName: string; status: string }) => (
                  <tr key={s.id} className="border-t hover:bg-slate-50/80">
                    <td className="p-3">
                      <p className="font-medium text-slate-900">{s.firstName} {s.lastName}</p>
                      <p className="text-xs text-slate-500">{s.email}</p>
                    </td>
                    <td className="p-3">{s.cohortName}</td>
                    <td className="p-3">
                      <span className={`rounded-full px-2 py-0.5 text-xs ${s.status === 'Active' ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-700'}`}>
                        {s.status}
                      </span>
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
