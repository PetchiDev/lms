interface SparklineProps {
  data: number[]
  height?: number
  className?: string
}

export function Sparkline({ data, height = 48, className }: SparklineProps) {
  if (data.length < 2) return null

  const max = Math.max(...data, 1)
  const min = Math.min(...data, 0)
  const range = max - min || 1
  const width = 120
  const points = data
    .map((v, i) => {
      const x = (i / (data.length - 1)) * width
      const y = height - ((v - min) / range) * (height - 4) - 2
      return `${x},${y}`
    })
    .join(' ')

  return (
    <svg viewBox={`0 0 ${width} ${height}`} className={className} preserveAspectRatio="none">
      <defs>
        <linearGradient id="sparkGrad" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="rgb(20 184 166)" stopOpacity="0.4" />
          <stop offset="100%" stopColor="rgb(20 184 166)" stopOpacity="0" />
        </linearGradient>
      </defs>
      <polygon points={`0,${height} ${points} ${width},${height}`} fill="url(#sparkGrad)" />
      <polyline points={points} fill="none" stroke="rgb(20 184 166)" strokeWidth="2" strokeLinecap="round" />
    </svg>
  )
}
