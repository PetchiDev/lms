import { BarChart3, BookOpen, Building2, ClipboardList, LayoutDashboard, Award, Layers } from 'lucide-react'
import type { NavItem } from '@/components/layout/DashboardShell'

export function getApolloNavItems(isAdmin: boolean): NavItem[] {
  return [
    { label: 'Overview', href: '/console', icon: LayoutDashboard },
    { label: 'Content Library', href: '/apollo/content', icon: BookOpen },
    { label: 'Catalogue', href: '/apollo/catalogue', icon: Layers },
    { label: 'Assessments', href: '/apollo/assessments', icon: ClipboardList },
    ...(isAdmin ? [
      { label: 'Universities', href: '/apollo/universities', icon: Building2 },
      { label: 'Certificates', href: '/apollo/certificates', icon: Award },
    ] : []),
    { label: 'Analytics', href: '/apollo/reports', icon: BarChart3 },
  ]
}
