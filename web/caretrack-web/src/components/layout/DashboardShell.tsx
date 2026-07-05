import { Link, useLocation, useNavigate } from 'react-router-dom'
import { GraduationCap, LogOut, type LucideIcon } from 'lucide-react'
import { authStore } from '@/lib/auth-store'
import { cn } from '@/lib/utils'

export interface NavItem {
  label: string
  href: string
  icon: LucideIcon
}

interface DashboardShellProps {
  roleLabel: string
  portalTitle: string
  userName: string
  tenantLabel?: string
  navItems: NavItem[]
  children: React.ReactNode
  accent?: 'teal' | 'indigo' | 'emerald'
}

const accentHeader = {
  teal: 'text-teal-600',
  indigo: 'text-indigo-600',
  emerald: 'text-emerald-600',
}

const accentBg = {
  teal: 'bg-gradient-to-br from-teal-600 to-cyan-800',
  indigo: 'bg-gradient-to-br from-indigo-600 to-violet-800',
  emerald: 'bg-gradient-to-br from-emerald-600 to-teal-800',
}

export function DashboardShell({
  roleLabel,
  portalTitle,
  userName,
  tenantLabel,
  navItems,
  children,
  accent = 'teal',
}: DashboardShellProps) {
  const location = useLocation()
  const navigate = useNavigate()

  return (
    <div className="flex min-h-screen bg-[#f4f7fb]">
      <aside className={cn('hidden w-64 flex-shrink-0 flex-col text-white lg:flex', accentBg[accent])}>
        <div className="border-b border-white/10 p-6">
          <div className="flex items-center gap-2">
            <GraduationCap className="h-7 w-7" />
            <div>
              <p className="text-lg font-bold tracking-tight">CareTrack</p>
              <p className="text-xs text-white/70">{roleLabel}</p>
            </div>
          </div>
        </div>
        <nav className="flex-1 space-y-1 p-4">
          {navItems.map((item) => {
            const active = location.pathname === item.href || location.pathname.startsWith(item.href + '/')
            const Icon = item.icon
            return (
              <Link
                key={item.href}
                to={item.href}
                className={cn(
                  'flex items-center gap-3 rounded-xl px-4 py-3 text-sm font-medium transition-all',
                  active ? 'bg-white/20 shadow-lg backdrop-blur' : 'hover:bg-white/10'
                )}
              >
                <Icon className="h-4 w-4" />
                {item.label}
              </Link>
            )
          })}
        </nav>
        <div className="border-t border-white/10 p-4">
          <div className="rounded-xl bg-white/10 p-3 backdrop-blur">
            <p className="truncate text-sm font-semibold">{userName}</p>
            {tenantLabel && <p className="truncate text-xs text-white/70">{tenantLabel}</p>}
          </div>
          <button
            type="button"
            onClick={() => { authStore.clear(); navigate('/login') }}
            className="mt-3 flex w-full items-center gap-2 rounded-xl px-4 py-2 text-sm hover:bg-white/10"
          >
            <LogOut className="h-4 w-4" />
            Sign out
          </button>
        </div>
      </aside>

      <div className="flex flex-1 flex-col">
        <header className="sticky top-0 z-10 border-b border-slate-200/80 bg-white/80 px-6 py-4 backdrop-blur-md lg:px-8">
          <div className="flex items-center justify-between">
            <div>
              <p className={cn('text-xs font-semibold uppercase tracking-widest', accentHeader[accent])}>{roleLabel}</p>
              <h1 className="text-2xl font-bold text-slate-900">{portalTitle}</h1>
            </div>
            <div className="flex items-center gap-3 lg:hidden">
              <button
                type="button"
                onClick={() => { authStore.clear(); navigate('/login') }}
                className="rounded-lg border px-3 py-1.5 text-sm"
              >
                Logout
              </button>
            </div>
          </div>
        </header>
        <main className="flex-1 p-6 lg:p-8">{children}</main>
      </div>
    </div>
  )
}

export function Panel({ title, children, className, action }: { title: string; children: React.ReactNode; className?: string; action?: React.ReactNode }) {
  return (
    <section data-animate-card className={cn('rounded-2xl border border-slate-200/80 bg-white p-6 shadow-sm', className)}>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-semibold text-slate-900">{title}</h2>
        {action}
      </div>
      {children}
    </section>
  )
}
