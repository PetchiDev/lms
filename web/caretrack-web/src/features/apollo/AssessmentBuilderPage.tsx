import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useMemo, useState } from 'react'
import {
  AlertCircle,
  CheckCircle2,
  CircleDot,
  ClipboardList,
  GripVertical,
  Lock,
  Plus,
  Save,
  Trash2,
} from 'lucide-react'
import { api, getErrorMessage } from '@/lib/api-client'
import { notify } from '@/lib/notify'
import { authStore } from '@/lib/auth-store'
import { getApolloNavItems } from '@/lib/apollo-nav'
import { useDashboardAnimation } from '@/animations/useDashboardAnimation'
import { DashboardShell, Panel } from '@/components/layout/DashboardShell'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { cn, createId } from '@/lib/utils'

interface ModuleSummary {
  moduleId: string
  moduleTitle: string
  yearNumber: number
  semesterNumber: number
  semesterName: string
  quizId: string | null
  quizTitle: string | null
  questionCount: number
  isActive: boolean
  attemptCount: number
}

interface QuestionDraft {
  key: string
  questionText: string
  points: number
  options: { key: string; optionText: string; isCorrect: boolean }[]
}

function newQuestion(): QuestionDraft {
  const base = createId()
  return {
    key: base,
    questionText: '',
    points: 1,
    options: [1, 2, 3, 4].map((i) => ({
      key: `${base}-opt-${i}`,
      optionText: '',
      isCorrect: i === 1,
    })),
  }
}

function mapQuestionsFromApi(
  questions: {
    id: string
    questionText: string
    points: number
    options: { id: string; optionText: string; isCorrect: boolean }[]
  }[],
): QuestionDraft[] {
  if (questions.length === 0) return [newQuestion()]
  return questions.map((q) => ({
    key: q.id,
    questionText: q.questionText,
    points: q.points,
    options: q.options.map((o) => ({
      key: o.id,
      optionText: o.optionText,
      isCorrect: o.isCorrect,
    })),
  }))
}

function ModuleStatusBadge({ module }: { module: ModuleSummary }) {
  if (!module.quizId || module.questionCount === 0) {
    return (
      <span className="rounded-full bg-amber-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-amber-700">
        No quiz
      </span>
    )
  }
  if (!module.isActive) {
    return (
      <span className="rounded-full bg-slate-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-slate-600">
        Inactive
      </span>
    )
  }
  return (
    <span className="rounded-full bg-emerald-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-emerald-700">
      Ready · {module.questionCount} Q
    </span>
  )
}

