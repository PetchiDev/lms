import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useMemo, useState } from 'react'
import { Loader2, UserPlus } from 'lucide-react'
import { api } from '@/lib/api-client'
import { notify } from '@/lib/notify'
import { authStore } from '@/lib/auth-store'
import { UniPanel } from '@/components/layout/UniversityShell'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { FileUpload } from '@/components/ui/file-upload'

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
      notify.success('Cohort created.')
    },
    onError: (err) => notify.error(err),
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
      notify.success('Student created.')
    },
    onError: (err) => notify.error(err),
  })

  const importStudents = useMutation({
    mutationFn: async (file: File) => {
      if (!cohortId) throw new Error('Select a cohort first.')
      const form = new FormData()
      form.append('file', file)
      return (await api.post(`/enrolments/students/import?cohortId=${cohortId}`, form, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })).data as {
        totalRows: number
        successCount: number
        failedCount: number
        errors: string[]
      }
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['students'] })
      queryClient.invalidateQueries({ queryKey: ['university-report'] })
      if (data.failedCount > 0) {
        notify.info(`Imported ${data.successCount}/${data.totalRows}. ${data.failedCount} row(s) failed.`)
      } else {
        notify.success(`Imported ${data.successCount} student(s).`)
      }
    },
    onError: (err) => notify.error(err),
  })

  function handleImportFile(file: File | null) {
    if (!file) return
    const name = file.name.toLowerCase()
    if (!name.endsWith('.csv') && !name.endsWith('.xlsx')) {
      notify.error('Only CSV or XLSX files are supported.')
      return
    }
    if (!cohortId) {
      notify.error('Select a cohort before importing.')
      return
    }
    importStudents.mutate(file)
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
        <div className="mt-6 flex flex-wrap gap-3">
          <Button
            onClick={() => createStudent.mutate()}
            disabled={!email || !password || !firstName || !lastName || !cohortId || createStudent.isPending}
            className="bg-[#004a8f] hover:bg-[#003a70]"
          >
            <UserPlus className="mr-2 h-4 w-4" />
            Create student
          </Button>
        </div>

        <div className="mt-8 border-t border-slate-100 pt-6">
          <Label className="mb-3 block text-base font-semibold text-slate-900">Bulk import (CSV or XLSX)</Label>
          <FileUpload
            accept=".csv,.xlsx,text/csv,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            hint="Columns: Email, FirstName, LastName · optional Password"
            disabled={!cohortId || importStudents.isPending}
            onChange={handleImportFile}
          />
          {importStudents.isPending && (
            <p className="mt-2 flex items-center gap-2 text-sm text-slate-500">
              <Loader2 className="h-4 w-4 animate-spin" />
              Importing students…
            </p>
          )}
          <p className="mt-3 text-xs text-slate-500">
            First row must be headers: Email, FirstName, LastName. Password is optional (defaults to Student@123).
          </p>
        </div>
      </UniPanel>
    </div>
  )
}
