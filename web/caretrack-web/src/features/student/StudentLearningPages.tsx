import { useEffect, useRef, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, CheckCircle, CheckCircle2, Download, Play } from 'lucide-react'
import { api } from '@/lib/api-client'
import { authStore } from '@/lib/auth-store'
import { STUDENT_NAV } from '@/lib/student-nav'
import { animateUnlock } from '@/animations/usePageTransition'
import { StudentShell } from '@/components/layout/StudentShell'
import { Button } from '@/components/ui/button'
import { ProgressBar } from '@/components/ui/label'

function useStudentShellProps() {
  const auth = authStore.get()!
  const { data } = useQuery({
    queryKey: ['student-dashboard'],
    queryFn: async () => (await api.get('/students/me/dashboard')).data,
  })
  return {
    userName: data?.studentName ?? auth.fullName,
    tenantLabel: data?.cohortName ? `${data.cohortName} · Apollo` : 'Meridian × Apollo',
    yearLabel: data ? `Year ${data.currentYear} · Semester ${data.currentSemester}` : undefined,
    navItems: STUDENT_NAV,
  }
}

export function ModulePage() {
  const { moduleId } = useParams<{ moduleId: string }>()
  const unlockRef = useRef<HTMLDivElement>(null)
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const shell = useStudentShellProps()

  const { data: module } = useQuery({
    queryKey: ['module', moduleId],
    queryFn: async () => (await api.get(`/students/me/modules/${moduleId}`)).data,
    enabled: !!moduleId,
  })

  const markAll = useMutation({
    mutationFn: async () => (await api.post(`/students/me/modules/${moduleId}/complete-all`)).data,
    onSuccess: (result: { allCurriculumComplete: boolean; moduleCompleted: boolean }) => {
      queryClient.invalidateQueries({ queryKey: ['module', moduleId] })
      queryClient.invalidateQueries({ queryKey: ['student-dashboard'] })
      if (result.allCurriculumComplete) {
        navigate('/dashboard/assessments')
      } else if (result.moduleCompleted) {
        navigate(`/learn/modules/${moduleId}/quiz`)
      }
    },
  })

  useEffect(() => {
    if (module && !module.isLocked) animateUnlock(unlockRef.current)
  }, [module])

  return (
    <StudentShell {...shell} resumeHref={`/learn/modules/${moduleId}`}>
      <div ref={unlockRef} className="mx-auto max-w-4xl">
        <Button variant="ghost" size="sm" className="mb-4 text-[#2d5f5a]" asChild>
          <Link to="/dashboard/curriculum"><ArrowLeft className="mr-1 h-4 w-4" /> Curriculum</Link>
        </Button>
        <h1 className="font-display text-3xl font-bold text-[#1a1d1f]">{module?.title ?? 'Module'}</h1>
        <p className="mt-2 text-slate-600">{module?.description}</p>
        <div className="my-6 flex flex-wrap items-center gap-4">
          <ProgressBar value={module?.progressPercent ?? 0} className="h-2 flex-1" />
          <Button
            variant="outline"
            size="sm"
            className="border-[#2d5f5a] text-[#2d5f5a]"
            onClick={() => markAll.mutate()}
            disabled={markAll.isPending || (module?.progressPercent ?? 0) >= 100}
          >
            <CheckCircle2 className="mr-2 h-4 w-4" />
            Mark all as done
          </Button>
        </div>
        <div className="space-y-3">
          {module?.lessons?.map((l: { id: string; title: string; progressPercent: number; isCompleted: boolean }) => (
            <div key={l.id} className="flex items-center justify-between rounded-2xl border border-[#e0e4d8] bg-white p-5 shadow-sm transition hover:shadow-md">
              <div>
                <p className="font-medium text-slate-900">{l.title}</p>
                <ProgressBar value={l.progressPercent} className="mt-2 max-w-xs" />
              </div>
              <div className="flex items-center gap-2">
                {l.isCompleted && <CheckCircle className="h-5 w-5 text-emerald-500" />}
                <Button size="sm" className="bg-[#2d5f5a]" asChild>
                  <Link to={`/learn/lessons/${l.id}`}>Open</Link>
                </Button>
              </div>
            </div>
          ))}
        </div>
        {module?.progressPercent >= 100 && (
          <Button className="mt-6 bg-[#2d5f5a]" asChild>
            <Link to={`/learn/modules/${moduleId}/quiz`}>Take Quiz →</Link>
          </Button>
        )}
      </div>
    </StudentShell>
  )
}

