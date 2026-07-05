import { useEffect, useRef } from 'react'
import gsap from 'gsap'

export function usePageTransition() {
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!ref.current) return
    gsap.fromTo(ref.current, { opacity: 0, y: 12 }, { opacity: 1, y: 0, duration: 0.4, ease: 'power2.out' })
  }, [])

  return ref
}

export function animateProgress(element: HTMLElement | null, value: number) {
  if (!element) return
  gsap.to(element, { width: `${value}%`, duration: 0.8, ease: 'power2.out' })
}

export function animateUnlock(element: HTMLElement | null) {
  if (!element) return
  gsap.fromTo(element, { scale: 0.95, opacity: 0.5 }, { scale: 1, opacity: 1, duration: 0.5, ease: 'back.out(1.7)' })
}
