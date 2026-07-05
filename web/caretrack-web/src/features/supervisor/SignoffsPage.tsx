import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CheckCircle, Clock, Stethoscope, XCircle } from 'lucide-react'
import { api, getErrorMessage } from '@/lib/api-client'
import { authStore } from '@/lib/auth-store'
import { useDashboardAnimation } from '@/animations/useDashboardAnimation'
import { StatCard } from '@/components/dashboard/StatCard'
import { DashboardShell, Panel } from '@/components/layout/DashboardShell'
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
  submittedAt: string
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

  const { data } = useQuery({
    queryKey: ['supervisor-dashboard'],
    queryFn: async () => (await api.get<SupervisorDashboard>('/signoffs/dashboard')).data,
  })

  const signOff = useMutation({
    mutationFn: async ({ id, action }: { id: string; action: 'approve' | 'reject' }) =>
      api.post(`/signoffs/${id}`, { action, remarks: remarks[id] ?? '' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['supervisor-dashboard'] }),
  })

  return (
    <DashboardShell
      accent="indigo"
      roleLabel="Clinical Supervisor"
      portalTitle="Sign-off Queue"
      userName={auth.fullName}
      tenantLabel="Mobile-first review · delegate on leave"
      navItems={[
        { label: 'Pending sign-offs', href: '/signoffs', icon: Stethoscope },
      ]}
    >
      <div ref={animRef} className="space-y-8">
        <div className="grid gap-4 sm:grid-cols-3">
          <StatCard title="Pending" value={data?.pendingCount ?? 0} icon={Clock} accent="amber" />
          <StatCard title="Escalated (7d+)" value={data?.escalatedCount ?? 0} change="Dept coordinator notified" icon={Stethoscope} accent="rose" trend={data?.escalatedCount ? 'down' : 'neutral'} />
          <StatCard title="Approved today" value={data?.approvedToday ?? 0} icon={CheckCircle} accent="teal" trend="up" />
        </div>

        <Panel title="Pending logbook entries">
          <div className="space-y-4">
            {data?.pendingEntries?.length === 0 && (
              <p className="text-sm text-slate-500">No pending entries — queue is clear.</p>
            )}
            {data?.pendingEntries?.map((entry) => (
              <div
                key={entry.id}
                data-animate-card
                className={`rounded-xl border p-5 ${entry.isEscalated ? 'border-amber-300 bg-amber-50/50' : 'border-slate-200 bg-white'}`}
              >
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <p className="font-semibold text-slate-900">{entry.studentName}</p>
                    <p className="text-sm text-indigo-600">{entry.procedure} · {entry.entryDate}</p>
                    <p className="mt-1 text-sm text-slate-600">{entry.notes}</p>
                    <p className="text-xs text-slate-400">{entry.location} · {entry.patientCount} patients</p>
                    {entry.isEscalated && (
                      <span className="mt-2 inline-block rounded-full bg-amber-100 px-2 py-0.5 text-xs text-amber-800">Escalated — 7+ days pending</span>
                    )}
                  </div>
                  <span className="rounded-full bg-slate-100 px-2 py-1 text-xs">{entry.status}</span>
                </div>
                <Input
                  placeholder="Remarks (required for reject)"
                  className="mt-3"
                  value={remarks[entry.id] ?? ''}
                  onChange={(e) => setRemarks((r) => ({ ...r, [entry.id]: e.target.value }))}
                />
                <div className="mt-3 flex gap-2">
                  <Button
                    size="sm"
                    onClick={() => signOff.mutate({ id: entry.id, action: 'approve' })}
                    disabled={signOff.isPending}
                  >
                    <CheckCircle className="mr-1 h-4 w-4" />Approve
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => signOff.mutate({ id: entry.id, action: 'reject' })}
                    disabled={signOff.isPending}
                  >
                    <XCircle className="mr-1 h-4 w-4" />Reject
                  </Button>
                </div>
                {signOff.error && <p className="mt-2 text-sm text-red-600">{getErrorMessage(signOff.error)}</p>}
              </div>
            ))}
          </div>
        </Panel>
      </div>
    </DashboardShell>
  )
}