export function LessonPlayerPage() {
  const { lessonId } = useParams<{ lessonId: string }>()
  const videoRef = useRef<HTMLVideoElement>(null)
  const queryClient = useQueryClient()
  const [watched, setWatched] = useState(0)
  const shell = useStudentShellProps()

  const { data: lesson } = useQuery({
    queryKey: ['lesson', lessonId],
    queryFn: async () => (await api.get(`/students/me/lessons/${lessonId}`)).data,
    enabled: !!lessonId,
  })

  const updateProgress = useMutation({
    mutationFn: async (payload: { watchedSeconds: number; progressPercent: number }) =>
      api.post(`/students/me/lessons/${lessonId}/progress`, payload),
  })

  const markComplete = useMutation({
    mutationFn: async () => api.post(`/students/me/lessons/${lessonId}/complete`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lesson', lessonId] })
      queryClient.invalidateQueries({ queryKey: ['student-dashboard'] })
    },
  })

  useEffect(() => {
    const interval = setInterval(() => {
      const video = videoRef.current
      if (!video || !lesson) return
      const progressPercent = video.duration ? Math.round((video.currentTime / video.duration) * 100) : 0
      setWatched(Math.floor(video.currentTime))
      updateProgress.mutate({ watchedSeconds: Math.floor(video.currentTime), progressPercent })
    }, 30000)
    return () => clearInterval(interval)
  }, [lesson, lessonId])

  const videoAsset = lesson?.assets?.find((a: { assetType: string }) => a.assetType === 'Video')
  const pdfAsset = lesson?.assets?.find((a: { assetType: string }) => a.assetType === 'Pdf')

  return (
    <StudentShell {...shell}>
      <div className="mx-auto max-w-4xl">
        <h1 className="font-display text-2xl font-bold text-[#1a1d1f]">{lesson?.title ?? 'Lesson'}</h1>
        <p className="mt-1 text-sm text-slate-500">Progress: {lesson?.progressPercent ?? 0}%</p>
        <div className="mt-6 overflow-hidden rounded-2xl border border-[#e0e4d8] bg-white shadow-lg">
          <div className="p-2">
            {videoAsset ? (
              <video ref={videoRef} controls className="w-full rounded-xl" src={videoAsset.blobUrl} />
            ) : pdfAsset ? (
              <iframe title="PDF" src={pdfAsset.blobUrl} className="h-[600px] w-full rounded-xl" />
            ) : (
              <div className="flex aspect-video items-center justify-center bg-slate-100">
                <Play className="h-12 w-12 text-slate-300" />
                <p className="ml-2 text-slate-500">No media attached yet</p>
              </div>
            )}
          </div>
          <div className="border-t border-slate-100 p-5">
            <ProgressBar value={lesson?.progressPercent ?? 0} className="h-2" />
            <p className="mt-2 text-xs text-slate-400">Watched: {watched}s · Auto-saves every 30s</p>
            <div className="mt-4 flex flex-wrap gap-2">
              <Button
                variant="outline"
                className="border-[#2d5f5a] text-[#2d5f5a]"
                onClick={() => markComplete.mutate()}
                disabled={markComplete.isPending || lesson?.status?.includes('Completed')}
              >
                <CheckCircle2 className="mr-2 h-4 w-4" />
                Mark as done
              </Button>
            </div>
          </div>
        </div>
      </div>
    </StudentShell>
  )
}

