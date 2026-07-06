interface BarChartProps {
  data: { label: string; value: number; color?: string }[]
  maxValue?: number
  className?: string
}

export function BarChart({ data, maxValue, className }: BarChartProps) {
  const max = maxValue ?? Math.max(...data.map((d) => d.value), 1)

  return (
    <div className={className}>
      <div className="space-y-4">
        {data.map((item) => (
          <div key={item.label}>
            <div className="mb-1.5 flex items-center justify-between text-sm">
              <span className="font-medium text-slate-700">{item.label}</span>
              <span className="tabular-nums text-slate-500">{Math.round(item.value)}%</span>
            </div>
            <div className="h-3 overflow-hidden rounded-full bg-slate-100">
              <div
                className="h-full rounded-full transition-all duration-700 ease-out"
                style={{
                  width: `${(item.value / max) * 100}%`,
                  backgroundColor: item.color ?? '#2081A1',
                }}
              />
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

interface DonutSegment {
  label: string
  value: number
  color: string
}

export function DonutChart({ segments, size = 160 }: { segments: DonutSegment[]; size?: number }) {
  const total = segments.reduce((s, x) => s + x.value, 0) || 1
  const r = 36
  const c = 2 * Math.PI * r
  let offset = 0

  return (
    <div className="flex flex-col items-center gap-4 sm:flex-row sm:items-center sm:gap-8">
      <svg width={size} height={size} viewBox="0 0 100 100" className="-rotate-90">
        <circle cx="50" cy="50" r={r} fill="none" stroke="#e2e8f0" strokeWidth="12" />
        {segments.map((seg) => {
          const dash = (seg.value / total) * c
          const el = (
            <circle
              key={seg.label}
              cx="50"
              cy="50"
              r={r}
              fill="none"
              stroke={seg.color}
              strokeWidth="12"
              strokeDasharray={`${dash} ${c - dash}`}
              strokeDashoffset={-offset}
              strokeLinecap="round"
            />
          )
          offset += dash
          return el
        })}
        <text
          x="50"
          y="50"
          textAnchor="middle"
          dominantBaseline="middle"
          className="rotate-90 fill-slate-900 text-[14px] font-bold"
          style={{ transform: 'rotate(90deg)', transformOrigin: '50px 50px' }}
        >
          {total}
        </text>
      </svg>
      <ul className="space-y-2 text-sm">
        {segments.map((seg) => (
          <li key={seg.label} className="flex items-center gap-2">
            <span className="h-2.5 w-2.5 rounded-full" style={{ backgroundColor: seg.color }} />
            <span className="text-slate-600">{seg.label}</span>
            <span className="ml-auto font-semibold tabular-nums text-slate-900">{seg.value}</span>
          </li>
        ))}
      </ul>
    </div>
  )
}

interface MiniBarChartProps {
  data: number[]
  labels?: string[]
  color?: string
  height?: number
}

export function MiniBarChart({ data, labels, color = '#2081A1', height = 120 }: MiniBarChartProps) {
  const max = Math.max(...data, 1)

  return (
    <div className="flex items-end justify-between gap-2" style={{ height }}>
      {data.map((v, i) => (
        <div key={i} className="flex flex-1 flex-col items-center gap-1">
          <div
            className="w-full max-w-[48px] rounded-t-md transition-all duration-700"
            style={{
              height: `${(v / max) * (height - 24)}px`,
              backgroundColor: color,
              opacity: 0.85 + (i / data.length) * 0.15,
            }}
          />
          {labels?.[i] && (
            <span className="text-[10px] text-slate-400">{labels[i]}</span>
          )}
        </div>
      ))}
    </div>
  )
}
