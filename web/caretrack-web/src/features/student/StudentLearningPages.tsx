import { useEffect, useRef, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, CheckCircle } from 'lucide-react'
import { api } from '@/lib/api-client'
import { usePageTransition, animateUnlock } from '@/animations/usePageTransition'
import { AppLayout, PageSection } from '@/components/layout/AppLayout'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { ProgressBar } from '@/components/ui/label'

export function ModulePage() {
  const { moduleId } = useParams<{ moduleId: string }>()
  const ref = usePageTransition()
  const unlockRef = useRef<HTMLDivElement>(null)

  const { data: module } = useQuery({
    queryKey: ['module', moduleId],
    queryFn: async () => (await api.get(`/students/me/modules/${moduleId}`)).data,
    enabled: !!moduleId,
  })

  useEffect(() => {
    if (module && !module.isLocked) animateUnlock(unlockRef.current)
  }, [module])

  return (
    <div ref={ref}>
      <AppLayout
        title={module?.title ?? 'Module'}
        subtitle={module?.description}
        nav={<Button variant="outline" asChild><Link to="/dashboard"><ArrowLeft className="mr-2 h-4 w-4" />Dashboard</Link></Button>}
      >
        <div ref={unlockRef}>
          <ProgressBar value={module?.progressPercent ?? 0} className="mb-6" />
          <PageSection title="Lessons">
            <div className="space-y-3">
              {module?.lessons?.map((l: { id: string; title: string; progressPercent: number; isCompleted: boolean }) => (
                <Card key={l.id}>
                  <CardContent className="flex items-center justify-between pt-6">
                    <div>
                      <p className="font-medium">{l.title}</p>
                      <ProgressBar value={l.progressPercent} className="mt-2 max-w-xs" />
                    </div>
                    <div className="flex items-center gap-2">
                      {l.isCompleted && <CheckCircle className="h-4 w-4 text-green-600" />}
                      <Button size="sm" asChild><Link to={`/learn/lessons/${l.id}`}>Open</Link></Button>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          </PageSection>
          {module?.progressPercent >= 100 && (
            <Button asChild className="mt-4"><Link to={`/learn/modules/${moduleId}/quiz`}>Take Quiz</Link></Button>
          )}
        </div>
      </AppLayout>
    </div>
  )
}

export function LessonPlayerPage() {
  const { lessonId } = useParams<{ lessonId: string }>()
  const ref = usePageTransition()
  const videoRef = useRef<HTMLVideoElement>(null)
  const queryClient = useQueryClient()
  const [watched, setWatched] = useState(0)

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
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['lesson', lessonId] }),
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
    <div ref={ref}>
      <AppLayout
        title={lesson?.title ?? 'Lesson'}
        subtitle={`Progress: ${lesson?.progressPercent ?? 0}%`}
        nav={<Button variant="outline" asChild><Link to="/dashboard"><ArrowLeft className="mr-2 h-4 w-4" />Back</Link></Button>}
      >
        <Card className="mb-6">
          <CardContent className="pt-6">
            {videoAsset ? (
              <video ref={videoRef} controls className="w-full rounded-lg" src={videoAsset.blobUrl} />
            ) : pdfAsset ? (
              <iframe title="PDF" src={pdfAsset.blobUrl} className="h-[600px] w-full rounded-lg border" />
            ) : (
              <p className="text-slate-500">No media attached to this lesson yet.</p>
            )}
            <ProgressBar value={lesson?.progressPercent ?? 0} className="mt-4" />
            <p className="mt-2 text-xs text-slate-400">Watched: {watched}s · Progress syncs every 30s</p>
            {(lesson?.progressPercent ?? 0) >= 90 && !lesson?.status?.includes('Completed') && (
              <Button className="mt-4" onClick={() => markComplete.mutate()} disabled={markComplete.isPending}>
                Mark Complete
              </Button>
            )}
          </CardContent>
        </Card>
      </AppLayout>
    </div>
  )
}

export function QuizPage() {
  const { moduleId } = useParams<{ moduleId: string }>()
  const ref = usePageTransition()
  const [answers, setAnswers] = useState<Record<string, string>>({})
  const [result, setResult] = useState<{ scorePercent: number; passed: boolean } | null>(null)

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
  }

  return (
    <div ref={ref}>
      <AppLayout title={quiz?.title ?? 'Quiz'} subtitle={`Pass: ${quiz?.passPercentage ?? 60}% · Attempts left: ${quiz?.remainingAttempts ?? 0}`}>
        {result ? (
          <Card>
            <CardContent className="pt-6">
              <p className="text-lg font-semibold">{result.passed ? 'Passed!' : 'Not passed'}</p>
              <p>Score: {result.scorePercent}%</p>
            </CardContent>
          </Card>
        ) : (
          <div className="space-y-6">
            {quiz?.questions?.map((q: { id: string; questionText: string; options: { id: string; optionText: string }[] }) => (
              <Card key={q.id}>
                <CardContent className="space-y-3 pt-6">
                  <p className="font-medium">{q.questionText}</p>
                  {q.options.map((o) => (
                    <label key={o.id} className="flex cursor-pointer items-center gap-2 rounded-md border p-3 hover:bg-slate-50">
                      <input type="radio" name={q.id} value={o.id} onChange={() => setAnswers({ ...answers, [q.id]: o.id })} />
                      {o.optionText}
                    </label>
                  ))}
                </CardContent>
              </Card>
            ))}
            <Button onClick={submit} disabled={!quiz || Object.keys(answers).length < (quiz?.questions?.length ?? 0)}>Submit Quiz</Button>
          </div>
        )}
      </AppLayout>
    </div>
  )
}
