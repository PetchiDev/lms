import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useMemo, useState } from 'react'
import { CheckCircle2, FileStack, Plus, Send, Trash2, Upload } from 'lucide-react'
import { getApolloNavItems } from '@/lib/apollo-nav'
import { api, getErrorMessage } from '@/lib/api-client'
import { notify } from '@/lib/notify'
import { authStore } from '@/lib/auth-store'
import { useDashboardAnimation } from '@/animations/useDashboardAnimation'
import { DashboardShell, Panel } from '@/components/layout/DashboardShell'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Modal } from '@/components/ui/modal'

interface SemesterOption {
  id: string
  label: string
}

interface YearOption {
  id: string
  yearNumber: number
  name: string
}

const NEW_YEAR = '__new__'

function yearLabel(yearNumber: number, name: string) {
  return name.trim() === `Year ${yearNumber}` ? `Year ${yearNumber}` : `Year ${yearNumber} — ${name}`
}

function LabelWithAdd({ label, onAdd, disabled }: { label: string; onAdd: () => void; disabled?: boolean }) {
  return (
    <div className="flex items-center gap-2">
      <Label>{label}</Label>
      <button
        type="button"
        onClick={onAdd}
        disabled={disabled}
        title={`Create new ${label.toLowerCase()}`}
        className="flex h-6 w-6 items-center justify-center rounded-md border border-[#2081A1]/30 bg-[#2081A1]/10 text-[#2081A1] transition hover:bg-[#2081A1]/20 disabled:cursor-not-allowed disabled:opacity-40"
      >
        <Plus className="h-3.5 w-3.5" />
      </button>
    </div>
  )
}

