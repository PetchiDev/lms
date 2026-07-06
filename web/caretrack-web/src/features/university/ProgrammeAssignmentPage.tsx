import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { BookMarked, Check, GraduationCap, Loader2, Search, Users } from 'lucide-react'
import { Link } from 'react-router-dom'
import { api, getErrorMessage } from '@/lib/api-client'
import { authStore } from '@/lib/auth-store'
import { UniPanel } from '@/components/layout/UniversityShell'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { cn } from '@/lib/utils'

interface Programme {
  id: string
  name: string
  code: string
}

interface Cohort {
  id: string
  programmeId: string
  name: string
  programmeName: string
  intakeYear: number
  currentYear: number
  currentSemester: number
}

interface StudentRow {
  id: string
  studentId: string
  cohortId: string
  email: string
  firstName: string
  lastName: string
  status: string
  cohortName: string
  programmeName: string
}

export function ProgrammeAssignmentPage() {
  const queryClient = useQueryClient()
  const auth = authStore.get()!
  const universityId = auth.universityId!

  const [search, setSearch] = useState('')
  const [selectedStudentId, setSelectedStudentId] = useState<string | null>(null)
  const [programmeId, setProgrammeId] = useState('')
  const [cohortId, setCohortId] = useState('')
  const [newCohortName, setNewCohortName] = useState('')
  const [showCreateCohort, setShowCreateCohort] = useState(false)

  const university = useQuery({
    queryKey: ['university', universityId],
    queryFn: async () => (await api.get(`/universities/${universityId}`)).data,
  })

  const programmes = useQuery({
    queryKey: ['programmes'],
    queryFn: async () => (await api.get('/programmes')).data as Programme[],
  })

  const cohorts = useQuery({
    queryKey: ['cohorts'],
    queryFn: async () => (await api.get('/cohorts')).data as Cohort[],
  })

  const students = useQuery({
    queryKey: ['students'],
    queryFn: async () => (await api.get('/enrolments/students', { params: { pageSize: 100 } })).data,
  })

  const linkedProgrammes = useMemo(() => {
    const ids = new Set(university.data?.programmeIds ?? [])
    return (programmes.data ?? []).filter((p) => ids.has(p.id))
  }, [programmes.data, university.data])

  const cohortsForProgramme = useMemo(
    () => (cohorts.data ?? []).filter((c) => c.programmeId === programmeId),
    [cohorts.data, programmeId],
  )

  const filteredStudents = useMemo(() => {
    const items: StudentRow[] = students.data?.items ?? []
    const q = search.trim().toLowerCase()
    if (!q) return items
    return items.filter(
      (s) =>
        s.firstName.toLowerCase().includes(q) ||
        s.lastName.toLowerCase().includes(q) ||
        s.email.toLowerCase().includes(q) ||
        s.programmeName?.toLowerCase().includes(q),
    )
  }, [students.data, search])

  const selectedStudent = filteredStudents.find((s) => s.studentId === selectedStudentId)

  const assignCohort = useMutation({
    mutationFn: async ({ studentId, cohortId: cid }: { studentId: string; cohortId: string }) =>
      api.patch(`/enrolments/students/${studentId}/cohort`, { cohortId: cid }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['students'] })
      queryClient.invalidateQueries({ queryKey: ['university-report'] })
      setCohortId('')
      setProgrammeId('')
      setShowCreateCohort(false)
      setNewCohortName('')
    },
  })

  const createCohort = useMutation({
    mutationFn: async () =>
      api.post('/cohorts', {
        universityId,
        programmeId,
        name: newCohortName,
        intakeYear: new Date().getFullYear(),
        currentYear: 1,
        currentSemester: 1,
      }),
    onSuccess: (res) => {
      queryClient.invalidateQueries({ queryKey: ['cohorts'] })
      setCohortId(res.data.id)
      setShowCreateCohort(false)
      setNewCohortName('')
    },
  })

  function selectStudent(student: StudentRow) {
    setSelectedStudentId(student.studentId)
    const cohort = (cohorts.data ?? []).find((c) => c.id === student.cohortId)
    if (cohort) {
      setProgrammeId(cohort.programmeId)
      setCohortId(student.cohortId)
    } else {
      setProgrammeId('')
      setCohortId('')
    }
  }

  const unassigned = (students.data?.items ?? []).filter((s: StudentRow) => !s.programmeName || s.programmeName === '—').length

  return (
    <div className="space-y-6">
        <div className="grid gap-4 sm:grid-cols-3">
          <div className="rounded-2xl border border-slate-200/60 bg-white p-5 shadow-sm">
            <div className="flex items-center gap-3">
              <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-[#2081A1]/10 text-[#2081A1]">
                <Users className="h-5 w-5" />
              </span>
              <div>
                <p className="text-xs font-medium uppercase tracking-wide text-slate-500">Total students</p>
                <p className="text-2xl font-bold text-[#0a1628]">{students.data?.totalCount ?? 0}</p>
              </div>
            </div>
          </div>
          <div className="rounded-2xl border border-slate-200/60 bg-white p-5 shadow-sm">
            <div className="flex items-center gap-3">
              <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-[#004a8f]/10 text-[#004a8f]">
                <BookMarked className="h-5 w-5" />
              </span>
              <div>
                <p className="text-xs font-medium uppercase tracking-wide text-slate-500">Linked programmes</p>
                <p className="text-2xl font-bold text-[#0a1628]">{linkedProgrammes.length}</p>
              </div>
            </div>
          </div>
          <div className="rounded-2xl border border-slate-200/60 bg-white p-5 shadow-sm">
            <div className="flex items-center gap-3">
              <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-amber-500/10 text-amber-600">
                <GraduationCap className="h-5 w-5" />
              </span>
              <div>
                <p className="text-xs font-medium uppercase tracking-wide text-slate-500">Active cohorts</p>
                <p className="text-2xl font-bold text-[#0a1628]">{cohorts.data?.length ?? 0}</p>
              </div>
            </div>
          </div>
        </div>

        <div className="grid gap-6 xl:grid-cols-5">
          <UniPanel title="Students" className="xl:col-span-2">
            <div className="relative mb-4">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <Input
                placeholder="Search by name, email or programme…"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="pl-9"
              />
            </div>
            <div className="max-h-[420px] space-y-2 overflow-y-auto student-scroll">
              {filteredStudents.length === 0 && (
                <p className="py-8 text-center text-sm text-slate-500">
                  No students yet.{' '}
                  <Link to="/admin/enrolment" className="font-medium text-[#2081A1] hover:underline">
                    Enrol students first
                  </Link>
                </p>
              )}
              {filteredStudents.map((s) => (
                <button
                  key={s.id}
                  type="button"
                  onClick={() => selectStudent(s)}
                  className={cn(
                    'w-full rounded-xl border p-3 text-left transition-all',
                    selectedStudentId === s.studentId
                      ? 'border-[#2081A1] bg-[#2081A1]/5 ring-1 ring-[#2081A1]/20'
                      : 'border-slate-200 hover:border-slate-300 hover:bg-slate-50',
                  )}
                >
                  <div className="flex items-start justify-between gap-2">
                    <div className="min-w-0">
                      <p className="truncate font-semibold text-slate-900">
                        {s.firstName} {s.lastName}
                      </p>
                      <p className="truncate text-xs text-slate-500">{s.email}</p>
                    </div>
                    <span
                      className={cn(
                        'shrink-0 rounded-full px-2 py-0.5 text-[10px] font-medium',
                        s.status === 'Active' ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-700',
                      )}
                    >
                      {s.status}
                    </span>
                  </div>
                  <p className="mt-1.5 text-xs text-slate-600">
                    <span className="font-medium text-[#004a8f]">{s.programmeName || 'Unassigned'}</span>
                    {s.cohortName && <span className="text-slate-400"> · {s.cohortName}</span>}
                  </p>
                </button>
              ))}
            </div>
          </UniPanel>

          <UniPanel
            title="Assign programme & cohort"
            className="xl:col-span-3"
            action={
              selectedStudent && (
                <span className="text-xs text-slate-500">
                  Editing: <strong className="text-slate-800">{selectedStudent.firstName} {selectedStudent.lastName}</strong>
                </span>
              )
            }
          >
            {!selectedStudent ? (
              <div className="flex flex-col items-center justify-center py-16 text-center">
                <div className="mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-slate-100">
                  <BookMarked className="h-8 w-8 text-slate-400" />
                </div>
                <p className="font-medium text-slate-700">Select a student</p>
                <p className="mt-1 max-w-sm text-sm text-slate-500">
                  Choose a student from the list to assign or change their programme and cohort.
                </p>
              </div>
            ) : (
              <div className="space-y-5">
                <div className="grid gap-4 sm:grid-cols-2">
                  <div className="space-y-2">
                    <Label>Programme</Label>
                    <select
                      className="h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm shadow-sm focus:border-[#2081A1] focus:outline-none focus:ring-2 focus:ring-[#2081A1]/20"
                      value={programmeId}
                      onChange={(e) => {
                        setProgrammeId(e.target.value)
                        setCohortId('')
                      }}
                    >
                      <option value="">Select programme</option>
                      {linkedProgrammes.map((p) => (
                        <option key={p.id} value={p.id}>
                          {p.name} ({p.code})
                        </option>
                      ))}
                    </select>
                    {linkedProgrammes.length === 0 && (
                      <p className="text-xs text-amber-600">No programmes linked to your university. Contact Apollo Admin.</p>
                    )}
                  </div>
                  <div className="space-y-2">
                    <Label>Cohort</Label>
                    <select
                      className="h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm shadow-sm focus:border-[#2081A1] focus:outline-none focus:ring-2 focus:ring-[#2081A1]/20 disabled:opacity-50"
                      value={cohortId}
                      onChange={(e) => setCohortId(e.target.value)}
                      disabled={!programmeId}
                    >
                      <option value="">Select cohort</option>
                      {cohortsForProgramme.map((c) => (
                        <option key={c.id} value={c.id}>
                          {c.name} · Y{c.currentYear} S{c.currentSemester} ({c.intakeYear})
                        </option>
                      ))}
                    </select>
                  </div>
                </div>

                {programmeId && cohortsForProgramme.length === 0 && !showCreateCohort && (
                  <div className="rounded-xl border border-dashed border-slate-300 bg-slate-50 p-4 text-sm text-slate-600">
                    No cohort exists for this programme.{' '}
                    <button
                      type="button"
                      onClick={() => setShowCreateCohort(true)}
                      className="font-semibold text-[#2081A1] hover:underline"
                    >
                      Create a cohort
                    </button>
                  </div>
                )}

                {showCreateCohort && programmeId && (
                  <div className="rounded-xl border border-[#2081A1]/30 bg-[#2081A1]/5 p-4">
                    <Label>New cohort name</Label>
                    <div className="mt-2 flex gap-2">
                      <Input
                        placeholder="e.g. BSc Nursing 2026 Intake"
                        value={newCohortName}
                        onChange={(e) => setNewCohortName(e.target.value)}
                      />
                      <Button
                        onClick={() => createCohort.mutate()}
                        disabled={!newCohortName.trim() || createCohort.isPending}
                      >
                        {createCohort.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Create'}
                      </Button>
                      <Button variant="outline" onClick={() => setShowCreateCohort(false)}>
                        Cancel
                      </Button>
                    </div>
                    {createCohort.error && (
                      <p className="mt-2 text-sm text-red-600">{getErrorMessage(createCohort.error)}</p>
                    )}
                  </div>
                )}

                {programmeId && cohortsForProgramme.length > 0 && (
                  <button
                    type="button"
                    onClick={() => setShowCreateCohort(true)}
                    className="text-xs font-medium text-[#2081A1] hover:underline"
                  >
                    + Add new cohort for this programme
                  </button>
                )}

                {assignCohort.error && (
                  <p className="text-sm text-red-600">{getErrorMessage(assignCohort.error)}</p>
                )}

                <div className="flex flex-wrap gap-3 border-t border-slate-100 pt-4">
                  <Button
                    onClick={() =>
                      assignCohort.mutate({ studentId: selectedStudent.studentId, cohortId })
                    }
                    disabled={!cohortId || assignCohort.isPending}
                    className="bg-[#004a8f] hover:bg-[#003a70]"
                  >
                    {assignCohort.isPending ? (
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    ) : (
                      <Check className="mr-2 h-4 w-4" />
                    )}
                    Save assignment
                  </Button>
                  <Button variant="outline" asChild>
                    <Link to="/admin/students">View all students</Link>
                  </Button>
                </div>
              </div>
            )}
          </UniPanel>
        </div>

        {unassigned > 0 && (
          <div className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
            <strong>{unassigned}</strong> student{unassigned !== 1 ? 's' : ''} without a programme assignment.
          </div>
        )}
    </div>
  )
}
