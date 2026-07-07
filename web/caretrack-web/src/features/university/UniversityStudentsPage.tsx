import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { BookMarked, Pencil, Search } from 'lucide-react'
import { Link } from 'react-router-dom'
import { api } from '@/lib/api-client'
import { notify } from '@/lib/notify'
import { UniPanel } from '@/components/layout/UniversityShell'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Modal } from '@/components/ui/modal'

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

interface CohortOption {
  id: string
  name: string
  programmeName: string
}

const STATUS_OPTIONS = ['Active', 'Invited', 'Suspended'] as const

export function UniversityStudentsPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [editOpen, setEditOpen] = useState(false)
  const [editingStudentId, setEditingStudentId] = useState<string | null>(null)
  const [editFirstName, setEditFirstName] = useState('')
  const [editLastName, setEditLastName] = useState('')
  const [editEmail, setEditEmail] = useState('')
  const [editPassword, setEditPassword] = useState('')
  const [editCohortId, setEditCohortId] = useState('')
  const [editStatus, setEditStatus] = useState<string>('Active')

  const students = useQuery({
    queryKey: ['students'],
    queryFn: async () => (await api.get('/enrolments/students', { params: { pageSize: 100 } })).data,
  })

  const cohorts = useQuery({
    queryKey: ['cohorts'],
    queryFn: async () => (await api.get('/cohorts')).data as CohortOption[],
  })

  const updateStudent = useMutation({
    mutationFn: async () => {
      if (!editingStudentId) throw new Error('No student selected')
      return api.put(`/enrolments/students/${editingStudentId}`, {
        firstName: editFirstName.trim(),
        lastName: editLastName.trim(),
        email: editEmail.trim() || null,
        password: editPassword || null,
        cohortId: editCohortId || null,
        status: editStatus || null,
      })
    },
    onSuccess: () => {
      notify.success('Student updated.')
      setEditOpen(false)
      setEditingStudentId(null)
      setEditPassword('')
      queryClient.invalidateQueries({ queryKey: ['students'] })
    },
    onError: (err) => notify.error(err),
  })

  const filtered = useMemo(() => {
    const items: StudentRow[] = students.data?.items ?? []
    const q = search.trim().toLowerCase()
    if (!q) return items
    return items.filter(
      (s) =>
        `${s.firstName} ${s.lastName}`.toLowerCase().includes(q) ||
        s.email.toLowerCase().includes(q) ||
        s.programmeName?.toLowerCase().includes(q) ||
        s.cohortName?.toLowerCase().includes(q),
    )
  }, [students.data, search])

  function openEdit(student: StudentRow) {
    setEditingStudentId(student.studentId)
    setEditFirstName(student.firstName)
    setEditLastName(student.lastName)
    setEditEmail(student.email)
    setEditPassword('')
    setEditCohortId(student.cohortId)
    setEditStatus(student.status || 'Active')
    setEditOpen(true)
  }

  function handleUpdate(e: React.FormEvent) {
    e.preventDefault()
    if (!editFirstName.trim() || !editLastName.trim()) {
      notify.error('First name and last name are required.')
      return
    }
    updateStudent.mutate()
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div className="relative w-full max-w-md">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
          <Input
            placeholder="Search students…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9"
          />
        </div>
        <div className="flex gap-2">
          <Button variant="outline" asChild>
            <Link to="/admin/enrolment">Enrol student</Link>
          </Button>
          <Button asChild className="bg-[#004a8f] hover:bg-[#003a70]">
            <Link to="/admin/programmes">
              <BookMarked className="mr-2 h-4 w-4" />
              Assign programmes
            </Link>
          </Button>
        </div>
      </div>

      <UniPanel title={`Student roster (${filtered.length})`}>
        <div className="overflow-hidden rounded-xl border border-slate-200">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
              <tr>
                <th className="p-4">Student</th>
                <th className="p-4">Programme</th>
                <th className="p-4">Cohort</th>
                <th className="p-4">Status</th>
                <th className="p-4 text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              {filtered.length === 0 && (
                <tr>
                  <td colSpan={5} className="p-8 text-center text-slate-500">
                    No students found.
                  </td>
                </tr>
              )}
              {filtered.map((s) => (
                <tr key={s.studentId} className="border-t border-slate-100 transition hover:bg-slate-50/80">
                  <td className="p-4">
                    <p className="font-semibold text-slate-900">
                      {s.firstName} {s.lastName}
                    </p>
                    <p className="text-xs text-slate-500">{s.email}</p>
                  </td>
                  <td className="p-4">
                    <span className={s.programmeName ? 'font-medium text-[#004a8f]' : 'text-amber-600'}>
                      {s.programmeName || 'Unassigned'}
                    </span>
                  </td>
                  <td className="p-4 text-slate-700">{s.cohortName || '—'}</td>
                  <td className="p-4">
                    <span
                      className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${
                        s.status === 'Active'
                          ? 'bg-emerald-100 text-emerald-700'
                          : s.status === 'Suspended'
                            ? 'bg-red-100 text-red-700'
                            : 'bg-amber-100 text-amber-700'
                      }`}
                    >
                      {s.status}
                    </span>
                  </td>
                  <td className="p-4 text-right">
                    <Button
                      variant="outline"
                      size="sm"
                      className="border-[#004a8f]/30 text-[#004a8f] hover:bg-[#004a8f]/5"
                      onClick={() => openEdit(s)}
                    >
                      <Pencil className="mr-1.5 h-3.5 w-3.5" />
                      Edit
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </UniPanel>

      <Modal
        open={editOpen}
        onClose={() => {
          setEditOpen(false)
          setEditingStudentId(null)
        }}
        title="Edit student"
      >
        <form onSubmit={handleUpdate} className="space-y-4">
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
          <div className="space-y-2">
            <Label>Email</Label>
            <Input type="email" value={editEmail} onChange={(e) => setEditEmail(e.target.value)} />
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
          <div className="space-y-2">
            <Label>Cohort / programme</Label>
            <select
              className="h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-[#004a8f]"
              value={editCohortId}
              onChange={(e) => setEditCohortId(e.target.value)}
            >
              <option value="">Select cohort</option>
              {(cohorts.data ?? []).map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name} · {c.programmeName}
                </option>
              ))}
            </select>
          </div>
          <div className="space-y-2">
            <Label>Status</Label>
            <select
              className="h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-[#004a8f]"
              value={editStatus}
              onChange={(e) => setEditStatus(e.target.value)}
            >
              {STATUS_OPTIONS.map((status) => (
                <option key={status} value={status}>
                  {status}
                </option>
              ))}
            </select>
          </div>
          <div className="flex gap-2 pt-2">
            <Button
              type="submit"
              className="bg-[#004a8f] hover:bg-[#003a70]"
              disabled={updateStudent.isPending}
            >
              {updateStudent.isPending ? 'Saving…' : 'Save changes'}
            </Button>
            <Button type="button" variant="outline" onClick={() => setEditOpen(false)}>
              Cancel
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  )
}
