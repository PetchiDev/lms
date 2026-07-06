import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { queryClient } from '@/lib/query-client'
import { ProtectedRoute } from '@/app/ProtectedRoute'
import { LoginPage } from '@/features/auth/LoginPage'
import { ApolloDashboard } from '@/features/apollo/ApolloDashboard'
import { ProgrammeCataloguePage } from '@/features/apollo/ProgrammeCataloguePage'
import { AssessmentBuilderPage } from '@/features/apollo/AssessmentBuilderPage'
import { CertificateTemplatePage } from '@/features/apollo/CertificateTemplatePage'
import { ContentLibraryPage } from '@/features/apollo/ContentLibraryPage'
import { UniversitiesPage } from '@/features/apollo/UniversitiesPage'
import { UniversityDetailPage } from '@/features/apollo/UniversityDetailPage'
import { UniversityAdminLayout } from '@/components/layout/UniversityAdminLayout'
import { UniversityDashboard } from '@/features/university/UniversityDashboard'
import { ProgrammeAssignmentPage } from '@/features/university/ProgrammeAssignmentPage'
import { UniversityEnrolmentPage } from '@/features/university/UniversityEnrolmentPage'
import { UniversityStudentsPage } from '@/features/university/UniversityStudentsPage'
import { StudentDashboard } from '@/features/student/StudentDashboard'
import { ClinicalRotationPage } from '@/features/student/ClinicalRotationPage'
import { CurriculumPage, LiveClassesPage, AssessmentsPage } from '@/features/student/StudentSubPages'
import { StudentCertificatesPage } from '@/features/student/StudentCertificatesPage'
import { SignoffsPage } from '@/features/supervisor/SignoffsPage'
import { ModulePage, LessonPlayerPage, QuizPage } from '@/features/student/StudentLearningPages'
import { UniversityReportsPage, ApolloReportsPage } from '@/features/reports/ReportsPages'
import { authStore } from '@/lib/auth-store'
import { getRoleRedirect } from '@/lib/utils'

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
            <Route path="/apollo/catalogue" element={<ProgrammeCataloguePage />} />
            <Route path="/apollo/assessments" element={<AssessmentBuilderPage />} />
            <Route path="/apollo/reports" element={<ApolloReportsPage />} />
            <Route path="/apollo/universities/:universityId" element={<UniversityDetailPage />} />
          </Route>

          <Route element={<ProtectedRoute roles={['ApolloAdmin']} />}>
            <Route path="/apollo/universities" element={<UniversitiesPage />} />
            <Route path="/apollo/certificates" element={<CertificateTemplatePage />} />
          </Route>

          <Route element={<ProtectedRoute roles={['UniversityAdmin']} />}>
            <Route element={<UniversityAdminLayout />}>
              <Route path="/admin" element={<UniversityDashboard />} />
              <Route path="/admin/programmes" element={<ProgrammeAssignmentPage />} />
              <Route path="/admin/enrolment" element={<UniversityEnrolmentPage />} />
              <Route path="/admin/students" element={<UniversityStudentsPage />} />
              <Route path="/university/reports" element={<UniversityReportsPage />} />
            </Route>
            <Route path="/university" element={<Navigate to="/admin" replace />} />
          </Route>

          <Route element={<ProtectedRoute roles={['Supervisor']} />}>
            <Route path="/signoffs" element={<SignoffsPage />} />
          </Route>

          <Route element={<ProtectedRoute roles={['Student']} />}>
            <Route path="/dashboard" element={<StudentDashboard />} />
            <Route path="/dashboard/curriculum" element={<CurriculumPage />} />
            <Route path="/dashboard/live" element={<LiveClassesPage />} />
            <Route path="/dashboard/clinical" element={<ClinicalRotationPage />} />
            <Route path="/dashboard/assessments" element={<AssessmentsPage />} />
            <Route path="/dashboard/certificates" element={<StudentCertificatesPage />} />
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