export function ContentLibraryPage() {
  const animRef = useDashboardAnimation()
  const auth = authStore.get()!
  const isAdmin = auth.role === 'ApolloAdmin'
  const queryClient = useQueryClient()

  // Module creation
  const [programmeId, setProgrammeId] = useState('')
  const [semesterId, setSemesterId] = useState('')
  const [moduleTitle, setModuleTitle] = useState('')
  const [moduleDescription, setModuleDescription] = useState('')

  // Existing content mapping
  const [existingModuleId, setExistingModuleId] = useState('')
  const [filterProgrammeId, setFilterProgrammeId] = useState('')

  // Lesson creation
  const [moduleId, setModuleId] = useState('')
  const [lessonTitle, setLessonTitle] = useState('')
  const [selectedLesson, setSelectedLesson] = useState<string | null>(null)

  // Publish
  const [selectedUniversities, setSelectedUniversities] = useState<string[]>([])
  const [publishMessage, setPublishMessage] = useState<string | null>(null)
  const [uploadMessage, setUploadMessage] = useState<string | null>(null)
  const [workflowMessage, setWorkflowMessage] = useState<string | null>(null)

  const selectedLessonQuery = useQuery({
    queryKey: ['content-lesson', selectedLesson],
    queryFn: async () => (await api.get(`/content/lessons/${selectedLesson}`)).data,
    enabled: !!selectedLesson,
  })

  const lessonStatus: string = selectedLessonQuery.data?.status ?? 'Draft'

  // Programme / Semester modals
  const [programmeModalOpen, setProgrammeModalOpen] = useState(false)
  const [semesterModalOpen, setSemesterModalOpen] = useState(false)
  const [newProgrammeName, setNewProgrammeName] = useState('')
  const [newProgrammeCode, setNewProgrammeCode] = useState('')
  const [newProgrammeDesc, setNewProgrammeDesc] = useState('')
  const [newProgrammeYears, setNewProgrammeYears] = useState('3')
  const [newSemesterYearId, setNewSemesterYearId] = useState('')
  const [newYearNumber, setNewYearNumber] = useState('1')
  const [newYearName, setNewYearName] = useState('Year 1')
  const [newSemesterNumber, setNewSemesterNumber] = useState('1')
  const [newSemesterName, setNewSemesterName] = useState('')
  const [modalError, setModalError] = useState<string | null>(null)

  const programmes = useQuery({
    queryKey: ['programmes'],
    queryFn: async () => (await api.get('/programmes')).data,
  })

  const programmeStructure = useQuery({
    queryKey: ['programme-structure', programmeId],
    queryFn: async () => (await api.get(`/programmes/${programmeId}`)).data,
    enabled: !!programmeId,
  })

  const semesters: SemesterOption[] = useMemo(() => {
    if (!programmeStructure.data?.years) return []
    return programmeStructure.data.years.flatMap(
      (y: { yearNumber: number; semesters: { id: string; semesterNumber: number; name: string }[] }) =>
        y.semesters.map((s) => ({
          id: s.id,
          label: `Year ${y.yearNumber} · ${s.name}`,
        })),
    )
  }, [programmeStructure.data])

  const years: YearOption[] = useMemo(() => {
    if (!programmeStructure.data?.years) return []
    return programmeStructure.data.years.map(
      (y: { id: string; yearNumber: number; name: string }) => ({
        id: y.id,
        yearNumber: y.yearNumber,
        name: y.name,
      }),
    )
  }, [programmeStructure.data])

  const modules = useQuery({
    queryKey: ['content-modules'],
    queryFn: async () => (await api.get('/content/modules')).data,
  })

  const filteredModules = useMemo(() => {
    const list = modules.data ?? []
    if (!filterProgrammeId) return list
    return list.filter((m: { programmeName: string }) => {
      const programme = programmes.data?.find((p: { id: string; name: string }) => p.id === filterProgrammeId)
      return programme && m.programmeName === programme.name
    })
  }, [modules.data, filterProgrammeId, programmes.data])

  const moduleLessons = useQuery({
    queryKey: ['module-lessons', existingModuleId],
    queryFn: async () => (await api.get(`/content/modules/${existingModuleId}/lessons`)).data,
    enabled: !!existingModuleId,
  })

  const universities = useQuery({
    queryKey: ['universities'],
    queryFn: async () => (await api.get('/universities', { params: { page: 1, pageSize: 100 } })).data,
  })

  const uniList: { id: string; name: string; domain: string }[] = universities.data?.items ?? []

  const createProgramme = useMutation({
    mutationFn: async () =>
      api.post('/programmes', {
        name: newProgrammeName.trim(),
        code: newProgrammeCode.trim().toUpperCase(),
        description: newProgrammeDesc.trim(),
        durationYears: parseInt(newProgrammeYears, 10) || 3,
      }),
    onSuccess: (res) => {
      setProgrammeId(res.data.id)
      setSemesterId('')
      setProgrammeModalOpen(false)
      setNewProgrammeName('')
      setNewProgrammeCode('')
      setNewProgrammeDesc('')
      setNewProgrammeYears('3')
      setModalError(null)
      queryClient.invalidateQueries({ queryKey: ['programmes'] })
      notify.success('Programme created.')
    },
    onError: (err) => {
      setModalError(getErrorMessage(err))
      notify.error(err)
    },
  })

  const createSemester = useMutation({
    mutationFn: async () => {
      let yearId = newSemesterYearId
      if (!yearId || yearId === NEW_YEAR) {
        const yearRes = await api.post(`/programmes/${programmeId}/years`, {
          yearNumber: parseInt(newYearNumber, 10) || 1,
          name: newYearName.trim() || `Year ${newYearNumber}`,
        })
        yearId = yearRes.data.id
      }
      return api.post(`/programmes/years/${yearId}/semesters`, {
        semesterNumber: parseInt(newSemesterNumber, 10) || 1,
        name: newSemesterName.trim(),
      })
    },
    onSuccess: (res) => {
      setSemesterId(res.data.id)
      setSemesterModalOpen(false)
      setNewSemesterName('')
      setNewSemesterNumber('1')
      setNewSemesterYearId('')
      setModalError(null)
      queryClient.invalidateQueries({ queryKey: ['programme-structure', programmeId] })
      notify.success('Semester created.')
    },
    onError: (err) => {
      setModalError(getErrorMessage(err))
      notify.error(err)
    },
  })

  function semesterDefaultsForYear(yearId: string) {
    if (!programmeStructure.data?.years) return { semNum: '1', semName: 'Semester 1' }
    const year = programmeStructure.data.years.find((y: { id: string }) => y.id === yearId)
    const count = year?.semesters?.length ?? 0
    const next = count + 1
    return { semNum: String(next), semName: `Semester ${next}` }
  }

  function onSemesterYearChange(yearId: string) {
    setNewSemesterYearId(yearId)
    if (yearId === NEW_YEAR) {
      const nextYear = years.length ? Math.max(...years.map((y) => y.yearNumber)) + 1 : 1
      setNewYearNumber(String(nextYear))
      setNewYearName(`Year ${nextYear}`)
      setNewSemesterNumber('1')
      setNewSemesterName('Semester 1')
      return
    }
    const { semNum, semName } = semesterDefaultsForYear(yearId)
    setNewSemesterNumber(semNum)
    setNewSemesterName(semName)
  }

  function openSemesterModal() {
    if (!programmeId) return
    setModalError(null)
    if (years.length > 0) {
      const lastYear = years[years.length - 1]
      setNewSemesterYearId(lastYear.id)
      const { semNum, semName } = semesterDefaultsForYear(lastYear.id)
      setNewSemesterNumber(semNum)
      setNewSemesterName(semName)
    } else {
      setNewSemesterYearId(NEW_YEAR)
      setNewYearNumber('1')
      setNewYearName('Year 1')
      setNewSemesterNumber('1')
      setNewSemesterName('Semester 1')
    }
    setSemesterModalOpen(true)
  }

  const createModule = useMutation({
    mutationFn: async () =>
      api.post(`/programmes/semesters/${semesterId}/modules`, {
        title: moduleTitle.trim(),
        description: moduleDescription.trim(),
        sortOrder: 1,
      }),
    onSuccess: (res) => {
      setModuleId(res.data.id)
      setModuleTitle('')
      setModuleDescription('')
      queryClient.invalidateQueries({ queryKey: ['content-modules'] })
      queryClient.invalidateQueries({ queryKey: ['programme-structure', programmeId] })
      notify.success('Module created.')
    },
    onError: (err) => notify.error(err),
  })

  const createLesson = useMutation({
    mutationFn: async () =>
      api.post('/content/lessons', { moduleId, title: lessonTitle.trim(), description: '', sortOrder: 1 }),
    onSuccess: (res) => {
      setSelectedLesson(res.data.id)
      setLessonTitle('')
      setPublishMessage(null)
      notify.success('Lesson created.')
    },
    onError: (err) => notify.error(err),
  })

  const updateStatus = useMutation({
    mutationFn: async ({ id, status }: { id: string; status: string }) =>
      api.patch(`/content/lessons/${id}/status`, { status }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['content-lesson', selectedLesson] })
      setWorkflowMessage('Submitted for review.')
      notify.success('Submitted for review.')
    },
    onError: (err) => {
      setWorkflowMessage(getErrorMessage(err))
      notify.error(err)
    },
  })

  const approveReview = useMutation({
    mutationFn: async (id: string) => api.post(`/content/lessons/${id}/review`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['content-lesson', selectedLesson] })
      setWorkflowMessage('Lesson approved. You can now publish to universities.')
      setPublishMessage(null)
      notify.success('Lesson approved.')
    },
    onError: (err) => {
      setWorkflowMessage(getErrorMessage(err))
      notify.error(err)
    },
  })

  const publish = useMutation({
    mutationFn: async (id: string) => {
      const ids = selectedUniversities.length > 0 ? selectedUniversities : null
      await api.post(`/content/lessons/${id}/publish`, { universityIds: ids })
    },
    onSuccess: () => {
      setPublishMessage('Mapped to selected universities. Programme auto-linked where needed.')
      queryClient.invalidateQueries({ queryKey: ['content-lesson', selectedLesson] })
      queryClient.invalidateQueries({ queryKey: ['module-lessons', existingModuleId] })
      notify.success('Lesson mapped to universities.')
    },
    onError: (err) => {
      setPublishMessage(getErrorMessage(err))
      notify.error(err)
    },
  })

  const publishModule = useMutation({
    mutationFn: async () => {
      const ids = selectedUniversities.length > 0 ? selectedUniversities : null
      return api.post(`/content/modules/${existingModuleId}/publish`, { universityIds: ids })
    },
    onSuccess: (res) => {
      setPublishMessage(`All ${res.data.lessonsPublished} lessons in module mapped to selected universities.`)
      queryClient.invalidateQueries({ queryKey: ['module-lessons', existingModuleId] })
      if (selectedLesson) queryClient.invalidateQueries({ queryKey: ['content-lesson', selectedLesson] })
      notify.success(`Module mapped (${res.data.lessonsPublished} lessons).`)
    },
    onError: (err) => {
      setPublishMessage(getErrorMessage(err))
      notify.error(err)
    },
  })

  function selectExistingLesson(
    lessonId: string,
    publishedTo: { universityId?: string | null; name: string }[],
  ) {
    setSelectedLesson(lessonId)
    setPublishMessage(null)
    setWorkflowMessage(null)
    if (publishedTo.some((p) => !p.universityId)) {
      setSelectedUniversities(uniList.map((u) => u.id))
    } else {
      setSelectedUniversities(
        publishedTo.map((p) => p.universityId).filter((id): id is string => !!id),
      )
    }
  }

  useEffect(() => {
    const publishedTo: { universityId?: string | null; name: string }[] =
      selectedLessonQuery.data?.publishedTo ?? []
    if (!selectedLesson || publishedTo.length === 0) return
    if (publishedTo.some((p) => !p.universityId)) {
      setSelectedUniversities(uniList.map((u) => u.id))
    } else {
      setSelectedUniversities(
        publishedTo.map((p) => p.universityId).filter((id): id is string => !!id),
      )
    }
  }, [selectedLessonQuery.data?.publishedTo, selectedLesson, uniList])

  function toggleUniversity(id: string) {
    setSelectedUniversities((prev) =>
      prev.includes(id) ? prev.filter((u) => u !== id) : [...prev, id],
    )
  }

  function selectAllUniversities() {
    if (selectedUniversities.length === uniList.length) {
      setSelectedUniversities([])
    } else {
      setSelectedUniversities(uniList.map((u) => u.id))
    }
  }

  async function uploadAsset(lessonId: string, file: File) {
    setUploadMessage(null)
    try {
      const form = new FormData()
      form.append('file', file)
      await api.post(`/content/lessons/${lessonId}/assets`, form, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      setUploadMessage(`Uploaded: ${file.name}`)
      queryClient.invalidateQueries({ queryKey: ['content-lesson', lessonId] })
      notify.success(`Uploaded: ${file.name}`)
    } catch (err) {
      setUploadMessage(getErrorMessage(err))
      notify.error(err)
    }
  }

  async function deleteAsset(lessonId: string, assetId: string) {
    setUploadMessage(null)
    try {
      await api.delete(`/content/lessons/${lessonId}/assets/${assetId}`)
      setUploadMessage('File removed.')
      queryClient.invalidateQueries({ queryKey: ['content-lesson', lessonId] })
      notify.success('File removed.')
    } catch (err) {
      setUploadMessage(getErrorMessage(err))
      notify.error(err)
    }
  }

  const lessonAssets: { id: string; fileName: string; assetType: string; blobUrl: string }[] =
    selectedLessonQuery.data?.assets ?? []

  const navItems = getApolloNavItems(isAdmin)

  return (
    <DashboardShell
      accent="apollo"
      roleLabel="Apollo Faculty"
      portalTitle="Content Library"
      userName={auth.fullName}
      tenantLabel="Upload once · publish to universities"
      navItems={navItems}
    >
      <div ref={animRef} className="mx-auto max-w-4xl space-y-8">
        <Panel title="Map existing programme content">
          <p className="mb-4 text-sm text-slate-600">
            Already uploaded lessons — select a module and map to more universities. No re-upload needed.
          </p>
          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label>Filter by programme (optional)</Label>
              <select
                className="h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-[#2081A1]"
                value={filterProgrammeId}
                onChange={(e) => {
                  setFilterProgrammeId(e.target.value)
                  setExistingModuleId('')
                }}
              >
                <option value="">All programmes</option>
                {programmes.data?.map((p: { id: string; name: string }) => (
                  <option key={p.id} value={p.id}>{p.name}</option>
                ))}
              </select>
            </div>
            <div className="space-y-2">
              <Label>Module</Label>
              <select
                className="h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-[#2081A1]"
                value={existingModuleId}
                onChange={(e) => {
                  setExistingModuleId(e.target.value)
                  setSelectedLesson(null)
                }}
              >
                <option value="">Select module with existing content</option>
                {filteredModules.map((m: { moduleId: string; moduleTitle: string; programmeName: string }) => (
                  <option key={m.moduleId} value={m.moduleId}>
                    {m.programmeName} · {m.moduleTitle}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {existingModuleId && (
            <div className="mt-4 space-y-2">
              <Label>Lessons in this module</Label>
              {(moduleLessons.data ?? []).length === 0 ? (
                <p className="text-sm text-slate-500">No lessons in this module yet.</p>
              ) : (
                <div className="space-y-2">
                  {(moduleLessons.data ?? []).map((l: {
                    id: string
                    title: string
                    status: string
                    assetCount: number
                    publishedTo: { universityId?: string | null; name: string }[]
                  }) => (
                    <button
                      key={l.id}
                      type="button"
                      onClick={() => selectExistingLesson(l.id, l.publishedTo)}
                      className={`flex w-full items-center justify-between gap-3 rounded-xl border px-4 py-3 text-left transition hover:border-[#2081A1]/40 hover:bg-[#2081A1]/5 ${
                        selectedLesson === l.id ? 'border-[#2081A1] bg-[#2081A1]/5 ring-1 ring-[#2081A1]/30' : 'border-slate-200 bg-white'
                      }`}
                    >
                      <div className="min-w-0">
                        <p className="font-medium text-slate-900">{l.title}</p>
                        <p className="text-xs text-slate-500">
                          {l.status} · {l.assetCount} file{l.assetCount === 1 ? '' : 's'}
                        </p>
                      </div>
                      <div className="shrink-0 text-right">
                        {l.publishedTo.length > 0 ? (
                          <p className="text-xs text-emerald-700">
                            {l.publishedTo.map((p) => p.name).join(', ')}
                          </p>
                        ) : (
                          <p className="text-xs text-amber-600">Not published</p>
                        )}
                      </div>
                    </button>
                  ))}
                </div>
              )}

              {(moduleLessons.data ?? []).length > 0 && (
                <>
                  <div className="mt-4 space-y-3 rounded-xl border border-slate-200 bg-slate-50 p-4">
                    <div className="flex items-center justify-between">
                      <Label>Universities to map</Label>
                      {uniList.length > 0 && (
                        <button
                          type="button"
                          onClick={selectAllUniversities}
                          className="text-xs font-medium text-[#2081A1] hover:underline"
                        >
                          {selectedUniversities.length === uniList.length ? 'Deselect all' : 'Select all'}
                        </button>
                      )}
                    </div>
                    <div className="max-h-40 space-y-2 overflow-y-auto">
                      {uniList.map((u) => (
                        <label
                          key={u.id}
                          className={`flex cursor-pointer items-center gap-3 rounded-lg px-3 py-2 transition hover:bg-white ${
                            selectedUniversities.includes(u.id) ? 'bg-white ring-1 ring-[#2081A1]/30' : ''
                          }`}
                        >
                          <input
                            type="checkbox"
                            checked={selectedUniversities.includes(u.id)}
                            onChange={() => toggleUniversity(u.id)}
                            className="h-4 w-4 rounded border-slate-300 text-[#2081A1]"
                          />
                          <div>
                            <p className="text-sm font-medium text-slate-900">{u.name}</p>
                            <p className="text-xs text-slate-500">{u.domain}</p>
                          </div>
                        </label>
                      ))}
                    </div>
                  </div>
                  <Button
                    variant="outline"
                    className="mt-3 border-[#2081A1] text-[#2081A1]"
                    onClick={() => publishModule.mutate()}
                    disabled={publishModule.isPending || selectedUniversities.length === 0}
                  >
                    <Send className="mr-2 h-4 w-4" />
                    {publishModule.isPending ? 'Mapping…' : 'Map entire module to selected universities'}
                  </Button>
                </>
              )}
            </div>
          )}
        </Panel>

        {isAdmin && (
          <Panel title="Create new module">
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <LabelWithAdd label="Programme" onAdd={() => { setModalError(null); setProgrammeModalOpen(true) }} />
                <select
                  className="h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-[#2081A1]"
                  value={programmeId}
                  onChange={(e) => {
                    setProgrammeId(e.target.value)
                    setSemesterId('')
                  }}
                >
                  <option value="">Select programme</option>
                  {programmes.data?.map((p: { id: string; name: string }) => (
                    <option key={p.id} value={p.id}>{p.name}</option>
                  ))}
                </select>
              </div>
              <div className="space-y-2">
                <LabelWithAdd label="Semester" onAdd={openSemesterModal} disabled={!programmeId} />
                <select
                  className="h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-[#2081A1]"
                  value={semesterId}
                  onChange={(e) => setSemesterId(e.target.value)}
                  disabled={!programmeId}
                >
                  <option value="">Select semester</option>
                  {semesters.map((s) => (
                    <option key={s.id} value={s.id}>{s.label}</option>
                  ))}
                </select>
              </div>
              <div className="space-y-2 md:col-span-2">
                <Label>Module title</Label>
                <Input
                  className="h-11 rounded-xl"
                  placeholder="e.g. Advanced Cardiovascular Assessment"
                  value={moduleTitle}
                  onChange={(e) => setModuleTitle(e.target.value)}
                />
              </div>
              <div className="space-y-2 md:col-span-2">
                <Label>Description (optional)</Label>
                <Input
                  className="h-11 rounded-xl"
                  placeholder="Brief module overview"
                  value={moduleDescription}
                  onChange={(e) => setModuleDescription(e.target.value)}
                />
              </div>
            </div>
            {createModule.error && (
              <p className="mt-2 text-sm text-red-600">{getErrorMessage(createModule.error)}</p>
            )}
            {createModule.isSuccess && (
              <p className="mt-2 flex items-center gap-1 text-sm text-emerald-600">
                <CheckCircle2 className="h-4 w-4" /> Module created — now add a lesson below.
              </p>
            )}
            <Button
              className="mt-4 bg-[#2081A1]"
              onClick={() => createModule.mutate()}
              disabled={!semesterId || !moduleTitle.trim() || createModule.isPending}
            >
              <FileStack className="mr-2 h-4 w-4" />
              {createModule.isPending ? 'Creating…' : 'Create module'}
            </Button>
          </Panel>
        )}

        <Panel title="Create lesson & upload assets">
          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2 md:col-span-2">
              <Label>Module</Label>
              <select
                className="h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-[#2081A1]"
                value={moduleId}
                onChange={(e) => setModuleId(e.target.value)}
              >
                <option value="">Select module</option>
                {modules.data?.map((m: { moduleId: string; moduleTitle: string; programmeName: string }) => (
                  <option key={m.moduleId} value={m.moduleId}>
                    {m.programmeName} · {m.moduleTitle}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-2 md:col-span-2">
              <Label>Lesson title</Label>
              <Input
                className="h-11 rounded-xl"
                value={lessonTitle}
                onChange={(e) => setLessonTitle(e.target.value)}
                placeholder="Introduction to Cardiovascular Assessment"
              />
            </div>
          </div>
          {createLesson.error && (
            <p className="mt-2 text-sm text-red-600">{getErrorMessage(createLesson.error)}</p>
          )}
          <Button
            className="mt-4 bg-[#2081A1]"
            onClick={() => createLesson.mutate()}
            disabled={!moduleId || !lessonTitle.trim() || createLesson.isPending}
          >
            <FileStack className="mr-2 h-4 w-4" /> Save as draft
          </Button>
        </Panel>

        {selectedLesson && (
          <Panel title="Publish workflow">
            <div className="mb-4 flex flex-wrap items-center gap-2">
              <p className="text-sm text-slate-500">
                {isAdmin
                  ? 'Upload assets, approve as admin, then publish to universities.'
                  : 'Upload assets, submit for review, then an Apollo admin will approve and publish.'}
              </p>
              <span className="rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-medium text-slate-600">
                Status: {lessonStatus}
              </span>
            </div>

            <div className="space-y-2">
              <Label>Upload PDF or video</Label>
              <label className="flex cursor-pointer items-center gap-2 rounded-xl border border-dashed border-slate-300 bg-slate-50 px-4 py-8 text-sm text-slate-500 hover:border-[#2081A1] hover:bg-[#2081A1]/5">
                <Upload className="h-5 w-5" />
                Click to upload asset
                <Input
                  type="file"
                  accept=".pdf,video/*"
                  className="hidden"
                  onChange={(e) => e.target.files?.[0] && uploadAsset(selectedLesson, e.target.files[0])}
                />
              </label>
              {uploadMessage && (
                <p className={`text-sm ${uploadMessage.startsWith('Uploaded') || uploadMessage === 'File removed.' ? 'text-emerald-600' : 'text-red-600'}`}>
                  {uploadMessage}
                </p>
              )}

              {lessonAssets.length > 0 && (
                <div className="mt-4 space-y-2">
                  <Label>Uploaded files</Label>
                  {lessonAssets.map((asset) => (
                    <div
                      key={asset.id}
                      className="flex items-center justify-between gap-3 rounded-xl border border-slate-200 bg-slate-50 px-4 py-3"
                    >
                      <div className="min-w-0">
                        <p className="truncate text-sm font-medium text-slate-800">{asset.fileName}</p>
                        <p className="text-xs text-slate-500">{asset.assetType}</p>
                      </div>
                      <button
                        type="button"
                        onClick={() => deleteAsset(selectedLesson, asset.id)}
                        className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg border border-red-200 text-red-600 transition hover:bg-red-50"
                        title="Remove file"
                      >
                        <Trash2 className="h-4 w-4" />
                      </button>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <div className="mt-6 flex flex-wrap gap-2">
              {!isAdmin && (
                <Button
                  variant="outline"
                  onClick={() => updateStatus.mutate({ id: selectedLesson, status: 'PendingReview' })}
                  disabled={updateStatus.isPending || lessonStatus === 'PendingReview' || lessonStatus === 'Published'}
                >
                  Submit for review
                </Button>
              )}
              {isAdmin && (
                <Button
                  variant="outline"
                  onClick={() => approveReview.mutate(selectedLesson)}
                  disabled={approveReview.isPending || lessonStatus === 'Published'}
                  className="border-[#004a8f]/30 text-[#004a8f] hover:bg-[#004a8f]/5"
                >
                  <CheckCircle2 className="mr-2 h-4 w-4" />
                  {lessonStatus === 'PendingReview' ? 'Re-approve' : 'Approve'}
                </Button>
              )}
            </div>
            {workflowMessage && (
              <p className={`mt-3 text-sm ${workflowMessage.includes('approved') || workflowMessage.includes('Submitted') ? 'text-emerald-600' : 'text-red-600'}`}>
                {workflowMessage}
              </p>
            )}

            <div className="mt-6 space-y-3">
              <div className="flex items-center justify-between">
                <Label>Publish to universities</Label>
                {uniList.length > 0 && (
                  <button
                    type="button"
                    onClick={selectAllUniversities}
                    className="text-xs font-medium text-[#2081A1] hover:underline"
                  >
                    {selectedUniversities.length === uniList.length ? 'Deselect all' : 'Select all'}
                  </button>
                )}
              </div>
              {uniList.length === 0 ? (
                <p className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
                  No universities found.{' '}
                  {isAdmin && (
                    <a href="/apollo/universities" className="font-medium underline">
                      Create a university first
                    </a>
                  )}
                </p>
              ) : (
                <div className="max-h-48 space-y-2 overflow-y-auto rounded-xl border border-slate-200 bg-white p-3">
                  {uniList.map((u) => (
                    <label
                      key={u.id}
                      className={`flex cursor-pointer items-center gap-3 rounded-lg px-3 py-2.5 transition hover:bg-slate-50 ${
                        selectedUniversities.includes(u.id) ? 'bg-[#2081A1]/5 ring-1 ring-[#2081A1]/30' : ''
                      }`}
                    >
                      <input
                        type="checkbox"
                        checked={selectedUniversities.includes(u.id)}
                        onChange={() => toggleUniversity(u.id)}
                        className="h-4 w-4 rounded border-slate-300 text-[#2081A1] focus:ring-[#2081A1]"
                      />
                      <div>
                        <p className="text-sm font-medium text-slate-900">{u.name}</p>
                        <p className="text-xs text-slate-500">{u.domain}</p>
                      </div>
                    </label>
                  ))}
                </div>
              )}
              <p className="text-xs text-slate-500">
                {selectedUniversities.length === 0
                  ? 'Select universities to map this content. Already-mapped universities are kept.'
                  : `${selectedUniversities.length} universit${selectedUniversities.length === 1 ? 'y' : 'ies'} selected — new selections are added without removing existing access.`}
              </p>
              <Button
                className="bg-[#2081A1]"
                onClick={() => publish.mutate(selectedLesson)}
                disabled={
                  publish.isPending
                  || selectedUniversities.length === 0
                  || (!isAdmin && lessonStatus !== 'PendingReview' && lessonStatus !== 'Published')
                }
              >
                {publish.isPending ? 'Mapping…' : 'Map to selected universities'}
              </Button>
              {publishMessage && (
                <p className={`text-sm ${publishMessage.includes('Mapped') || publishMessage.includes('mapped') || publishMessage.includes('lessons') ? 'text-emerald-600' : 'text-red-600'}`}>
                  {publishMessage}
                </p>
              )}
            </div>
          </Panel>
        )}
      </div>

      <Modal open={programmeModalOpen} onClose={() => { setProgrammeModalOpen(false); setModalError(null) }} title="Create new programme">
        <form
          className="space-y-4"
          onSubmit={(e) => {
            e.preventDefault()
            setModalError(null)
            if (!newProgrammeName.trim() || !newProgrammeCode.trim()) {
              const msg = 'Name and code are required.'
              setModalError(msg)
              notify.error(msg)
              return
            }
            createProgramme.mutate()
          }}
        >
          <div className="space-y-2">
            <Label>Programme name</Label>
            <Input
              className="h-11 rounded-xl"
              placeholder="e.g. B.Sc Nursing"
              value={newProgrammeName}
              onChange={(e) => setNewProgrammeName(e.target.value)}
              autoFocus
            />
          </div>
          <div className="space-y-2">
            <Label>Programme code</Label>
            <Input
              className="h-11 rounded-xl"
              placeholder="e.g. BSC-NUR"
              value={newProgrammeCode}
              onChange={(e) => setNewProgrammeCode(e.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label>Description (optional)</Label>
            <Input
              className="h-11 rounded-xl"
              placeholder="Brief description"
              value={newProgrammeDesc}
              onChange={(e) => setNewProgrammeDesc(e.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label>Duration (years)</Label>
            <Input
              type="number"
              min={1}
              max={6}
              className="h-11 rounded-xl"
              value={newProgrammeYears}
              onChange={(e) => setNewProgrammeYears(e.target.value)}
            />
          </div>
          {modalError && programmeModalOpen && <p className="text-sm text-red-600">{modalError}</p>}
          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={() => setProgrammeModalOpen(false)}>Cancel</Button>
            <Button type="submit" className="bg-[#2081A1]" disabled={createProgramme.isPending}>
              {createProgramme.isPending ? 'Saving…' : 'Save programme'}
            </Button>
          </div>
        </form>
      </Modal>

      <Modal open={semesterModalOpen} onClose={() => { setSemesterModalOpen(false); setModalError(null) }} title="Create new semester">
        <form
          className="space-y-4"
          onSubmit={(e) => {
            e.preventDefault()
            setModalError(null)
            if (!newSemesterName.trim()) {
              const msg = 'Semester name is required.'
              setModalError(msg)
              notify.error(msg)
              return
            }
            if (newSemesterYearId === NEW_YEAR) {
              const yr = parseInt(newYearNumber, 10)
              if (years.some((y) => y.yearNumber === yr)) {
                const msg = `Year ${yr} already exists. Select it from the dropdown instead.`
                setModalError(msg)
                notify.error(msg)
                return
              }
            }
            createSemester.mutate()
          }}
        >
          <p className="text-sm text-slate-500">
            Adding to: <span className="font-medium text-slate-700">
              {programmes.data?.find((p: { id: string }) => p.id === programmeId)?.name ?? '—'}
            </span>
          </p>
          {years.length > 0 ? (
            <div className="space-y-3">
              <div className="space-y-2">
                <Label>Programme year</Label>
                <select
                  className="h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-[#2081A1]"
                  value={newSemesterYearId}
                  onChange={(e) => onSemesterYearChange(e.target.value)}
                >
                  {years.map((y) => (
                    <option key={y.id} value={y.id}>{yearLabel(y.yearNumber, y.name)}</option>
                  ))}
                  <option value={NEW_YEAR}>+ Add new programme year…</option>
                </select>
              </div>
              {newSemesterYearId === NEW_YEAR && (
                <div className="rounded-xl border border-[#2081A1]/20 bg-[#2081A1]/5 p-3 space-y-3">
                  <p className="text-xs text-[#1a6d89]">Create a new year, then add the semester under it.</p>
                  <div className="grid grid-cols-2 gap-3">
                    <div className="space-y-1">
                      <Label className="text-xs">Year number</Label>
                      <Input
                        type="number"
                        min={1}
                        className="h-10 rounded-lg"
                        value={newYearNumber}
                        onChange={(e) => {
                          setNewYearNumber(e.target.value)
                          setNewYearName(`Year ${e.target.value}`)
                        }}
                      />
                    </div>
                    <div className="space-y-1">
                      <Label className="text-xs">Year name</Label>
                      <Input
                        className="h-10 rounded-lg"
                        placeholder="Year 2"
                        value={newYearName}
                        onChange={(e) => setNewYearName(e.target.value)}
                      />
                    </div>
                  </div>
                </div>
              )}
            </div>
          ) : (
            <div className="rounded-xl border border-amber-200 bg-amber-50 p-3 space-y-3">
              <p className="text-xs text-amber-800">No years yet — create the first year for this programme.</p>
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <Label className="text-xs">Year number</Label>
                  <Input
                    type="number"
                    min={1}
                    className="h-10 rounded-lg"
                    value={newYearNumber}
                    onChange={(e) => setNewYearNumber(e.target.value)}
                  />
                </div>
                <div className="space-y-1">
                  <Label className="text-xs">Year name</Label>
                  <Input
                    className="h-10 rounded-lg"
                    placeholder="Year 1"
                    value={newYearName}
                    onChange={(e) => setNewYearName(e.target.value)}
                  />
                </div>
              </div>
            </div>
          )}
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label>Semester number</Label>
              <Input
                type="number"
                min={1}
                className="h-11 rounded-xl"
                value={newSemesterNumber}
                onChange={(e) => setNewSemesterNumber(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label>Semester name</Label>
              <Input
                className="h-11 rounded-xl"
                placeholder="Semester 1"
                value={newSemesterName}
                onChange={(e) => setNewSemesterName(e.target.value)}
                autoFocus
              />
            </div>
          </div>
          {modalError && semesterModalOpen && <p className="text-sm text-red-600">{modalError}</p>}
          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={() => setSemesterModalOpen(false)}>Cancel</Button>
            <Button type="submit" className="bg-[#2081A1]" disabled={createSemester.isPending}>
              {createSemester.isPending ? 'Saving…' : 'Save semester'}
            </Button>
          </div>
        </form>
      </Modal>
    </DashboardShell>
  )
}
