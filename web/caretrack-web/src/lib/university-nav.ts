import {
  Award,
  BarChart3,
  BookMarked,
  LayoutDashboard,
  UserPlus,
  Users,
  type LucideIcon,
} from 'lucide-react'

export const UNIVERSITY_NAV: { label: string; href: string; icon: LucideIcon }[] = [
  { label: 'Dashboard', href: '/admin', icon: LayoutDashboard },
  { label: 'Programme Assignment', href: '/admin/programmes', icon: BookMarked },
  { label: 'Enrolment', href: '/admin/enrolment', icon: UserPlus },
  { label: 'Students', href: '/admin/students', icon: Users },
  { label: 'Certificates', href: '/admin/certificates', icon: Award },
  { label: 'Reports', href: '/university/reports', icon: BarChart3 },
]
