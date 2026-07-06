import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { BookOpen, Building2, GraduationCap, Stethoscope } from 'lucide-react'
import gsap from 'gsap'
import { api, getErrorMessage } from '@/lib/api-client'
import { authStore } from '@/lib/auth-store'
import { getRoleRedirect } from '@/lib/utils'
import { BrandLogo } from '@/components/brand/BrandLogo'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

const features = [
  { icon: GraduationCap, title: 'End-to-end learning', desc: 'Enrolment through certification in one place.' },
  { icon: Stethoscope, title: 'Clinical rotations', desc: 'Logbooks and supervisor sign-offs.' },
  { icon: Building2, title: 'Multi-university', desc: 'Apollo content, tenant-safe delivery.' },
  { icon: BookOpen, title: 'Role-based portals', desc: 'Tailored for every role.' },
]

export function LoginPage() {
  const navigate = useNavigate()
  const heroRef = useRef<HTMLDivElement>(null)
  const formRef = useRef<HTMLFormElement>(null)
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (!heroRef.current) return
    gsap.fromTo(heroRef.current.children, { opacity: 0, y: 28 }, { opacity: 1, y: 0, duration: 0.65, stagger: 0.1, ease: 'power3.out' })
    if (formRef.current) {
      gsap.fromTo(formRef.current, { opacity: 0, y: 20 }, { opacity: 1, y: 0, duration: 0.5, delay: 0.2, ease: 'power3.out' })
    }
  }, [])

  async function handleLogin(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    if (!email.includes('@')) {
      setError('Enter a valid email address')
      return
    }
    if (!password) {
      setError('Enter your password')
      return
    }
    setLoading(true)
    try {
      const { data } = await api.post('/auth/login', { email, password })
      authStore.set(data)
      navigate(getRoleRedirect(data.role))
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex min-h-screen">
      <div
        ref={heroRef}
        className="relative hidden w-[52%] overflow-hidden bg-[#0a1628] lg:flex lg:flex-col lg:justify-between"
      >
        <div className="absolute inset-0 bg-gradient-to-br from-[#2081A1]/30 via-[#0a1628] to-[#061018]" />
        <div className="absolute -right-32 top-20 h-96 w-96 rounded-full bg-[#2081A1]/20 blur-3xl" />
        <div className="relative z-10 p-10 xl:p-14">
          <BrandLogo size="lg" showCareTrack variant="light" />
          <h1 className="font-display mt-14 max-w-lg text-4xl font-bold leading-[1.15] text-white xl:text-[2.75rem]">
            Your clinical journey starts here
          </h1>
          <p className="mt-5 max-w-md text-base leading-relaxed text-slate-300">
            Learn, practise, and grow — from classroom to hospital ward.
          </p>
        </div>
        <div className="relative z-10 grid grid-cols-2 gap-3 p-10 pt-0 xl:p-14">
          {features.map(({ icon: Icon, title, desc }) => (
            <div key={title} className="rounded-2xl border border-white/10 bg-white/5 p-4 backdrop-blur-sm">
              <Icon className="mb-2 h-4 w-4 text-[#5ec4e0]" />
              <p className="text-sm font-semibold text-white">{title}</p>
              <p className="mt-1 text-xs text-slate-400">{desc}</p>
            </div>
          ))}
        </div>
      </div>

      <div className="flex flex-1 flex-col items-center justify-center bg-gradient-to-b from-slate-50 to-slate-100/80 p-6">
        <div className="w-full max-w-[420px]">
          <form
            ref={formRef}
            onSubmit={handleLogin}
            className="space-y-5 rounded-2xl border border-slate-200/80 bg-white p-8 shadow-[0_8px_40px_-12px_rgba(32,129,161,0.15)]"
          >
            <div className="flex flex-col items-center text-center">
              <BrandLogo size="md" showCareTrack={false} className="mb-4" />
              <h2 className="text-2xl font-bold tracking-tight text-slate-900">Welcome back</h2>
              <p className="mt-1.5 text-sm text-slate-500">Sign in with your email and password</p>
            </div>

            <div className="space-y-2">
              <Label htmlFor="email">Email address</Label>
              <Input
                id="email"
                type="email"
                autoComplete="username"
                autoFocus
                placeholder="you@meridian.edu"
                className="h-11 border-slate-200 focus-visible:ring-[#2081A1]"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                autoComplete="current-password"
                placeholder="Enter your password"
                className="h-11 border-slate-200 focus-visible:ring-[#2081A1]"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
              />
            </div>

            {error && <p className="text-sm text-red-600">{error}</p>}

            <Button type="submit" className="h-11 w-full bg-[#2081A1] hover:bg-[#1a6d89]" disabled={loading}>
              {loading ? 'Signing in…' : 'Sign in to CareTrack'}
            </Button>
          </form>

          <details className="mt-5 rounded-xl border border-slate-200/80 bg-white/80">
            <summary className="cursor-pointer px-4 py-3 text-xs font-medium text-slate-500 hover:text-slate-700">
              Demo accounts for testing
            </summary>
            <div className="space-y-1 border-t border-slate-100 px-4 py-3 text-xs text-slate-500">
              <p><span className="font-medium text-slate-600">Apollo:</span> admin@apollo.edu / Admin@123</p>
              <p><span className="font-medium text-slate-600">Faculty:</span> faculty@apollo.edu / Faculty@123</p>
              <p><span className="font-medium text-slate-600">Univ Admin:</span> admin@meridian.edu / UnivAdmin@123</p>
              <p><span className="font-medium text-slate-600">Supervisor:</span> supervisor@meridian.edu / Supervisor@123</p>
              <p><span className="font-medium text-slate-600">Student:</span> student@meridian.edu / Student@123</p>
            </div>
          </details>
        </div>
      </div>
    </div>
  )
}
