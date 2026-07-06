import { useQuery } from '@tanstack/react-query'
import { AlertTriangle, ArrowRight, BookMarked, GraduationCap, UserPlus, Users } from 'lucide-react'
import { Link } from 'react-router-dom'
import { api } from '@/lib/api-client'
import { ProgressRing } from '@/components/dashboard/ProgressRing'
import { Sparkline } from '@/components/dashboard/Sparkline'
import { StatCard } from '@/components/dashboard/StatCard'
import { UniPanel } from '@/components/layout/UniversityShell'
import { Button } from '@/components/ui/button'

const quickLinks = [
  {
    title: 'Programme Assignment',
    description: 'Assign programmes and cohorts to students',
    href: '/admin/programmes',
    icon: BookMarked,
    accent: 'bg-[#2081A1]/10 text-[#2081A1]',
  },
  {
    title: 'Enrol Students',
    description: 'Invite new students or import CSV',
    href: '/admin/enrolment',
    icon: UserPlus,
    accent: 'bg-[#004a8f]/10 text-[#004a8f]',
  },
  {
    title: 'View Reports',
    description: 'Cohort analytics and exports',
    href: '/university/reports',
    icon: GraduationCap,
    accent: 'bg-violet-500/10 text-violet-600',
  },
]

export function UniversityDashboard() {
  const cohorts = useQuery({ queryKey: ['cohorts'], queryFn: async () => (await api.get('/cohorts')).data })
  const report = useQuery({ queryKey: ['university-report'], queryFn: async () => (await api.get('/reports/university/students')).data })

  const avgProgress = Math.round(report.data?.averageProgress ?? 0)
  const atRisk = report.data?.atRiskCount ?? 0

  return (
    <div className="space-y-8">
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard title="Total students" value={report.data?.totalStudents ?? 0} icon={Users} accent="teal" />
        <StatCard title="Avg progress" value={`${avgProgress}%`} icon={GraduationCap} accent="blue" trend="up" />
        <StatCard
          title="At risk"
          value={atRisk}
          change={atRisk > 0 ? 'Needs attention' : 'All on track'}
          icon={AlertTriangle}
          accent="amber"
          trend={atRisk > 0 ? 'down' : 'up'}
        />
        <StatCard title="Active cohorts" value={cohorts.data?.length ?? 0} icon={GraduationCap} accent="violet" />
      </div>

      <div className="grid gap-6 xl:grid-cols-3">
        <UniPanel title="Cohort health" className="xl:col-span-1">
          <div className="flex flex-col items-center py-4">
            <ProgressRing value={avgProgress} label="Avg" sublabel="Semester progress" />
          </div>
          <Sparkline data={[30, 35, 38, 42, 45, 48, avgProgress]} className="mt-4 h-12 w-full" />
        </UniPanel>

        <UniPanel title="Quick actions" className="xl:col-span-2">
          <div className="grid gap-3 sm:grid-cols-3">
            {quickLinks.map((link) => {
              const Icon = link.icon
              return (
                <Link
                  key={link.href}
                  to={link.href}
                  className="group flex flex-col rounded-xl border border-slate-200 p-4 transition hover:border-[#2081A1]/40 hover:shadow-md"
                >
                  <span className={`mb-3 flex h-10 w-10 items-center justify-center rounded-lg ${link.accent}`}>
                    <Icon className="h-5 w-5" />
                  </span>
                  <p className="font-semibold text-slate-900 group-hover:text-[#004a8f]">{link.title}</p>
                  <p className="mt-1 flex-1 text-xs text-slate-500">{link.description}</p>
                  <ArrowRight className="mt-3 h-4 w-4 text-slate-400 transition group-hover:translate-x-0.5 group-hover:text-[#2081A1]" />
                </Link>
              )
            })}
          </div>
          <div className="mt-6 flex gap-2">
            <Button asChild className="bg-[#004a8f] hover:bg-[#003a70]">
              <Link to="/admin/programmes">Assign programmes</Link>
            </Button>
            <Button variant="outline" asChild>
              <Link to="/admin/students">View students</Link>
            </Button>
          </div>
        </UniPanel>
      </div>
    </div>
  )
}
