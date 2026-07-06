import { Link, useLocation, useNavigate } from 'react-router-dom'
import { Bell, ChevronDown, LogOut, Menu, Settings, X, type LucideIcon } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import gsap from 'gsap'
import { authStore } from '@/lib/auth-store'
import { clearStudentCache } from '@/lib/query-client'
import { BrandLogo } from '@/components/brand/BrandLogo'
import { cn } from '@/lib/utils'

export interface StudentNavItem {
  label: string
  href: string
  icon: LucideIcon
}

interface StudentShellProps {
  userName: string
  tenantLabel?: string
  yearLabel?: string
  navItems: StudentNavItem[]
  children: React.ReactNode
  resumeHref?: string
  resumeLabel?: string
}

function UserAvatar({ name }: { name: string }) {
  const initials = name.split(' ').map((n) => n[0]).join('').slice(0, 2).toUpperCase()
  return (
    <div className="flex h-10 w-10 items-center justify-center rounded-full bg-gradient-to-br from-[#2d5f5a] to-[#3d7a70] text-sm font-semibold text-white ring-2 ring-white shadow-md">
      {initials}
    </div>
  )
}

export function StudentShell({
  userName,
  tenantLabel = 'Meridian × Apollo',
  yearLabel,
  navItems,
  children,
  resumeHref,
  resumeLabel = 'Resume Lesson',
}: StudentShellProps) {
  const location = useLocation()
  const navigate = useNavigate()
  const mainRef = useRef<HTMLDivElement>(null)
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [menuOpen, setMenuOpen] = useState(false)

  useEffect(() => {
    if (!mainRef.current) return
    gsap.fromTo(mainRef.current, { opacity: 0, y: 16 }, { opacity: 1, y: 0, duration: 0.5, ease: 'power3.out' })
  }, [location.pathname])

  const sidebar = (
    <>
      <div className="border-b border-white/8 px-5 py-5">
        <BrandLogo variant="light" size="sm" />
        <p className="mt-2 text-[11px] font-medium tracking-wide text-slate-500">{tenantLabel}</p>
      </div>

      <nav className="flex-1 space-y-0.5 overflow-y-auto px-3 py-4 student-scroll">
        {navItems.map((item) => {
          const active = location.pathname === item.href || (item.href !== '/dashboard' && location.pathname.startsWith(item.href))
          const Icon = item.icon
          return (
            <Link
              key={item.label + item.href}
              to={item.href}
              onClick={() => setSidebarOpen(false)}
              className={cn(
                'flex items-center gap-3 rounded-xl px-3.5 py-2.5 text-sm font-medium transition-all duration-200',
                active
                  ? 'bg-[#c8e6d9] text-[#1a3d38] shadow-sm'
                  : 'text-slate-400 hover:bg-white/5 hover:text-white'
              )}
            >
              <Icon className="h-[18px] w-[18px] shrink-0" strokeWidth={1.75} />
              {item.label}
            </Link>
          )
        })}
      </nav>

      {resumeHref && (
        <div className="border-t border-white/8 p-4">
          <Link
            to={resumeHref}
            className="flex w-full items-center justify-center gap-2 rounded-xl bg-white py-3 text-sm font-semibold text-[#141a21] shadow-lg transition hover:bg-slate-100"
          >
            ▶ {resumeLabel}
          </Link>
        </div>
      )}

      <div className="border-t border-white/8 px-4 py-3">
        <div className="flex gap-4 text-xs text-slate-500">
          <button type="button" className="flex items-center gap-1.5 hover:text-slate-300">
            <Settings className="h-3.5 w-3.5" /> Support
          </button>
        </div>
      </div>
    </>
  )

  return (
    <div className="flex h-screen overflow-hidden bg-[#f7f8f2]">
      <aside className="hidden h-full w-[240px] flex-shrink-0 flex-col overflow-y-auto bg-[#141a21] lg:flex overscroll-none">{sidebar}</aside>

      {sidebarOpen && (
        <div className="fixed inset-0 z-50 lg:hidden">
          <div className="absolute inset-0 bg-black/70 backdrop-blur-sm" onClick={() => setSidebarOpen(false)} />
          <aside className="relative flex h-full w-[260px] flex-col bg-[#141a21] shadow-2xl">
            <button type="button" onClick={() => setSidebarOpen(false)} className="absolute right-3 top-3 p-2 text-slate-400">
              <X className="h-5 w-5" />
            </button>
            {sidebar}
          </aside>
        </div>
      )}

      <div className="flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden">
        <header className="z-30 flex h-16 shrink-0 items-center justify-between border-b border-[#e8ebe0] bg-[#f7f8f2]/90 px-4 backdrop-blur-md lg:px-8">
          <button
            type="button"
            onClick={() => setSidebarOpen(true)}
            className="rounded-lg p-2 text-slate-600 hover:bg-white lg:hidden"
          >
            <Menu className="h-5 w-5" />
          </button>
          <div className="hidden lg:block" />

          <div className="flex items-center gap-3">
            <button type="button" className="relative rounded-full p-2 text-slate-500 transition hover:bg-white hover:text-slate-700">
              <Bell className="h-5 w-5" />
              <span className="absolute right-1.5 top-1.5 h-2 w-2 rounded-full bg-red-500 ring-2 ring-[#f7f8f2]" />
            </button>

            <div className="relative">
              <button
                type="button"
                onClick={() => setMenuOpen((o) => !o)}
                className="flex items-center gap-2.5 rounded-full border border-[#e0e4d8] bg-white py-1 pl-1 pr-3 shadow-sm transition hover:shadow-md"
              >
                <UserAvatar name={userName} />
                <div className="hidden text-left sm:block">
                  <p className="text-sm font-semibold text-slate-900">{userName}</p>
                  {yearLabel && <p className="text-[10px] text-slate-500">{yearLabel}</p>}
                </div>
                <ChevronDown className={cn('hidden h-4 w-4 text-slate-400 sm:block', menuOpen && 'rotate-180')} />
              </button>
              {menuOpen && (
                <>
                  <div className="fixed inset-0 z-40" onClick={() => setMenuOpen(false)} />
                  <div className="absolute right-0 top-full z-50 mt-2 w-52 rounded-xl border border-slate-200 bg-white py-1 shadow-xl">
                    <div className="border-b border-slate-100 px-4 py-3">
                      <p className="font-semibold text-slate-900">{userName}</p>
                      <p className="text-xs text-slate-500">{tenantLabel}</p>
                    </div>
                    <button
                      type="button"
                      onClick={() => { authStore.clear(); clearStudentCache(); navigate('/login') }}
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

        <main ref={mainRef} className="min-h-0 flex-1 overflow-y-auto overscroll-none p-4 lg:p-8">{children}</main>
      </div>
    </div>
  )
}
