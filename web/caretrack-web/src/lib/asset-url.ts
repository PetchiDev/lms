const API_BASE = import.meta.env.VITE_API_URL ?? ''

export function assetUrl(url?: string | null) {
  if (!url) return null
  if (url.startsWith('http')) return url
  const base = API_BASE || ''
  return `${base}${url}`
}
