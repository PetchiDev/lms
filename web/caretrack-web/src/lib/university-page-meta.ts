export interface UniversityPageHandle {
  title: string
  subtitle?: string
}

export const UNIVERSITY_PAGE_META: Record<string, UniversityPageHandle> = {
  '/admin': {
    title: 'College Dashboard',
    subtitle: 'Enrolment, compliance and cohort health at a glance',
  },
  '/admin/programmes': {
    title: 'Programme Assignment',
    subtitle: 'Assign allied health programmes and cohorts to enrolled students',
  },
  '/admin/enrolment': {
    title: 'Student Enrolment',
    subtitle: 'Create students and import cohort rosters',
  },
  '/admin/students': {
    title: 'Students',
    subtitle: 'Full roster with programme and cohort details',
  },
  '/university/reports': {
    title: 'Cohort Analytics',
    subtitle: 'Student progress and compliance reports',
  },
}

export function getUniversityPageMeta(pathname: string): UniversityPageHandle {
  return UNIVERSITY_PAGE_META[pathname] ?? { title: 'College Portal' }
}
