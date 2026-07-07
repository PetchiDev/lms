import { useEffect } from 'react'

/** Ensures hash routes use `/#/path` not `/login#/path`. */
export function HashUrlFix() {
  useEffect(() => {
    const { pathname, hash, search } = window.location
    if (pathname === '/' || pathname === '') return

    if (hash.startsWith('#/')) {
      window.history.replaceState(null, '', `/${hash}${search}`)
      return
    }

    if (pathname === '/login') {
      window.history.replaceState(null, '', `/#/login${search}`)
      return
    }

    window.history.replaceState(null, '', `/${hash || '#/'}`)
  }, [])

  return null
}
