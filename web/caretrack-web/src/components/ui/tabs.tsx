import * as TabsPrimitive from '@radix-ui/react-tabs'
import { cn } from '@/lib/utils'

export const Tabs = TabsPrimitive.Root

export function TabsList({ className, ...props }: React.ComponentPropsWithoutRef<typeof TabsPrimitive.List>) {
  return (
    <TabsPrimitive.List
      className={cn(
        'inline-flex h-11 items-center gap-1 rounded-xl bg-slate-100 p-1',
        className,
      )}
      {...props}
    />
  )
}

export function TabsTrigger({ className, ...props }: React.ComponentPropsWithoutRef<typeof TabsPrimitive.Trigger>) {
  return (
    <TabsPrimitive.Trigger
      className={cn(
        'inline-flex items-center justify-center rounded-lg px-4 py-2 text-sm font-medium text-slate-600 transition-all',
        'hover:text-slate-900 data-[state=active]:bg-white data-[state=active]:text-[#2081A1] data-[state=active]:shadow-sm',
        'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2081A1]/40',
        className,
      )}
      {...props}
    />
  )
}

export function TabsContent({ className, ...props }: React.ComponentPropsWithoutRef<typeof TabsPrimitive.Content>) {
  return (
    <TabsPrimitive.Content
      className={cn(
        'mt-6 focus-visible:outline-none',
        'data-[state=active]:animate-in data-[state=active]:fade-in-0 data-[state=active]:slide-in-from-bottom-2 data-[state=active]:duration-300',
        className,
      )}
      {...props}
    />
  )
}
