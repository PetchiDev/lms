import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { Award, Save, Upload } from 'lucide-react'
import { api, getErrorMessage } from '@/lib/api-client'
import { authStore } from '@/lib/auth-store'
import { getApolloNavItems } from '@/lib/apollo-nav'
import { useDashboardAnimation } from '@/animations/useDashboardAnimation'
import { DashboardShell, Panel } from '@/components/layout/DashboardShell'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5193'

function assetUrl(url?: string | null) {
  if (!url) return null
  if (url.startsWith('http')) return url
  return `${API_BASE}${url}`
}

export function CertificateTemplatePage() {
  const animRef = useDashboardAnimation()
  const auth = authStore.get()!
  const isAdmin = auth.role === 'ApolloAdmin'
  const queryClient = useQueryClient()
  const [message, setMessage] = useState<string | null>(null)

  const template = useQuery({
    queryKey: ['certificate-template'],
    queryFn: async () => (await api.get('/certificates/template')).data,
  })

  const [form, setForm] = useState({
    title: '',
    organizationName: '',
    tagline: '',
    awardedToLabel: '',
    bodyText: '',
    datePrefix: '',
    location: '',
    footerLocation: '',
    websiteUrl: '',
    leftSignatoryTitle: '',
    rightSignatoryTitle: '',
    logoUrl: '',
    leftSignatureImageUrl: '',
    rightSignatureImageUrl: '',
    primaryColor: '#003366',
    accentColor: '#C9A227',
  })

  useEffect(() => {
    if (!template.data) return
    setForm({
      title: template.data.title ?? '',
      organizationName: template.data.organizationName ?? '',
      tagline: template.data.tagline ?? '',
      awardedToLabel: template.data.awardedToLabel ?? '',
      bodyText: template.data.bodyText ?? '',
      datePrefix: template.data.datePrefix ?? '',
      location: template.data.location ?? '',
      footerLocation: template.data.footerLocation ?? '',
      websiteUrl: template.data.websiteUrl ?? '',
      leftSignatoryTitle: template.data.leftSignatoryTitle ?? '',
      rightSignatoryTitle: template.data.rightSignatoryTitle ?? '',
      logoUrl: template.data.logoUrl ?? '',
      leftSignatureImageUrl: template.data.leftSignatureImageUrl ?? '',
      rightSignatureImageUrl: template.data.rightSignatureImageUrl ?? '',
      primaryColor: template.data.primaryColor ?? '#003366',
      accentColor: template.data.accentColor ?? '#C9A227',
    })
  }, [template.data])

  const save = useMutation({
    mutationFn: async () => (await api.put('/certificates/template', form)).data,
    onSuccess: () => {
      setMessage('Certificate template saved.')
      queryClient.invalidateQueries({ queryKey: ['certificate-template'] })
    },
    onError: (err) => setMessage(getErrorMessage(err)),
  })

  async function uploadAsset(file: File, field: 'logoUrl' | 'leftSignatureImageUrl' | 'rightSignatureImageUrl') {
    const fd = new FormData()
    fd.append('file', file)
    const { data } = await api.post('/certificates/template/assets', fd, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    setForm((prev) => ({ ...prev, [field]: data.url }))
  }

  function field(id: keyof typeof form, label: string, multiline = false) {
    return (
      <div className="space-y-1">
        <Label htmlFor={id}>{label}</Label>
        {multiline ? (
          <textarea
            id={id}
            className="min-h-[80px] w-full rounded-xl border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#2081A1] disabled:bg-slate-100"
            value={form[id]}
            disabled={!isAdmin}
            onChange={(e) => setForm((p) => ({ ...p, [id]: e.target.value }))}
          />
        ) : (
          <Input
            id={id}
            value={form[id]}
            disabled={!isAdmin}
            onChange={(e) => setForm((p) => ({ ...p, [id]: e.target.value }))}
          />
        )}
      </div>
    )
  }

  return (
    <DashboardShell
      accent="apollo"
      roleLabel="Apollo Console"
      portalTitle="Certificate Template"
      userName={auth.fullName}
      tenantLabel="PDF layout · branding · signatories"
      navItems={getApolloNavItems(isAdmin)}
    >
      <div ref={animRef} className="space-y-6">
        <div className="rounded-2xl border border-[#2081A1]/20 bg-gradient-to-br from-[#2081A1]/5 to-amber-50/40 p-6">
          <div className="flex items-center gap-2 text-[#2081A1]">
            <Award className="h-5 w-5" />
            <span className="text-xs font-semibold uppercase tracking-widest">Apollo Certificate</span>
          </div>
          <h1 className="mt-2 font-display text-2xl font-bold text-slate-900">Configure certificate template</h1>
          <p className="mt-1 text-sm text-slate-600">
            Students receive a landscape Apollo Hospitals certificate PDF when they pass assessments (≥60%). Use {'{ProgrammeName}'} and {'{StudentName}'} in body text.
          </p>
        </div>

        <div className="grid gap-6 xl:grid-cols-[1fr_380px]">
          <div className="space-y-6">
            <Panel title="Header & title">
              <div className="grid gap-4 md:grid-cols-2">
                {field('organizationName', 'Organization name')}
                {field('tagline', 'Tagline')}
                {field('title', 'Certificate title')}
                {field('awardedToLabel', 'Awarded-to label')}
              </div>
            </Panel>

            <Panel title="Body & date">
              {field('bodyText', 'Body text', true)}
              <div className="mt-4 grid gap-4 md:grid-cols-2">
                {field('datePrefix', 'Date prefix (e.g. Given at)')}
                {field('location', 'Location (e.g. Chennai, India)')}
              </div>
            </Panel>

            <Panel title="Footer & signatories">
              <div className="grid gap-4 md:grid-cols-2">
                {field('footerLocation', 'Footer location line')}
                {field('websiteUrl', 'Website URL')}
                {field('leftSignatoryTitle', 'Left signatory title')}
                {field('rightSignatoryTitle', 'Right signatory title')}
              </div>
            </Panel>

            <Panel title="Brand colors">
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-1">
                  <Label>Primary color</Label>
                  <div className="flex gap-2">
                    <input type="color" value={form.primaryColor} disabled={!isAdmin} onChange={(e) => setForm((p) => ({ ...p, primaryColor: e.target.value }))} className="h-11 w-14 rounded-lg border" />
                    <Input value={form.primaryColor} disabled={!isAdmin} onChange={(e) => setForm((p) => ({ ...p, primaryColor: e.target.value }))} />
                  </div>
                </div>
                <div className="space-y-1">
                  <Label>Accent (gold) color</Label>
                  <div className="flex gap-2">
                    <input type="color" value={form.accentColor} disabled={!isAdmin} onChange={(e) => setForm((p) => ({ ...p, accentColor: e.target.value }))} className="h-11 w-14 rounded-lg border" />
                    <Input value={form.accentColor} disabled={!isAdmin} onChange={(e) => setForm((p) => ({ ...p, accentColor: e.target.value }))} />
                  </div>
                </div>
              </div>
            </Panel>

            {isAdmin && (
              <div className="flex items-center justify-between gap-4">
                {message && <p className={`text-sm ${message.includes('saved') ? 'text-emerald-600' : 'text-red-600'}`}>{message}</p>}
                <Button className="ml-auto bg-[#2081A1] hover:bg-[#1a6d89]" disabled={save.isPending} onClick={() => { setMessage(null); save.mutate() }}>
                  <Save className="mr-2 h-4 w-4" />{save.isPending ? 'Saving…' : 'Save template'}
                </Button>
              </div>
            )}
          </div>

          <div className="space-y-6">
            <Panel title="Live preview">
              <div
                className="relative overflow-hidden rounded-xl border-4 p-4 text-center shadow-inner"
                style={{ borderColor: form.accentColor, background: 'linear-gradient(180deg, #fffdf8 0%, #fff 100%)' }}
              >
                {assetUrl(form.logoUrl) ? (
                  <img src={assetUrl(form.logoUrl)!} alt="Logo" className="mx-auto h-10 object-contain" />
                ) : (
                  <img src="/apollo_logo.png" alt="Apollo" className="mx-auto h-10 object-contain" />
                )}
                <p className="mt-1 text-[10px] tracking-widest" style={{ color: form.accentColor }}>{form.tagline}</p>
                <p className="mt-3 text-sm font-bold tracking-wide" style={{ color: form.primaryColor }}>{form.title}</p>
                <p className="mt-3 text-xs italic" style={{ color: form.primaryColor }}>{form.awardedToLabel}</p>
                <p className="mt-1 text-lg font-bold" style={{ color: form.primaryColor }}>STUDENT NAME</p>
                <p className="mt-3 px-2 text-[10px] leading-relaxed text-slate-700">
                  {form.bodyText.replace('{ProgrammeName}', 'B.Sc Nursing').replace('{StudentName}', 'Student Name')}
                </p>
                <p className="mt-2 text-[10px] text-slate-600">{form.datePrefix} {form.location}</p>
                <p className="mt-4 text-[9px] font-semibold" style={{ color: form.primaryColor }}>{form.footerLocation}</p>
              </div>
            </Panel>

            {isAdmin && (
              <Panel title="Images">
                <div className="space-y-4">
                  {([
                    ['logoUrl', 'Logo'],
                    ['leftSignatureImageUrl', 'Left signature'],
                    ['rightSignatureImageUrl', 'Right signature'],
                  ] as const).map(([key, label]) => (
                    <div key={key} className="space-y-2">
                      <Label>{label}</Label>
                      {form[key] && assetUrl(form[key]) && (
                        <img src={assetUrl(form[key])!} alt={label} className="h-12 object-contain" />
                      )}
                      <label className="flex cursor-pointer items-center gap-2 rounded-xl border border-dashed border-slate-300 px-4 py-3 text-sm text-slate-600 hover:bg-slate-50">
                        <Upload className="h-4 w-4" />
                        Upload {label.toLowerCase()}
                        <input type="file" accept="image/*" className="hidden" onChange={(e) => e.target.files?.[0] && uploadAsset(e.target.files[0], key)} />
                      </label>
                    </div>
                  ))}
                </div>
              </Panel>
            )}
          </div>
        </div>
      </div>
    </DashboardShell>
  )
}
