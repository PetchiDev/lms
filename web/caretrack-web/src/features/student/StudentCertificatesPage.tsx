import { useQuery } from '@tanstack/react-query'
import { Award, Download, ExternalLink } from 'lucide-react'
import { api } from '@/lib/api-client'
import { authStore } from '@/lib/auth-store'
import { STUDENT_NAV } from '@/lib/student-nav'
import { StudentShell } from '@/components/layout/StudentShell'
import { Button } from '@/components/ui/button'
import { Panel } from '@/components/layout/DashboardShell'

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5193'

function pdfHref(url?: string | null) {
  if (!url) return '#'
  if (url.startsWith('http')) return url
  return `${API_BASE}${url}`
}

export function StudentCertificatesPage() {
  const auth = authStore.get()!

  const { data: certificates, isLoading } = useQuery({
    queryKey: ['my-certificates'],
    queryFn: async () => (await api.get('/students/me/certificates')).data,
  })

  const items: {
    id: string
    certificateNumber: string
    programmeName: string
    issuedAt: string
    pdfBlobUrl?: string
  }[] = certificates ?? []

  return (
    <StudentShell
      userName={auth.fullName}
      tenantLabel="Meridian × Apollo"
      yearLabel="Certificates"
      navItems={STUDENT_NAV}
    >
      <div className="space-y-6">
        <div className="rounded-2xl border border-emerald-200 bg-gradient-to-br from-emerald-50 to-white p-6">
          <div className="flex items-center gap-2 text-emerald-700">
            <Award className="h-5 w-5" />
            <span className="text-xs font-semibold uppercase tracking-widest">Your achievements</span>
          </div>
          <h1 className="mt-2 font-display text-2xl font-bold text-slate-900">Certificates</h1>
          <p className="mt-1 text-sm text-slate-600">
            Certificates earned by passing programme assessments with 60% or above.
          </p>
        </div>

        {isLoading ? (
          <Panel title="Loading…"><p className="text-sm text-slate-500">Loading your certificates…</p></Panel>
        ) : items.length === 0 ? (
          <Panel title="No certificates yet">
            <p className="text-sm text-slate-600">
              Complete your module assessments and score at least 60% to earn your Apollo certificate.
            </p>
          </Panel>
        ) : (
          <div className="grid gap-4 md:grid-cols-2">
            {items.map((cert) => (
              <div
                key={cert.id}
                className="group overflow-hidden rounded-2xl border border-[#C9A227]/40 bg-gradient-to-br from-[#fffdf8] via-white to-[#2081A1]/5 shadow-sm transition hover:shadow-md"
              >
                <div className="border-b border-[#C9A227]/30 bg-[#003366]/5 px-5 py-4">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="text-[10px] font-bold uppercase tracking-widest text-[#C9A227]">Certificate of Completion</p>
                      <p className="mt-1 font-semibold text-[#003366]">{cert.programmeName}</p>
                    </div>
                    <Award className="h-8 w-8 text-[#C9A227]/80" />
                  </div>
                </div>
                <div className="space-y-3 px-5 py-4">
                  <div className="flex justify-between text-sm">
                    <span className="text-slate-500">Certificate No.</span>
                    <span className="font-mono font-medium text-slate-800">{cert.certificateNumber}</span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span className="text-slate-500">Issued</span>
                    <span className="text-slate-800">{new Date(cert.issuedAt).toLocaleDateString('en-IN', { day: 'numeric', month: 'long', year: 'numeric' })}</span>
                  </div>
                  <div className="flex gap-2 pt-2">
                    {cert.pdfBlobUrl && (
                      <>
                        <Button className="flex-1 bg-[#2081A1] hover:bg-[#1a6d89]" asChild>
                          <a href={pdfHref(cert.pdfBlobUrl)} download target="_blank" rel="noreferrer">
                            <Download className="mr-2 h-4 w-4" />Download PDF
                          </a>
                        </Button>
                        <Button variant="outline" asChild>
                          <a href={pdfHref(cert.pdfBlobUrl)} target="_blank" rel="noreferrer">
                            <ExternalLink className="h-4 w-4" />
                          </a>
                        </Button>
                      </>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </StudentShell>
  )
}