export function AssessmentBuilderPage() {
  const animRef = useDashboardAnimation()
  const auth = authStore.get()!
  const isAdmin = auth.role === 'ApolloAdmin'
  const queryClient = useQueryClient()

  const [programmeId, setProgrammeId] = useState('')
  const [selectedModuleId, setSelectedModuleId] = useState<string | null>(null)
  const [title, setTitle] = useState('')
  const [passPercentage, setPassPercentage] = useState('60')
  const [timeLimitMinutes, setTimeLimitMinutes] = useState('30')
  const [maxAttempts, setMaxAttempts] = useState('3')
  const [cooldownHours, setCooldownHours] = useState('24')
  const [isActive, setIsActive] = useState(true)
  const [questions, setQuestions] = useState<QuestionDraft[]>([newQuestion()])
  const [questionsLocked, setQuestionsLocked] = useState(false)
  const [saveMessage, setSaveMessage] = useState<string | null>(null)

  const programmes = useQuery({
    queryKey: ['programmes'],
    queryFn: async () => (await api.get('/programmes')).data,
  })

  useEffect(() => {
    if (!programmeId && programmes.data?.length) {
      setProgrammeId(programmes.data[0].id)
    }
  }, [programmes.data, programmeId])

  const overview = useQuery({
    queryKey: ['assessment-overview', programmeId],
    queryFn: async () => (await api.get(`/assessments/programmes/${programmeId}/overview`)).data,
    enabled: !!programmeId,
  })

  const modules: ModuleSummary[] = overview.data?.modules ?? []

  const groupedModules = useMemo(() => {
    const groups = new Map<string, ModuleSummary[]>()
    for (const m of modules) {
      const key = `Year ${m.yearNumber} · ${m.semesterName}`
      if (!groups.has(key)) groups.set(key, [])
      groups.get(key)!.push(m)
    }
    return [...groups.entries()]
  }, [modules])

  const readyCount = modules.filter((m) => m.quizId && m.questionCount > 0 && m.isActive).length

  const quizDetail = useQuery({
    queryKey: ['admin-quiz', selectedModuleId],
    queryFn: async () => (await api.get(`/assessments/modules/${selectedModuleId}`)).data,
    enabled: !!selectedModuleId,
  })

  useEffect(() => {
    if (!quizDetail.data) return
    const d = quizDetail.data
    setTitle(d.title)
    setPassPercentage(String(d.passPercentage))
    setTimeLimitMinutes(String(d.timeLimitMinutes))
    setMaxAttempts(String(d.maxAttempts))
    setCooldownHours(String(d.cooldownHours))
    setIsActive(d.isActive)
    setQuestionsLocked(d.questionsLocked)
    setQuestions(mapQuestionsFromApi(d.questions))
  }, [quizDetail.data])

  useEffect(() => {
    if (!selectedModuleId && modules.length > 0) {
      setSelectedModuleId(modules[0].moduleId)
    }
  }, [modules, selectedModuleId])

  const saveQuiz = useMutation({
    mutationFn: async () => {
      if (!selectedModuleId) throw new Error('Select a module')
      const payload = {
        title,
        passPercentage: Number(passPercentage),
        timeLimitMinutes: Number(timeLimitMinutes),
        maxAttempts: Number(maxAttempts),
        cooldownHours: Number(cooldownHours),
        isActive,
        questions: questionsLocked
          ? []
          : questions.map((q) => ({
              questionText: q.questionText,
              points: q.points,
              options: q.options.map((o) => ({
                optionText: o.optionText,
                isCorrect: o.isCorrect,
              })),
            })),
      }
      return (await api.put(`/assessments/modules/${selectedModuleId}`, payload)).data
    },
    onSuccess: () => {
      setSaveMessage('Assessment saved successfully.')
      queryClient.invalidateQueries({ queryKey: ['assessment-overview', programmeId] })
      queryClient.invalidateQueries({ queryKey: ['admin-quiz', selectedModuleId] })
      notify.success('Assessment saved.')
    },
    onError: (err) => {
      setSaveMessage(getErrorMessage(err))
      notify.error(err)
    },
  })

  function updateQuestion(key: string, patch: Partial<QuestionDraft>) {
    setQuestions((prev) => prev.map((q) => (q.key === key ? { ...q, ...patch } : q)))
  }

  function updateOption(questionKey: string, optionKey: string, patch: Partial<QuestionDraft['options'][0]>) {
    setQuestions((prev) =>
      prev.map((q) => {
        if (q.key !== questionKey) return q
        return {
          ...q,
          options: q.options.map((o) => (o.key === optionKey ? { ...o, ...patch } : o)),
        }
      }),
    )
  }

  function setCorrectOption(questionKey: string, optionKey: string) {
    setQuestions((prev) =>
      prev.map((q) => {
        if (q.key !== questionKey) return q
        return {
          ...q,
          options: q.options.map((o) => ({ ...o, isCorrect: o.key === optionKey })),
        }
      }),
    )
  }

  function addQuestion() {
    setQuestions((prev) => [...prev, newQuestion()])
  }

  function removeQuestion(key: string) {
    setQuestions((prev) => (prev.length <= 1 ? prev : prev.filter((q) => q.key !== key)))
  }

  function addOption(questionKey: string) {
    setQuestions((prev) =>
      prev.map((q) => {
        if (q.key !== questionKey) return q
        return {
          ...q,
          options: [
            ...q.options,
            { key: createId(), optionText: '', isCorrect: false },
          ],
        }
      }),
    )
  }

  function removeOption(questionKey: string, optionKey: string) {
    setQuestions((prev) =>
      prev.map((q) => {
        if (q.key !== questionKey || q.options.length <= 2) return q
        const next = q.options.filter((o) => o.key !== optionKey)
        if (!next.some((o) => o.isCorrect)) next[0].isCorrect = true
        return { ...q, options: next }
      }),
    )
  }

  const selectedModule = modules.find((m) => m.moduleId === selectedModuleId)

  return (
    <DashboardShell
      accent="apollo"
      roleLabel="Apollo Faculty"
      portalTitle="Assessment Builder"
      userName={auth.fullName}
      tenantLabel="Question sets per programme module"
      navItems={getApolloNavItems(isAdmin)}
    >
      <div ref={animRef} className="space-y-6">
        <div className="flex flex-col gap-4 rounded-2xl border border-[#2081A1]/20 bg-gradient-to-br from-[#2081A1]/5 via-white to-indigo-50/40 p-6 md:flex-row md:items-end md:justify-between">
          <div>
            <div className="flex items-center gap-2 text-[#2081A1]">
              <ClipboardList className="h-5 w-5" />
              <span className="text-xs font-semibold uppercase tracking-widest">Apollo Assessments</span>
            </div>
            <h1 className="mt-2 font-display text-2xl font-bold text-slate-900 md:text-3xl">
              Build question sets by programme
            </h1>
            <p className="mt-1 max-w-2xl text-sm text-slate-600">
              Select a programme, pick a module, and configure MCQ assessments. Students unlock quizzes after completing module lessons. Pass mark ≥ 60% triggers certificate generation.
            </p>
          </div>
          <div className="flex flex-wrap gap-3">
            <div className="rounded-xl bg-white/80 px-4 py-3 shadow-sm ring-1 ring-slate-200">
              <p className="text-xs text-slate-500">Modules ready</p>
              <p className="text-xl font-bold text-emerald-600">{readyCount}<span className="text-sm font-normal text-slate-400"> / {modules.length}</span></p>
            </div>
            <div className="min-w-[200px] space-y-1">
              <Label>Programme</Label>
              <select
                className="h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-[#2081A1]"
                value={programmeId}
                onChange={(e) => {
                  setProgrammeId(e.target.value)
                  setSelectedModuleId(null)
                }}
              >
                {programmes.data?.map((p: { id: string; name: string }) => (
                  <option key={p.id} value={p.id}>{p.name}</option>
                ))}
              </select>
            </div>
          </div>
        </div>

        <div className="grid gap-6 xl:grid-cols-[320px_1fr]">
          <Panel title="Modules" className="xl:sticky xl:top-24 xl:self-start">
            <div className="max-h-[70vh] space-y-4 overflow-y-auto pr-1">
              {groupedModules.length === 0 && (
                <p className="text-sm text-slate-500">No modules in this programme yet. Create modules in Content Library first.</p>
              )}
              {groupedModules.map(([group, items]) => (
                <div key={group}>
                  <p className="mb-2 text-[10px] font-bold uppercase tracking-widest text-slate-400">{group}</p>
                  <div className="space-y-1">
                    {items.map((m) => {
                      const active = m.moduleId === selectedModuleId
                      return (
                        <button
                          key={m.moduleId}
                          type="button"
                          onClick={() => setSelectedModuleId(m.moduleId)}
                          className={cn(
                            'w-full rounded-xl border px-3 py-3 text-left transition',
                            active
                              ? 'border-[#2081A1]/40 bg-[#2081A1]/10 shadow-sm'
                              : 'border-slate-100 bg-slate-50/80 hover:border-[#2081A1]/20 hover:bg-white',
                          )}
                        >
                          <div className="flex items-start justify-between gap-2">
                            <p className="text-sm font-semibold text-slate-800">{m.moduleTitle}</p>
                            {active && <CircleDot className="h-4 w-4 shrink-0 text-[#2081A1]" />}
                          </div>
                          <div className="mt-2 flex flex-wrap items-center gap-2">
                            <ModuleStatusBadge module={m} />
                            {m.attemptCount > 0 && (
                              <span className="text-[10px] text-slate-500">{m.attemptCount} attempts</span>
                            )}
                          </div>
                        </button>
                      )
                    })}
                  </div>
                </div>
              ))}
            </div>
          </Panel>

          <div className="space-y-6">
            {!selectedModuleId ? (
              <Panel title="Question editor">
                <p className="text-sm text-slate-500">Select a module to configure its assessment.</p>
              </Panel>
            ) : quizDetail.isLoading ? (
              <Panel title="Loading…">
                <p className="text-sm text-slate-500">Loading assessment…</p>
              </Panel>
            ) : (
              <>
                <Panel title={selectedModule?.moduleTitle ?? 'Assessment settings'}>
                  <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                    <div className="space-y-1 md:col-span-2">
                      <Label>Assessment title</Label>
                      <Input value={title} onChange={(e) => setTitle(e.target.value)} placeholder="e.g. Cardiovascular Assessment" />
                    </div>
                    <div className="space-y-1">
                      <Label>Pass %</Label>
                      <Input type="number" min={1} max={100} value={passPercentage} onChange={(e) => setPassPercentage(e.target.value)} />
                    </div>
                    <div className="space-y-1">
                      <Label>Time limit (min)</Label>
                      <Input type="number" min={1} value={timeLimitMinutes} onChange={(e) => setTimeLimitMinutes(e.target.value)} />
                    </div>
                    <div className="space-y-1">
                      <Label>Max attempts</Label>
                      <Input type="number" min={1} value={maxAttempts} onChange={(e) => setMaxAttempts(e.target.value)} />
                    </div>
                    <div className="space-y-1">
                      <Label>Retry cooldown (hrs)</Label>
                      <Input type="number" min={0} value={cooldownHours} onChange={(e) => setCooldownHours(e.target.value)} />
                    </div>
                    <div className="flex items-end">
                      <label className="flex cursor-pointer items-center gap-2 rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm">
                        <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} className="rounded border-slate-300 text-[#2081A1] focus:ring-[#2081A1]" />
                        Active for students
                      </label>
                    </div>
                  </div>
                </Panel>

                {questionsLocked && (
                  <div className="flex items-start gap-3 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
                    <Lock className="mt-0.5 h-4 w-4 shrink-0" />
                    <p>Students have already attempted this assessment. Questions are locked — you can still update title, pass mark, and attempt settings.</p>
                  </div>
                )}

                <Panel title={`Questions (${questions.length})`}>
                  <div className="space-y-4">
                    {questions.map((q, index) => (
                      <div
                        key={q.key}
                        className="rounded-2xl border border-slate-200 bg-gradient-to-br from-white to-slate-50/80 p-5 shadow-sm"
                      >
                        <div className="mb-4 flex items-center justify-between gap-3">
                          <div className="flex items-center gap-2">
                            <GripVertical className="h-4 w-4 text-slate-300" />
                            <span className="rounded-lg bg-[#2081A1]/10 px-2.5 py-1 text-xs font-bold text-[#2081A1]">
                              Q{index + 1}
                            </span>
                          </div>
                          <div className="flex items-center gap-2">
                            <div className="flex items-center gap-1">
                              <Label className="text-xs">Points</Label>
                              <Input
                                type="number"
                                min={1}
                                className="h-8 w-16"
                                value={q.points}
                                disabled={questionsLocked}
                                onChange={(e) => updateQuestion(q.key, { points: Number(e.target.value) || 1 })}
                              />
                            </div>
                            {!questionsLocked && (
                              <Button type="button" variant="outline" size="sm" onClick={() => removeQuestion(q.key)} disabled={questions.length <= 1}>
                                <Trash2 className="h-3.5 w-3.5" />
                              </Button>
                            )}
                          </div>
                        </div>

                        <textarea
                          className="min-h-[80px] w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#2081A1] disabled:bg-slate-100"
                          placeholder="Enter question text…"
                          value={q.questionText}
                          disabled={questionsLocked}
                          onChange={(e) => updateQuestion(q.key, { questionText: e.target.value })}
                        />

                        <div className="mt-4 space-y-2">
                          <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Answer options · select correct</p>
                          {q.options.map((o, optIndex) => (
                            <div key={o.key} className="flex items-center gap-2">
                              <button
                                type="button"
                                disabled={questionsLocked}
                                onClick={() => setCorrectOption(q.key, o.key)}
                                className={cn(
                                  'flex h-8 w-8 shrink-0 items-center justify-center rounded-full border transition',
                                  o.isCorrect
                                    ? 'border-emerald-500 bg-emerald-500 text-white'
                                    : 'border-slate-300 bg-white text-slate-400 hover:border-emerald-400',
                                  questionsLocked && 'cursor-not-allowed opacity-60',
                                )}
                                title="Mark as correct answer"
                              >
                                {o.isCorrect ? <CheckCircle2 className="h-4 w-4" /> : <span className="text-xs font-medium">{String.fromCharCode(65 + optIndex)}</span>}
                              </button>
                              <Input
                                value={o.optionText}
                                disabled={questionsLocked}
                                onChange={(e) => updateOption(q.key, o.key, { optionText: e.target.value })}
                                placeholder={`Option ${String.fromCharCode(65 + optIndex)}`}
                                className={cn(o.isCorrect && 'border-emerald-300 bg-emerald-50/50')}
                              />
                              {!questionsLocked && q.options.length > 2 && (
                                <Button type="button" variant="outline" size="sm" onClick={() => removeOption(q.key, o.key)}>
                                  <Trash2 className="h-3.5 w-3.5" />
                                </Button>
                              )}
                            </div>
                          ))}
                          {!questionsLocked && (
                            <Button type="button" variant="outline" size="sm" onClick={() => addOption(q.key)}>
                              <Plus className="mr-1 h-3.5 w-3.5" /> Add option
                            </Button>
                          )}
                        </div>
                      </div>
                    ))}

                    {!questionsLocked && (
                      <Button type="button" variant="outline" className="w-full border-dashed" onClick={addQuestion}>
                        <Plus className="mr-2 h-4 w-4" /> Add question
                      </Button>
                    )}
                  </div>
                </Panel>

                <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                  <div>
                    {saveMessage && (
                      <p className={cn('flex items-center gap-1 text-sm', saveMessage.includes('success') ? 'text-emerald-600' : 'text-red-600')}>
                        {!saveMessage.includes('success') && <AlertCircle className="h-4 w-4" />}
                        {saveMessage.includes('success') && <CheckCircle2 className="h-4 w-4" />}
                        {saveMessage}
                      </p>
                    )}
                  </div>
                  <Button
                    className="bg-[#2081A1] hover:bg-[#1a6d89]"
                    disabled={saveQuiz.isPending || !title.trim()}
                    onClick={() => {
                      setSaveMessage(null)
                      saveQuiz.mutate()
                    }}
                  >
                    <Save className="mr-2 h-4 w-4" />
                    {saveQuiz.isPending ? 'Saving…' : 'Save assessment'}
                  </Button>
                </div>
              </>
            )}
          </div>
        </div>
      </div>
    </DashboardShell>
  )
}
