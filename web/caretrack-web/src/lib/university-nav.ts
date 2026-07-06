import {
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
  { label: 'Reports', href: '/university/reports', icon: BarChart3 },
]
