import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft, Upload } from 'lucide-react'
import { api, getErrorMessage } from '@/lib/api-client'
import { usePageTransition } from '@/animations/usePageTransition'
import { AppLayout, PageSection } from '@/components/layout/AppLayout'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent } from '@/components/ui/card'

export function ContentLibraryPage() {
  const ref = usePageTransition()
  const queryClient = useQueryClient()
  const [moduleId, setModuleId] = useState('')
  const [title, setTitle] = useState('')
  const [selectedLesson, setSelectedLesson] = useState<string | null>(null)
  const [universityIds, setUniversityIds] = useState('')

  const modules = useQuery({
    queryKey: ['content-modules'],
    queryFn: async () => (await api.get('/content/modules')).data,
  })

  const createLesson = useMutation({
    mutationFn: async () =>
      api.post('/content/lessons', { moduleId, title, description: '', sortOrder: 1 }),
    onSuccess: (res) => {
      setSelectedLesson(res.data.id)
      setTitle('')
      queryClient.invalidateQueries({ queryKey: ['lessons'] })
    },
  })

  const updateStatus = useMutation({
    mutationFn: async ({ id, status }: { id: string; status: string }) =>
      api.patch(`/content/lessons/${id}/status`, { status }),
  })

  const publish = useMutation({
    mutationFn: async (id: string) => {
      const ids = universityIds.trim() ? universityIds.split(',').map((s) => s.trim()) : null
      await api.post(`/content/lessons/${id}/publish`, { universityIds: ids })
    },
  })

  async function uploadAsset(lessonId: string, file: File) {
    const form = new FormData()
    form.append('file', file)
    await api.post(`/content/lessons/${lessonId}/assets`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  }

  return (
    <div ref={ref}>
      <AppLayout
        title="Content Library"
        subtitle="Upload once, publish to universities"
        nav={<Button variant="outline" asChild><Link to="/console"><ArrowLeft className="mr-2 h-4 w-4" />Back</Link></Button>}
      >
        <PageSection title="Create Lesson">
          <Card className="max-w-xl">
            <CardContent className="space-y-4 pt-6">
              <div className="space-y-2">
                <Label>Module</Label>
                <select className="h-10 w-full rounded-md border border-slate-200 px-3 text-sm" value={moduleId} onChange={(e) => setModuleId(e.target.value)}>
                  <option value="">Select module</option>
                  {modules.data?.map((m: { moduleId: string; moduleTitle: string; programmeName: string }) => (
                    <option key={m.moduleId} value={m.moduleId}>{m.programmeName} · {m.moduleTitle}</option>
                  ))}
                </select>
              </div>
              <div className="space-y-2">
                <Label>Lesson Title</Label>
                <Input value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Introduction to Cardiovascular Assessment" />
              </div>
              {createLesson.error && <p className="text-sm text-red-600">{getErrorMessage(createLesson.error)}</p>}
              <Button onClick={() => createLesson.mutate()} disabled={!moduleId || !title}>Save as Draft</Button>
            </CardContent>
          </Card>
        </PageSection>

        {selectedLesson && (
          <PageSection title="Lesson Workflow">
            <Card className="max-w-xl">
              <CardContent className="space-y-4 pt-6">
                <p className="text-sm text-slate-500">Lesson ID: {selectedLesson}</p>
                <div className="flex flex-wrap gap-2">
                  <Button variant="outline" onClick={() => updateStatus.mutate({ id: selectedLesson, status: 'PendingReview' })}>Submit for Review</Button>
                  <Button variant="outline" onClick={() => api.post(`/content/lessons/${selectedLesson}/review`)}>Approve</Button>
                </div>
                <div className="space-y-2">
                  <Label>Publish to University IDs (comma-separated, empty = all)</Label>
                  <Input value={universityIds} onChange={(e) => setUniversityIds(e.target.value)} placeholder="Leave empty for all universities" />
                  <Button onClick={() => publish.mutate(selectedLesson)}>Publish</Button>
                </div>
                <div className="space-y-2">
                  <Label>Upload PDF/Video</Label>
                  <Input type="file" accept=".pdf,video/*" onChange={(e) => e.target.files?.[0] && uploadAsset(selectedLesson, e.target.files[0])} />
                  <Upload className="h-4 w-4 text-slate-400" />
                </div>
              </CardContent>
            </Card>
          </PageSection>
        )}
      </AppLayout>
    </div>
  )
}
