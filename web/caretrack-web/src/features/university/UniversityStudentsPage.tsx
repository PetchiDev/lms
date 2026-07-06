import { useQuery } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { BookMarked, Search } from 'lucide-react'
import { Link } from 'react-router-dom'
import { api } from '@/lib/api-client'
import { UniPanel } from '@/components/layout/UniversityShell'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

export function UniversityStudentsPage() {
  const [search, setSearch] = useState('')

  const students = useQuery({
    queryKey: ['students'],
    queryFn: async () => (await api.get('/enrolments/students', { params: { pageSize: 100 } })).data,
  })

  const filtered = useMemo(() => {
    const items = students.data?.items ?? []
    const q = search.trim().toLowerCase()
    if (!q) return items
    return items.filter(
      (s: { firstName: string; lastName: string; email: string; programmeName: string; cohortName: string }) =>
        `${s.firstName} ${s.lastName}`.toLowerCase().includes(q) ||
        s.email.toLowerCase().includes(q) ||
        s.programmeName?.toLowerCase().includes(q) ||
        s.cohortName?.toLowerCase().includes(q),
    )
  }, [students.data, search])

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
              </tr>
            </thead>
            <tbody>
              {filtered.length === 0 && (
                <tr>
                  <td colSpan={4} className="p-8 text-center text-slate-500">
                    No students found.
                  </td>
                </tr>
              )}
              {filtered.map((s: {
                id: string
                firstName: string
                lastName: string
                email: string
                programmeName: string
                cohortName: string
                status: string
              }) => (
                <tr key={s.id} className="border-t border-slate-100 transition hover:bg-slate-50/80">
                  <td className="p-4">
                    <p className="font-semibold text-slate-900">
                      {s.firstName} {s.lastName}
                    </p>
                    <p className="text-xs text-slate-500">{s.email}</p>
                  </td>
                  <td className="p-4">
                    <span className={s.programmeName ? 'text-[#004a8f] font-medium' : 'text-amber-600'}>
                      {s.programmeName || 'Unassigned'}
                    </span>
                  </td>
                  <td className="p-4 text-slate-700">{s.cohortName || '—'}</td>
                  <td className="p-4">
                    <span
                      className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${
                        s.status === 'Active'
                          ? 'bg-emerald-100 text-emerald-700'
                          : 'bg-amber-100 text-amber-700'
                      }`}
                    >
                      {s.status}
                    </span>
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