export function QuizPage() {
  const { moduleId } = useParams<{ moduleId: string }>()
  const [answers, setAnswers] = useState<Record<string, string>>({})
  const [result, setResult] = useState<{
    scorePercent: number
    passed: boolean
    certificate?: { pdfBlobUrl?: string; certificateNumber?: string }
    semesterAdvance?: { completed: boolean; message: string; newYear?: number; newSemester?: number }
  } | null>(null)
  const shell = useStudentShellProps()
  const queryClient = useQueryClient()

  const { data: quiz } = useQuery({
    queryKey: ['quiz', moduleId],
    queryFn: async () => (await api.get(`/students/me/modules/${moduleId}/quiz`)).data,
    enabled: !!moduleId,
  })

  async function submit() {
    if (!quiz) return
    const payload = {
      answers: quiz.questions.map((q: { id: string }) => ({
        questionId: q.id,
        selectedOptionId: answers[q.id],
      })),
    }
    const { data } = await api.post(`/students/me/quizzes/${quiz.id}/attempts`, payload)
    setResult(data)
    queryClient.invalidateQueries({ queryKey: ['student-dashboard'] })
  }

  return (
    <StudentShell {...shell}>
      <div className="mx-auto max-w-2xl">
        <h1 className="font-display text-3xl font-bold text-[#1a1d1f]">{quiz?.title ?? 'Assessment'}</h1>
        <p className="mt-2 text-slate-500">Pass mark: {quiz?.passPercentage ?? 60}% · Attempts left: {quiz?.remainingAttempts ?? 0}</p>

        {result ? (
          <div className={`mt-8 rounded-2xl border p-8 text-center ${result.passed ? 'border-emerald-200 bg-emerald-50' : 'border-red-200 bg-red-50'}`}>
            <p className="font-display text-2xl font-bold">{result.passed ? '🎉 Passed!' : 'Keep learning'}</p>
            <p className="mt-2 text-lg">Score: {result.scorePercent}%</p>
            {result.passed && result.scorePercent >= 60 && result.certificate?.pdfBlobUrl && (
              <Button className="mt-4 gap-2 bg-[#2d5f5a]" asChild>
                <a href={result.certificate.pdfBlobUrl} download target="_blank" rel="noreferrer">
                  <Download className="h-4 w-4" />
                  Download certificate (PDF)
                </a>
              </Button>
            )}
            {result.passed && result.scorePercent >= 60 && (
              <p className="mt-3 text-sm text-emerald-700">
                Certificate generated ·{' '}
                <a href="/dashboard/certificates" className="font-medium underline">View all certificates</a>
              </p>
            )}
            {result.passed && result.semesterAdvance?.completed && result.semesterAdvance.newYear && (
              <div className="mt-4 rounded-xl border border-[#2d5f5a]/30 bg-white px-4 py-3 text-sm text-[#2d5f5a]">
                <p className="font-semibold">Next semester unlocked!</p>
                <p className="mt-1">{result.semesterAdvance.message}</p>
                <Button className="mt-3 bg-[#2d5f5a]" asChild>
                  <Link to="/dashboard/curriculum">Go to new curriculum →</Link>
                </Button>
              </div>
            )}
            <Button className="mt-4" variant="outline" asChild><Link to="/dashboard/assessments">Back to assessments</Link></Button>
          </div>
        ) : (
          <div className="mt-8 space-y-5">
            {quiz?.questions?.map((q: { id: string; questionText: string; options: { id: string; optionText: string }[] }, i: number) => (
              <div key={q.id} className="rounded-2xl border border-[#e0e4d8] bg-white p-6 shadow-sm">
                <p className="mb-4 font-medium text-slate-900"><span className="text-[#2d5f5a]">Q{i + 1}.</span> {q.questionText}</p>
                <div className="space-y-2">
                  {q.options.map((o) => (
                    <label
                      key={o.id}
                      className={`flex cursor-pointer items-center gap-3 rounded-xl border p-3.5 transition ${
                        answers[q.id] === o.id ? 'border-[#2d5f5a] bg-[#2d5f5a]/5' : 'border-slate-100 hover:border-slate-200'
                      }`}
                    >
                      <input type="radio" name={q.id} value={o.id} onChange={() => setAnswers({ ...answers, [q.id]: o.id })} className="accent-[#2d5f5a]" />
                      {o.optionText}
                    </label>
                  ))}
                </div>
              </div>
            ))}
            <Button
              className="w-full bg-[#2d5f5a] py-6 text-base"
              onClick={submit}
              disabled={!quiz || Object.keys(answers).length < (quiz?.questions?.length ?? 0)}
            >
              Submit Assessment
            </Button>
          </div>
        )}
      </div>
    </StudentShell>
  )
}
