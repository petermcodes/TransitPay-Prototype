import { useState, useEffect } from 'react'
import { QRCodeSVG } from 'qrcode.react'
import {
  ArrowLeft, Eye, EyeOff, Home, Wallet as WalletIcon, QrCode, User,
  ChevronRight, ArrowUpRight, ArrowDownLeft, RotateCcw,
  Bus, CreditCard, Phone, Lock, Plus, LogOut, Copy,
  CheckCircle, AlertCircle, Clock, TrendingUp, Bell,
  Shield, HelpCircle, ChevronDown, RefreshCw, Zap,
  FileText, Upload, X, Check, Download, MapPin
} from 'lucide-react'
import { authService, type User as UserType } from './lib/auth'
import { qrService } from './lib/payment'
import { discountService, getDiscountStatusName, type DiscountType, type DiscountApplication, getCardTheme } from './lib/discount'
import { walletService, computeWalletStats, type Wallet, type Transaction } from './lib/wallet'
import { resolveCardId, getMyCard, type Card } from './lib/card'
import { api } from './lib/api'
import { tripPlanService, type TripPlan } from './lib/tripPlan'

type Screen =
  | 'splash' | 'welcome' | 'login' | 'register' | 'forgot' | 'otp'
  | 'home' | 'wallet' | 'topup' | 'qr' | 'profile'
  | 'discounts' | 'apply-discount' | 'discount-status'
  | 'plan-trip' | 'trip-plan-history' | 'trip-plan-detail'
  | 'transaction-detail'

// ── Design tokens ─────────────────────────────────────────────────────────────

function Btn({ children, variant = 'primary', className = '', onClick, disabled, size = 'md', type = 'button' }: {
  children: React.ReactNode; variant?: 'primary' | 'secondary' | 'ghost' | 'danger'
  className?: string; onClick?: () => void; disabled?: boolean; size?: 'sm' | 'md' | 'lg'
  type?: 'button' | 'submit'
}) {
  const base = 'inline-flex items-center justify-center gap-2 font-semibold rounded-2xl transition-all active:scale-[0.97] cursor-pointer select-none font-poppins'
  const sizes = { sm: 'px-4 py-2 text-sm', md: 'px-5 py-3 text-sm', lg: 'px-6 py-4 text-base w-full' }
  const variants = {
    primary: 'bg-blue-gradient text-white shadow-md hover:shadow-lg hover:brightness-105 disabled:opacity-50 disabled:cursor-not-allowed',
    secondary: 'bg-white text-[#1976D2] border-2 border-[#1976D2] hover:bg-[#EFF6FF] disabled:opacity-50',
    ghost: 'text-[#1976D2] hover:bg-[#EFF6FF] disabled:opacity-50',
    danger: 'bg-red-500 text-white hover:bg-red-600 disabled:opacity-50',
  }
  return (
    <button type={type} disabled={disabled} onClick={onClick}
      className={`${base} ${sizes[size]} ${variants[variant]} ${className}`}>
      {children}
    </button>
  )
}

function Input({ label, type = 'text', placeholder, value, onChange, icon, trailing }: {
  label?: string; type?: string; placeholder?: string; value?: string
  onChange?: (v: string) => void; icon?: React.ReactNode; trailing?: React.ReactNode
}) {
  return (
    <div className="flex flex-col gap-1.5">
      {label && <label className="text-xs font-semibold text-slate-500 uppercase tracking-wider">{label}</label>}
      <div className="relative">
        {icon && <span className="absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400">{icon}</span>}
        <input type={type} value={value} placeholder={placeholder}
          onChange={e => onChange?.(e.target.value)}
          className={`tp-input w-full rounded-2xl border border-slate-200 bg-white px-4 py-3.5 text-sm text-slate-800 placeholder:text-slate-400 transition-all ${icon ? 'pl-10' : ''} ${trailing ? 'pr-12' : ''}`} />
        {trailing && <span className="absolute right-3.5 top-1/2 -translate-y-1/2">{trailing}</span>}
      </div>
    </div>
  )
}

function Select({ label, value, onChange, options }: {
  label?: string; value: string; onChange: (v: string) => void; options: string[]
}) {
  return (
    <div className="flex flex-col gap-1.5">
      {label && <label className="text-xs font-semibold text-slate-500 uppercase tracking-wider">{label}</label>}
      <div className="relative">
        <select value={value} onChange={e => onChange(e.target.value)}
          className="tp-input w-full appearance-none rounded-2xl border border-slate-200 bg-white px-4 py-3.5 text-sm text-slate-800 transition-all pr-10">
          <option value="">Select...</option>
          {options.map(o => <option key={o} value={o}>{o}</option>)}
        </select>
        <ChevronDown size={16} className="absolute right-4 top-1/2 -translate-y-1/2 text-slate-400 pointer-events-none" />
      </div>
    </div>
  )
}

function StatusChip({ status }: { status: string | number }) {
  const map: Record<string, string> = {
    completed: 'bg-green-50 text-green-700',
    pending: 'bg-yellow-50 text-yellow-700',
    failed: 'bg-red-50 text-red-700',
    refunded: 'bg-blue-50 text-blue-700',
    approved: 'bg-green-50 text-green-700',
    rejected: 'bg-red-50 text-red-700',
    expired: 'bg-slate-50 text-slate-700',
  }
  const statusStr = String(status).toLowerCase()
  return <span className={`chip ${map[statusStr] || map.completed}`}>{status}</span>
}

function TxIcon({ type }: { type: string }) {
  const t = type.toLowerCase()
  if (t === 'payment' || t === 'fare') return <div className="w-10 h-10 rounded-2xl bg-blue-50 flex items-center justify-center shrink-0"><Bus size={18} className="text-blue-600" /></div>
  if (t === 'top_up' || t === 'topup') return <div className="w-10 h-10 rounded-2xl bg-green-50 flex items-center justify-center shrink-0"><ArrowDownLeft size={18} className="text-green-600" /></div>
  return <div className="w-10 h-10 rounded-2xl bg-orange-50 flex items-center justify-center shrink-0"><RotateCcw size={18} className="text-orange-500" /></div>
}

function formatTxType(type: string): string {
  const t = type.toLowerCase()
  if (t === 'payment' || t === 'fare') return 'fare'
  if (t === 'top_up' || t === 'topup') return 'topup'
  return t
}

function formatTxDate(iso: string): string {
  return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
}

// ── SPLASH ────────────────────────────────────────────────────────────────────

function SplashScreen({ next }: { next: () => void }) {
  useEffect(() => { const t = setTimeout(next, 2200); return () => clearTimeout(t) }, [next])
  return (
    <div className="flex-1 flex flex-col items-center justify-center bg-blue-gradient min-h-full gap-4">
      <div className="relative">
        <div className="w-24 h-24 rounded-3xl bg-white/20 backdrop-blur flex items-center justify-center shadow-2xl">
          <Bus size={44} className="text-white" />
        </div>
        <div className="absolute -inset-2 rounded-[36px] border-2 border-white/30 pulse-ring" />
      </div>
      <div className="text-center">
        <h1 className="font-poppins text-4xl font-bold text-white tracking-tight">TransitPay</h1>
        <p className="text-blue-100 mt-1 text-sm">Cashless. Seamless. Commute.</p>
      </div>
      <div className="absolute bottom-16 flex gap-1.5">
        {[0,1,2].map(i => (
          <div key={i} className="w-2 h-2 rounded-full bg-white/40" style={{ animationDelay: `${i * 0.2}s` }}>
            <div className="w-full h-full rounded-full bg-white animate-pulse" style={{ animationDelay: `${i * 0.3}s` }} />
          </div>
        ))}
      </div>
    </div>
  )
}

// ── WELCOME ───────────────────────────────────────────────────────────────────

function WelcomeScreen({ go }: { go: (s: Screen) => void }) {
  return (
    <div className="flex-1 flex flex-col min-h-full bg-white">
      <div className="flex-1 flex flex-col items-center justify-center bg-blue-gradient px-6 py-12 relative overflow-hidden">
        <div className="absolute top-[-60px] right-[-60px] w-52 h-52 rounded-full bg-white/10" />
        <div className="absolute bottom-[-40px] left-[-40px] w-40 h-40 rounded-full bg-white/10" />
        <div className="w-20 h-20 rounded-3xl bg-white/20 backdrop-blur flex items-center justify-center mb-6 shadow-xl">
          <Bus size={38} className="text-white" />
        </div>
        <h1 className="font-poppins text-3xl font-bold text-white text-center">Welcome to<br />TransitPay</h1>
        <p className="text-blue-100 text-center mt-3 text-sm leading-relaxed max-w-xs">
          Your all-in-one cashless payment solution for public transportation across the Philippines.
        </p>
        <div className="flex gap-6 mt-8">
          {[{ icon: Shield, text: 'Secure' }, { icon: Zap, text: 'Instant' }, { icon: Bus, text: 'Smart' }].map(({ icon: Icon, text }) => (
            <div key={text} className="flex flex-col items-center gap-1">
              <div className="w-10 h-10 rounded-2xl bg-white/20 flex items-center justify-center">
                <Icon size={18} className="text-white" />
              </div>
              <span className="text-xs text-blue-100">{text}</span>
            </div>
          ))}
        </div>
      </div>
      <div className="p-6 flex flex-col gap-3 bg-white">
        <Btn variant="primary" size="lg" onClick={() => go('login')}>Log In</Btn>
        <Btn variant="secondary" size="lg" onClick={() => go('register')}>Create Account</Btn>
        <p className="text-center text-xs text-slate-400 mt-2">
          By continuing, you agree to our <span className="text-blue-600 font-medium">Terms</span> and <span className="text-blue-600 font-medium">Privacy Policy</span>
        </p>
      </div>
    </div>
  )
}

// ── LOGIN ─────────────────────────────────────────────────────────────────────

