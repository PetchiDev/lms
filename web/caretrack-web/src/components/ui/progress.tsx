import * as ProgressPrimitive from '@radix-ui/react-progress'
import { cn } from '@/lib/utils'

interface ProgressProps extends React.ComponentPropsWithoutRef<typeof ProgressPrimitive.Root> {
  value?: number
  indicatorClassName?: string
}

export function Progress({ className, value = 0, indicatorClassName, ...props }: ProgressProps) {
  return (
    <ProgressPrimitive.Root
      className={cn('relative h-2 w-full overflow-hidden rounded-full bg-slate-100', className)}
      {...props}
    >
      <ProgressPrimitive.Indicator
        className={cn('h-full rounded-full bg-[#2081A1] transition-all duration-700 ease-out', indicatorClassName)}
        style={{ transform: `translateX(-${100 - Math.min(100, Math.max(0, value))}%)` }}
      />
    </ProgressPrimitive.Root>
  )
}
