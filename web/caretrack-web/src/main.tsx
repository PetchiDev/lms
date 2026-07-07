import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

// Fix legacy URLs like /login#/apollo/... before React mounts
{
  const { pathname, hash, search } = window.location
  if (pathname !== '/' && pathname !== '') {
    if (hash.startsWith('#/')) {
      window.history.replaceState(null, '', `/${hash}${search}`)
    } else if (pathname === '/login') {
      window.history.replaceState(null, '', `/#/login${search}`)
    }
  }
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
