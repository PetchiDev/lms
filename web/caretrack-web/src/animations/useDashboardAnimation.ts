import { useEffect, useRef } from 'react'
import gsap from 'gsap'

export function useDashboardAnimation() {
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const el = containerRef.current
    if (!el) return

    const cards = el.querySelectorAll('[data-animate-card]')
    const rings = el.querySelectorAll('[data-animate-ring]')

    gsap.fromTo(
      cards,
      { opacity: 0, y: 24, scale: 0.98 },
      { opacity: 1, y: 0, scale: 1, duration: 0.55, stagger: 0.08, ease: 'power3.out' }
    )

    rings.forEach((ring, i) => {
      const el = ring as SVGCircleElement
      const progress = Number(el.dataset.progress ?? 0)
      const circumference = Number(el.getAttribute('stroke-dasharray') ?? 0)
      if (!circumference) return
      gsap.fromTo(
        el,
        { strokeDashoffset: circumference },
        { strokeDashoffset: circumference - (circumference * progress) / 100, duration: 1.2, delay: 0.2 + i * 0.1, ease: 'power2.out' }
      )
    })
  }, [])

  return containerRef
}

export function animateCounter(element: HTMLElement | null, value: number) {
  if (!element) return
  const obj = { val: 0 }
  gsap.to(obj, {
    val: value,
    duration: 1,
    ease: 'power2.out',
    onUpdate: () => {
      element.textContent = `${Math.round(obj.val)}`
    },
  })
}
