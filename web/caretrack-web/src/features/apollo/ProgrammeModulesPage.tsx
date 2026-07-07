import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ChevronDown, Layers, Plus, Trash2 } from 'lucide-react'
import { getApolloNavItems } from '@/lib/apollo-nav'
import { api, getErrorMessage } from '@/lib/api-client'
import { notify } from '@/lib/notify'
import { authStore } from '@/lib/auth-store'
import { useDashboardAnimation } from '@/animations/useDashboardAnimation'
import { DashboardShell, Panel } from '@/components/layout/DashboardShell'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { cn } from '@/lib/utils'

interface ModuleRow {
  id: string
  title: string
  description: string
  sortOrder: number
}

interface SemesterRow {
  id: string
  semesterNumber: number
  name: string
  modules: ModuleRow[]
}

interface YearRow {
  id: string
  yearNumber: number
  name: string
  semesters: SemesterRow[]
}

export function ProgrammeModulesPage() {
  const animRef = useDashboardAnimation()
  const auth = authStore.get()!
  const queryClient = useQueryClient()
  const [programmeId, setProgrammeId] = useState('')
  const [semesterId, setSemesterId] = useState('')
  const [moduleTitle, setModuleTitle] = useState('')
  const [moduleDescription, setModuleDescription] = useState('')
  const [expandedYears, setExpandedYears] = useState<Record<string, boolean>>({})

  const programmes = useQuery({
    queryKey: ['programmes'],
    queryFn: async () => (await api.get('/programmes')).data as { id: string; name: string; code: string }[],
  })

  const structure = useQuery({
    queryKey: ['programme-structure', programmeId],
    queryFn: async () => (await api.get(`/programmes/${programmeId}`)).data as {
      id: string
      name: string
      years: YearRow[]
    },
    enabled: !!programmeId,
  })

  const semesters = useMemo(() => {
    if (!structure.data?.years) return []
    return structure.data.years.flatMap((y) =>
      y.semesters.map((s) => ({
        id: s.id,
        label: `Year ${y.yearNumber} · ${s.name}`,
      })),
    )
  }, [structure.data])

  const createModule = useMutation({
    mutationFn: async () =>
      api.post(`/programmes/semesters/${semesterId}/modules`, {
        title: moduleTitle.trim(),
        description: moduleDescription.trim(),
        sortOrder: 0,
      }),
    onSuccess: () => {
      notify.success('Module created.')
      setModuleTitle('')
      setModuleDescription('')
      queryClient.invalidateQueries({ queryKey: ['programme-structure', programmeId] })
    },
    onError: (err) => notify.error(err),
  })

  const deleteModule = useMutation({
    mutationFn: async (moduleId: string) => api.delete(`/programmes/modules/${moduleId}`),
    onSuccess: () => {
      notify.success('Module deleted.')
      queryClient.invalidateQueries({ queryKey: ['programme-structure', programmeId] })
    },
    onError: (err) => notify.error(err),
  })

  function toggleYear(yearId: string) {
    setExpandedYears((prev) => ({ ...prev, [yearId]: !prev[yearId] }))
  }

  function handleCreateModule() {
    if (!semesterId || !moduleTitle.trim()) {
      notify.error('Select a semester and enter a module title.')
      return
    }
    createModule.mutate()
  }

  function handleDeleteModule(moduleId: string, title: string) {
    if (!window.confirm(`Delete module "${title}" and all its lessons?`)) return
    deleteModule.mutate(moduleId)
  }

  return (
    <DashboardShell
      accent="apollo"
      roleLabel="Apollo Console"
      portalTitle="Programme Modules"
      userName={auth.fullName}
      tenantLabel="View & manage module structure"
      navItems={getApolloNavItems(auth.role === 'ApolloAdmin')}
    >
      <div ref={animRef} className="mx-auto max-w-5xl space-y-8">
        <Panel title="Select programme">
          <div className="max-w-xl space-y-2">
            <Label>Programme</Label>
            <select
              className="h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-[#2081A1]"
              value={programmeId}
              onChange={(e) => {
                setProgrammeId(e.target.value)
                setSemesterId('')
                setExpandedYears({})
              }}
            >
              <option value="">Choose a programme</option>
              {programmes.data?.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name} ({p.code})
                </option>
              ))}
            </select>
          </div>
        </Panel>

        {programmeId && (
          <>
            <Panel title={`Modules in ${structure.data?.name ?? 'programme'}`}>
              {!structure.data?.years?.length ? (
                <p className="text-sm text-slate-500">No years/semesters defined for this programme yet.</p>
              ) : (
                <div className="space-y-4">
                  {structure.data.years.map((year) => {
                    const open = expandedYears[year.id] ?? true
                    const moduleCount = year.semesters.reduce((n, s) => n + s.modules.length, 0)
                    return (
                      <div key={year.id} className="rounded-xl border border-slate-200 bg-white">
                        <button
                          type="button"
                          onClick={() => toggleYear(year.id)}
                          className="flex w-full items-center justify-between px-4 py-3 text-left"
                        >
                          <div className="flex items-center gap-2">
                            <Layers className="h-4 w-4 text-[#2081A1]" />
                            <span className="font-semibold text-slate-900">
                              Year {year.yearNumber} — {year.name}
                            </span>
                            <span className="text-xs text-slate-500">({moduleCount} modules)</span>
                          </div>
                          <ChevronDown className={cn('h-4 w-4 text-slate-400 transition', open && 'rotate-180')} />
                        </button>
                        {open && (
                          <div className="space-y-4 border-t border-slate-100 px-4 py-4">
                            {year.semesters.map((semester) => (
                              <div key={semester.id}>
                                <p className="mb-2 text-sm font-medium text-slate-700">
                                  Semester {semester.semesterNumber} — {semester.name}
                                </p>
                                {semester.modules.length === 0 ? (
                                  <p className="text-sm text-slate-400">No modules in this semester.</p>
                                ) : (
                                  <div className="space-y-2">
                                    {semester.modules.map((mod) => (
                                      <div
                                        key={mod.id}
                                        className="flex items-center justify-between gap-3 rounded-lg border border-slate-100 bg-slate-50 px-4 py-3"
                                      >
                                        <div>
                                          <p className="font-medium text-slate-900">{mod.title}</p>
                                          {mod.description && (
                                            <p className="text-xs text-slate-500">{mod.description}</p>
                                          )}
                                        </div>
                                        <Button
                                          variant="outline"
                                          size="sm"
                                          className="shrink-0 border-red-200 text-red-600 hover:bg-red-50"
                                          disabled={deleteModule.isPending}
                                          onClick={() => handleDeleteModule(mod.id, mod.title)}
                                        >
                                          <Trash2 className="mr-1 h-3.5 w-3.5" />
                                          Delete
                                        </Button>
                                      </div>
                                    ))}
                                  </div>
                                )}
                              </div>
                            ))}
                          </div>
                        )}
                      </div>
                    )
                  })}
                </div>
              )}
            </Panel>

            <Panel title="Create module">
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2 md:col-span-2">
                  <Label>Semester</Label>
                  <select
                    className="h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-[#2081A1]"
                    value={semesterId}
                    onChange={(e) => setSemesterId(e.target.value)}
                  >
                    <option value="">Select semester</option>
                    {semesters.map((s) => (
                      <option key={s.id} value={s.id}>
                        {s.label}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="space-y-2 md:col-span-2">
                  <Label>Module title</Label>
                  <Input
                    className="h-11 rounded-xl"
                    placeholder="e.g. Cardiovascular Assessment"
                    value={moduleTitle}
                    onChange={(e) => setModuleTitle(e.target.value)}
                  />
                </div>
                <div className="space-y-2 md:col-span-2">
                  <Label>Description (optional)</Label>
                  <Input
                    className="h-11 rounded-xl"
                    placeholder="Brief overview"
                    value={moduleDescription}
                    onChange={(e) => setModuleDescription(e.target.value)}
                  />
                </div>
              </div>
              {createModule.error && (
                <p className="mt-2 text-sm text-red-600">{getErrorMessage(createModule.error)}</p>
              )}
              <Button className="mt-4 bg-[#2081A1]" onClick={handleCreateModule} disabled={createModule.isPending}>
                <Plus className="mr-2 h-4 w-4" />
                {createModule.isPending ? 'Creating…' : 'Create module'}
              </Button>
            </Panel>
          </>
        )}
      </div>
    </DashboardShell>
  )
}
