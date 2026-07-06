import {
  Award,
  BookOpen,
  ClipboardCheck,
  LayoutDashboard,
  Stethoscope,
  Video,
  type LucideIcon,
} from 'lucide-react'

export const STUDENT_NAV: { label: string; href: string; icon: LucideIcon }[] = [
  { label: 'Dashboard', href: '/dashboard', icon: LayoutDashboard },
  { label: 'Curriculum', href: '/dashboard/curriculum', icon: BookOpen },
  { label: 'Live Classes', href: '/dashboard/live', icon: Video },
  { label: 'Clinical Rotations', href: '/dashboard/clinical', icon: Stethoscope },
  { label: 'Assessments', href: '/dashboard/assessments', icon: ClipboardCheck },
  { label: 'Certificates', href: '/dashboard/certificates', icon: Award },
]
