import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Building2, Pencil, Plus, Trash2 } from 'lucide-react'
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
import { Modal } from '@/components/ui/modal'
import { FileUpload } from '@/components/ui/file-upload'

interface UniversityItem {
  id: string
  name: string
  domain: string
  isActive: boolean
  logoUrl?: string
  programmeIds?: string[]
}

export function UniversitiesPage() {
  const animRef = useDashboardAnimation()
  const auth = authStore.get()!
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [domain, setDomain] = useState('')
  const [programmeId, setProgrammeId] = useState('')
  const [logoFile, setLogoFile] = useState<File | null>(null)
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null)
  const [editOpen, setEditOpen] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editName, setEditName] = useState('')
  const [editDomain, setEditDomain] = useState('')
  const [editIsActive, setEditIsActive] = useState(true)
  const [editLogoUrl, setEditLogoUrl] = useState<string | null>(null)
  const [editLogoFile, setEditLogoFile] = useState<File | null>(null)
  const [editProgrammeIds, setEditProgrammeIds] = useState<string[]>([])

  const platformBranding = useQuery({
    queryKey: ['platform-branding'],
    queryFn: async () => (await api.get('/platform/branding')).data as { logoUrl?: string | null },
  })

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

  const uploadApolloLogo = useMutation({
    mutationFn: async (file: File) => {
      const form = new FormData()
      form.append('file', file)
      return api.post('/platform/logo', form, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
    },
    onSuccess: () => {
      notify.success('Apollo logo updated.')
      queryClient.invalidateQueries({ queryKey: ['platform-branding'] })
    },
    onError: (err) => notify.error(err),
  })

  const updateUniversity = useMutation({
    mutationFn: async () => {
      if (!editingId) throw new Error('No university selected')
      await api.put(`/universities/${editingId}`, {
        name: editName.trim(),
        domain: editDomain.trim().toLowerCase(),
        isActive: editIsActive,
      })
      if (editLogoFile) {
        const form = new FormData()
        form.append('file', editLogoFile)
        await api.post(`/universities/${editingId}/logo`, form, {
          headers: { 'Content-Type': 'multipart/form-data' },
        })
      }
      await api.put(`/universities/${editingId}/programmes`, {
        programmeIds: editProgrammeIds,
      })
    },
    onSuccess: () => {
      notify.success('University updated.')
      setEditOpen(false)
      setEditingId(null)
      setEditLogoFile(null)
      queryClient.invalidateQueries({ queryKey: ['universities'] })
    },
    onError: (err) => notify.error(err),
  })

  const deleteUniversity = useMutation({
    mutationFn: async (id: string) => api.delete(`/universities/${id}`),
    onSuccess: () => {
      notify.success('University deleted.')
      queryClient.invalidateQueries({ queryKey: ['universities'] })
    },
    onError: (err) => notify.error(err),
  })

  const deleteAllUniversities = useMutation({
    mutationFn: async () => (await api.delete('/universities')).data as {
      deleted: number
      failed: number
      errors: string[]
    },
    onSuccess: (data) => {
      if (data.failed > 0) {
        notify.info(`Deleted ${data.deleted}, failed ${data.failed}.`)
      } else {
        notify.success(`Deleted ${data.deleted} universities.`)
      }
      queryClient.invalidateQueries({ queryKey: ['universities'] })
    },
    onError: (err) => notify.error(err),
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

  async function openEdit(university: UniversityItem) {
    try {
      const { data } = await api.get(`/universities/${university.id}`)
      setEditingId(university.id)
      setEditName(data.name ?? university.name)
      setEditDomain(data.domain ?? university.domain)
      setEditIsActive(data.isActive ?? university.isActive)
      setEditLogoUrl(data.logoUrl ?? university.logoUrl ?? null)
      setEditLogoFile(null)
      setEditProgrammeIds(data.programmeIds ?? [])
      setEditOpen(true)
    } catch (err) {
      notify.error(err)
    }
  }

  function handleUpdate(e: React.FormEvent) {
    e.preventDefault()
    if (!editName.trim() || !editDomain.trim()) {
      notify.error('University name and domain are required.')
      return
    }
    updateUniversity.mutate()
  }

  function toggleEditProgramme(programmeId: string) {
    setEditProgrammeIds((prev) =>
      prev.includes(programmeId) ? prev.filter((id) => id !== programmeId) : [...prev, programmeId],
    )
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
        <Panel title="Apollo platform logo">
          <p className="mb-4 text-sm text-slate-600">
            Upload the logo shown in the Apollo admin sidebar and login screen.
          </p>
          <div className="flex flex-wrap items-start gap-6">
            {assetUrl(platformBranding.data?.logoUrl) && !uploadApolloLogo.isPending && (
              <img
                src={assetUrl(platformBranding.data?.logoUrl)!}
                alt="Apollo logo"
                className="h-14 object-contain"
              />
            )}
            <div className="min-w-[280px] flex-1">
              <FileUpload
                accept="image/*"
                hint="Shown in Apollo admin sidebar and login"
                onChange={(file) => {
                  if (file) uploadApolloLogo.mutate(file)
                }}
                disabled={uploadApolloLogo.isPending}
              />
              {uploadApolloLogo.isPending && (
                <p className="mt-2 text-sm text-slate-500">Uploading…</p>
              )}
            </div>
          </div>
        </Panel>

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
              <FileUpload
                accept="image/*"
                hint="Stored in Azure Blob storage"
                onChange={setLogoFile}
              />
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
          <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
            <p className="text-sm text-slate-500">
              {universities.data?.items?.length ?? 0} partner universities
            </p>
            {(universities.data?.items?.length ?? 0) > 0 && (
              <Button
                variant="outline"
                className="border-red-200 text-red-600 hover:bg-red-50"
                disabled={deleteAllUniversities.isPending}
                onClick={() => {
                  if (
                    !window.confirm(
                      'Delete ALL partner universities? Universities with enrolled students will be skipped.',
                    )
                  )
                    return
                  deleteAllUniversities.mutate()
                }}
              >
                <Trash2 className="mr-2 h-4 w-4" />
                {deleteAllUniversities.isPending ? 'Deleting…' : 'Delete all universities'}
              </Button>
            )}
          </div>
          <div className="grid gap-4 md:grid-cols-2">
            {universities.data?.items?.map((u: UniversityItem) => (
              <div
                key={u.id}
                data-animate-card
                className="relative flex items-start gap-3 rounded-xl border border-slate-100 bg-gradient-to-br from-white to-slate-50 p-5 transition hover:border-[#2081A1]/40 hover:shadow-md"
              >
                <Link to={`/apollo/universities/${u.id}`} className="flex flex-1 items-start gap-3">
                  <div className="flex h-12 w-12 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-[#2081A1]/10 p-1 text-[#2081A1]">
                    {assetUrl(u.logoUrl) ? (
                      <img src={assetUrl(u.logoUrl)!} alt="" className="h-full w-full object-contain" />
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
                <div className="flex shrink-0 flex-col gap-1">
                  <Button
                    variant="ghost"
                    size="sm"
                    className="text-[#2081A1] hover:bg-[#2081A1]/10"
                    onClick={(e) => {
                      e.preventDefault()
                      openEdit(u)
                    }}
                  >
                    <Pencil className="h-4 w-4" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="text-red-600 hover:bg-red-50 hover:text-red-700"
                    disabled={deleteUniversity.isPending}
                    onClick={(e) => {
                      e.preventDefault()
                      if (!window.confirm(`Delete ${u.name}?`)) return
                      deleteUniversity.mutate(u.id)
                    }}
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              </div>
            ))}
            {!universities.data?.items?.length && (
              <p className="text-sm text-slate-500 md:col-span-2">No universities yet. Create one above.</p>
            )}
          </div>
        </Panel>

        <Modal
          open={editOpen}
          onClose={() => {
            setEditOpen(false)
            setEditingId(null)
            setEditLogoFile(null)
          }}
          title="Edit university"
          size="lg"
        >
          <form onSubmit={handleUpdate} className="space-y-4">
            <div className="space-y-2">
              <Label>University name</Label>
              <Input
                className="h-11 rounded-xl"
                value={editName}
                onChange={(e) => setEditName(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label>Email domain</Label>
              <Input
                className="h-11 rounded-xl"
                value={editDomain}
                onChange={(e) => setEditDomain(e.target.value)}
              />
            </div>
            <label className="flex cursor-pointer items-center gap-2 text-sm text-slate-700">
              <input
                type="checkbox"
                checked={editIsActive}
                onChange={(e) => setEditIsActive(e.target.checked)}
                className="h-4 w-4 rounded border-slate-300 text-[#2081A1]"
              />
              Active partner
            </label>
            <div className="space-y-2">
              <Label>University logo</Label>
              <FileUpload
                accept="image/*"
                hint="Replace the partner university logo"
                previewUrl={editLogoFile ? undefined : assetUrl(editLogoUrl)}
                onChange={setEditLogoFile}
              />
            </div>
            <div className="space-y-2">
              <Label>Linked programmes</Label>
              <div className="max-h-48 space-y-2 overflow-y-auto rounded-xl border border-slate-200 p-3">
                {(programmes.data ?? []).map((p: { id: string; name: string; code: string }) => (
                  <label
                    key={p.id}
                    className={`flex cursor-pointer items-center gap-3 rounded-lg px-2 py-2 hover:bg-slate-50 ${
                      editProgrammeIds.includes(p.id) ? 'bg-[#2081A1]/5 ring-1 ring-[#2081A1]/30' : ''
                    }`}
                  >
                    <input
                      type="checkbox"
                      checked={editProgrammeIds.includes(p.id)}
                      onChange={() => toggleEditProgramme(p.id)}
                      className="h-4 w-4 rounded border-slate-300 text-[#2081A1]"
                    />
                    <span className="text-sm text-slate-800">
                      {p.name} ({p.code})
                    </span>
                  </label>
                ))}
              </div>
            </div>
            <div className="sticky bottom-0 flex gap-2 border-t border-slate-100 bg-white pt-4">
              <Button type="submit" className="bg-[#2081A1]" disabled={updateUniversity.isPending}>
                {updateUniversity.isPending ? 'Saving…' : 'Save changes'}
              </Button>
              <Button
                type="button"
                variant="outline"
                onClick={() => {
                  setEditOpen(false)
                  setEditingId(null)
                  setEditLogoFile(null)
                }}
              >
                Cancel
              </Button>
            </div>
          </form>
        </Modal>
      </div>
    </DashboardShell>
  )
}
