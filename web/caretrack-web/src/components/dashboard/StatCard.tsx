import { cn } from '@/lib/utils'
import type { LucideIcon } from 'lucide-react'

interface StatCardProps {
  title: string
  value: string | number
  change?: string
  icon: LucideIcon
  trend?: 'up' | 'down' | 'neutral'
  accent?: 'teal' | 'blue' | 'amber' | 'rose' | 'violet'
}

const accentMap = {
  teal: 'from-[#2081A1]/15 to-[#2081A1]/5 text-[#1a6d89] border-[#2081A1]/25',
  blue: 'from-blue-500/20 to-blue-600/5 text-blue-700 border-blue-200/60',
  amber: 'from-amber-500/20 to-amber-600/5 text-amber-700 border-amber-200/60',
  rose: 'from-rose-500/20 to-rose-600/5 text-rose-700 border-rose-200/60',
  violet: 'from-violet-500/20 to-violet-600/5 text-violet-700 border-violet-200/60',
}

export function StatCard({ title, value, change, icon: Icon, trend = 'neutral', accent = 'teal' }: StatCardProps) {
  return (
    <div
      data-animate-card
      className={cn(
        'relative overflow-hidden rounded-2xl border bg-gradient-to-br p-5 shadow-sm backdrop-blur-sm',
        accentMap[accent]
      )}
    >
      <div className="flex items-start justify-between">
        <div>
          <p className="text-sm font-medium opacity-80">{title}</p>
          <p className="mt-2 text-3xl font-bold tracking-tight text-slate-900">{value}</p>
          {change && (
            <p
              className={cn(
                'mt-1 text-xs font-medium',
                trend === 'up' && 'text-emerald-600',
                trend === 'down' && 'text-rose-600',
                trend === 'neutral' && 'text-slate-500'
              )}
            >
              {change}
            </p>
          )}
        </div>
        <div className="rounded-xl bg-white/70 p-3 shadow-sm">
          <Icon className="h-5 w-5" />
        </div>
      </div>
    </div>
  )
}
