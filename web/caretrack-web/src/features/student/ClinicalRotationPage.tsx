import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { AlertTriangle, MapPin, User } from 'lucide-react'
import { api } from '@/lib/api-client'
import { notify } from '@/lib/notify'
import { authStore } from '@/lib/auth-store'
import { STUDENT_NAV } from '@/lib/student-nav'
import { useDashboardAnimation } from '@/animations/useDashboardAnimation'
import { StudentShell } from '@/components/layout/StudentShell'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ProgressBar } from '@/components/ui/label'

export function ClinicalRotationPage() {
  const animRef = useDashboardAnimation()
  const auth = authStore.get()!
  const queryClient = useQueryClient()
  const [procedure, setProcedure] = useState('')
  const [location, setLocation] = useState('')
  const [patientCount, setPatientCount] = useState(1)
  const [notes, setNotes] = useState('')

  const { data: rotations } = useQuery({
    queryKey: ['my-rotations'],
    queryFn: async () => (await api.get('/students/me/rotations')).data,
  })

  const { data: logbook } = useQuery({
    queryKey: ['my-logbook'],
    queryFn: async () => (await api.get('/students/me/logbook')).data,
  })

  const active = rotations?.[0]
  const attendance = active?.attendancePercent ?? 0
  const procedures = active?.completedProcedureCount ?? 0
  const requiredProcedures = 5

  const createEntry = useMutation({
    mutationFn: async () =>
      api.post('/students/me/logbook', {
        rotationAssignmentId: active?.id,
        entryDate: new Date().toISOString().slice(0, 10),
        procedure,
        patientCount,
        notes,
        location,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['my-logbook'] })
      setProcedure('')
      setNotes('')
      notify.success('Logbook entry submitted.')
    },
    onError: (err) => notify.error(err),
  })

  return (
    <StudentShell
      userName={auth.fullName}
      tenantLabel="Meridian × Apollo"
      yearLabel="Year 1 · Clinical"
      navItems={STUDENT_NAV}
    >
      <div ref={animRef} className="mx-auto max-w-6xl space-y-6">
        <div className="rounded-xl border border-amber-200 bg-amber-50/90 px-4 py-3 text-sm text-amber-900">
          <AlertTriangle className="mr-2 inline h-4 w-4" />
          Entries pending &gt; 7 days will be escalated to Dept Coordinator.
        </div>

        <div data-animate-card className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <h1 className="font-display text-3xl font-bold text-[#1a1d1f]">
              Clinical Rotation: {active?.rotationName ?? 'Cardiology'}
            </h1>
            <div className="mt-2 flex flex-wrap gap-2">
              <span className="rounded-full bg-[#c8e6d9] px-2.5 py-0.5 text-xs font-semibold text-[#2d5f5a]">ACTIVE ROTATION</span>
              <span className="rounded-full bg-slate-100 px-2.5 py-0.5 text-xs text-slate-600">Week 3 of 6</span>
            </div>
            <div className="mt-2 flex gap-4 text-sm text-slate-500">
              <span className="flex items-center gap-1"><MapPin className="h-3.5 w-3.5" /> Apollo Main Hospital</span>
              <span className="flex items-center gap-1"><User className="h-3.5 w-3.5" /> Dr. Priya Sharma</span>
            </div>
          </div>
          <Button variant="outline" className="border-[#2d5f5a] text-[#2d5f5a]">Export Logbook</Button>
        </div>

        <div className="grid gap-6 lg:grid-cols-5">
          <div className="space-y-6 lg:col-span-2">
            <section data-animate-card className="rounded-2xl border border-[#e0e4d8] bg-white p-6 shadow-sm">
              <h3 className="font-semibold text-slate-900">Rotation Progress</h3>
              <div className="mt-4 space-y-4">
                <div>
                  <div className="mb-1 flex justify-between text-sm"><span>Attendance</span><span className="font-semibold">{attendance}%</span></div>
                  <ProgressBar value={attendance} />
                </div>
                <div>
                  <div className="mb-1 flex justify-between text-sm"><span>Procedures Met</span><span className="font-semibold">{procedures}/{requiredProcedures}</span></div>
                  <ProgressBar value={(procedures / requiredProcedures) * 100} />
                </div>
                <div>
                  <div className="mb-1 flex justify-between text-sm"><span>Sign-offs</span><span className="font-semibold">{logbook?.filter((e: { status: string }) => e.status === 'Approved').length ?? 0}/{logbook?.length ?? 0}</span></div>
                  <ProgressBar value={50} />
                </div>
              </div>
              <p className="mt-4 rounded-lg bg-[#fafbf7] p-3 text-xs text-slate-500">
                💡 Tip: Log entries same day for faster supervisor sign-off.
              </p>
            </section>
          </div>

          <div className="space-y-6 lg:col-span-3">
            <section data-animate-card className="rounded-2xl border border-[#e0e4d8] bg-white p-6 shadow-sm">
              <h3 className="font-semibold text-slate-900">Add New Entry</h3>
              <div className="mt-4 grid gap-4 sm:grid-cols-2">
                <div className="space-y-2 sm:col-span-2">
                  <Label>Procedure / Activity</Label>
                  <Input value={procedure} onChange={(e) => setProcedure(e.target.value)} placeholder="e.g. ECG Interpretation" />
                </div>
                <div className="space-y-2">
                  <Label>Location</Label>
                  <Input value={location} onChange={(e) => setLocation(e.target.value)} placeholder="Cardiology Ward" />
                </div>
                <div className="space-y-2">
                  <Label>Patient Count</Label>
                  <Input type="number" min={0} value={patientCount} onChange={(e) => setPatientCount(Number(e.target.value))} />
                </div>
                <div className="space-y-2 sm:col-span-2">
                  <Label>Clinical Notes</Label>
                  <textarea
                    className="min-h-[80px] w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#2d5f5a]"
                    value={notes}
                    onChange={(e) => setNotes(e.target.value)}
                    placeholder="Describe what you observed and learned..."
                  />
                </div>
              </div>
              <Button
                className="mt-4 bg-[#2d5f5a] hover:bg-[#234a46]"
                onClick={() => createEntry.mutate()}
                disabled={!procedure || createEntry.isPending}
              >
                Submit for Sign-off
              </Button>
            </section>

            <section data-animate-card className="rounded-2xl border border-[#e0e4d8] bg-white p-6 shadow-sm">
              <h3 className="font-semibold text-slate-900">Daily Logbook Entries</h3>
              <div className="mt-4 space-y-3">
                {logbook?.length === 0 && <p className="text-sm text-slate-500">No entries yet.</p>}
                {logbook?.map((e: { id: string; procedure: string; status: string; entryDate: string; location: string; notes: string; supervisorRemarks?: string }) => (
                  <div key={e.id} className="rounded-xl border border-slate-100 bg-[#fafbf7] p-4">
                    <div className="flex items-start justify-between">
                      <p className="font-medium text-slate-900">{e.procedure}</p>
                      <span className={`rounded-full px-2 py-0.5 text-[10px] font-bold uppercase ${
                        e.status === 'Approved' ? 'bg-emerald-100 text-emerald-700' :
                        e.status === 'Rejected' ? 'bg-red-100 text-red-700' :
                        'bg-amber-100 text-amber-700'
                      }`}>{e.status}</span>
                    </div>
                    <p className="mt-1 text-xs text-slate-500">{e.entryDate} · {e.location}</p>
                    <p className="mt-2 text-sm text-slate-600">{e.notes}</p>
                    {e.supervisorRemarks && <p className="mt-2 text-xs italic text-slate-400">Supervisor: {e.supervisorRemarks}</p>}
                  </div>
                ))}
              </div>
            </section>
          </div>
        </div>
      </div>
    </StudentShell>
  )
}
