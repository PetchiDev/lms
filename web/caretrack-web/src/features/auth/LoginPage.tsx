import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Building2, GraduationCap, KeyRound, Shield } from 'lucide-react'
import gsap from 'gsap'
import { api, getErrorMessage } from '@/lib/api-client'
import { authStore } from '@/lib/auth-store'
import { getRoleRedirect, resolveIdpFromEmail } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

export function LoginPage() {
  const navigate = useNavigate()
  const heroRef = useRef<HTMLDivElement>(null)
  const formRef = useRef<HTMLFormElement>(null)
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [step, setStep] = useState<'email' | 'auth'>('email')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const idp = resolveIdpFromEmail(email)

  useEffect(() => {
    if (!heroRef.current) return
    gsap.fromTo(heroRef.current.children, { opacity: 0, y: 30 }, { opacity: 1, y: 0, duration: 0.7, stagger: 0.12, ease: 'power3.out' })
  }, [])

  function handleEmailContinue(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    if (!email.includes('@')) {
      setError('Enter a valid email address')
      return
    }
    setStep('auth')
    if (formRef.current) {
      gsap.fromTo(formRef.current, { opacity: 0, x: 20 }, { opacity: 1, x: 0, duration: 0.4, ease: 'power2.out' })
    }
  }

  async function handlePasswordLogin(e: React.FormEvent) {
    e.preventDefault()
    setError('')
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

  function handleSsoRedirect() {
    setError('SSO redirect will connect to your university IdP in production. Use password login for demo.')
  }

  return (
    <div className="flex min-h-screen">
      <div
        ref={heroRef}
        className="relative hidden w-1/2 overflow-hidden bg-gradient-to-br from-teal-700 via-emerald-800 to-slate-900 p-12 text-white lg:flex lg:flex-col lg:justify-between"
      >
        <div>
          <div className="flex items-center gap-3">
            <GraduationCap className="h-10 w-10" />
            <div>
              <p className="text-2xl font-bold">CareTrack</p>
              <p className="text-sm text-white/70">Apollo × University LMS</p>
            </div>
          </div>
          <h1 className="mt-16 max-w-md text-4xl font-bold leading-tight">
            One login. Every role. Tenant-safe by design.
          </h1>
          <p className="mt-4 max-w-sm text-white/80">
            Email domain resolves your university and sign-in method — SSO, Entra ID, or native password.
          </p>
        </div>
        <div className="grid grid-cols-2 gap-4">
          {[
            { icon: Shield, label: 'JWT + tenant scope' },
            { icon: Building2, label: 'Multi-university' },
            { icon: GraduationCap, label: '3-year journey' },
            { icon: KeyRound, label: 'Role-based portals' },
          ].map(({ icon: Icon, label }) => (
            <div key={label} className="rounded-2xl border border-white/10 bg-white/5 p-4 backdrop-blur">
              <Icon className="mb-2 h-5 w-5 text-emerald-300" />
              <p className="text-sm font-medium">{label}</p>
            </div>
          ))}
        </div>
        <div className="absolute -right-20 -top-20 h-80 w-80 rounded-full bg-emerald-400/20 blur-3xl" />
        <div className="absolute -bottom-10 -left-10 h-60 w-60 rounded-full bg-teal-300/10 blur-3xl" />
      </div>

      <div className="flex flex-1 items-center justify-center bg-[#f4f7fb] p-6">
        <div className="w-full max-w-md">
          <div className="mb-8 lg:hidden">
            <div className="flex items-center gap-2 text-teal-700">
              <GraduationCap className="h-7 w-7" />
              <span className="text-xl font-bold">CareTrack</span>
            </div>
          </div>

          {step === 'email' ? (
            <form onSubmit={handleEmailContinue} className="space-y-6 rounded-2xl border border-slate-200 bg-white p-8 shadow-xl">
              <div>
                <h2 className="text-2xl font-bold text-slate-900">Sign in</h2>
                <p className="mt-1 text-sm text-slate-500">Enter your institutional email</p>
              </div>
              <div className="space-y-2">
                <Label htmlFor="email">Email</Label>
                <Input
                  id="email"
                  type="email"
                  autoFocus
                  placeholder="you@meridian.edu"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                />
              </div>
              {error && <p className="text-sm text-red-600">{error}</p>}
              <Button type="submit" className="w-full">Continue</Button>
            </form>
          ) : (
            <form ref={formRef} onSubmit={idp ? (e) => { e.preventDefault(); handleSsoRedirect() } : handlePasswordLogin} className="space-y-6 rounded-2xl border border-slate-200 bg-white p-8 shadow-xl">
              <div>
                <button type="button" onClick={() => setStep('email')} className="text-sm text-teal-600 hover:underline">
                  ← Change email
                </button>
                <h2 className="mt-2 text-2xl font-bold text-slate-900">
                  {idp ? 'Single sign-on' : 'Enter password'}
                </h2>
                <p className="mt-1 text-sm text-slate-500">{email}</p>
              </div>

              {idp ? (
                <div className="rounded-xl border border-indigo-100 bg-indigo-50/80 p-5">
                  <div className="flex items-center gap-3">
                    <Shield className="h-8 w-8 text-indigo-600" />
                    <div>
                      <p className="font-semibold text-slate-900">{idp.name}</p>
                      <p className="text-sm text-slate-500">
                        {idp.type === 'entra' ? 'Microsoft Entra ID' : 'SAML / OIDC'} redirect
                      </p>
                    </div>
                  </div>
                  <Button type="submit" className="mt-4 w-full">Continue with SSO</Button>
                  <p className="mt-3 text-center text-xs text-slate-500">
                    Demo: use password accounts below instead
                  </p>
                </div>
              ) : (
                <div className="space-y-2">
                  <Label htmlFor="password">Password</Label>
                  <Input
                    id="password"
                    type="password"
                    autoFocus
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required
                  />
                </div>
              )}

              {error && <p className="text-sm text-red-600">{error}</p>}
              {!idp && (
                <Button type="submit" className="w-full" disabled={loading}>
                  {loading ? 'Signing in...' : 'Sign in'}
                </Button>
              )}
            </form>
          )}

          <div className="mt-6 rounded-xl border border-slate-200 bg-white p-4 text-xs text-slate-500">
            <p className="mb-2 font-semibold text-slate-700">Demo accounts (password)</p>
            <p>Apollo: admin@apollo.edu / Admin@123</p>
            <p>Faculty: faculty@apollo.edu / Faculty@123</p>
            <p>Univ Admin: admin@meridian.edu / UnivAdmin@123</p>
            <p>Supervisor: supervisor@meridian.edu / Supervisor@123</p>
            <p>Student: student@meridian.edu / Student@123</p>
          </div>
        </div>
      </div>
    </div>
  )
}
