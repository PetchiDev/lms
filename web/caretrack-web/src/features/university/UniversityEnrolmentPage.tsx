import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useMemo, useState } from 'react'
import { Loader2, Upload, UserPlus } from 'lucide-react'
import { api, getErrorMessage } from '@/lib/api-client'
import { authStore } from '@/lib/auth-store'
import { UniPanel } from '@/components/layout/UniversityShell'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

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
}

export function UniversityEnrolmentPage() {
  const queryClient = useQueryClient()
  const auth = authStore.get()!
  const universityId = auth.universityId!

  const [email, setEmail] = useState('')
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [password, setPassword] = useState('')
  const [programmeId, setProgrammeId] = useState('')
  const [cohortId, setCohortId] = useState('')

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

  const linkedProgrammes = useMemo(() => {
    const ids = new Set(university.data?.programmeIds ?? [])
    return (programmes.data ?? []).filter((p) => ids.has(p.id))
  }, [programmes.data, university.data])

  const cohortsForProgramme = useMemo(
    () => (cohorts.data ?? []).filter((c) => c.programmeId === programmeId),
    [cohorts.data, programmeId],
  )

  useEffect(() => {
    if (linkedProgrammes.length === 1 && !programmeId) {
      setProgrammeId(linkedProgrammes[0].id)
    }
  }, [linkedProgrammes, programmeId])

  useEffect(() => {
    if (!programmeId) {
      setCohortId('')
      return
    }
    if (cohortsForProgramme.length === 1) {
      setCohortId(cohortsForProgramme[0].id)
    } else if (!cohortsForProgramme.some((c) => c.id === cohortId)) {
      setCohortId('')
    }
  }, [programmeId, cohortsForProgramme, cohortId])

  const createCohort = useMutation({
    mutationFn: async () =>
      api.post('/cohorts', {
        universityId,
        programmeId,
        name: `${new Date().getFullYear()} Intake`,
        intakeYear: new Date().getFullYear(),
        currentYear: 1,
        currentSemester: 1,
      }),
    onSuccess: (res) => {
      queryClient.invalidateQueries({ queryKey: ['cohorts'] })
      setCohortId(res.data.id)
    },
  })

  const createStudent = useMutation({
    mutationFn: async () =>
      api.post('/enrolments/students', { email, firstName, lastName, cohortId, password }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['students'] })
      queryClient.invalidateQueries({ queryKey: ['university-report'] })
      setEmail('')
      setFirstName('')
      setLastName('')
      setPassword('')
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

  const selectedProgramme = linkedProgrammes.find((p) => p.id === programmeId)

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <UniPanel title="Create new student">
        <p className="mb-5 text-sm text-slate-600">
          Pick a programme — cohort is selected automatically when only one exists. No invite email is sent; share login credentials with the student.
        </p>
        <div className="grid gap-4 md:grid-cols-2">
          <div className="space-y-2 md:col-span-2">
            <Label>Programme</Label>
            <select
              className="h-11 w-full rounded-xl border border-slate-200 px-3 text-sm"
              value={programmeId}
              onChange={(e) => setProgrammeId(e.target.value)}
            >
              <option value="">Select programme</option>
              {linkedProgrammes.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name} ({p.code})
                </option>
              ))}
            </select>
            {linkedProgrammes.length === 0 && (
              <p className="text-xs text-amber-600">
                No programmes linked yet. Ask Apollo Admin to link a programme to your university.
              </p>
            )}
          </div>

          {programmeId && (
            <div className="space-y-2 md:col-span-2">
              <Label>Cohort</Label>
              {cohortsForProgramme.length > 0 ? (
                <select
                  className="h-11 w-full rounded-xl border border-slate-200 px-3 text-sm"
                  value={cohortId}
                  onChange={(e) => setCohortId(e.target.value)}
                >
                  <option value="">Select cohort</option>
                  {cohortsForProgramme.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name} — {c.programmeName} ({c.intakeYear})
                    </option>
                  ))}
                </select>
              ) : (
                <div className="rounded-xl border border-dashed border-[#2081A1]/40 bg-[#2081A1]/5 p-4">
                  <p className="text-sm text-slate-600">
                    No cohort for <strong>{selectedProgramme?.name}</strong> yet.
                  </p>
                  <Button
                    className="mt-3 bg-[#004a8f] hover:bg-[#003a70]"
                    onClick={() => createCohort.mutate()}
                    disabled={createCohort.isPending}
                  >
                    {createCohort.isPending ? (
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    ) : null}
                    Create {new Date().getFullYear()} Intake cohort
                  </Button>
                  {createCohort.error && (
                    <p className="mt-2 text-sm text-red-600">{getErrorMessage(createCohort.error)}</p>
                  )}
                </div>
              )}
            </div>
          )}

          <div className="space-y-2">
            <Label>Email</Label>
            <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="student@college.edu" />
          </div>
          <div className="space-y-2">
            <Label>Password</Label>
            <Input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Min 8 chars, upper, lower, number"
            />
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
        {createStudent.error && (
          <p className="mt-3 text-sm text-red-600">{getErrorMessage(createStudent.error)}</p>
        )}
        {createStudent.isSuccess && (
          <p className="mt-3 text-sm text-emerald-600">Student created successfully.</p>
        )}
        <div className="mt-6 flex flex-wrap gap-3">
          <Button
            onClick={() => createStudent.mutate()}
            disabled={!email || !password || !firstName || !lastName || !cohortId || createStudent.isPending}
            className="bg-[#004a8f] hover:bg-[#003a70]"
          >
            <UserPlus className="mr-2 h-4 w-4" />
            Create student
          </Button>
          <Label className="inline-flex cursor-pointer items-center gap-2 rounded-xl border border-slate-200 px-4 py-2.5 text-sm hover:bg-slate-50">
            <Upload className="h-4 w-4" />
            CSV import
            <Input
              type="file"
              accept=".csv"
              className="hidden"
              onChange={(e) => e.target.files?.[0] && importCsv(e.target.files[0])}
            />
          </Label>
        </div>
        <p className="mt-3 text-xs text-slate-500">
          CSV columns: Email, FirstName, LastName. Optional Password (defaults to Student@123).
        </p>
      </UniPanel>
    </div>
  )
}
