import { useQuery } from '@tanstack/react-query'
import { cn } from '@/lib/utils'
import { authStore } from '@/lib/auth-store'
import { api } from '@/lib/api-client'
import { assetUrl } from '@/lib/asset-url'

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
  const platformBranding = useQuery({
    queryKey: ['platform-branding'],
    queryFn: async () => (await api.get('/platform/branding')).data as { logoUrl?: string | null },
    enabled: !auth?.universityLogoUrl,
    staleTime: 5 * 60 * 1000,
  })

  const platformLogo = assetUrl(platformBranding.data?.logoUrl)
  const src = auth?.universityLogoUrl || platformLogo || '/apollo_logo.png'
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
