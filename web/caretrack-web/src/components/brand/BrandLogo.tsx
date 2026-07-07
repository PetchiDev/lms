import { cn } from '@/lib/utils'
import { authStore } from '@/lib/auth-store'

interface BrandLogoProps {
  variant?: 'light' | 'dark'
  size?: 'sm' | 'md' | 'lg'
  showCareTrack?: boolean
  className?: string
}

const sizes = {
  sm: 'h-8',
  md: 'h-11',
  lg: 'h-14',
}

export function BrandLogo({ variant = 'dark', size = 'md', showCareTrack = true, className }: BrandLogoProps) {
  const auth = authStore.get()
  const src = auth?.universityLogoUrl || '/apollo_logo.png'
  return (
    <div className={cn('flex items-center gap-3', className)}>
      <img
        src={src}
        alt="Logo"
        className={cn(sizes[size], 'w-auto object-contain', variant === 'light' && !src.startsWith('http') && 'brightness-0 invert')}
      />
      {showCareTrack && (
        <div className="hidden sm:block">
          <p className={cn('text-sm font-bold leading-tight tracking-tight', variant === 'light' ? 'text-white' : 'text-slate-900')}>
            CareTrack
          </p>
          <p className={cn('text-[10px] font-medium uppercase tracking-widest', variant === 'light' ? 'text-white/60' : 'text-[#2081A1]')}>
            Learning Platform
          </p>
        </div>
      )}
    </div>
  )
}
