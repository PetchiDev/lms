import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Save, Upload } from 'lucide-react'
import { api, getErrorMessage } from '@/lib/api-client'
import { UniPanel } from '@/components/layout/UniversityShell'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5193'

function assetUrl(url?: string | null) {
  if (!url) return null
  if (url.startsWith('http')) return url
  return `${API_BASE}${url}`
}

export function UniversityCertificateTemplatePage() {
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

  const uploadAsset = useMutation({
    mutationFn: async (file: File) => {
      const fd = new FormData()
      fd.append('file', file)
      return (await api.post('/certificates/template/assets', fd, { headers: { 'Content-Type': 'multipart/form-data' } })).data as { url: string }
    },
    onError: (err) => setMessage(getErrorMessage(err)),
  })

  async function handleAsset(file: File, field: 'logoUrl' | 'leftSignatureImageUrl' | 'rightSignatureImageUrl') {
    const res = await uploadAsset.mutateAsync(file)
    setForm((f) => ({ ...f, [field]: res.url }))
    setMessage('Asset uploaded.')
  }

  return (
    <div className="space-y-6">
      {message && (
        <div className="rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 shadow-sm">{message}</div>
      )}

      <UniPanel
        title="Certificate template"
        action={
          <Button onClick={() => save.mutate()} disabled={save.isPending}>
            <Save className="mr-2 h-4 w-4" /> Save
          </Button>
        }
      >
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2">
            <Label>Title</Label>
            <Input value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} />
          </div>
          <div className="space-y-2">
            <Label>Organization name</Label>
            <Input value={form.organizationName} onChange={(e) => setForm({ ...form, organizationName: e.target.value })} />
          </div>
          <div className="space-y-2">
            <Label>Tagline</Label>
            <Input value={form.tagline} onChange={(e) => setForm({ ...form, tagline: e.target.value })} />
          </div>
          <div className="space-y-2">
            <Label>Awarded-to label</Label>
            <Input value={form.awardedToLabel} onChange={(e) => setForm({ ...form, awardedToLabel: e.target.value })} />
          </div>
          <div className="space-y-2 sm:col-span-2">
            <Label>Body text</Label>
            <Input value={form.bodyText} onChange={(e) => setForm({ ...form, bodyText: e.target.value })} />
          </div>
          <div className="space-y-2">
            <Label>Date prefix</Label>
            <Input value={form.datePrefix} onChange={(e) => setForm({ ...form, datePrefix: e.target.value })} />
          </div>
          <div className="space-y-2">
            <Label>Location</Label>
            <Input value={form.location} onChange={(e) => setForm({ ...form, location: e.target.value })} />
          </div>
          <div className="space-y-2 sm:col-span-2">
            <Label>Footer location</Label>
            <Input value={form.footerLocation} onChange={(e) => setForm({ ...form, footerLocation: e.target.value })} />
          </div>
          <div className="space-y-2">
            <Label>Website</Label>
            <Input value={form.websiteUrl} onChange={(e) => setForm({ ...form, websiteUrl: e.target.value })} />
          </div>
          <div className="space-y-2">
            <Label>Primary color</Label>
            <Input value={form.primaryColor} onChange={(e) => setForm({ ...form, primaryColor: e.target.value })} />
          </div>
          <div className="space-y-2">
            <Label>Accent color</Label>
            <Input value={form.accentColor} onChange={(e) => setForm({ ...form, accentColor: e.target.value })} />
          </div>
          <div className="space-y-2">
            <Label>Left signatory title</Label>
            <Input value={form.leftSignatoryTitle} onChange={(e) => setForm({ ...form, leftSignatoryTitle: e.target.value })} />
          </div>
          <div className="space-y-2">
            <Label>Right signatory title</Label>
            <Input value={form.rightSignatoryTitle} onChange={(e) => setForm({ ...form, rightSignatoryTitle: e.target.value })} />
          </div>
        </div>
      </UniPanel>

      <UniPanel title="Assets (logo & signatures)">
        <div className="grid gap-4 sm:grid-cols-3">
          <div className="space-y-2">
            <Label>Logo</Label>
            {assetUrl(form.logoUrl) && <img src={assetUrl(form.logoUrl)!} alt="" className="h-16 w-full object-contain rounded-md border border-slate-200 bg-white" />}
            <Button variant="outline" className="w-full" asChild>
              <label>
                <Upload className="mr-2 h-4 w-4" /> Upload
                <input
                  type="file"
                  accept="image/*"
                  className="hidden"
                  onChange={(e) => {
                    const f = e.target.files?.[0]
                    if (f) void handleAsset(f, 'logoUrl')
                  }}
                />
              </label>
            </Button>
          </div>
          <div className="space-y-2">
            <Label>Left signature</Label>
            {assetUrl(form.leftSignatureImageUrl) && <img src={assetUrl(form.leftSignatureImageUrl)!} alt="" className="h-16 w-full object-contain rounded-md border border-slate-200 bg-white" />}
            <Button variant="outline" className="w-full" asChild>
              <label>
                <Upload className="mr-2 h-4 w-4" /> Upload
                <input
                  type="file"
                  accept="image/*"
                  className="hidden"
                  onChange={(e) => {
                    const f = e.target.files?.[0]
                    if (f) void handleAsset(f, 'leftSignatureImageUrl')
                  }}
                />
              </label>
            </Button>
          </div>
          <div className="space-y-2">
            <Label>Right signature</Label>
            {assetUrl(form.rightSignatureImageUrl) && <img src={assetUrl(form.rightSignatureImageUrl)!} alt="" className="h-16 w-full object-contain rounded-md border border-slate-200 bg-white" />}
            <Button variant="outline" className="w-full" asChild>
              <label>
                <Upload className="mr-2 h-4 w-4" /> Upload
                <input
                  type="file"
                  accept="image/*"
                  className="hidden"
                  onChange={(e) => {
                    const f = e.target.files?.[0]
                    if (f) void handleAsset(f, 'rightSignatureImageUrl')
                  }}
                />
              </label>
            </Button>
          </div>
        </div>
      </UniPanel>
    </div>
  )
}

