import { Link, useLocation, useNavigate } from 'react-router-dom'
import { Bell, ChevronDown, LogOut, Menu, X, type LucideIcon } from 'lucide-react'
import { useState } from 'react'
import { authStore } from '@/lib/auth-store'
import { BrandLogo } from '@/components/brand/BrandLogo'
import { cn } from '@/lib/utils'

export interface UniversityNavItem {
  label: string
  href: string
  icon: LucideIcon
}

interface UniversityShellProps {
  portalTitle: string
  portalSubtitle?: string
  userName: string
  tenantLabel?: string
  navItems: UniversityNavItem[]
  children: React.ReactNode
}

function UserAvatar({ name }: { name: string }) {
  const initials = name.split(' ').map((n) => n[0]).join('').slice(0, 2).toUpperCase()
  return (
    <div className="flex h-10 w-10 items-center justify-center rounded-full bg-gradient-to-br from-[#2081A1] to-[#004a8f] text-sm font-bold text-white shadow-md ring-2 ring-white">
      {initials}
    </div>
  )
}

export function UniversityShell({
  portalTitle,
  portalSubtitle,
  userName,
  tenantLabel,
  navItems,
  children,
}: UniversityShellProps) {
  const location = useLocation()
  const navigate = useNavigate()
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [menuOpen, setMenuOpen] = useState(false)

  const sidebar = (
    <>
      <div className="relative border-b border-white/10 px-5 py-6">
        <div className="absolute inset-x-0 top-0 h-1 bg-gradient-to-r from-[#c9a227] via-[#2081A1] to-[#004a8f]" />
        <BrandLogo variant="light" size="sm" showCareTrack />
        {tenantLabel && (
          <p className="mt-2 text-[11px] font-medium tracking-wide text-slate-400">{tenantLabel}</p>
        )}
      </div>

      <p className="px-5 pt-4 text-[10px] font-semibold uppercase tracking-[0.2em] text-slate-500">Navigation</p>
      <nav className="flex-1 space-y-1 overflow-y-auto px-3 py-3 student-scroll">
        {navItems.map((item) => {
          const active =
            location.pathname === item.href ||
            (item.href !== '/admin' && location.pathname.startsWith(item.href))
          const Icon = item.icon
          return (
            <Link
              key={item.label + item.href}
              to={item.href}
              onClick={() => setSidebarOpen(false)}
              className={cn(
                'group flex items-center gap-3 rounded-xl px-3.5 py-3 text-sm font-medium transition-all duration-200',
                active
                  ? 'bg-white/10 text-white shadow-inner ring-1 ring-white/10'
                  : 'text-slate-400 hover:bg-white/5 hover:text-white',
              )}
            >
              <span
                className={cn(
                  'flex h-8 w-8 items-center justify-center rounded-lg transition-colors',
                  active ? 'bg-[#2081A1] text-white' : 'bg-white/5 text-slate-400 group-hover:bg-white/10 group-hover:text-white',
                )}
              >
                <Icon className="h-4 w-4" strokeWidth={1.75} />
              </span>
              {item.label}
              {active && <span className="ml-auto h-1.5 w-1.5 rounded-full bg-[#c9a227]" />}
            </Link>
          )
        })}
      </nav>

      <div className="border-t border-white/10 p-4">
        <p className="text-center text-[10px] text-slate-500">Powered by Apollo Hospitals</p>
      </div>
    </>
  )

  return (
    <div className="flex h-screen overflow-hidden bg-[#f4f6f8]">
      <aside className="hidden h-full w-[272px] flex-shrink-0 flex-col overflow-y-auto overscroll-none bg-gradient-to-b from-[#001a33] via-[#002952] to-[#001428] lg:flex">
        {sidebar}
      </aside>

      {sidebarOpen && (
        <div className="fixed inset-0 z-50 lg:hidden">
          <div className="absolute inset-0 bg-black/70 backdrop-blur-sm" onClick={() => setSidebarOpen(false)} />
          <aside className="relative flex h-full w-[280px] flex-col bg-gradient-to-b from-[#001a33] to-[#001428] shadow-2xl">
            <button type="button" onClick={() => setSidebarOpen(false)} className="absolute right-4 top-4 rounded-lg p-1 text-slate-400">
              <X className="h-5 w-5" />
            </button>
            {sidebar}
          </aside>
        </div>
      )}

      <div className="flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden">
        <header className="relative z-30 shrink-0 border-b border-slate-200/80 bg-white shadow-[0_1px_3px_rgba(0,40,80,0.06)]">
          <div className="absolute inset-x-0 top-0 h-0.5 bg-gradient-to-r from-[#004a8f] via-[#2081A1] to-[#c9a227]" />
          <div className="flex min-h-[5.5rem] items-center justify-between gap-4 px-5 py-4 pt-5 lg:px-8 lg:py-5 lg:pt-6">
            <div className="flex min-w-0 items-center gap-3">
              <button
                type="button"
                onClick={() => setSidebarOpen(true)}
                className="rounded-lg border border-slate-200 p-2 text-slate-600 hover:bg-slate-50 lg:hidden"
              >
                <Menu className="h-5 w-5" />
              </button>
              <div className="min-w-0 space-y-1">
                <span className="inline-flex items-center gap-1.5 rounded-full bg-[#004a8f]/8 px-2.5 py-1 text-[10px] font-semibold uppercase tracking-wider text-[#004a8f]">
                  University Admin
                </span>
                <h1 className="font-display truncate text-xl font-bold leading-tight tracking-tight text-[#0a1628] lg:text-2xl">
                  {portalTitle}
                </h1>
                {portalSubtitle && (
                  <p className="truncate text-sm leading-snug text-slate-500">{portalSubtitle}</p>
                )}
              </div>
            </div>

            <div className="relative flex items-center gap-2">
              <button
                type="button"
                className="hidden rounded-xl p-2.5 text-slate-500 transition hover:bg-slate-100 sm:flex"
                aria-label="Notifications"
              >
                <Bell className="h-5 w-5" />
              </button>
              <button
                type="button"
                onClick={() => setMenuOpen((o) => !o)}
                className="flex items-center gap-3 rounded-xl border border-slate-200/80 bg-slate-50/50 py-1.5 pl-1.5 pr-3 transition hover:border-[#2081A1]/30 hover:bg-white"
              >
                <UserAvatar name={userName} />
                <div className="hidden text-left sm:block">
                  <p className="max-w-[140px] truncate text-sm font-semibold text-slate-900">{userName}</p>
                  <p className="text-[11px] text-slate-500">College portal</p>
                </div>
                <ChevronDown className={cn('hidden h-4 w-4 text-slate-400 sm:block', menuOpen && 'rotate-180')} />
              </button>
              {menuOpen && (
                <>
                  <div className="fixed inset-0 z-40" onClick={() => setMenuOpen(false)} />
                  <div className="absolute right-0 top-full z-50 mt-2 w-56 overflow-hidden rounded-xl border border-slate-200 bg-white py-1 shadow-xl">
                    <div className="border-b border-slate-100 px-4 py-3">
                      <p className="text-sm font-semibold text-slate-900">{userName}</p>
                      <p className="text-xs text-slate-500">{tenantLabel}</p>
                    </div>
                    <button
                      type="button"
                      onClick={() => { authStore.clear(); navigate('/login') }}
                      className="flex w-full items-center gap-2 px-4 py-2.5 text-sm text-red-600 hover:bg-red-50"
                    >
                      <LogOut className="h-4 w-4" /> Sign out
                    </button>
                  </div>
                </>
              )}
            </div>
          </div>
        </header>

        <main className="min-h-0 flex-1 overflow-y-auto overscroll-none p-4 lg:p-8">
          {children}
        </main>
      </div>
    </div>
  )
}

export function UniPanel({
  title,
  children,
  className,
  action,
}: {
  title: string
  children: React.ReactNode
  className?: string
  action?: React.ReactNode
}) {
  return (
    <section
      className={cn(
        'rounded-2xl border border-slate-200/60 bg-white p-6 shadow-[0_2px_12px_rgba(0,40,80,0.04)]',
        className,
      )}
    >
      <div className="mb-5 flex items-center justify-between gap-4">
        <h2 className="text-base font-semibold text-[#0a1628]">{title}</h2>
        {action}
      </div>
      {children}
    </section>
  )
}
