import { Link, useLocation } from 'react-router-dom'
import { Bell, ChevronDown, LogOut, Menu, X, type LucideIcon } from 'lucide-react'
import { useState } from 'react'
import { authStore } from '@/lib/auth-store'
import { BrandLogo } from '@/components/brand/BrandLogo'
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
  accent?: 'apollo' | 'indigo' | 'emerald'
}

const accentActive = {
  apollo: 'bg-[#2081A1]/15 text-[#2081A1] border-[#2081A1]/30',
  indigo: 'bg-indigo-500/15 text-indigo-300 border-indigo-500/30',
  emerald: 'bg-emerald-500/15 text-emerald-300 border-emerald-500/30',
}

const accentBadge = {
  apollo: 'bg-[#2081A1]/10 text-[#2081A1]',
  indigo: 'bg-indigo-50 text-indigo-700',
  emerald: 'bg-emerald-50 text-emerald-700',
}

function UserAvatar({ name }: { name: string }) {
  const initials = name
    .split(' ')
    .map((n) => n[0])
    .join('')
    .slice(0, 2)
    .toUpperCase()
  return (
    <div className="flex h-9 w-9 items-center justify-center rounded-full bg-gradient-to-br from-[#2081A1] to-[#1a6d89] text-xs font-bold text-white shadow-sm ring-2 ring-white">
      {initials}
    </div>
  )
}

export function DashboardShell({
  roleLabel,
  portalTitle,
  userName,
  tenantLabel,
  navItems,
  children,
  accent = 'apollo',
}: DashboardShellProps) {
  const location = useLocation()
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [userMenuOpen, setUserMenuOpen] = useState(false)

  function handleLogout() {
    authStore.clear()
    window.location.replace('/#/login')
  }

  const sidebarContent = (
    <>
      <div className="flex h-16 items-center border-b border-white/10 px-5">
        <BrandLogo variant="light" size="sm" />
      </div>

      <div className="px-4 py-4">
        <p className="px-3 text-[10px] font-semibold uppercase tracking-widest text-slate-500">Navigation</p>
        <nav className="mt-2 space-y-0.5">
          {navItems.map((item) => {
            const active = location.pathname === item.href || (item.href !== '/' && location.pathname.startsWith(item.href + '/'))
            const Icon = item.icon
            return (
              <Link
                key={item.label + item.href}
                to={item.href}
                onClick={() => setSidebarOpen(false)}
                className={cn(
                  'flex items-center gap-3 rounded-xl border border-transparent px-3 py-2.5 text-sm font-medium transition-all',
                  active
                    ? cn('border', accentActive[accent])
                    : 'text-slate-400 hover:bg-white/5 hover:text-white'
                )}
              >
                <Icon className="h-4 w-4 shrink-0" />
                {item.label}
              </Link>
            )
          })}
        </nav>
      </div>
    </>
  )

  return (
    <div className="flex h-screen overflow-hidden bg-[#f7f8f2]">
      {/* Desktop sidebar */}
      <aside className="hidden h-full w-[260px] flex-shrink-0 flex-col overflow-y-auto border-r border-slate-800/50 bg-[#0c1624] lg:flex overscroll-none">
        {sidebarContent}
        <div className="mt-auto border-t border-white/10 p-4">
          <p className="text-center text-[10px] text-slate-600">Powered by Apollo Hospitals</p>
        </div>
      </aside>

      {/* Mobile sidebar overlay */}
      {sidebarOpen && (
        <div className="fixed inset-0 z-50 lg:hidden">
          <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" onClick={() => setSidebarOpen(false)} />
          <aside className="relative flex h-full w-[280px] flex-col bg-[#0c1624] shadow-2xl">
            <button
              type="button"
              onClick={() => setSidebarOpen(false)}
              className="absolute right-4 top-4 rounded-lg p-1 text-slate-400 hover:text-white"
            >
              <X className="h-5 w-5" />
            </button>
            {sidebarContent}
          </aside>
        </div>
      )}

      <div className="flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden">
        {/* Header */}
        <header className="z-40 shrink-0 border-b border-[#e8ebe0] bg-[#f7f8f2]/95 shadow-sm backdrop-blur-md">
          <div className="flex h-16 items-center justify-between gap-4 px-4 lg:px-8">
            <div className="flex min-w-0 items-center gap-3">
              <button
                type="button"
                onClick={() => setSidebarOpen(true)}
                className="rounded-lg border border-slate-200 p-2 text-slate-600 hover:bg-slate-50 lg:hidden"
              >
                <Menu className="h-5 w-5" />
              </button>
              <div className="min-w-0">
                <span className={cn('inline-block rounded-full px-2.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider', accentBadge[accent])}>
                  {roleLabel}
                </span>
                <h1 className="truncate font-display text-lg font-bold tracking-tight text-[#1a1d1f] lg:text-xl">{portalTitle}</h1>
              </div>
            </div>

            {/* Top-right user */}
            <div className="relative flex items-center gap-2">
              <button
                type="button"
                className="hidden rounded-lg p-2 text-slate-500 hover:bg-slate-100 sm:flex"
                aria-label="Notifications"
              >
                <Bell className="h-5 w-5" />
              </button>

              <button
                type="button"
                onClick={() => setUserMenuOpen((o) => !o)}
                className="flex items-center gap-3 rounded-xl border border-slate-200/80 bg-slate-50/80 py-1.5 pl-1.5 pr-3 transition hover:border-slate-300 hover:bg-white"
              >
                <UserAvatar name={userName} />
                <div className="hidden text-left sm:block">
                  <p className="max-w-[140px] truncate text-sm font-semibold text-slate-900">{userName}</p>
                  {tenantLabel && (
                    <p className="max-w-[140px] truncate text-[11px] text-slate-500">{tenantLabel}</p>
                  )}
                </div>
                <ChevronDown className={cn('hidden h-4 w-4 text-slate-400 transition sm:block', userMenuOpen && 'rotate-180')} />
              </button>

              {userMenuOpen && (
                <>
                  <div className="fixed inset-0 z-40" onClick={() => setUserMenuOpen(false)} />
                  <div className="absolute right-0 top-full z-50 mt-2 w-56 overflow-hidden rounded-xl border border-slate-200 bg-white py-1 shadow-xl">
                    <div className="border-b border-slate-100 px-4 py-3">
                      <p className="text-sm font-semibold text-slate-900">{userName}</p>
                      <p className="text-xs text-slate-500">{roleLabel}</p>
                      {tenantLabel && <p className="mt-0.5 text-xs text-slate-400">{tenantLabel}</p>}
                    </div>
                    <button
                      type="button"
                      onClick={handleLogout}
                      className="flex w-full items-center gap-2 px-4 py-2.5 text-sm text-red-600 hover:bg-red-50"
                    >
                      <LogOut className="h-4 w-4" />
                      Sign out
                    </button>
                  </div>
                </>
              )}
            </div>
          </div>
        </header>

        <main className="min-h-0 flex-1 overflow-y-auto overscroll-none p-4 lg:p-8">{children}</main>
      </div>
    </div>
  )
}

export function Panel({
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
      data-animate-card
      className={cn(
        'rounded-2xl border border-slate-200/60 bg-white p-6 shadow-[0_1px_3px_rgba(15,23,42,0.04)]',
        className
      )}
    >
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-base font-semibold text-slate-900">{title}</h2>
        {action}
      </div>
      {children}
    </section>
  )
}
