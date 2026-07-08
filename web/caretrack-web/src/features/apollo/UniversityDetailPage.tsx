import { useEffect, useMemo, useRef, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  AlertTriangle,
  ArrowLeft,
  Building2,
  ChevronLeft,
  ChevronRight,
  GraduationCap,
  Mail,
  Search,
  Users,
} from 'lucide-react'
import { getApolloNavItems } from '@/lib/apollo-nav'
import { api, getErrorMessage } from '@/lib/api-client'
import { notify } from '@/lib/notify'
import { assetUrl } from '@/lib/asset-url'
import { authStore } from '@/lib/auth-store'
import { StatCard } from '@/components/dashboard/StatCard'
import { ProgressRing } from '@/components/dashboard/ProgressRing'
import { BarChart } from '@/components/dashboard/Charts'
import { DashboardShell, Panel } from '@/components/layout/DashboardShell'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Progress } from '@/components/ui/progress'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import * as Separator from '@radix-ui/react-separator'

interface StudentRow {
  studentId: string
  fullName: string
  email: string
  cohortName: string
  progressPercent: number
  isAtRisk: boolean
  lastActivityAt?: string
}

const STUDENTS_PAGE_SIZE = 10

export function UniversityDetailPage() {
  const { universityId } = useParams<{ universityId: string }>()
  const auth = authStore.get()!
  const queryClient = useQueryClient()
  const pageRef = useRef<HTMLDivElement>(null)
  const [linkedProgrammes, setLinkedProgrammes] = useState<string[]>([])
  const [programmeMessage, setProgrammeMessage] = useState<string | null>(null)
  const [adminEmail, setAdminEmail] = useState('')
  const [adminPassword, setAdminPassword] = useState('')
  const [adminFirstName, setAdminFirstName] = useState('')
  const [adminLastName, setAdminLastName] = useState('')
  const [adminMessage, setAdminMessage] = useState<string | null>(null)
  const [createdAdmin, setCreatedAdmin] = useState<{ email: string; password: string } | null>(null)
  const [editAdminId, setEditAdminId] = useState<string | null>(null)
  const [editEmail, setEditEmail] = useState('')
  const [editPassword, setEditPassword] = useState('')
  const [editFirstName, setEditFirstName] = useState('')
  const [editLastName, setEditLastName] = useState('')
  const [studentSearch, setStudentSearch] = useState('')
  const [studentPage, setStudentPage] = useState(1)

  const universityAdmins = useQuery({
    queryKey: ['university-admins', universityId],
    queryFn: async () =>
      (await api.get('/users/university-admins', { params: { universityId } })).data as {
        userId: string
        email: string
        fullName: string
        universityId: string
      }[],
    enabled: !!universityId,
  })

  const university = useQuery({
    queryKey: ['university', universityId],
    queryFn: async () => (await api.get(`/universities/${universityId}`)).data,
    enabled: !!universityId,
  })

  const programmes = useQuery({
    queryKey: ['programmes'],
    queryFn: async () => (await api.get('/programmes')).data as { id: string; name: string; code: string }[],
  })

  useEffect(() => {
    if (university.data?.programmeIds) {
      setLinkedProgrammes(university.data.programmeIds)
    }
  }, [university.data?.programmeIds])

  const saveProgrammes = useMutation({
    mutationFn: async () =>
      api.put(`/universities/${universityId}/programmes`, { programmeIds: linkedProgrammes }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['university', universityId] })
      setProgrammeMessage('Programmes linked successfully.')
      notify.success('Programmes linked.')
    },
    onError: (err) => {
      const msg = getErrorMessage(err)
      setProgrammeMessage(msg)
      notify.error(err)
    },
  })

  const createAdmin = useMutation({
    mutationFn: async () =>
      api.post('/users/university-admins/direct', {
        email: adminEmail,
        firstName: adminFirstName,
        lastName: adminLastName,
        universityId,
        password: adminPassword,
      }),
    onSuccess: () => {
      setCreatedAdmin({ email: adminEmail, password: adminPassword })
      setAdminMessage('College admin created. Share these login credentials with the admin.')
      setAdminEmail('')
      setAdminPassword('')
      setAdminFirstName('')
      setAdminLastName('')
      queryClient.invalidateQueries({ queryKey: ['university-admins', universityId] })
      notify.success('College admin created.')
    },
    onError: (err) => {
      setCreatedAdmin(null)
      const msg = getErrorMessage(err)
      setAdminMessage(msg)
      notify.error(err)
    },
  })

  const updateAdmin = useMutation({
    mutationFn: async () =>
      api.put(`/users/university-admins/${editAdminId}`, {
        userId: editAdminId,
        universityId,
        email: editEmail || null,
        password: editPassword || null,
        firstName: editFirstName || null,
        lastName: editLastName || null,
      }),
    onSuccess: () => {
      notify.success('College admin updated.')
      setEditAdminId(null)
      setEditEmail('')
      setEditPassword('')
      setEditFirstName('')
      setEditLastName('')
      queryClient.invalidateQueries({ queryKey: ['university-admins', universityId] })
    },
    onError: (err) => notify.error(err),
  })

  function startEditAdmin(admin: { userId: string; email: string; fullName: string }) {
    const [first, ...rest] = admin.fullName.split(' ')
    setEditAdminId(admin.userId)
    setEditEmail(admin.email)
    setEditPassword('')
    setEditFirstName(first ?? '')
    setEditLastName(rest.join(' '))
  }

  const comparison = useQuery({
    queryKey: ['apollo-universities'],
    queryFn: async () => (await api.get('/reports/apollo/universities')).data,
  })

  const report = useQuery({
    queryKey: ['university-report', universityId],
    queryFn: async () =>
      (await api.get('/reports/university/students', { params: { universityId } })).data,
    enabled: !!universityId,
  })

  const cohorts = useQuery({
    queryKey: ['cohorts'],
    queryFn: async () => (await api.get('/cohorts')).data,
  })

  const uniStats = comparison.data?.find(
    (u: { universityId: string }) => u.universityId === universityId,
  )

  const uniCohorts = (cohorts.data ?? []).filter(
    (c: { universityId: string }) => c.universityId === universityId,
  )

  const students: StudentRow[] = report.data?.students ?? []

  const filteredStudents = useMemo(() => {
    const q = studentSearch.trim().toLowerCase()
    if (!q) return students
    return students.filter(
      (s) =>
        s.fullName.toLowerCase().includes(q) ||
        s.email.toLowerCase().includes(q) ||
        s.cohortName?.toLowerCase().includes(q),
    )
  }, [students, studentSearch])

  const totalStudentPages = Math.max(1, Math.ceil(filteredStudents.length / STUDENTS_PAGE_SIZE))

  const paginatedStudents = useMemo(() => {
    const start = (studentPage - 1) * STUDENTS_PAGE_SIZE
    return filteredStudents.slice(start, start + STUDENTS_PAGE_SIZE)
  }, [filteredStudents, studentPage])

  useEffect(() => {
    setStudentPage(1)
  }, [studentSearch])

  useEffect(() => {
    if (studentPage > totalStudentPages) {
      setStudentPage(totalStudentPages)
    }
  }, [studentPage, totalStudentPages])

  const avgProgress = Math.round(report.data?.averageProgress ?? uniStats?.averageProgress ?? 0)
  const totalStudents = report.data?.totalStudents ?? uniStats?.totalStudents ?? 0
  const atRisk = report.data?.atRiskCount ?? uniStats?.atRiskCount ?? 0

  const progressBars = students.slice(0, 8).map((s) => ({
    label: s.fullName.split(' ')[0],
    value: s.progressPercent,
  }))

  const navItems = getApolloNavItems(true)

  return (
    <DashboardShell
      accent="apollo"
      roleLabel="Apollo Console"
      portalTitle={university.data?.name ?? 'University'}
      userName={auth.fullName}
      tenantLabel={university.data?.domain ?? 'Partner university'}
      navItems={navItems}
    >
      <div ref={pageRef} className="space-y-8">
        <div className="flex flex-wrap items-center gap-4">
          <Button variant="outline" size="sm" asChild>
            <Link to="/console">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to console
            </Link>
          </Button>
          {university.data && (
            <span
              className={`rounded-full px-3 py-1 text-xs font-medium ${
                university.data.isActive
                  ? 'bg-emerald-100 text-emerald-700'
                  : 'bg-slate-100 text-slate-600'
              }`}
            >
              {university.data.isActive ? 'Active partner' : 'Inactive'}
            </span>
          )}
          {assetUrl(university.data?.logoUrl) && (
            <img
              src={assetUrl(university.data?.logoUrl)!}
              alt=""
              className="h-10 object-contain"
            />
          )}
        </div>

        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <StatCard title="Total students" value={totalStudents} icon={Users} accent="teal" />
          <StatCard title="Avg progress" value={`${avgProgress}%`} icon={GraduationCap} accent="blue" trend="up" />
          <StatCard title="At risk" value={atRisk} icon={AlertTriangle} accent="amber" trend={atRisk > 0 ? 'down' : 'up'} />
          <StatCard title="Active cohorts" value={uniCohorts.length} icon={Building2} accent="violet" />
        </div>

        <div className="grid gap-6 xl:grid-cols-3">
          <Panel title="Overall progress" className="xl:col-span-1">
            <div className="flex flex-col items-center py-4">
              <ProgressRing value={avgProgress} label="Avg" sublabel="Across all students" />
            </div>
            <Separator.Root className="my-4 h-px bg-slate-100" />
            <div className="space-y-3">
              <div className="flex justify-between text-sm">
                <span className="text-slate-500">On track</span>
                <span className="font-semibold text-emerald-600">{totalStudents - atRisk}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-slate-500">At risk</span>
                <span className="font-semibold text-amber-600">{atRisk}</span>
              </div>
            </div>
          </Panel>

          <Panel title="Progress distribution" className="xl:col-span-2">
            {progressBars.length > 0 ? (
              <BarChart data={progressBars} />
            ) : (
              <p className="py-8 text-center text-sm text-slate-500">No student progress data yet.</p>
            )}
          </Panel>
        </div>

        <Panel title="University insights">
          <Tabs defaultValue="students">
            <TabsList>
              <TabsTrigger value="students">Students ({totalStudents})</TabsTrigger>
              <TabsTrigger value="cohorts">Cohorts ({uniCohorts.length})</TabsTrigger>
              <TabsTrigger value="programmes">Programmes ({linkedProgrammes.length})</TabsTrigger>
              <TabsTrigger value="admin">College admin</TabsTrigger>
            </TabsList>

            <TabsContent value="students">
              {students.length === 0 ? (
                <p className="text-sm text-slate-500">No enrolled students yet.</p>
              ) : (
                <div className="space-y-4">
                  <div className="relative max-w-md">
                    <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                    <Input
                      placeholder="Search by student name…"
                      value={studentSearch}
                      onChange={(e) => setStudentSearch(e.target.value)}
                      className="pl-9"
                    />
                  </div>

                  {filteredStudents.length === 0 ? (
                    <p className="text-sm text-slate-500">No students match your search.</p>
                  ) : (
                    <>
                      <div className="space-y-3">
                        {paginatedStudents.map((s) => (
                          <div
                            key={s.studentId}
                            className="flex flex-col gap-3 rounded-xl border border-slate-100 bg-gradient-to-r from-white to-slate-50/80 p-4 sm:flex-row sm:items-center"
                          >
                            <div className="flex min-w-0 flex-1 items-center gap-3">
                              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-[#2081A1]/10 text-sm font-bold text-[#2081A1]">
                                {s.fullName.charAt(0)}
                              </div>
                              <div className="min-w-0">
                                <p className="truncate font-medium text-slate-900">{s.fullName}</p>
                                <p className="flex items-center gap-1 truncate text-xs text-slate-500">
                                  <Mail className="h-3 w-3" />
                                  {s.email}
                                </p>
                              </div>
                            </div>
                            <div className="flex flex-1 items-center gap-4 sm:max-w-xs">
                              <Progress value={s.progressPercent} className="flex-1" />
                              <span className="w-10 text-right text-sm font-semibold tabular-nums text-[#2081A1]">
                                {s.progressPercent}%
                              </span>
                            </div>
                            <div className="flex items-center gap-2 sm:w-28 sm:justify-end">
                              {s.isAtRisk ? (
                                <span className="inline-flex items-center gap-1 rounded-full bg-amber-100 px-2 py-0.5 text-xs text-amber-700">
                                  <AlertTriangle className="h-3 w-3" />
                                  At risk
                                </span>
                              ) : (
                                <span className="rounded-full bg-emerald-100 px-2 py-0.5 text-xs text-emerald-700">
                                  On track
                                </span>
                              )}
                            </div>
                          </div>
                        ))}
                      </div>

                      {totalStudentPages > 1 && (
                        <div className="flex flex-wrap items-center justify-between gap-3 border-t border-slate-100 pt-4">
                          <p className="text-sm text-slate-500">
                            Showing {(studentPage - 1) * STUDENTS_PAGE_SIZE + 1}–
                            {Math.min(studentPage * STUDENTS_PAGE_SIZE, filteredStudents.length)} of{' '}
                            {filteredStudents.length}
                            {studentSearch.trim() ? ` (filtered from ${students.length})` : ''}
                          </p>
                          <div className="flex items-center gap-2">
                            <Button
                              variant="outline"
                              size="sm"
                              disabled={studentPage <= 1}
                              onClick={() => setStudentPage((p) => p - 1)}
                            >
                              <ChevronLeft className="h-4 w-4" />
                              Previous
                            </Button>
                            <span className="text-sm tabular-nums text-slate-600">
                              Page {studentPage} of {totalStudentPages}
                            </span>
                            <Button
                              variant="outline"
                              size="sm"
                              disabled={studentPage >= totalStudentPages}
                              onClick={() => setStudentPage((p) => p + 1)}
                            >
                              Next
                              <ChevronRight className="h-4 w-4" />
                            </Button>
                          </div>
                        </div>
                      )}
                    </>
                  )}
                </div>
              )}
            </TabsContent>

            <TabsContent value="cohorts">
              {uniCohorts.length === 0 ? (
                <p className="text-sm text-slate-500">No cohorts configured for this university.</p>
              ) : (
                <div className="grid gap-4 md:grid-cols-2">
                  {uniCohorts.map((c: { id: string; name: string; intakeYear: number; currentYear: number; currentSemester: number; programmeName: string }) => (
                    <div
                      key={c.id}
                      className="rounded-xl border border-slate-100 bg-white p-5 shadow-sm transition hover:border-[#2081A1]/30 hover:shadow-md"
                    >
                      <p className="font-semibold text-slate-900">{c.name}</p>
                      <p className="text-sm text-slate-500">{c.programmeName}</p>
                      <Separator.Root className="my-3 h-px bg-slate-100" />
                      <div className="flex gap-4 text-xs text-slate-500">
                        <span>Intake {c.intakeYear}</span>
                        <span>Year {c.currentYear}</span>
                        <span>Sem {c.currentSemester}</span>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </TabsContent>

            <TabsContent value="programmes">
              <p className="mb-4 text-sm text-slate-600">
                Link programmes this university can use for cohorts and student assignment. College admins only see linked programmes.
              </p>
              <div className="max-h-56 space-y-2 overflow-y-auto rounded-xl border border-slate-200 p-3">
                {(programmes.data ?? []).map((p) => (
                  <label
                    key={p.id}
                    className={`flex cursor-pointer items-center gap-3 rounded-lg px-3 py-2.5 hover:bg-slate-50 ${
                      linkedProgrammes.includes(p.id) ? 'bg-[#2081A1]/5 ring-1 ring-[#2081A1]/30' : ''
                    }`}
                  >
                    <input
                      type="checkbox"
                      checked={linkedProgrammes.includes(p.id)}
                      onChange={() =>
                        setLinkedProgrammes((prev) =>
                          prev.includes(p.id) ? prev.filter((id) => id !== p.id) : [...prev, p.id],
                        )
                      }
                      className="h-4 w-4 rounded border-slate-300 text-[#2081A1]"
                    />
                    <div>
                      <p className="font-medium text-slate-900">{p.name}</p>
                      <p className="text-xs text-slate-500">{p.code}</p>
                    </div>
                  </label>
                ))}
              </div>
              <div className="mt-4 flex items-center gap-3">
                <Button
                  className="bg-[#2081A1]"
                  onClick={() => saveProgrammes.mutate()}
                  disabled={saveProgrammes.isPending}
                >
                  Save programme links
                </Button>
                {programmeMessage && (
                  <p className={`text-sm ${programmeMessage.includes('success') ? 'text-emerald-600' : 'text-red-600'}`}>
                    {programmeMessage}
                  </p>
                )}
              </div>
            </TabsContent>

            <TabsContent value="admin">
              {(universityAdmins.data ?? []).length > 0 && (
                <div className="mb-8 space-y-3">
                  <h3 className="font-semibold text-slate-900">Existing college admins</h3>
                  {(universityAdmins.data ?? []).map((admin) => (
                    <div
                      key={admin.userId}
                      className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-slate-100 bg-slate-50 px-4 py-3"
                    >
                      <div>
                        <p className="font-medium text-slate-900">{admin.fullName}</p>
                        <p className="text-sm text-slate-500">{admin.email}</p>
                      </div>
                      <Button variant="outline" size="sm" onClick={() => startEditAdmin(admin)}>
                        Update credentials
                      </Button>
                    </div>
                  ))}
                </div>
              )}

              {editAdminId && (
                <div className="mb-8 rounded-xl border border-[#2081A1]/30 bg-[#2081A1]/5 p-5">
                  <h3 className="mb-4 font-semibold text-slate-900">Update college admin</h3>
                  <div className="grid max-w-lg gap-4">
                    <div className="space-y-2">
                      <Label>Email</Label>
                      <Input value={editEmail} onChange={(e) => setEditEmail(e.target.value)} />
                    </div>
                    <div className="space-y-2">
                      <Label>New password (leave blank to keep)</Label>
                      <Input
                        type="password"
                        value={editPassword}
                        onChange={(e) => setEditPassword(e.target.value)}
                        placeholder="Min 8 characters"
                      />
                    </div>
                    <div className="grid gap-4 sm:grid-cols-2">
                      <div className="space-y-2">
                        <Label>First name</Label>
                        <Input value={editFirstName} onChange={(e) => setEditFirstName(e.target.value)} />
                      </div>
                      <div className="space-y-2">
                        <Label>Last name</Label>
                        <Input value={editLastName} onChange={(e) => setEditLastName(e.target.value)} />
                      </div>
                    </div>
                    <div className="flex gap-2">
                      <Button
                        className="bg-[#2081A1]"
                        disabled={updateAdmin.isPending}
                        onClick={() => updateAdmin.mutate()}
                      >
                        Save changes
                      </Button>
                      <Button variant="outline" onClick={() => setEditAdminId(null)}>
                        Cancel
                      </Button>
                    </div>
                  </div>
                </div>
              )}

              <p className="mb-4 text-sm text-slate-600">
                Creating a university only registers the partner — it does not create a login. Add a college admin here with email and password (no invite email).
              </p>
              <div className="grid max-w-lg gap-4">
                <div className="space-y-2">
                  <Label>Email</Label>
                  <Input
                    type="email"
                    value={adminEmail}
                    onChange={(e) => setAdminEmail(e.target.value)}
                    placeholder="admin@mit.edu"
                  />
                </div>
                <div className="space-y-2">
                  <Label>Password</Label>
                  <Input
                    type="password"
                    value={adminPassword}
                    onChange={(e) => setAdminPassword(e.target.value)}
                    placeholder="Min 8 characters"
                  />
                </div>
                <div className="grid gap-4 sm:grid-cols-2">
                  <div className="space-y-2">
                    <Label>First name</Label>
                    <Input value={adminFirstName} onChange={(e) => setAdminFirstName(e.target.value)} />
                  </div>
                  <div className="space-y-2">
                    <Label>Last name</Label>
                    <Input value={adminLastName} onChange={(e) => setAdminLastName(e.target.value)} />
                  </div>
                </div>
                <Button
                  className="w-fit bg-[#2081A1]"
                  disabled={
                    createAdmin.isPending ||
                    !adminEmail ||
                    !adminPassword ||
                    !adminFirstName ||
                    !adminLastName
                  }
                  onClick={() => createAdmin.mutate()}
                >
                  Create college admin
                </Button>
                {createdAdmin && (
                  <div className="rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-sm text-emerald-900">
                    <p className="font-medium">Login credentials</p>
                    <p className="mt-1">Email: {createdAdmin.email}</p>
                    <p>Password: {createdAdmin.password}</p>
                  </div>
                )}
                {adminMessage && (
                  <p className={`text-sm ${adminMessage.includes('created') ? 'text-emerald-600' : 'text-red-600'}`}>
                    {adminMessage}
                  </p>
                )}
              </div>
            </TabsContent>
          </Tabs>
        </Panel>
      </div>
    </DashboardShell>
  )
}