function LoginScreen({ go }: { go: (s: Screen) => void }) {
  const [mobile, setMobile] = useState(() => {
    // Remember last logged-in mobile number
    return localStorage.getItem('transitpay_last_mobile') || ''
  })
  const [pass, setPass] = useState('')
  const [show, setShow] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const submit = async () => {
    setLoading(true)
    setError('')
    try {
      // Save mobile number for next time
      localStorage.setItem('transitpay_last_mobile', mobile)
      // Use mobile number as username for login
      await authService.login({ username: mobile, password: pass })
      go('home')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex-1 flex flex-col min-h-full bg-[#F0F4FF]">
      <div className="bg-blue-gradient px-6 pt-12 pb-16 relative overflow-hidden">
        <div className="absolute top-[-40px] right-[-40px] w-40 h-40 rounded-full bg-white/10" />
        <button onClick={() => go('welcome')} className="text-white/80 mb-4 flex items-center gap-1">
          <ArrowLeft size={18} /> Back
        </button>
        <h2 className="font-poppins text-2xl font-bold text-white">Welcome back!</h2>
        <p className="text-blue-100 text-sm mt-1">Sign in to your TransitPay account</p>
      </div>
      <div className="flex-1 -mt-6 bg-white rounded-t-3xl px-6 pt-8 pb-6 flex flex-col gap-5">
        <Input label="Mobile Number" type="tel" placeholder="09XX XXX XXXX" value={mobile} onChange={setMobile}
          icon={<Phone size={16} />} />
        <Input label="Password" type={show ? 'text' : 'password'} placeholder="Enter password" value={pass} onChange={setPass}
          icon={<Lock size={16} />}
          trailing={<button onClick={() => setShow(!show)} className="text-slate-400">{show ? <EyeOff size={16} /> : <Eye size={16} />}</button>} />
        {error && (
          <div className="bg-red-50 border border-red-200 rounded-2xl p-3 flex items-start gap-2">
            <AlertCircle size={15} className="text-red-600 shrink-0 mt-0.5" />
            <p className="text-xs text-red-600">{error}</p>
          </div>
        )}
        <button onClick={() => go('forgot')} className="text-right text-sm text-blue-600 font-medium -mt-2">Forgot Password?</button>
        <Btn variant="primary" size="lg" onClick={submit} disabled={loading}>
          {loading ? <><RefreshCw size={16} className="animate-spin" /> Signing in...</> : 'Log In'}
        </Btn>
        <p className="text-center text-sm text-slate-500">
          Don't have an account? <button onClick={() => go('register')} className="text-blue-600 font-semibold">Sign Up</button>
        </p>
      </div>
    </div>
  )
}

// ── REGISTER ──────────────────────────────────────────────────────────────────

function RegisterScreen({ go }: { go: (s: Screen) => void }) {
  const [form, setForm] = useState({ first: '', last: '', mobile: '', pass: '', confirm: '' })
  const set = (k: string) => (v: string) => setForm(f => ({ ...f, [k]: v }))
  const [show, setShow] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const submit = async () => {
    if (form.pass !== form.confirm) {
      setError('Passwords do not match.')
      return
    }
    setLoading(true)
    setError('')
    try {
      // Register the user
      const registerResult = await authService.register({
        username: form.mobile, // Use mobile number as username
        firstName: form.first,
        lastName: form.last,
        mobileNumber: form.mobile,
        password: form.pass,
      })
      
      if (registerResult.success) {
        // Automatically log in the user after successful registration
        try {
          const loginResult = await authService.login({
            username: form.mobile,
            password: form.pass,
          })
          
          // login() already stores token and user in localStorage
          if (loginResult && loginResult.token) {
            go('home')
          } else {
            // Registration succeeded but auto-login failed - redirect to login
            go('login')
          }
        } catch (loginErr) {
          // Registration succeeded but auto-login failed - redirect to login
          go('login')
        }
      } else {
        setError(registerResult.message || 'Registration failed')
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Registration failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex-1 flex flex-col min-h-full bg-[#F0F4FF]">
      <div className="bg-blue-gradient px-6 pt-12 pb-14 relative overflow-hidden">
        <div className="absolute top-[-40px] right-[-40px] w-40 h-40 rounded-full bg-white/10" />
        <button onClick={() => go('welcome')} className="text-white/80 mb-4 flex items-center gap-1">
          <ArrowLeft size={18} /> Back
        </button>
        <h2 className="font-poppins text-2xl font-bold text-white">Create Account</h2>
        <p className="text-blue-100 text-sm mt-1">Join TransitPay for free today</p>
      </div>
      <div className="flex-1 -mt-6 bg-white rounded-t-3xl px-6 pt-8 pb-6 flex flex-col gap-4 overflow-y-auto mobile-scroll">
        <div className="grid grid-cols-2 gap-3">
          <Input label="First Name" placeholder="Juan" value={form.first} onChange={set('first')} />
          <Input label="Last Name" placeholder="Dela Cruz" value={form.last} onChange={set('last')} />
        </div>
        <Input label="Mobile Number" type="tel" placeholder="09XX XXX XXXX" value={form.mobile} onChange={set('mobile')} icon={<Phone size={16} />} />
        <Input label="Password" type={show ? 'text' : 'password'} placeholder="Min. 8 characters" value={form.pass} onChange={set('pass')}
          icon={<Lock size={16} />}
          trailing={<button onClick={() => setShow(!show)} className="text-slate-400">{show ? <EyeOff size={16} /> : <Eye size={16} />}</button>} />
        <Input label="Confirm Password" type={show ? 'text' : 'password'} placeholder="Re-enter password" value={form.confirm} onChange={set('confirm')} icon={<Lock size={16} />} />
        <div className="flex items-start gap-2 mt-1">
          <input type="checkbox" id="terms" className="mt-0.5 accent-blue-600" />
          <label htmlFor="terms" className="text-xs text-slate-500 leading-relaxed">
            I agree to TransitPay's <span className="text-blue-600 font-medium">Terms of Service</span> and <span className="text-blue-600 font-medium">Privacy Policy</span>
          </label>
        </div>
        {error && (
          <div className="bg-red-50 border border-red-200 rounded-2xl p-3 flex items-start gap-2">
            <AlertCircle size={15} className="text-red-600 shrink-0 mt-0.5" />
            <p className="text-xs text-red-600">{error}</p>
          </div>
        )}
        <Btn variant="primary" size="lg" onClick={submit} disabled={loading}>
          {loading ? <><RefreshCw size={16} className="animate-spin" /> Processing...</> : 'Create Account'}
        </Btn>
        <p className="text-center text-sm text-slate-500">
          Already have an account? <button onClick={() => go('login')} className="text-blue-600 font-semibold">Log In</button>
        </p>
      </div>
    </div>
  )
}

// ── FORGOT PASSWORD ───────────────────────────────────────────────────────────

function ForgotScreen({ go }: { go: (s: Screen) => void }) {
  const [mobile, setMobile] = useState('')
  const [sent, setSent] = useState(false)
  return (
    <div className="flex-1 flex flex-col min-h-full bg-[#F0F4FF]">
      <div className="bg-blue-gradient px-6 pt-12 pb-14">
        <button onClick={() => go('login')} className="text-white/80 mb-4 flex items-center gap-1"><ArrowLeft size={18} /> Back</button>
        <h2 className="font-poppins text-2xl font-bold text-white">Forgot Password</h2>
        <p className="text-blue-100 text-sm mt-1">We'll send a reset code to your number</p>
      </div>
      <div className="flex-1 -mt-6 bg-white rounded-t-3xl px-6 pt-8 pb-6 flex flex-col gap-5">
        {!sent ? (
          <>
            <div className="w-16 h-16 rounded-3xl bg-blue-50 flex items-center justify-center mx-auto">
              <Phone size={28} className="text-blue-600" />
            </div>
            <p className="text-center text-sm text-slate-500 -mt-2">Enter your registered mobile number and we'll send you a 6-digit OTP.</p>
            <Input label="Mobile Number" type="tel" placeholder="09XX XXX XXXX" value={mobile} onChange={setMobile} icon={<Phone size={16} />} />
            <Btn variant="primary" size="lg" onClick={() => setSent(true)}>Send OTP</Btn>
          </>
        ) : (
          <>
            <div className="w-16 h-16 rounded-3xl bg-green-50 flex items-center justify-center mx-auto">
              <CheckCircle size={28} className="text-green-600" />
            </div>
            <p className="font-poppins text-center font-semibold text-slate-800">OTP Sent!</p>
            <p className="text-center text-sm text-slate-500">A 6-digit code was sent to <span className="font-semibold text-slate-700">{mobile || '09XX XXX XXXX'}</span></p>
            <Btn variant="primary" size="lg" onClick={() => go('otp')}>Enter OTP</Btn>
          </>
        )}
      </div>
    </div>
  )
}

// ── OTP ───────────────────────────────────────────────────────────────────────

function OTPScreen({ go }: { go: (s: Screen) => void }) {
  const [otp, setOtp] = useState(['', '', '', '', '', ''])
  const full = otp.every(d => d !== '')

  const handleChange = (i: number, v: string) => {
    if (!/^\d?$/.test(v)) return
    const next = [...otp]; next[i] = v; setOtp(next)
    if (v && i < 5) (document.getElementById(`otp-${i + 1}`) as HTMLInputElement)?.focus()
  }

  return (
    <div className="flex-1 flex flex-col min-h-full bg-[#F0F4FF]">
      <div className="bg-blue-gradient px-6 pt-12 pb-14">
        <button onClick={() => go('register')} className="text-white/80 mb-4 flex items-center gap-1"><ArrowLeft size={18} /> Back</button>
        <h2 className="font-poppins text-2xl font-bold text-white">Verify OTP</h2>
        <p className="text-blue-100 text-sm mt-1">Enter the 6-digit code we sent you</p>
      </div>
      <div className="flex-1 -mt-6 bg-white rounded-t-3xl px-6 pt-8 pb-6 flex flex-col gap-6">
        <div className="flex gap-2 justify-center">
          {otp.map((d, i) => (
            <input key={i} id={`otp-${i}`} maxLength={1} value={d}
              onChange={e => handleChange(i, e.target.value)}
              className="w-11 h-14 text-center text-xl font-bold rounded-2xl border-2 border-slate-200 bg-slate-50 text-slate-800 transition-all focus:outline-none focus:border-blue-500 focus:bg-white" />
          ))}
        </div>
        <p className="text-center text-sm text-slate-500">Didn't receive code? <button className="text-blue-600 font-medium">Resend in 0:45</button></p>
        <Btn variant="primary" size="lg" disabled={!full} onClick={() => go('home')}>Verify & Continue</Btn>
      </div>
    </div>
  )
}

// ── TRANSIT CARD NOT FOUND ────────────────────────────────────────────────────

function TransitCardNotFound({ go }: { go: (s: Screen) => void }) {
  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF] overflow-y-auto mobile-scroll">
      <div className="bg-blue-gradient px-5 pt-10 pb-20 relative overflow-hidden">
        <div className="absolute top-[-40px] right-[-40px] w-40 h-40 rounded-full bg-white/10" />
        <button onClick={() => go('home')} className="text-white/80 mb-4 flex items-center gap-1">
          <ArrowLeft size={18} /> Back
        </button>
        <h2 className="font-poppins text-xl font-bold text-white">My Wallet</h2>
      </div>
      <div className="mx-4 -mt-12 bg-white rounded-3xl shadow-lg p-8 text-center relative z-10">
        <div className="w-16 h-16 rounded-2xl bg-slate-100 flex items-center justify-center mx-auto mb-4">
          <CreditCard size={28} className="text-slate-400" />
        </div>
        <p className="font-poppins font-bold text-slate-800">Transit Card Not Found</p>
        <p className="text-sm text-slate-500 mt-2 leading-relaxed">
          Your account is not yet linked to a Transit Card.
        </p>
        <p className="text-xs text-slate-400 mt-2 leading-relaxed">
          Wallet, QR Code, and Transaction History are unavailable until a card is linked to your account.
        </p>
        <Btn variant="primary" size="lg" className="mt-6" onClick={() => go('home')}>
          Back to Home
        </Btn>
      </div>
    </div>
  )
}

// ── HOME ──────────────────────────────────────────────────────────────────────

function HomeScreen({ go, cardId, onNavigateToDetail }: { go: (s: Screen) => void; cardId: number | null; onNavigateToDetail?: (planId?: number) => void }) {
  const [showBal, setShowBal] = useState(true)
  const [wallet, setWallet] = useState<Wallet | null>(null)
  const [recentTx, setRecentTx] = useState<Transaction[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [cardInfo, setCardInfo] = useState<Card | null>(null)
  const [discountType, setDiscountType] = useState<DiscountType | null>(null)
  const [activePlan, setActivePlan] = useState<TripPlan | null>(null)
  const [activePlanFare, setActivePlanFare] = useState<{ normalFare: number; discountPercentage: number | null; discountAmount: number | null; finalFare: number } | null>(null)

  useEffect(() => {
    if (cardId === null) {
      setLoading(false)
      return
    }
    const load = async () => {
      setLoading(true)
      setError('')
      const user = await authService.getUser()
      try {
        const [w, txs, card, plan, terminalsData] = await Promise.all([
          walletService.getWallet(cardId),
          walletService.getTransactions(cardId, 1, 4),
          getMyCard(user!.userId),
          tripPlanService.getActiveTripPlan().catch(() => null),
          api.get<{ success: boolean; data: { terminalId: number; terminalName: string }[] }>('/api/terminal').catch(() => ({ success: false, data: [] })),
        ])
        setWallet(w)
        setRecentTx(txs.data)
        setCardInfo(card)
        // If the API doesn't return terminal names, map them from terminals list
        if (plan && (!plan.originTerminalName || !plan.destinationTerminalName)) {
          const originTerminal = terminalsData.data.find(t => t.terminalId === plan.originTerminalId)
          const destinationTerminal = terminalsData.data.find(t => t.terminalId === plan.destinationTerminalId)
          if (originTerminal && destinationTerminal) {
            plan.originTerminalName = originTerminal.terminalName
            plan.destinationTerminalName = destinationTerminal.terminalName
          }
        }
        setActivePlan(plan)
        // Fetch accurate fare from backend for the active plan
        if (plan && cardId) {
          try {
            const fare = await tripPlanService.calculateFare(plan.originTerminalId, plan.destinationTerminalId, cardId)
            setActivePlanFare(fare)
          } catch {
            setActivePlanFare(null)
          }
        } else {
          setActivePlanFare(null)
        }
        const dt = await discountService.getCurrentDiscountType(cardId)
        setDiscountType(dt)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load wallet')
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [cardId])

  const [user, setUser] = useState<UserType | null>(null)
  
  useEffect(() => {
    authService.getUser().then(setUser)
  }, [])

  const displayName = user ? `${user.firstName} ${user.lastName}`.trim() : 'Passenger'
  const theme = getCardTheme(cardInfo?.passengerType, discountType?.name)

  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF] overflow-y-auto mobile-scroll">
      {/* Header */}
      <div className="bg-blue-gradient px-5 pt-10 pb-20 relative overflow-hidden">
        <div className="absolute top-[-60px] right-[-60px] w-52 h-52 rounded-full bg-white/10" />
        <div className="absolute top-4 right-5">
          <button className="relative w-10 h-10 rounded-full bg-white/20 flex items-center justify-center">
            <Bell size={18} className="text-white" />
            <span className="absolute top-1.5 right-1.5 w-2 h-2 rounded-full bg-orange-400" />
          </button>
        </div>
        <p className="text-blue-100 text-sm">Good morning,</p>
        <h2 className="font-poppins text-2xl font-bold text-white mt-0.5">{displayName} 👋</h2>
      </div>

      {/* Themed Transit Card */}
      {cardInfo && (
        <div
          className="mx-4 -mt-14 rounded-3xl shadow-2xl p-5 relative z-10 overflow-hidden"
          style={{
            background: `linear-gradient(135deg, ${theme.from}, ${theme.to})`,
          }}
        >
          <div className="absolute top-0 right-0 w-32 h-32 rounded-full bg-white/10 -translate-y-8 translate-x-8" />
          <div className="absolute bottom-0 left-0 w-24 h-24 rounded-full bg-white/5 translate-y-6 -translate-x-6" />
          <div className="relative z-10">
            <div className="flex items-center justify-between mb-3">
              <div className="flex items-center gap-2">
                <div className="w-6 h-6 rounded-lg bg-white/20 flex items-center justify-center">
                  <Bus size={12} className="text-white" />
                </div>
                <span className="font-poppins font-bold text-white text-sm">TransitPay</span>
              </div>
              <span className="text-xs text-white/80 bg-white/20 px-2 py-1 rounded-full">{theme.label}</span>
            </div>
            <p className="text-xs text-white/70 font-mono mt-1">
              {cardInfo.maskedCardNumber || '•••• •••• •••• ••••'}
            </p>
            <p className="font-poppins text-3xl font-bold text-white mt-2">
              {showBal ? `₱${wallet?.balance.toFixed(2) || '0.00'}` : '₱ ••••••'}
            </p>
            <div className="flex items-center justify-between mt-3">
              <button onClick={() => setShowBal(!showBal)} className="text-white/60">
                {showBal ? <Eye size={14} /> : <EyeOff size={14} />}
              </button>
              <div className="text-right">
                <p className="text-xs text-white/70">Card ID</p>
                <p className="text-xs text-white/90 font-mono">#{cardInfo.cardId}</p>
              </div>
            </div>
          </div>
        </div>
      )}


      {/* Fallback balance card (if no card info) */}
      {!cardInfo && (
        <div className="mx-4 -mt-14 bg-white rounded-3xl shadow-lg p-5 relative z-10">
          <div className="flex items-start justify-between mb-1">
            <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Wallet Balance</p>
            <button onClick={() => setShowBal(!showBal)} className="text-slate-400">
              {showBal ? <Eye size={16} /> : <EyeOff size={16} />}
            </button>
          </div>
          {loading ? (
            <div className="flex items-center gap-2 py-2">
              <RefreshCw size={18} className="text-blue-400 animate-spin" />
              <span className="text-sm text-slate-400">Loading balance...</span>
            </div>
          ) : error ? (
            <p className="text-sm text-red-500 py-2">{error}</p>
          ) : wallet ? (
            <>
              <p className="font-poppins text-3xl font-bold text-slate-800 mt-1">
                {showBal ? `₱${wallet.balance.toFixed(2)}` : '₱ ••••••'}
              </p>
              <p className="text-xs text-slate-400 mt-1 font-mono">Card: •••• •••• {wallet.cardId.toString().padStart(4, '0').slice(-4)}</p>
            </>
          ) : (
            <p className="text-sm text-slate-400 py-2">Transit Card Not Found</p>
          )}
          <div className="flex gap-2 mt-4">
            <Btn variant="primary" size="sm" className="flex-1 !rounded-xl" onClick={() => go('topup')} disabled={!wallet}>
              <Plus size={14} /> Top Up
            </Btn>
            <Btn variant="ghost" size="sm" className="!rounded-xl px-3 bg-blue-50" onClick={() => go('qr')} disabled={!wallet}>
              <QrCode size={14} />
            </Btn>
          </div>
        </div>
      )}

      {/* Quick Actions */}
      <div className="px-4 mt-5">
        <p className="font-poppins font-semibold text-slate-800 text-sm mb-3">Quick Actions</p>
        <div className="grid grid-cols-4 gap-2">
          {[
            { icon: QrCode, label: 'My QR', screen: 'qr' as Screen, color: 'bg-blue-50 text-blue-600' },
            { icon: WalletIcon, label: 'Wallet', screen: 'wallet' as Screen, color: 'bg-green-50 text-green-600' },
            { icon: MapPin, label: 'Plan a Trip', screen: 'plan-trip' as Screen, color: 'bg-indigo-50 text-indigo-600' },
            { icon: FileText, label: 'Discounts', screen: 'discounts' as Screen, color: 'bg-purple-50 text-purple-600' },
          ].map(({ icon: Icon, label, screen, color }) => (
            <button key={label} onClick={() => go(screen)}
              className="flex flex-col items-center gap-1.5 p-3 bg-white rounded-2xl shadow-sm hover:shadow-md transition-all card-hover">
              <div className={`w-10 h-10 rounded-2xl ${color} flex items-center justify-center`}>
                <Icon size={18} />
              </div>
              <span className="text-[10px] font-semibold text-slate-600">{label}</span>
            </button>
          ))}
        </div>
      </div>

      {/* Active Plan */}
      {activePlan && (
        <div className="px-4 mt-5">
          <button
            onClick={() => onNavigateToDetail?.(activePlan.planId)}
            className="w-full bg-blue-50 border border-blue-100 rounded-2xl p-3 text-left hover:bg-blue-100 transition-colors"
          >
            <div className="flex items-center justify-between">
              <p className="text-xs text-slate-500">Active Plan</p>
              <span className="text-[10px] font-semibold text-blue-600 bg-blue-100 px-2 py-0.5 rounded-full">Active</span>
            </div>
            <p className="text-sm font-semibold text-slate-800 mt-1">{activePlan.originTerminalName} → {activePlan.destinationTerminalName}</p>
            <div className="flex items-center justify-between mt-1">
              <p className="text-xs text-slate-500">Click to view details</p>
              <p className="text-xs font-semibold text-blue-600">₱{(activePlanFare?.finalFare ?? activePlan.finalFarePrice ?? 0).toFixed(2)}</p>
            </div>
          </button>
        </div>
      )}

      {/* Recent transactions */}
      <div className="px-4 mt-5 mb-4">
        <div className="flex items-center justify-between mb-3">
          <p className="font-poppins font-semibold text-slate-800 text-sm">Recent Transactions</p>
          <button onClick={() => go('wallet')} className="text-xs text-blue-600 font-medium flex items-center gap-0.5">
            See all <ChevronRight size={12} />
          </button>
        </div>
        {loading ? (
          <div className="flex items-center justify-center py-8">
            <RefreshCw size={24} className="text-blue-400 animate-spin" />
          </div>
        ) : recentTx.length === 0 ? (
          <div className="bg-white rounded-2xl p-6 text-center">
            <Clock size={32} className="text-slate-300 mx-auto mb-2" />
            <p className="text-sm text-slate-400">No recent transactions</p>
          </div>
        ) : (
          <div className="flex flex-col gap-2">
            {recentTx.map(tx => (
              <div key={tx.transactionId} className="bg-white rounded-2xl p-3.5 flex items-center gap-3 shadow-sm">
                <TxIcon type={tx.transactionType} />
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-semibold text-slate-800 truncate">{tx.transactionName}</p>
                  <div className="flex items-center gap-2 mt-0.5">
                    <p className="text-xs text-slate-400 font-mono">{formatTxDate(tx.createdAt)}</p>
                    <StatusChip status={tx.status || 'completed'} />
                  </div>
                </div>
                <p className={`text-sm font-bold shrink-0 ${tx.amount > 0 ? 'text-green-600' : 'text-slate-800'}`}>
                  {tx.amount > 0 ? '+' : ''}₱{Math.abs(tx.amount).toFixed(2)}
                </p>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

// ── WALLET ────────────────────────────────────────────────────────────────────

function WalletScreen({ go, cardId, onSelectTx }: { go: (s: Screen) => void; cardId: number | null; onSelectTx?: (tx: Transaction) => void }) {
  const [filter, setFilter] = useState('all')
  const [wallet, setWallet] = useState<Wallet | null>(null)
  const [transactions, setTransactions] = useState<Transaction[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    if (cardId === null) {
      setLoading(false)
      return
    }
    const load = async () => {
      setLoading(true)
      setError('')
      try {
        const [w, txs] = await Promise.all([
          walletService.getWallet(cardId),
          walletService.getTransactions(cardId, 1, 50),
        ])
        setWallet(w)
        setTransactions(txs.data)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load wallet')
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [cardId])

  const stats = computeWalletStats(transactions)
  const filtered = filter === 'all' ? transactions : transactions.filter(t => formatTxType(t.transactionType) === filter)

  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF] overflow-y-auto mobile-scroll">
      <div className="bg-blue-gradient px-5 pt-10 pb-20 relative overflow-hidden">
        <div className="absolute top-[-40px] right-[-40px] w-40 h-40 rounded-full bg-white/10" />
        <button onClick={() => go('home')} className="text-white/80 mb-4 flex items-center gap-1">
          <ArrowLeft size={18} /> Back
        </button>
        <h2 className="font-poppins text-xl font-bold text-white">My Wallet</h2>
      </div>

      {/* Balance */}
      <div className="mx-4 -mt-12 bg-card-gradient rounded-3xl shadow-lg p-5 relative z-10">
        <p className="text-blue-200 text-xs uppercase tracking-wider font-semibold">Available Balance</p>
        {loading ? (
          <div className="flex items-center gap-2 py-2">
            <RefreshCw size={18} className="text-white/70 animate-spin" />
            <span className="text-sm text-blue-200">Loading...</span>
          </div>
        ) : error ? (
          <p className="text-sm text-red-200 py-2">{error}</p>
        ) : wallet ? (
          <p className="font-poppins text-4xl font-bold text-white mt-1">₱{wallet.balance.toFixed(2)}</p>
        ) : (
          <p className="text-sm text-blue-200 py-2">Transit Card Not Found</p>
        )}
        <div className="flex gap-3 mt-4">
          <Btn variant="primary" size="sm" className="flex-1 bg-white/20 hover:bg-white/30 !text-white border-white/30" onClick={() => go('topup')} disabled={!wallet}>
            <ArrowDownLeft size={14} /> Top Up
          </Btn>
        </div>
      </div>

      {/* Stats */}
      <div className="mx-4 mt-4 grid grid-cols-2 gap-3">
        <div className="bg-white rounded-2xl p-4 shadow-sm">
          <div className="flex items-center gap-2 mb-1">
            <ArrowDownLeft size={14} className="text-green-500" />
            <span className="text-xs text-slate-500 font-medium">Total Top Up</span>
          </div>
          <p className="font-poppins text-xl font-bold text-slate-800">₱{stats.totalTopUp.toFixed(2)}</p>
          <p className="text-xs text-slate-400">This month</p>
        </div>
        <div className="bg-white rounded-2xl p-4 shadow-sm">
          <div className="flex items-center gap-2 mb-1">
            <ArrowUpRight size={14} className="text-blue-500" />
            <span className="text-xs text-slate-500 font-medium">Total Spent</span>
          </div>
          <p className="font-poppins text-xl font-bold text-slate-800">₱{stats.totalSpent.toFixed(2)}</p>
          <p className="text-xs text-slate-400">This month</p>
        </div>
      </div>

      {/* Filter chips */}
      <div className="px-4 mt-4">
        <div className="flex gap-2 overflow-x-auto pb-1">
          {['all', 'fare', 'topup', 'refund'].map(f => (
            <button key={f} onClick={() => setFilter(f)}
              className={`px-4 py-1.5 rounded-full text-xs font-semibold whitespace-nowrap transition-all ${filter === f ? 'bg-blue-600 text-white shadow-sm' : 'bg-white text-slate-600 border border-slate-200'}`}>
              {f.charAt(0).toUpperCase() + f.slice(1)}
            </button>
          ))}
        </div>
      </div>

      {/* Transaction list */}
      <div className="px-4 mt-3 mb-4 flex flex-col gap-2">
        <p className="font-poppins font-semibold text-sm text-slate-700">Transaction History</p>
        {loading ? (
          <div className="flex items-center justify-center py-8">
            <RefreshCw size={24} className="text-blue-400 animate-spin" />
          </div>
        ) : filtered.length === 0 ? (
          <div className="bg-white rounded-2xl p-6 text-center">
            <Clock size={32} className="text-slate-300 mx-auto mb-2" />
            <p className="text-sm text-slate-400">No transactions found</p>
          </div>
        ) : (
          filtered.map(tx => (
            <button
              key={tx.transactionId}
              onClick={() => onSelectTx?.(tx)}
              className="bg-white rounded-2xl p-3.5 flex items-center gap-3 shadow-sm text-left hover:bg-slate-50 transition-colors cursor-pointer"
            >
              <TxIcon type={tx.transactionType} />
              <div className="flex-1 min-w-0">
                <p className="text-sm font-semibold text-slate-800 truncate">{tx.transactionName}</p>
                <div className="flex items-center gap-2 mt-0.5">
                  <p className="text-xs text-slate-400 font-mono">{formatTxDate(tx.createdAt)}</p>
                  <StatusChip status={tx.status || 'completed'} />
                </div>
              </div>
              <p className={`text-sm font-bold shrink-0 ${tx.amount > 0 ? 'text-green-600' : 'text-slate-800'}`}>
                {tx.amount > 0 ? '+' : ''}₱{Math.abs(tx.amount).toFixed(2)}
              </p>
            </button>
          ))
        )}
      </div>
    </div>
  )
}

// ── TOP UP ────────────────────────────────────────────────────────────────────

function TopUpScreen({ go, cardId }: { go: (s: Screen) => void; cardId: number | null }) {
  const [wallet, setWallet] = useState<Wallet | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    if (cardId === null) {
      setLoading(false)
      return
    }
    const load = async () => {
      setLoading(true)
      setError('')
      try {
        const w = await walletService.getWallet(cardId)
        setWallet(w)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load wallet')
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [cardId])

  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF] overflow-y-auto mobile-scroll">
      <div className="bg-blue-gradient px-5 pt-10 pb-14">
        <button onClick={() => go('wallet')} className="text-white/80 mb-4 flex items-center gap-1"><ArrowLeft size={18} /> Back</button>
        <h2 className="font-poppins text-xl font-bold text-white">Top Up Wallet</h2>
        <p className="text-blue-100 text-sm mt-1">
          Current Balance:{' '}
          {loading ? <RefreshCw size={12} className="inline animate-spin" /> : error ? <span className="text-red-200">{error}</span> : wallet ? <span className="font-bold text-white">₱{wallet.balance.toFixed(2)}</span> : <span className="font-bold text-white">—</span>}
        </p>
      </div>
      <div className="-mt-6 bg-white rounded-t-3xl flex-1 px-5 pt-6 pb-6 flex flex-col gap-5">
        {/* Admin credit notice */}
        <div className="bg-blue-50 border border-blue-100 rounded-2xl p-5 flex flex-col items-center gap-3 text-center">
          <div className="w-14 h-14 rounded-2xl bg-blue-100 flex items-center justify-center">
            <Shield size={28} className="text-blue-600" />
          </div>
          <div>
            <p className="font-poppins font-bold text-slate-800">Admin-Managed Credits</p>
            <p className="text-xs text-slate-500 mt-1 leading-relaxed">
              Online payment methods (GCash/Maya) are temporarily disabled for testing.
              Please contact your administrator to add credits to your wallet.
            </p>
          </div>
        </div>

        <div className="bg-slate-50 rounded-2xl p-4 flex flex-col gap-2">
          <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider">How to get credits:</p>
          <div className="flex items-start gap-2">
            <CheckCircle size={14} className="text-green-500 shrink-0 mt-0.5" />
            <p className="text-xs text-slate-600">Contact your system administrator</p>
          </div>
          <div className="flex items-start gap-2">
            <CheckCircle size={14} className="text-green-500 shrink-0 mt-0.5" />
            <p className="text-xs text-slate-600">Provide your mobile number or card ID</p>
          </div>
          <div className="flex items-start gap-2">
            <CheckCircle size={14} className="text-green-500 shrink-0 mt-0.5" />
            <p className="text-xs text-slate-600">Admin will credit your wallet directly</p>
          </div>
        </div>

        <Btn variant="secondary" size="lg" onClick={() => go('wallet')}>
          <ArrowLeft size={16} /> Back to Wallet
        </Btn>
      </div>
    </div>
  )
}

// ── QR CODE ───────────────────────────────────────────────────────────────────

function QRScreen({ go, cardId }: { go: (s: Screen) => void; cardId: number | null }) {
  const [qrData, setQrData] = useState('')
  const [qrSignature, setQrSignature] = useState('')
  const [qrCardNumber, setQrCardNumber] = useState('')
  const [wallet, setWallet] = useState<Wallet | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [copied, setCopied] = useState(false)
  const [cardInfo, setCardInfo] = useState<Card | null>(null)
  const [discountType, setDiscountType] = useState<DiscountType | null>(null)
  const [user, setUser] = useState<UserType | null>(null)
  const [userLoaded, setUserLoaded] = useState(false)

  useEffect(() => {
    authService.getUser().then(setUser).finally(() => setUserLoaded(true))
  }, [])

  useEffect(() => {
    console.log('[QR] useEffect triggered, userLoaded:', userLoaded, 'user:', !!user, 'cardId:', cardId)
    
    // Wait for both user to load AND cardId to be available
    if (!userLoaded || !user || cardId === null) {
      console.log('[QR] Waiting for user/cardId to load')
      if (userLoaded && !cardId) {
        console.log('[QR] No cardId available')
        setError('No card linked to your account')
        setLoading(false)
      }
      return
    }
    
    // Clear error when cardId becomes available
    console.log('[QR] User and CardId available, fetching QR data...')
    setError('')
    
    const fetchQR = async () => {
      setLoading(true)
      try {
        console.log('[QR] Fetching QR, wallet, and card data...')
        const [ticket, w, card] = await Promise.all([
          qrService.getQR(cardId),
          walletService.getWallet(cardId),
          getMyCard(user.userId),
        ])
        console.log('[QR] QR ticket received:', ticket)
        console.log('[QR] Wallet received:', w)
        console.log('[QR] Card received:', card)
        setQrData(ticket.data)
        setQrSignature(ticket.signature)
        setQrCardNumber(ticket.maskedCardNumber || '')
        setWallet(w)
        setCardInfo(card)
        console.log('[QR] Fetching discount type...')
        const dt = await discountService.getCurrentDiscountType(cardId)
        console.log('[QR] Discount type received:', dt)
        setDiscountType(dt)
        console.log('[QR] All data loaded successfully')
      } catch (err) {
        console.error('[QR] Error fetching QR data:', err)
        setError(err instanceof Error ? err.message : 'Failed to get QR')
      } finally {
        setLoading(false)
      }
    }
    fetchQR()
  }, [cardId, userLoaded]) // Add userLoaded to dependencies

  const displayName = user ? `${user.firstName} ${user.lastName}`.trim() : 'Passenger'
  const theme = getCardTheme(cardInfo?.passengerType, discountType?.name)

  // Combine data and signature for QR code scanning (format: data.signature)
  const qrCodeValue = qrData && qrSignature ? `${qrData}.${qrSignature}` : qrData

  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF] overflow-y-auto mobile-scroll">
      <div className="bg-blue-gradient px-5 pt-10 pb-14">
        <button onClick={() => go('home')} className="text-white/80 mb-4 flex items-center gap-1"><ArrowLeft size={18} /> Back</button>
        <h2 className="font-poppins text-xl font-bold text-white">My QR Code</h2>
        <p className="text-blue-100 text-sm mt-1">Show this QR to the driver</p>
      </div>
      <div className="-mt-6 bg-white rounded-t-3xl flex-1 px-5 pt-6 pb-6 flex flex-col items-center gap-5">
        {/* Themed QR card */}
        <div
          className="rounded-3xl shadow-xl p-6 w-full flex flex-col items-center gap-4 relative overflow-hidden"
          style={{
            background: `linear-gradient(135deg, ${theme.from}, ${theme.to})`,
          }}
        >
          <div className="absolute top-0 right-0 w-24 h-24 rounded-full bg-white/10 -translate-y-6 translate-x-6" />
          <div className="absolute bottom-0 left-0 w-20 h-20 rounded-full bg-white/5 translate-y-4 -translate-x-4" />
          <div className="relative z-10 flex flex-col items-center gap-4 w-full">
            <div className="flex items-center gap-2">
              <div className="w-6 h-6 rounded-lg bg-white/20 flex items-center justify-center">
                <Bus size={12} className="text-white" />
              </div>
              <span className="font-poppins font-bold text-white text-sm">TransitPay</span>
            </div>
            <div className="p-3 bg-white rounded-2xl shadow-inner border border-slate-100">
              {loading ? (
                <div className="w-[200px] h-[200px] flex items-center justify-center">
                  <RefreshCw size={32} className="text-blue-400 animate-spin" />
                </div>
              ) : error ? (
                <div className="w-[200px] h-[200px] flex items-center justify-center text-center px-4">
                  <p className="text-xs text-red-500">{error}</p>
                </div>
              ) : qrCodeValue ? (
                <QRCodeSVG value={qrCodeValue} size={256} level="H" bgColor="#ffffff" fgColor="#1565C0" />
              ) : (
                <div className="w-[200px] h-[200px] flex items-center justify-center text-center px-4">
                  <p className="text-xs text-slate-400">Transit Card Not Found</p>
                </div>
              )}
            </div>
            <div className="text-center">
              <p className="font-poppins font-bold text-white">{displayName}</p>
              <p className="text-xs text-white/70 font-mono mt-0.5">{qrCardNumber || 'No card linked'}</p>
            </div>
            <div className="w-full bg-white/15 rounded-2xl px-4 py-3 flex items-center justify-between">
              <div>
                <p className="text-xs text-white/70">Wallet Balance</p>
                <p className="font-poppins font-bold text-white">{wallet ? `₱${wallet.balance.toFixed(2)}` : '—'}</p>
              </div>
              <span className="text-xs text-white/80 bg-white/20 px-2 py-1 rounded-full">{theme.label}</span>
            </div>
          </div>
        </div>

        <div className="bg-yellow-50 border border-yellow-200 rounded-2xl p-3.5 w-full flex items-start gap-2">
          <AlertCircle size={15} className="text-yellow-600 shrink-0 mt-0.5" />
          <p className="text-xs text-slate-600 leading-relaxed">
            This is your permanent TransitPay QR code. It uniquely identifies your card — it does not change per trip. Do not share it with others.
          </p>
        </div>

        <div className="flex gap-2 w-full">
          <Btn variant="secondary" size="md" className="flex-1" onClick={() => { setCopied(true); setTimeout(() => setCopied(false), 1500) }} disabled={!qrCardNumber}>
            {copied ? <><CheckCircle size={14} /> Copied!</> : <><Copy size={14} /> Copy ID</>}
          </Btn>
        </div>
      </div>
    </div>
  )
}

// ── DISCOUNTS ─────────────────────────────────────────────────────────────────

function DiscountsScreen({ go, cardId }: { go: (s: Screen) => void; cardId: number | null }) {
  const [applications, setApplications] = useState<DiscountApplication[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    if (cardId === null) {
      setLoading(false)
      return
    }
    loadApplications()
  }, [cardId])

  const loadApplications = async () => {
    setLoading(true)
    setError('')
    try {
      const apps = await discountService.getMyApplications(cardId!)
      setApplications(apps)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load applications')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF] overflow-y-auto mobile-scroll">
      <div className="bg-blue-gradient px-5 pt-10 pb-16 relative overflow-hidden">
        <div className="absolute top-[-40px] right-[-40px] w-40 h-40 rounded-full bg-white/10" />
        <button onClick={() => go('home')} className="text-white/80 mb-4 flex items-center gap-1">
          <ArrowLeft size={18} /> Back
        </button>
        <h2 className="font-poppins text-xl font-bold text-white">My Discounts</h2>
        <p className="text-blue-100 text-sm mt-1">View and apply for discounts</p>
      </div>

      <div className="-mt-6 bg-[#F0F4FF] rounded-t-3xl pt-4">
        <div className="px-4 mb-4">
          <Btn variant="primary" size="lg" onClick={() => go('apply-discount')} disabled={cardId === null}>
            <Plus size={18} /> Apply for New Discount
          </Btn>
        </div>

        <div className="px-4 flex flex-col gap-2 pb-4">
          <p className="font-poppins font-semibold text-sm text-slate-700">My Applications</p>
          {loading ? (
            <div className="flex items-center justify-center py-8">
              <RefreshCw size={24} className="text-blue-400 animate-spin" />
            </div>
          ) : error ? (
            <div className="bg-red-50 border border-red-200 rounded-2xl p-3 flex items-start gap-2">
              <AlertCircle size={15} className="text-red-600 shrink-0 mt-0.5" />
              <p className="text-xs text-red-600">{error}</p>
            </div>
          ) : applications.length === 0 ? (
            <div className="bg-white rounded-2xl p-6 text-center">
              <FileText size={32} className="text-slate-300 mx-auto mb-2" />
              <p className="text-sm text-slate-400">No discount applications yet</p>
              <p className="text-xs text-slate-400 mt-1">Apply for a discount to get started</p>
            </div>
          ) : (
            applications.map(app => (
              <div key={app.discountApplicationId} className="bg-white rounded-2xl p-4 shadow-sm">
                <div className="flex items-start justify-between mb-2">
                  <div>
                    <p className="font-semibold text-slate-800">{app.discountTypeName || 'Discount'}</p>
                    <p className="text-xs text-slate-500 mt-0.5">{app.discountPercentage}% discount</p>
                  </div>
                  <StatusChip status={app.status} />
                </div>
                <p className="text-xs text-slate-400">Applied on {new Date(app.createdAt).toLocaleDateString()}</p>
                {app.rejectionReason && (
                  <div className="mt-2 bg-red-50 rounded-xl p-2.5">
                    <p className="text-xs text-red-600"><strong>Reason:</strong> {app.rejectionReason}</p>
                  </div>
                )}
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  )
}

// ── APPLY FOR DISCOUNT ────────────────────────────────────────────────────────

function ApplyDiscountScreen({ go, cardId }: { go: (s: Screen) => void; cardId: number | null }) {
  const [discountTypes, setDiscountTypes] = useState<DiscountType[]>([])
  const [selectedType, setSelectedType] = useState<DiscountType | null>(null)
  const [document, setDocument] = useState('')
  const [documentFile, setDocumentFile] = useState<File | null>(null)
  const [loading, setLoading] = useState(false)
  const [application, setApplication] = useState<DiscountApplication | null>(null)
  const [loadingApp, setLoadingApp] = useState(true)
  const [error, setError] = useState('')
  const [showForm, setShowForm] = useState(false)

  // Load discount types and check for existing application from the database
  useEffect(() => {
    loadDiscountTypes()
    if (cardId !== null) {
      loadExistingApplication()
    } else {
      setLoadingApp(false)
    }
  }, [cardId])

  const loadDiscountTypes = async () => {
    setError('')
    try {
      const types = await discountService.getDiscountTypes()
      setDiscountTypes(types.filter(t => t.isActive))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load discount types')
    }
  }

  // Query the database for the passenger's existing discount application
  const loadExistingApplication = async () => {
    setLoadingApp(true)
    setError('')
    try {
      const apps = await discountService.getMyApplications(cardId!)
      // Get the most recent application
      const latest = apps.length > 0 ? apps[0] : null
      setApplication(latest)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load application status')
    } finally {
      setLoadingApp(false)
    }
  }

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) {
      setDocumentFile(file)
      // Convert file to base64 for upload
      const reader = new FileReader()
      reader.onload = () => {
        setDocument(reader.result as string)
      }
      reader.readAsDataURL(file)
    }
  }

  const handleSubmit = async () => {
    if (!selectedType || cardId === null) return
    setLoading(true)
    setError('')
    try {
      const app = await discountService.applyForDiscount(cardId, selectedType.discountTypeId, document || undefined)
      setApplication(app)
      setShowForm(false)
      // Reset form fields for next time
      setSelectedType(null)
      setDocument('')
      setDocumentFile(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to apply for discount')
    } finally {
      setLoading(false)
    }
  }

  // ── STATUS VIEW (application exists and not resubmitting) ──────────────────
  if (application && !showForm) {
    const statusName = getDiscountStatusName(application.status)
    const canReapply = statusName === 'Expired' || statusName === 'Rejected'

    const statusConfig: Record<string, { dot: string; label: string; note: string }> = {
      Pending: { dot: 'bg-yellow-400', label: 'Pending', note: 'The application is being reviewed by the admin.' },
      Approved: { dot: 'bg-green-400', label: 'Active', note: 'Your application has been approved and your account is now active.' },
      Expired: { dot: 'bg-red-400', label: 'Expired', note: 'Your discount has expired. Please contact the administrator for assistance.' },
      Rejected: { dot: 'bg-red-400', label: 'Rejected', note: 'Your application has been rejected. Please contact the administrator for assistance.' },
    }
    const config = statusConfig[statusName] || statusConfig.Pending

    const isImageDoc = application.discountDocument?.startsWith('data:image')
    const downloadHref = isImageDoc
      ? application.discountDocument
      : `data:text/plain,${encodeURIComponent(application.discountDocument || '')}`
    const downloadFilename = isImageDoc ? 'discount-document.png' : 'discount-document.txt'

    return (
      <div className="flex-1 flex flex-col bg-[#F0F4FF] overflow-y-auto mobile-scroll">
        <div className="bg-blue-gradient px-5 pt-10 pb-14">
          <button onClick={() => go('discounts')} className="text-white/80 mb-4 flex items-center gap-1">
            <ArrowLeft size={18} /> Back
          </button>
          <h2 className="font-poppins text-xl font-bold text-white">Application Status</h2>
        </div>
        <div className="-mt-6 bg-white rounded-t-3xl flex-1 px-5 pt-6 pb-6 flex flex-col gap-5">
          {/* Discount Type — read-only */}
          <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100">
            <p className="text-xs text-slate-500 uppercase tracking-wider font-semibold">Discount Type</p>
            <p className="font-poppins text-lg font-bold text-slate-800 mt-1">
              {application.discountTypeName || 'Discount'}
            </p>
          </div>

          {/* Discount % — always shown */}
          <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100">
            <p className="text-xs text-slate-500 uppercase tracking-wider font-semibold">Discount %</p>
            <p className="font-poppins text-lg font-bold text-slate-800 mt-1">
              {application.discountPercentage !== undefined && application.discountPercentage !== null
                ? `${application.discountPercentage}%`
                : '-'}
            </p>
          </div>

          {/* Discount Status — colored dot */}
          <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100">
            <p className="text-xs text-slate-500 uppercase tracking-wider font-semibold">Discount Status</p>
            <div className="flex items-center gap-3 mt-2">
              <span className={`w-3 h-3 rounded-full ${config.dot}`}></span>
              <p className="font-poppins text-lg font-bold text-slate-800">{config.label}</p>
            </div>
          </div>

          {/* Note */}
          <div className="bg-slate-50 rounded-2xl p-4">
            <p className="text-sm text-slate-600 leading-relaxed">
              {config.note}
            </p>
          </div>

          {/* Submit button — always visible below notes, disabled unless Expired/Rejected */}
          <Btn
            variant="primary"
            size="lg"
            onClick={() => setShowForm(true)}
            disabled={!canReapply}
          >
            {canReapply ? 'Submit New Application' : 'Submit Application'}
          </Btn>

          {/* Navigation buttons */}
          <div className="flex gap-2">
            <Btn variant="secondary" size="lg" className="flex-1" onClick={() => go('discounts')}>
              View My Applications
            </Btn>
            <Btn variant="ghost" size="lg" className="flex-1" onClick={() => go('home')}>
              Back to Home
            </Btn>
          </div>
        </div>
      </div>
    )
  }

  // ── APPLY FORM (no application exists) ──────────────────────────────────────
  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF]">
      <div className="bg-blue-gradient px-5 pt-10 pb-14">
        <button onClick={() => go('discounts')} className="text-white/80 mb-4 flex items-center gap-1">
          <ArrowLeft size={18} /> Back
        </button>
        <h2 className="font-poppins text-xl font-bold text-white">Apply for Discount</h2>
        <p className="text-blue-100 text-sm mt-1">Select a discount type and upload your ID</p>
      </div>
      <div className="-mt-6 bg-white rounded-t-3xl flex-1 px-5 pt-6 pb-6 flex flex-col gap-4">
        {loadingApp ? (
          <div className="flex items-center justify-center py-8">
            <RefreshCw size={24} className="text-blue-400 animate-spin" />
          </div>
        ) : (
          <>
            {error && (
              <div className="bg-red-50 border border-red-200 rounded-2xl p-3 flex items-start gap-2">
                <AlertCircle size={15} className="text-red-600 shrink-0 mt-0.5" />
                <p className="text-xs text-red-600">{error}</p>
              </div>
            )}
            <div className="flex flex-col gap-2">
              <label className="text-sm font-semibold text-slate-700">Discount Type</label>
              <div className="relative">
                <select
                  value={selectedType?.discountTypeId || ''}
                  onChange={e => {
                    const type = discountTypes.find(t => t.discountTypeId === parseInt(e.target.value))
                    setSelectedType(type || null)
                  }}
                  className="tp-input w-full appearance-none rounded-2xl border-2 border-slate-200 bg-white px-4 py-3.5 text-sm text-slate-800 transition-all pr-10 focus:border-blue-500 focus:ring-1 focus:ring-blue-200"
                >
                  <option value="">Select a discount type</option>
                  {discountTypes.map(type => (
                    <option key={type.discountTypeId} value={type.discountTypeId}>
                      {type.name}
                    </option>
                  ))}
                </select>
                <ChevronDown size={16} className="absolute right-4 top-1/2 -translate-y-1/2 text-slate-400 pointer-events-none" />
              </div>
              {selectedType && (
                <div className="bg-blue-50 rounded-2xl p-3 mt-1">
                  <p className="font-semibold text-slate-800">{selectedType.name}</p>
                  <p className="text-xs text-slate-500 mt-0.5">{selectedType.description || 'No description available'}</p>
                  <p className="text-xs text-blue-600 font-semibold mt-1">{selectedType.discountPercentage}% discount rate</p>
                </div>
              )}
            </div>

            <div className="flex flex-col gap-2">
              <label className="text-sm font-semibold text-slate-700">Document / ID Number (Optional)</label>
              <div className="flex flex-col gap-2">
                <Input
                  placeholder="Enter your ID number or document reference"
                  value={document}
                  onChange={setDocument}
                  icon={<FileText size={16} />}
                />
                <div className="flex items-center gap-2">
                  <label className="flex items-center gap-2 px-4 py-2.5 bg-slate-50 border-2 border-dashed border-slate-300 rounded-2xl cursor-pointer hover:border-blue-400 hover:bg-blue-50 transition-all">
                    <Upload size={16} className="text-slate-400" />
                    <span className="text-xs text-slate-600 font-medium">
                      {documentFile ? documentFile.name : 'Upload Document (Optional)'}
                    </span>
                    <input
                      type="file"
                      accept="image/*,.pdf"
                      onChange={handleFileChange}
                      className="hidden"
                    />
                  </label>
                  {documentFile && (
                    <button
                      onClick={() => { setDocumentFile(null); setDocument('') }}
                      className="p-2 text-red-500 hover:bg-red-50 rounded-xl"
                    >
                      <X size={16} />
                    </button>
                  )}
                </div>
              </div>
              <p className="text-xs text-slate-500">Upload a photo or PDF of your ID for verification</p>
            </div>

            <Btn variant="primary" size="lg" onClick={handleSubmit} disabled={!selectedType || loading || cardId === null}>
              {loading ? <><RefreshCw size={16} className="animate-spin" /> Submitting...</> : 'Submit Application'}
            </Btn>
          </>
        )}
      </div>
    </div>
  )
}

// ── PROFILE ───────────────────────────────────────────────────────────────────

function ProfileScreen({ go, cardId }: { go: (s: Screen) => void; cardId: number | null }) {
  const [wallet, setWallet] = useState<Wallet | null>(null)
  const [loading, setLoading] = useState(true)
  const [user, setUser] = useState<UserType | null>(null)

  useEffect(() => {
    authService.getUser().then(setUser)
  }, [])

  useEffect(() => {
    if (cardId === null) {
      setLoading(false)
      return
    }
    const load = async () => {
      setLoading(true)
      try {
        const w = await walletService.getWallet(cardId)
        setWallet(w)
      } catch {
        // Wallet unavailable — show empty state
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [cardId])

  const displayName = user ? `${user.firstName} ${user.lastName}`.trim() : 'Passenger'
  const initials = user ? `${user.firstName[0] || ''}${user.lastName[0] || ''}`.toUpperCase() : 'P'
  const mobile = user ? user.mobileNumber : ''

  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF] overflow-y-auto mobile-scroll">
      <div className="bg-blue-gradient px-5 pt-10 pb-20 relative overflow-hidden">
        <div className="absolute top-[-40px] right-[-40px] w-40 h-40 rounded-full bg-white/10" />
        <h2 className="font-poppins text-xl font-bold text-white">Profile</h2>
      </div>
      {/* Avatar */}
      <div className="flex flex-col items-center -mt-14 relative z-10">
        <div className="w-24 h-24 rounded-full bg-blue-700 flex items-center justify-center border-4 border-white shadow-lg">
          <span className="font-poppins text-3xl font-bold text-white">{initials}</span>
        </div>
        <p className="font-poppins font-bold text-xl text-slate-800 mt-3">{displayName}</p>
        <p className="text-sm text-slate-500">{mobile ? `+63 ${mobile.slice(1)}` : 'No mobile number'}</p>
        <div className="mt-1"><StatusChip status="completed" /></div>
      </div>

      {/* Balance card */}
      <div className="mx-4 mt-4 bg-blue-gradient rounded-2xl p-4 flex justify-between items-center">
        <div>
          <p className="text-blue-100 text-xs">Wallet Balance</p>
          {loading ? (
            <RefreshCw size={18} className="text-white/70 animate-spin mt-1" />
          ) : wallet ? (
            <p className="font-poppins text-2xl font-bold text-white">₱{wallet.balance.toFixed(2)}</p>
          ) : (
            <p className="font-poppins text-2xl font-bold text-white">—</p>
          )}
        </div>
        <Btn variant="primary" size="sm" className="bg-white/20 !text-white" onClick={() => go('topup')} disabled={!wallet}>
          <Plus size={14} /> Top Up
        </Btn>
      </div>

      {/* Menu items */}
      <div className="mx-4 mt-4 bg-white rounded-2xl overflow-hidden shadow-sm">
        {[
          { icon: QrCode, label: 'My QR Code', sub: 'Show to driver', screen: 'qr' as Screen },
          { icon: FileText, label: 'Discounts', sub: 'Apply and view status', screen: 'discounts' as Screen },
          { icon: CreditCard, label: 'Linked Card', sub: wallet ? `Card #${wallet.cardId}` : 'No card linked' },
          { icon: Lock, label: 'Change Password', sub: 'Update your security' },
          { icon: Bell, label: 'Notifications', sub: 'Manage alerts' },
          { icon: HelpCircle, label: 'Help & Support', sub: 'FAQs and contact' },
          { icon: Shield, label: 'Privacy & Security', sub: 'Manage permissions' },
        ].map(({ icon: Icon, label, sub, screen }, i, arr) => (
          <button key={label} onClick={() => screen && go(screen)} className={`w-full flex items-center gap-3 px-4 py-4 hover:bg-slate-50 transition-colors ${i < arr.length - 1 ? 'border-b border-slate-100' : ''}`}>
            <div className="w-9 h-9 rounded-2xl bg-blue-50 flex items-center justify-center shrink-0">
              <Icon size={16} className="text-blue-600" />
            </div>
            <div className="flex-1 text-left">
              <p className="text-sm font-semibold text-slate-800">{label}</p>
              <p className="text-xs text-slate-400">{sub}</p>
            </div>
            {screen && <ChevronRight size={16} className="text-slate-300" />}
          </button>
        ))}
      </div>

      <div className="mx-4 mt-3 mb-4">
        <button onClick={async () => { await authService.logout(); go('welcome') }}
          className="w-full flex items-center justify-center gap-2 py-4 rounded-2xl bg-red-50 border border-red-100 text-red-600 font-semibold text-sm hover:bg-red-100 transition-colors">
          <LogOut size={16} /> Log Out
        </button>
      </div>
    </div>
  )
}

// ── BOTTOM NAV ────────────────────────────────────────────────────────────────

function BottomNav({ current, go }: { current: Screen; go: (s: Screen) => void }) {
  const tabs = [
    { screen: 'home' as Screen, icon: Home, label: 'Home' },
    { screen: 'wallet' as Screen, icon: WalletIcon, label: 'Wallet' },
    { screen: 'qr' as Screen, icon: QrCode, label: 'My QR' },
    { screen: 'profile' as Screen, icon: User, label: 'Profile' },
  ]
  return (
    <div className="bg-white border-t border-slate-100 shadow-[0_-4px_16px_rgba(0,0,0,0.06)]">
      <div className="flex">
        {tabs.map(({ screen, icon: Icon, label }) => {
          const active = current === screen || (screen === 'home' && ['splash', 'welcome', 'login', 'register'].includes(current))
          const isQr = screen === 'qr'
          return (
            <button key={screen} onClick={() => go(screen)} className={`bnav-item ${active ? 'active' : ''}`}>
              {isQr ? (
                <div className="w-12 h-12 rounded-2xl bg-blue-gradient flex items-center justify-center shadow-md -mt-4">
                  <Icon size={22} className="text-white" />
                </div>
              ) : (
                <Icon size={22} />
              )}
              <span className={`text-[10px] font-semibold ${isQr ? 'mt-1' : ''}`}>{label}</span>
            </button>
          )
        })}
      </div>
    </div>
  )
}

// ── PLAN TRIP ────────────────────────────────────────────────────────────────

function PlanTripScreen({ go, cardId, onNavigateToDetail }: { go: (s: Screen) => void; cardId: number | null; onNavigateToDetail?: (planId?: number) => void }) {
  const [terminals, setTerminals] = useState<{ terminalId: number; terminalName: string }[]>([])
  const [originId, setOriginId] = useState('')
  const [destinationId, setDestinationId] = useState('')
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [cancelling, setCancelling] = useState(false)
  const [error, setError] = useState('')
  const [activePlan, setActivePlan] = useState<TripPlan | null>(null)
  const [discountType, setDiscountType] = useState<DiscountType | null>(null)
  const [fareCalculation, setFareCalculation] = useState<{ normalFare: number; discountPercentage: number | null; discountAmount: number | null; finalFare: number } | null>(null)
  const [calculatingFare, setCalculatingFare] = useState(false)
  const [savedFare, setSavedFare] = useState<{ normalFare: number; discountPercentage: number | null; discountAmount: number | null; finalFare: number } | null>(null)
  const [wallet, setWallet] = useState<Wallet | null>(null)

  useEffect(() => {
    if (cardId === null) {
      setLoading(false)
      return
    }
    const load = async () => {
      setLoading(true)
      setError('')
      try {
        const [terminalsData, plan, discount, walletData] = await Promise.all([
          api.get<{ success: boolean; data: { terminalId: number; terminalName: string }[] }>('/api/terminal').catch(() => ({ success: false, data: [] })),
          tripPlanService.getActiveTripPlan().catch(() => null),
          discountService.getCurrentDiscountType(cardId).catch(() => null),
          walletService.getWallet(cardId).catch(() => null),
        ])
        setActivePlan(plan)
        setDiscountType(discount)
        setWallet(walletData)
        if (terminalsData.success && terminalsData.data) {
          setTerminals(terminalsData.data)
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load terminals')
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [cardId])

  const handleSubmit = async () => {
    if (!originId || !destinationId || cardId === null) return
    if (originId === destinationId) {
      setError('Origin and destination must be different.')
      return
    }
    setSubmitting(true)
    setError('')
    try {
      // Step 1: Calculate fare FIRST (before creating trip plan)
      setCalculatingFare(true)
      const fareCalc = await tripPlanService.calculateFare(Number(originId), Number(destinationId), cardId)
      setCalculatingFare(false)

      // Step 2: Check wallet balance
      if (!wallet || wallet.balance < fareCalc.finalFare) {
        const shortfall = fareCalc.finalFare - (wallet?.balance || 0)
        setError(`Insufficient balance. You need ₱${fareCalc.finalFare.toFixed(2)} but your wallet balance is ₱${(wallet?.balance || 0).toFixed(2)}. Please top up your wallet first.`)
        setSubmitting(false)
        return
      }

      // Step 3: Create trip plan only if balance is sufficient
      const createdPlan = await tripPlanService.createTripPlan(Number(originId), Number(destinationId))
      // Use the created plan's fare data returned by the backend (most accurate,
      // matches what was actually saved to the trip_plans table)
      setSavedFare({
        normalFare: createdPlan.normalFare,
        discountPercentage: createdPlan.discountPercentage,
        discountAmount: createdPlan.discountAmount,
        finalFare: createdPlan.finalFarePrice,
      })
      // Fetch the active plan again to get complete data with terminal names
      const activePlan = await tripPlanService.getActiveTripPlan()
      // If the API doesn't return terminal names, map them from terminals list
      if (activePlan && (!activePlan.originTerminalName || !activePlan.destinationTerminalName)) {
        const originTerminal = terminals.find(t => t.terminalId === activePlan.originTerminalId)
        const destinationTerminal = terminals.find(t => t.terminalId === activePlan.destinationTerminalId)
        if (originTerminal && destinationTerminal) {
          activePlan.originTerminalName = originTerminal.terminalName
          activePlan.destinationTerminalName = destinationTerminal.terminalName
        }
      }
      setActivePlan(activePlan)
      setOriginId('')
      setDestinationId('')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create trip plan')
    } finally {
      setSubmitting(false)
    }
  }

  const handleCancelPlan = async () => {
    if (!activePlan) return
    setCancelling(true)
    setError('')
    try {
      await tripPlanService.cancelTripPlan(activePlan.planId)
      // Clear state to show form again
      setActivePlan(null)
      setOriginId('')
      setDestinationId('')
      setSavedFare(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to cancel trip plan')
    } finally {
      setCancelling(false)
    }
  }

  const hasActivePlan = activePlan !== null

  // Fetch fare from backend when origin and destination change
  useEffect(() => {
    if (!originId || !destinationId || hasActivePlan || !cardId) {
      setFareCalculation(null)
      return
    }

    const fetchFare = async () => {
      setCalculatingFare(true)
      try {
        const fare = await tripPlanService.calculateFare(Number(originId), Number(destinationId), cardId)
        setFareCalculation(fare)
      } catch (err) {
        // Silently fail - fare calculation is optional
        setFareCalculation(null)
      } finally {
        setCalculatingFare(false)
      }
    }

    fetchFare()
  }, [originId, destinationId, cardId, hasActivePlan])

  // Restore fare when component loads with an active plan
  useEffect(() => {
    if (hasActivePlan && activePlan && cardId && !savedFare) {
      const fetchFare = async () => {
        setCalculatingFare(true)
        try {
          const fare = await tripPlanService.calculateFare(activePlan.originTerminalId, activePlan.destinationTerminalId, cardId)
          setSavedFare(fare)
        } catch (err) {
          console.error('Failed to calculate fare for active plan:', err)
        } finally {
          setCalculatingFare(false)
        }
      }
      fetchFare()
    }
  }, [hasActivePlan, activePlan, cardId, savedFare])

  const showFare = (!hasActivePlan && originId && destinationId && fareCalculation !== null) || (hasActivePlan && savedFare !== null)

  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF] overflow-y-auto mobile-scroll">
      <div className="bg-blue-gradient px-5 pt-10 pb-14">
        <button onClick={() => go('home')} className="text-white/80 mb-4 flex items-center gap-1"><ArrowLeft size={18} /> Back</button>
        <h2 className="font-poppins text-xl font-bold text-white">Plan Trip</h2>
        <p className="text-blue-100 text-sm mt-1">Select your origin and destination</p>
      </div>
      <div className="-mt-6 bg-white rounded-t-3xl flex-1 px-5 pt-6 pb-6 flex flex-col gap-4">
        {activePlan && (
          <button
            onClick={() => onNavigateToDetail?.(activePlan.planId)}
            className="w-full bg-blue-50 border border-blue-100 rounded-2xl p-3 text-left hover:bg-blue-100 transition-colors"
          >
            <p className="text-xs text-slate-500">Active Plan</p>
            <p className="text-sm font-semibold text-slate-800">{activePlan.originTerminalName} → {activePlan.destinationTerminalName}</p>
            <p className="text-xs text-slate-500">Click to view details</p>
          </button>
        )}
        {error && (
          <div className="bg-red-50 border border-red-200 rounded-2xl p-3 flex items-start gap-2">
            <AlertCircle size={15} className="text-red-600 shrink-0 mt-0.5" />
            <p className="text-xs text-red-600">{error}</p>
          </div>
        )}
        {loading ? (
          <div className="flex items-center justify-center py-8">
            <RefreshCw size={24} className="text-blue-400 animate-spin" />
          </div>
        ) : (
          <>
            {!hasActivePlan ? (
              <>
                <div className="flex flex-col gap-2">
                  <label className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Origin</label>
                  <div className="relative">
                    <select
                      value={originId}
                      onChange={e => setOriginId(e.target.value)}
                      className="tp-input w-full appearance-none rounded-2xl border border-slate-200 bg-white px-4 py-3.5 text-sm text-slate-800 transition-all pr-10"
                    >
                      <option value="">Select origin...</option>
                      {terminals.map(t => (
                        <option key={t.terminalId} value={t.terminalId}>{t.terminalName}</option>
                      ))}
                    </select>
                    <ChevronDown size={16} className="absolute right-4 top-1/2 -translate-y-1/2 text-slate-400 pointer-events-none" />
                  </div>
                </div>
                <div className="flex flex-col gap-2">
                  <label className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Destination</label>
                  <div className="relative">
                    <select
                      value={destinationId}
                      onChange={e => setDestinationId(e.target.value)}
                      className="tp-input w-full appearance-none rounded-2xl border border-slate-200 bg-white px-4 py-3.5 text-sm text-slate-800 transition-all pr-10"
                    >
                      <option value="">Select destination...</option>
                      {terminals.filter(t => t.terminalId !== Number(originId)).map(t => (
                        <option key={t.terminalId} value={t.terminalId}>{t.terminalName}</option>
                      ))}
                    </select>
                    <ChevronDown size={16} className="absolute right-4 top-1/2 -translate-y-1/2 text-slate-400 pointer-events-none" />
                  </div>
                </div>
                {calculatingFare ? (
                  <div className="bg-gradient-to-br from-blue-50 to-indigo-50 rounded-2xl p-4 border border-blue-100 flex items-center justify-center">
                    <RefreshCw size={20} className="text-blue-600 animate-spin mr-2" />
                    <span className="text-sm text-slate-600">Calculating fare...</span>
                  </div>
                ) : showFare && (hasActivePlan ? savedFare : fareCalculation) ? (
                  <div className="bg-gradient-to-br from-blue-50 to-indigo-50 rounded-2xl p-4 border border-blue-100">
                    <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2">Fare Estimate</p>
                    <div className="flex items-center justify-between mb-1">
                      <span className="text-sm text-slate-600">Normal Fare</span>
                      <span className="text-sm font-semibold text-slate-700">₱{(hasActivePlan ? savedFare!.normalFare : fareCalculation!.normalFare).toFixed(2)}</span>
                    </div>
                    {(hasActivePlan ? savedFare!.discountPercentage : fareCalculation!.discountPercentage) && (
                      <div className="flex items-center justify-between mb-1">
                        <span className="text-sm text-green-600">Discount ({hasActivePlan ? savedFare!.discountPercentage : fareCalculation!.discountPercentage}%)</span>
                        <span className="text-sm font-semibold text-green-600">-₱{(hasActivePlan ? savedFare!.discountAmount : fareCalculation!.discountAmount)?.toFixed(2)}</span>
                      </div>
                    )}
                    <div className="flex items-center justify-between pt-2 border-t border-blue-200 mt-2">
                      <span className="text-base font-bold text-slate-800">Final Fare</span>
                      <span className="text-lg font-bold text-blue-600">₱{(hasActivePlan ? savedFare!.finalFare : fareCalculation!.finalFare).toFixed(2)}</span>
                    </div>
                  </div>
                ) : null}
                <Btn variant="primary" size="lg" onClick={handleSubmit} disabled={!originId || !destinationId || submitting}>
                  {submitting ? <><RefreshCw size={16} className="animate-spin" /> Saving...</> : 'Save Trip Plan'}
                </Btn>
              </>
            ) : (
              <>
                <div className="bg-slate-50 rounded-2xl p-4">
                  <p className="text-xs text-slate-500 uppercase tracking-wider font-semibold">Origin</p>
                  <p className="font-poppins text-lg font-bold text-slate-800 mt-1">{activePlan.originTerminalName}</p>
                </div>
                <div className="bg-slate-50 rounded-2xl p-4">
                  <p className="text-xs text-slate-500 uppercase tracking-wider font-semibold">Destination</p>
                  <p className="font-poppins text-lg font-bold text-slate-800 mt-1">{activePlan.destinationTerminalName}</p>
                </div>
                {/* Show fare estimate even with active plan */}
                {savedFare && (
                  <div className="bg-gradient-to-br from-blue-50 to-indigo-50 rounded-2xl p-4 border border-blue-100">
                    <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2">Fare Estimate</p>
                    <div className="flex items-center justify-between mb-1">
                      <span className="text-sm text-slate-600">Normal Fare</span>
                      <span className="text-sm font-semibold text-slate-700">₱{savedFare.normalFare.toFixed(2)}</span>
                    </div>
                    {savedFare.discountPercentage && (
                      <div className="flex items-center justify-between mb-1">
                        <span className="text-sm text-green-600">Discount ({savedFare.discountPercentage}%)</span>
                        <span className="text-sm font-semibold text-green-600">-₱{savedFare.discountAmount?.toFixed(2)}</span>
                      </div>
                    )}
                    <div className="flex items-center justify-between pt-2 border-t border-blue-200 mt-2">
                      <span className="text-base font-bold text-slate-800">Final Fare</span>
                      <span className="text-lg font-bold text-blue-600">₱{savedFare.finalFare.toFixed(2)}</span>
                    </div>
                  </div>
                )}
                <Btn variant="danger" size="lg" onClick={handleCancelPlan} disabled={cancelling}>
                  {cancelling ? <><RefreshCw size={16} className="animate-spin" /> Cancelling...</> : 'Cancel Plan'}
                </Btn>
              </>
            )}
            <Btn variant="ghost" size="lg" onClick={() => go('trip-plan-history')}>View Trip Plan History</Btn>
          </>
        )}
      </div>
    </div>
  )
}

// ── TRIP PLAN HISTORY ────────────────────────────────────────────────────────

function TripPlanHistoryScreen({ go, cardId, onSelectPlan }: { go: (s: Screen) => void; cardId: number | null; onSelectPlan?: (planId: number) => void }) {
  const [plans, setPlans] = useState<TripPlan[]>([])
  const [allPlans, setAllPlans] = useState<TripPlan[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [filter, setFilter] = useState<'all' | 'used' | 'cancelled'>('all')

  useEffect(() => {
    if (cardId === null) {
      setLoading(false)
      return
    }
    const load = async () => {
      setLoading(true)
      setError('')
      try {
        const data = await tripPlanService.getTripPlanHistory()
        // Filter to show only Cancelled and Used plans
        const filteredPlans = data.filter(plan => 
          plan.status.toLowerCase() === 'cancelled' || plan.status.toLowerCase() === 'used'
        )
        setAllPlans(filteredPlans)
        setPlans(filteredPlans)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load trip plan history')
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [cardId])

  useEffect(() => {
    if (filter === 'all') {
      setPlans(allPlans)
    } else {
      setPlans(allPlans.filter(plan => plan.status.toLowerCase() === filter))
    }
  }, [filter, allPlans])

  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF] overflow-y-auto mobile-scroll">
      <div className="bg-blue-gradient px-5 pt-10 pb-16">
        <button onClick={() => go('plan-trip')} className="text-white/80 mb-4 flex items-center gap-1"><ArrowLeft size={18} /> Back</button>
        <h2 className="font-poppins text-xl font-bold text-white">Trip Plan History</h2>
        <p className="text-blue-100 text-sm mt-1">Your past trip plans</p>
      </div>
      <div className="-mt-6 bg-[#F0F4FF] rounded-t-3xl pt-4">
        {error && (
          <div className="mx-4 mb-3 bg-red-50 border border-red-200 rounded-2xl p-3 flex items-start gap-2">
            <AlertCircle size={15} className="text-red-600 shrink-0 mt-0.5" />
            <p className="text-xs text-red-600">{error}</p>
          </div>
        )}
        {/* Filter chips */}
        <div className="px-4 mb-3">
          <div className="flex gap-2 overflow-x-auto pb-1">
            {[
              { key: 'all', label: 'All' },
              { key: 'used', label: 'Paid' },
              { key: 'cancelled', label: 'Cancelled' },
            ].map(({ key, label }) => (
              <button
                key={key}
                onClick={() => setFilter(key as 'all' | 'used' | 'cancelled')}
                className={`px-4 py-1.5 rounded-full text-xs font-semibold whitespace-nowrap transition-all ${
                  filter === key
                    ? 'bg-blue-600 text-white shadow-sm'
                    : 'bg-white text-slate-600 border border-slate-200'
                }`}
              >
                {label}
              </button>
            ))}
          </div>
        </div>
        <div className="px-4 flex flex-col gap-2 pb-4">
          {loading ? (
            <div className="flex items-center justify-center py-8">
              <RefreshCw size={24} className="text-blue-400 animate-spin" />
            </div>
          ) : plans.length === 0 ? (
            <div className="bg-white rounded-2xl p-6 text-center">
              <Clock size={32} className="text-slate-300 mx-auto mb-2" />
              <p className="text-sm text-slate-400">No trip plan history yet</p>
            </div>
          ) : (
            plans.map(plan => (
              <button
                key={plan.planId}
                onClick={() => onSelectPlan ? onSelectPlan(plan.planId) : go('trip-plan-detail')}
                className="bg-white rounded-2xl p-4 shadow-sm text-left"
              >
                <div className="flex items-center justify-between mb-2">
                  <p className="font-semibold text-slate-800">{plan.originTerminalName} → {plan.destinationTerminalName}</p>
                  <StatusChip status={plan.status.toLowerCase()} />
                </div>
                <p className="text-xs text-slate-400">Planned on {new Date(plan.createdAt).toLocaleDateString()}</p>
                {plan.usedAt && <p className="text-xs text-slate-400">Used on {new Date(plan.usedAt).toLocaleDateString()}</p>}
              </button>
            ))
          )}
        </div>
      </div>
    </div>
  )
}

// ── TRIP PLAN DETAIL ─────────────────────────────────────────────────────────

function TripPlanDetailScreen({ go, cardId, planId }: { go: (s: Screen) => void; cardId: number | null; planId?: number }) {
  const [plan, setPlan] = useState<TripPlan | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [discountType, setDiscountType] = useState<DiscountType | null>(null)
  const [calculatedFare, setCalculatedFare] = useState<{ normalFare: number; discountPercentage: number | null; discountAmount: number | null; finalFare: number } | null>(null)
  const [calculating, setCalculating] = useState(false)

  useEffect(() => {
    if (cardId === null) {
      setLoading(false)
      return
    }
    const load = async () => {
      setLoading(true)
      setError('')
      try {
        let data: TripPlan | null = null
        
        // If planId is provided, fetch that specific plan (for history items)
        if (planId) {
          data = await tripPlanService.getTripPlanById(planId)
        } else {
          // Otherwise fetch the active plan
          data = await tripPlanService.getActiveTripPlan()
        }
        
        setPlan(data)
        
        // Fetch accurate fare from backend based on origin and destination
        if (data && cardId) {
          setCalculating(true)
          try {
            const fare = await tripPlanService.calculateFare(data.originTerminalId, data.destinationTerminalId, cardId)
            setCalculatedFare(fare)
          } catch (err) {
            // If fare calculation fails, we'll use stored values
            console.error('Failed to calculate fare:', err)
          } finally {
            setCalculating(false)
          }
        }
        
        const discount = await discountService.getCurrentDiscountType(cardId).catch(() => null)
        setDiscountType(discount)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load trip plan')
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [cardId, planId])

  const isActive = plan?.status.toLowerCase() === 'active'
  const isHistoryPlan = !isActive && plan !== null

  // Use calculated fare from backend (most accurate), fallback to stored values, then 0.00
  const displayNormalFare = calculatedFare?.normalFare ?? plan?.normalFare ?? 0
  const displayDiscountAmount = calculatedFare?.discountAmount ?? plan?.discountAmount ?? 0
  const displayFinalFare = calculatedFare?.finalFare ?? plan?.finalFarePrice ?? 0
  const displayDiscountPercentage = calculatedFare?.discountPercentage ?? plan?.discountPercentage ?? null

  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF] overflow-y-auto mobile-scroll">
      <div className="bg-blue-gradient px-5 pt-10 pb-16">
        <button onClick={() => go(isActive ? 'plan-trip' : 'trip-plan-history')} className="text-white/80 mb-4 flex items-center gap-1"><ArrowLeft size={18} /> Back</button>
        <h2 className="font-poppins text-xl font-bold text-white">Trip Plan</h2>
      </div>
      <div className="-mt-6 bg-white rounded-t-3xl flex-1 px-5 pt-6 pb-6 flex flex-col gap-4">
        {error && (
          <div className="bg-red-50 border border-red-200 rounded-2xl p-3 flex items-start gap-2">
            <AlertCircle size={15} className="text-red-600 shrink-0 mt-0.5" />
            <p className="text-xs text-red-600">{error}</p>
          </div>
        )}
        {loading ? (
          <div className="flex items-center justify-center py-8">
            <RefreshCw size={24} className="text-blue-400 animate-spin" />
          </div>
        ) : plan ? (
          <>
            <div className="bg-slate-50 rounded-2xl p-4">
              <p className="text-xs text-slate-500 uppercase tracking-wider font-semibold">Status</p>
              <p className="font-poppins text-lg font-bold text-slate-800 mt-1">{plan.status}</p>
            </div>
            <div className="bg-slate-50 rounded-2xl p-4">
              <p className="text-xs text-slate-500 uppercase tracking-wider font-semibold">Origin</p>
              <p className="font-poppins text-lg font-bold text-slate-800 mt-1">{plan.originTerminalName}</p>
            </div>
            <div className="bg-slate-50 rounded-2xl p-4">
              <p className="text-xs text-slate-500 uppercase tracking-wider font-semibold">Destination</p>
              <p className="font-poppins text-lg font-bold text-slate-800 mt-1">{plan.destinationTerminalName}</p>
            </div>
            {/* Final Fare Price - Prominently displayed below destination */}
            <div className="bg-gradient-to-br from-blue-50 to-indigo-50 rounded-2xl p-4 border border-blue-100">
              <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2">Final Fare</p>
              <p className="font-poppins text-2xl font-bold text-blue-600">₱{displayFinalFare.toFixed(2)}</p>
              {displayDiscountPercentage && (
                <p className="text-xs text-green-600 mt-1">Includes {displayDiscountPercentage}% discount</p>
              )}
            </div>
            <div className="bg-slate-50 rounded-2xl p-4">
              <p className="text-xs text-slate-500 uppercase tracking-wider font-semibold">Created</p>
              <p className="font-poppins text-lg font-bold text-slate-800 mt-1">{new Date(plan.createdAt).toLocaleString()}</p>
            </div>
            {plan.expiresAt && (
              <div className="bg-slate-50 rounded-2xl p-4">
                <p className="text-xs text-slate-500 uppercase tracking-wider font-semibold">Expires</p>
                <p className="font-poppins text-lg font-bold text-slate-800 mt-1">{new Date(plan.expiresAt).toLocaleString()}</p>
              </div>
            )}
            {plan.usedAt && (
              <div className="bg-slate-50 rounded-2xl p-4">
                <p className="text-xs text-slate-500 uppercase tracking-wider font-semibold">Used</p>
                <p className="font-poppins text-lg font-bold text-slate-800 mt-1">{new Date(plan.usedAt).toLocaleString()}</p>
              </div>
            )}
            {/* Fare Information */}
            <div className="bg-gradient-to-br from-blue-50 to-indigo-50 rounded-2xl p-4 border border-blue-100">
              <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2">Fare Information</p>
              <div className="flex items-center justify-between mb-1">
                <span className="text-sm text-slate-600">Normal Fare</span>
                <span className="text-sm font-semibold text-slate-700">₱{displayNormalFare.toFixed(2)}</span>
              </div>
              {displayDiscountPercentage && displayDiscountAmount !== null && displayDiscountAmount > 0 && (
                <div className="flex items-center justify-between mb-1">
                  <span className="text-sm text-green-600">Discount ({displayDiscountPercentage}%)</span>
                  <span className="text-sm font-semibold text-green-600">-₱{displayDiscountAmount.toFixed(2)}</span>
                </div>
              )}
              <div className="flex items-center justify-between pt-2 border-t border-blue-200 mt-2">
                <span className="text-base font-bold text-slate-800">Final Fare</span>
                <span className="text-lg font-bold text-blue-600">₱{displayFinalFare.toFixed(2)}</span>
              </div>
            </div>
            {isActive && (
              <div className="flex gap-2 mt-2">
                <Btn variant="ghost" size="lg" className="flex-1" onClick={() => go('plan-trip')}>
                  Return
                </Btn>
              </div>
            )}
          </>
        ) : (
          <div className="bg-white rounded-2xl p-6 text-center">
            <p className="text-sm text-slate-400">No active trip plan</p>
            <Btn variant="primary" size="lg" className="mt-4" onClick={() => go('plan-trip')}>Plan a Trip</Btn>
          </div>
        )}
      </div>
    </div>
  )
}

// ── TRANSACTION DETAIL ──────────────────────────────────────────────────────

function TransactionDetailScreen({ go, tx }: { go: (s: Screen) => void; tx: Transaction }) {
  const isPayment = tx.transactionType.toLowerCase() === 'payment' || tx.transactionType.toLowerCase() === 'fare'
  const isTopUp = tx.transactionType.toLowerCase() === 'top_up' || tx.transactionType.toLowerCase() === 'topup'
  
  const balanceAfter = tx.remainingBalance
  const amountLabel = isPayment ? (tx.finalFare ?? tx.amount) : tx.amount
  const amountSign = isTopUp ? '+' : ''

  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF] overflow-y-auto mobile-scroll">
      <div className="bg-blue-gradient px-5 pt-10 pb-16 relative overflow-hidden">
        <div className="absolute top-[-40px] right-[-40px] w-40 h-40 rounded-full bg-white/10" />
        <button onClick={() => go('wallet')} className="text-white/80 mb-4 flex items-center gap-1">
          <ArrowLeft size={18} /> Back
        </button>
        <h2 className="font-poppins text-xl font-bold text-white">Transaction Details</h2>
        <p className="text-blue-100 text-sm mt-1">{isTopUp ? 'Wallet top-up receipt' : 'Fare payment receipt'}</p>
      </div>

      <div className="-mt-6 bg-white rounded-t-3xl flex-1 px-5 pt-6 pb-6 flex flex-col gap-4">
        {/* Type icon + amount */}
        <div className="flex flex-col items-center gap-2 py-2">
          <TxIcon type={tx.transactionType} />
          <p className={`font-poppins text-3xl font-bold ${isTopUp ? 'text-green-600' : 'text-slate-800'}`}>
            {amountSign}₱{Math.abs(amountLabel ?? tx.amount).toFixed(2)}
          </p>
          <p className="text-xs text-slate-400">{tx.transactionName}</p>
        </div>

        {/* Receipt No */}
        <div className="bg-slate-50 rounded-2xl p-4">
          <p className="text-xs text-slate-500 uppercase tracking-wider font-semibold">Receipt No.</p>
          <p className="font-poppins text-sm font-bold text-blue-600 mt-1 font-mono">{tx.transactionReferenceNumber || 'TRN-N/A'}</p>
        </div>

        {isPayment ? (
          <>
            {/* Driver */}
            <div className="bg-slate-50 rounded-2xl p-4">
              <p className="text-xs text-slate-500 uppercase tracking-wider font-semibold">Driver</p>
              <p className="font-poppins text-sm font-bold text-slate-800 mt-1">{tx.driverName || '—'}</p>
            </div>

            {/* Route */}
            <div className="bg-slate-50 rounded-2xl p-4">
              <p className="text-xs text-slate-500 uppercase tracking-wider font-semibold">Route</p>
              <p className="font-poppins text-sm font-bold text-slate-800 mt-1">
                {tx.originTerminalName || 'Origin'} → {tx.destinationTerminalName || 'Destination'}
              </p>
            </div>

            {/* Fare */}
            <div className="bg-gradient-to-br from-blue-50 to-indigo-50 rounded-2xl p-4 border border-blue-100">
              <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2">Fare</p>
              <p className="font-poppins text-lg font-bold text-blue-600">₱{(tx.finalFare ?? tx.amount).toFixed(2)}</p>
            </div>
          </>
        ) : (
          <>
            {/* Card Name */}
            <div className="bg-slate-50 rounded-2xl p-4">
              <p className="text-xs text-slate-500 uppercase tracking-wider font-semibold">Card Name</p>
              <p className="font-poppins text-sm font-bold text-slate-800 mt-1 font-mono">{tx.maskedCardNumber || '****-****-****-0000'}</p>
            </div>

            {/* Payment Mode */}
            <div className="bg-slate-50 rounded-2xl p-4">
              <p className="text-xs text-slate-500 uppercase tracking-wider font-semibold">Payment Mode</p>
              <p className="font-poppins text-sm font-bold text-slate-800 mt-1">{tx.paymentMode || 'Admin'}</p>
            </div>
          </>
        )}

        {/* Balance / New Balance */}
        <div className="bg-slate-50 rounded-2xl p-4">
          <p className="text-xs text-slate-500 uppercase tracking-wider font-semibold">{isPayment ? 'Balance' : 'New Balance'}</p>
          <p className="font-poppins text-sm font-bold text-slate-800 mt-1">₱{(balanceAfter ?? 0).toFixed(2)}</p>
        </div>

        {/* Date */}
        <div className="bg-slate-50 rounded-2xl p-4">
          <p className="text-xs text-slate-500 uppercase tracking-wider font-semibold">Date</p>
          <p className="font-poppins text-sm font-bold text-slate-800 mt-1">{new Date(tx.createdAt).toLocaleString()}</p>
        </div>

        <Btn variant="secondary" size="lg" onClick={() => go('wallet')}>
          <ArrowLeft size={16} /> Back to Wallet
        </Btn>
      </div>
    </div>
  )
}

// ── MAIN ──────────────────────────────────────────────────────────────────────

const showNav: Screen[] = ['home', 'wallet', 'qr', 'profile', 'discounts', 'trip-plan-history']

export default function PassengerApp() {
  const [screen, setScreen] = useState<Screen>('splash')
  const [cardId, setCardId] = useState<number | null>(null)
  const [cardResolved, setCardResolved] = useState(false)
  const [selectedPlanId, setSelectedPlanId] = useState<number | undefined>()
  const [selectedTransaction, setSelectedTransaction] = useState<Transaction | null>(null)
  const go = (s: Screen) => setScreen(s)

  const navigateToTripPlanDetail = (planId?: number) => {
    setSelectedPlanId(planId)
    go('trip-plan-detail')
  }

  const navigateToTransactionDetail = (tx: Transaction) => {
    setSelectedTransaction(tx)
    go('transaction-detail')
  }

  // Resolve the authenticated user's card ID once after login/register.
  useEffect(() => {
    const resolveCard = async () => {
      const user = await authService.getUser()
      if (!user) {
        setCardResolved(true)
        return
      }
      let cancelled = false
      const id = await resolveCardId(user.userId)
      if (!cancelled) {
        setCardId(id)
        setCardResolved(true)
      }
      return () => { cancelled = true }
    }
    resolveCard()
  }, [screen === 'home' || screen === 'wallet' || screen === 'qr' || screen === 'profile'])

  const showCardEmptyState = cardResolved && cardId === null && ['wallet', 'qr', 'topup'].includes(screen)

  return (
    <div className="flex flex-col h-full overflow-hidden">
      <div className="flex-1 flex flex-col overflow-hidden">
        {screen === 'splash' && <SplashScreen next={() => go('welcome')} />}
        {screen === 'welcome' && <WelcomeScreen go={go} />}
        {screen === 'login' && <LoginScreen go={go} />}
        {screen === 'register' && <RegisterScreen go={go} />}
        {screen === 'forgot' && <ForgotScreen go={go} />}
        {screen === 'otp' && <OTPScreen go={go} />}
        {screen === 'home' && <HomeScreen go={go} cardId={cardId} onNavigateToDetail={navigateToTripPlanDetail} />}
        {screen === 'wallet' && (showCardEmptyState ? <TransitCardNotFound go={go} /> : <WalletScreen go={go} cardId={cardId} onSelectTx={navigateToTransactionDetail} />)}
        {screen === 'topup' && (showCardEmptyState ? <TransitCardNotFound go={go} /> : <TopUpScreen go={go} cardId={cardId} />)}
        {screen === 'qr' && (showCardEmptyState ? <TransitCardNotFound go={go} /> : <QRScreen go={go} cardId={cardId} />)}
        {screen === 'profile' && <ProfileScreen go={go} cardId={cardId} />}
        {screen === 'discounts' && <DiscountsScreen go={go} cardId={cardId} />}
        {screen === 'apply-discount' && <ApplyDiscountScreen go={go} cardId={cardId} />}
        {screen === 'plan-trip' && <PlanTripScreen go={go} cardId={cardId} onNavigateToDetail={navigateToTripPlanDetail} />}
        {screen === 'trip-plan-history' && <TripPlanHistoryScreen go={go} cardId={cardId} onSelectPlan={(planId) => navigateToTripPlanDetail(planId)} />}
        {screen === 'trip-plan-detail' && <TripPlanDetailScreen go={go} cardId={cardId} planId={selectedPlanId} />}
        {screen === 'transaction-detail' && selectedTransaction && <TransactionDetailScreen go={go} tx={selectedTransaction} />}
      </div>
      {showNav.includes(screen) && <BottomNav current={screen} go={go} />}
    </div>
  )
}