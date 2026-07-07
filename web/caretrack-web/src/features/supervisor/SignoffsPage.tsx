import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { AlertTriangle, CheckCircle, ClipboardList, Stethoscope } from 'lucide-react'
import { api } from '@/lib/api-client'
import { notify } from '@/lib/notify'
import { authStore } from '@/lib/auth-store'
import { useDashboardAnimation } from '@/animations/useDashboardAnimation'
import { DashboardShell } from '@/components/layout/DashboardShell'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

interface LogbookEntry {
  id: string
  entryDate: string
  procedure: string
  patientCount: number
  notes: string
  location: string
  status: string
  studentName: string
  supervisorRemarks?: string
  isEscalated: boolean
}

interface SupervisorDashboard {
  pendingCount: number
  escalatedCount: number
  approvedToday: number
  pendingEntries: LogbookEntry[]
}

export function SignoffsPage() {
  const animRef = useDashboardAnimation()
  const queryClient = useQueryClient()
  const auth = authStore.get()!
  const [remarks, setRemarks] = useState<Record<string, string>>({})
  const [tab, setTab] = useState<'signoffs' | 'rotations' | 'content'>('signoffs')

  const { data } = useQuery({
    queryKey: ['supervisor-dashboard'],
    queryFn: async () => (await api.get<SupervisorDashboard>('/signoffs/dashboard')).data,
  })

  const signOff = useMutation({
    mutationFn: async ({ id, action }: { id: string; action: 'approve' | 'reject' }) =>
      api.post(`/signoffs/${id}`, { action, remarks: remarks[id] ?? '' }),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['supervisor-dashboard'] })
      notify.success(variables.action === 'approve' ? 'Entry approved.' : 'Entry rejected.')
    },
    onError: (err) => notify.error(err),
  })

  return (
    <DashboardShell
      accent="apollo"
      roleLabel="Faculty Portal"
      portalTitle="Pending Sign-offs"
      userName={auth.fullName}
      tenantLabel="Clinical supervisor"
      navItems={[{ label: 'Sign-offs', href: '/signoffs', icon: Stethoscope }]}
    >
      <div ref={animRef} className="mx-auto max-w-4xl space-y-6">
        <div className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
          <AlertTriangle className="mr-2 inline h-4 w-4" />
          Escalation: {data?.escalatedCount ?? 0} entries pending 7+ days — auto-escalate to Head of Dept.
        </div>

        <div className="flex gap-6 border-b border-[#e0e4d8]">
          {[
            { id: 'signoffs' as const, label: 'Pending Sign-offs' },
            { id: 'rotations' as const, label: 'Student Rotations' },
            { id: 'content' as const, label: 'Content Review' },
          ].map((t) => (
            <button
              key={t.id}
              type="button"
              onClick={() => setTab(t.id)}
              className={`pb-3 text-sm font-medium transition ${
                tab === t.id ? 'border-b-2 border-[#1a1d1f] text-[#1a1d1f]' : 'text-slate-400 hover:text-slate-600'
              }`}
            >
              {t.label}
            </button>
          ))}
        </div>

        {tab === 'signoffs' && (
          <section>
            <div className="mb-4 flex items-center justify-between">
              <h2 className="font-display text-xl font-bold text-[#1a1d1f]">Sign-off Queue</h2>
              <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">
                {data?.pendingCount ?? 0} Pending
              </span>
            </div>
            <div className="space-y-5">
              {data?.pendingEntries?.map((entry) => (
                <div key={entry.id} data-animate-card className="overflow-hidden rounded-2xl border border-[#e0e4d8] bg-white shadow-sm">
                  <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4">
                    <div className="flex items-center gap-3">
                      <div className="flex h-10 w-10 items-center justify-center rounded-full bg-[#c8e6d9] text-sm font-bold text-[#2d5f5a]">
                        {entry.studentName.split(' ').map((n) => n[0]).join('').slice(0, 2)}
                      </div>
                      <div>
                        <p className="font-semibold text-slate-900">{entry.studentName}</p>
                        <p className="text-xs text-slate-500">Student · {entry.entryDate}</p>
                      </div>
                    </div>
                    {entry.isEscalated && (
                      <span className="rounded-full bg-amber-100 px-2 py-0.5 text-[10px] font-bold text-amber-800">ESCALATED</span>
                    )}
                  </div>
                  <div className="bg-[#f0faf5] px-5 py-4">
                    <p className="font-display text-lg font-semibold text-[#1a3d38]">{entry.procedure}</p>
                    <p className="mt-2 text-sm leading-relaxed text-slate-600">{entry.notes}</p>
                    <p className="mt-2 text-xs text-slate-400">{entry.location} · {entry.patientCount} patients</p>
                  </div>
                  <div className="grid grid-cols-2 gap-3 p-4">
                    <Button
                      className="bg-[#2d5f5a] hover:bg-[#234a46]"
                      onClick={() => signOff.mutate({ id: entry.id, action: 'approve' })}
                      disabled={signOff.isPending}
                    >
                      <CheckCircle className="mr-2 h-4 w-4" /> Approve
                    </Button>
                    <Button
                      variant="outline"
                      onClick={() => signOff.mutate({ id: entry.id, action: 'reject' })}
                      disabled={signOff.isPending}
                    >
                      <ClipboardList className="mr-2 h-4 w-4" /> Remarks
                    </Button>
                  </div>
                  <div className="px-4 pb-4">
                    <Input
                      placeholder="Add remarks (required for reject)..."
                      value={remarks[entry.id] ?? ''}
                      onChange={(e) => setRemarks((r) => ({ ...r, [entry.id]: e.target.value }))}
                      className="rounded-xl"
                    />
                  </div>
                </div>
              ))}
              {!data?.pendingEntries?.length && (
                <p className="py-12 text-center text-slate-500">Queue is clear — well done!</p>
              )}
            </div>
          </section>
        )}

        {tab !== 'signoffs' && (
          <p className="py-12 text-center text-slate-500">Coming soon in next release.</p>
        )}
      </div>
    </DashboardShell>
  )
}
