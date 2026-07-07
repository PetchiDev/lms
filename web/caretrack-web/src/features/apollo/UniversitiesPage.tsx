import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Building2, Plus } from 'lucide-react'
import { getApolloNavItems } from '@/lib/apollo-nav'
import { api, getErrorMessage } from '@/lib/api-client'
import { notify } from '@/lib/notify'
import { assetUrl } from '@/lib/asset-url'
import { authStore } from '@/lib/auth-store'
import { useDashboardAnimation } from '@/animations/useDashboardAnimation'
import { DashboardShell, Panel } from '@/components/layout/DashboardShell'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

export function UniversitiesPage() {
  const animRef = useDashboardAnimation()
  const auth = authStore.get()!
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [domain, setDomain] = useState('')
  const [programmeId, setProgrammeId] = useState('')
  const [logoFile, setLogoFile] = useState<File | null>(null)
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null)

  const universities = useQuery({
    queryKey: ['universities'],
    queryFn: async () => (await api.get('/universities', { params: { page: 1, pageSize: 100 } })).data,
  })

  const programmes = useQuery({
    queryKey: ['programmes'],
    queryFn: async () => (await api.get('/programmes')).data,
  })

  const createUniversity = useMutation({
    mutationFn: async () => {
      const { data } = await api.post('/universities', {
        name: name.trim(),
        domain: domain.trim().toLowerCase(),
        programmeId: programmeId || null,
      })
      if (logoFile) {
        const form = new FormData()
        form.append('file', logoFile)
        await api.post(`/universities/${data.id}/logo`, form, {
          headers: { 'Content-Type': 'multipart/form-data' },
        })
      }
      return data
    },
    onSuccess: () => {
      notify.success(`${name} created.`)
      setName('')
      setDomain('')
      setProgrammeId('')
      setLogoFile(null)
      setMessage(null)
      queryClient.invalidateQueries({ queryKey: ['universities'] })
    },
    onError: (err) => {
      const msg = getErrorMessage(err)
      setMessage({ type: 'error', text: msg })
      notify.error(err)
    },
  })

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setMessage(null)
    if (!name.trim() || !domain.trim()) {
      const msg = 'University name and domain are required.'
      setMessage({ type: 'error', text: msg })
      notify.error(msg)
      return
    }
    createUniversity.mutate()
  }

  return (
    <DashboardShell
      accent="apollo"
      roleLabel="Apollo Admin"
      portalTitle="Partner Universities"
      userName={auth.fullName}
      tenantLabel="Onboard & manage tenants"
      navItems={getApolloNavItems(true)}
    >
      <div ref={animRef} className="mx-auto max-w-4xl space-y-8">
        <Panel title="Create new university">
          <form onSubmit={handleSubmit} className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="uni-name">University name</Label>
              <Input
                id="uni-name"
                className="h-11 rounded-xl"
                placeholder="e.g. Sunrise Medical College"
                value={name}
                onChange={(e) => setName(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="uni-domain">Email domain</Label>
              <Input
                id="uni-domain"
                className="h-11 rounded-xl"
                placeholder="e.g. sunrise.edu"
                value={domain}
                onChange={(e) => setDomain(e.target.value)}
              />
              <p className="text-xs text-slate-500">Students & staff sign in with @{domain || 'domain.edu'}</p>
            </div>
            <div className="space-y-2 md:col-span-2">
              <Label htmlFor="uni-logo">University logo (optional)</Label>
              <div className="flex flex-wrap items-center gap-4">
                <Input
                  id="uni-logo"
                  type="file"
                  accept="image/*"
                  className="h-11 rounded-xl"
                  onChange={(e) => setLogoFile(e.target.files?.[0] ?? null)}
                />
                {logoFile && (
                  <img
                    src={URL.createObjectURL(logoFile)}
                    alt="Logo preview"
                    className="h-12 w-12 rounded-lg border object-contain"
                  />
                )}
              </div>
              <p className="text-xs text-slate-500">Stored in Azure Blob storage.</p>
            </div>
            <div className="space-y-2 md:col-span-2">
              <Label>Link programme (optional)</Label>
              <select
                className="h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-[#2081A1]"
                value={programmeId}
                onChange={(e) => setProgrammeId(e.target.value)}
              >
                <option value="">No programme linked yet</option>
                {programmes.data?.map((p: { id: string; name: string; code: string }) => (
                  <option key={p.id} value={p.id}>{p.name} ({p.code})</option>
                ))}
              </select>
            </div>
            {message && (
              <p className={`md:col-span-2 text-sm ${message.type === 'success' ? 'text-emerald-600' : 'text-red-600'}`}>
                {message.text}
              </p>
            )}
            <div className="md:col-span-2">
              <Button type="submit" className="bg-[#2081A1]" disabled={createUniversity.isPending}>
                <Plus className="mr-2 h-4 w-4" />
                {createUniversity.isPending ? 'Creating…' : 'Create university'}
              </Button>
            </div>
          </form>
        </Panel>

        <Panel title="All partner universities">
          <div className="grid gap-4 md:grid-cols-2">
            {universities.data?.items?.map((u: { id: string; name: string; domain: string; isActive: boolean; logoUrl?: string }) => (
              <Link
                key={u.id}
                to={`/apollo/universities/${u.id}`}
                data-animate-card
                className="flex items-start gap-3 rounded-xl border border-slate-100 bg-gradient-to-br from-white to-slate-50 p-5 transition hover:border-[#2081A1]/40 hover:shadow-md"
              >
                <div className="rounded-lg bg-[#2081A1]/10 p-2 text-[#2081A1]">
                  {assetUrl(u.logoUrl) ? (
                    <img src={assetUrl(u.logoUrl)!} alt="" className="h-5 w-5 object-contain" />
                  ) : (
                    <Building2 className="h-5 w-5" />
                  )}
                </div>
                <div>
                  <p className="font-semibold text-slate-900">{u.name}</p>
                  <p className="text-sm text-slate-500">{u.domain}</p>
                  <span className={`mt-2 inline-block rounded-full px-2 py-0.5 text-xs ${u.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-600'}`}>
                    {u.isActive ? 'Active' : 'Inactive'}
                  </span>
                </div>
              </Link>
            ))}
            {!universities.data?.items?.length && (
              <p className="text-sm text-slate-500 md:col-span-2">No universities yet. Create one above.</p>
            )}
          </div>
        </Panel>
      </div>
    </DashboardShell>
  )
}
