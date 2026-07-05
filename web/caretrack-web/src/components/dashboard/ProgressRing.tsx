import { cn } from '@/lib/utils'

interface ProgressRingProps {
  value: number
  size?: number
  stroke?: number
  label?: string
  sublabel?: string
  className?: string
}

export function ProgressRing({ value, size = 120, stroke = 10, label, sublabel, className }: ProgressRingProps) {
  const radius = (size - stroke) / 2
  const circumference = 2 * Math.PI * radius
  const offset = circumference - (circumference * Math.min(100, Math.max(0, value))) / 100

  return (
    <div className={cn('relative inline-flex flex-col items-center', className)}>
      <svg width={size} height={size} className="-rotate-90">
        <circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          fill="none"
          stroke="currentColor"
          strokeWidth={stroke}
          className="text-slate-200"
        />
        <circle
          data-animate-ring
          data-progress={value}
          cx={size / 2}
          cy={size / 2}
          r={radius}
          fill="none"
          stroke="currentColor"
          strokeWidth={stroke}
          strokeLinecap="round"
          strokeDasharray={circumference}
          strokeDashoffset={offset}
          className="text-teal-500 transition-all"
        />
      </svg>
      <div className="absolute inset-0 flex flex-col items-center justify-center">
        <span className="text-2xl font-bold text-slate-900">{value}%</span>
        {label && <span className="text-xs text-slate-500">{label}</span>}
      </div>
      {sublabel && <p className="mt-2 text-center text-xs text-slate-500">{sublabel}</p>}
    </div>
  )
}
