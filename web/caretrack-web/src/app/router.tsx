import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ProtectedRoute } from '@/app/ProtectedRoute'
import { LoginPage } from '@/features/auth/LoginPage'
import { ApolloDashboard } from '@/features/apollo/ApolloDashboard'
import { ContentLibraryPage } from '@/features/apollo/ContentLibraryPage'
import { UniversityDashboard } from '@/features/university/UniversityDashboard'
import { StudentDashboard } from '@/features/student/StudentDashboard'
import { SignoffsPage } from '@/features/supervisor/SignoffsPage'
import { ModulePage, LessonPlayerPage, QuizPage } from '@/features/student/StudentLearningPages'
import { UniversityReportsPage, ApolloReportsPage } from '@/features/reports/ReportsPages'
import { authStore } from '@/lib/auth-store'
import { getRoleRedirect } from '@/lib/utils'

const queryClient = new QueryClient({
  defaultOptions: { queries: { staleTime: 30_000, retry: 1 } },
})

function RootRedirect() {
  const auth = authStore.get()
  if (auth && authStore.isAuthenticated()) {
    return <Navigate to={getRoleRedirect(auth.role)} replace />
  }
  return <Navigate to="/login" replace />
}

export function AppRouter() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<RootRedirect />} />
          <Route path="/login" element={<LoginPage />} />

          <Route element={<ProtectedRoute roles={['ApolloAdmin', 'ApolloFaculty']} />}>
            <Route path="/console" element={<ApolloDashboard />} />
            <Route path="/apollo" element={<Navigate to="/console" replace />} />
            <Route path="/apollo/content" element={<ContentLibraryPage />} />
            <Route path="/apollo/reports" element={<ApolloReportsPage />} />
          </Route>

          <Route element={<ProtectedRoute roles={['UniversityAdmin']} />}>
            <Route path="/admin" element={<UniversityDashboard />} />
            <Route path="/university" element={<Navigate to="/admin" replace />} />
            <Route path="/university/reports" element={<UniversityReportsPage />} />
          </Route>

          <Route element={<ProtectedRoute roles={['Supervisor']} />}>
            <Route path="/signoffs" element={<SignoffsPage />} />
          </Route>

          <Route element={<ProtectedRoute roles={['Student']} />}>
            <Route path="/dashboard" element={<StudentDashboard />} />
            <Route path="/learn" element={<Navigate to="/dashboard" replace />} />
            <Route path="/learn/modules/:moduleId" element={<ModulePage />} />
            <Route path="/learn/modules/:moduleId/quiz" element={<QuizPage />} />
            <Route path="/learn/lessons/:lessonId" element={<LessonPlayerPage />} />
          </Route>

          <Route path="*" element={<RootRedirect />} />
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  )
}
