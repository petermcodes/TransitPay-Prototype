import { useState, useEffect } from 'react'
import { QRCodeSVG } from 'qrcode.react'
import {
  ArrowLeft, Eye, EyeOff, Home, Wallet, QrCode, User,
  ChevronRight, ArrowUpRight, ArrowDownLeft, RotateCcw,
  Bus, CreditCard, Phone, Lock, Plus, LogOut, Copy,
  CheckCircle, AlertCircle, Clock, TrendingUp, Bell,
  Shield, HelpCircle, ChevronDown, RefreshCw, Zap,
  FileText, Upload, X, Check
} from 'lucide-react'
import { authService } from './lib/auth'
import { paymentService, type PaymentSessionData } from './lib/payment'
import { discountService, type DiscountType, type DiscountApplication } from './lib/discount'

type Screen =
  | 'splash' | 'welcome' | 'login' | 'register' | 'forgot' | 'otp'
  | 'home' | 'wallet' | 'topup' | 'qr' | 'profile'
  | 'discounts' | 'apply-discount' | 'discount-status'

const TOWNS = ['Quezon City', 'Marikina', 'Pasig', 'Mandaluyong', 'Manila', 'Parañaque', 'Las Piñas']
const STATIONS: Record<string, string[]> = {
  'Quezon City': ['Cubao Station', 'Commonwealth Station', 'Fairview Terminal', 'Novaliches Station'],
  'Marikina': ['Marikina Station', 'Concepcion Station', 'Sto. Niño Station'],
  'Pasig': ['Ortigas Station', 'Kapitolyo Station', 'Shaw Station'],
  'Mandaluyong': ['Boni Station', 'Santolan Station', 'Pioneer Station'],
  'Manila': ['Divisoria Station', 'Lawton Station', 'Quiapo Station', 'Tondo Station'],
  'Parañaque': ['BF Homes Station', 'Sucat Station', 'Airport Station'],
  'Las Piñas': ['Alabang Station', 'Zapote Station', 'Pamplona Station'],
}

const txHistory = [
  { id: 'TX-001', type: 'fare', desc: 'Cubao → Ortigas', date: 'Aug 2, 2026', amount: -23, status: 'completed' },
  { id: 'TX-002', type: 'topup', desc: 'GCash Top Up', date: 'Aug 1, 2026', amount: 500, status: 'completed' },
  { id: 'TX-003', type: 'fare', desc: 'Marikina → Cubao', date: 'Jul 31, 2026', amount: -18, status: 'completed' },
  { id: 'TX-004', type: 'refund', desc: 'Trip Refund', date: 'Jul 30, 2026', amount: 23, status: 'completed' },
  { id: 'TX-005', type: 'fare', desc: 'Lawton → Shaw', date: 'Jul 29, 2026', amount: -28, status: 'completed' },
  { id: 'TX-006', type: 'topup', desc: 'Maya Top Up', date: 'Jul 28, 2026', amount: 300, status: 'completed' },
]

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

function StatusChip({ status }: { status: string }) {
  const map: Record<string, string> = {
    completed: 'bg-green-50 text-green-700',
    pending: 'bg-yellow-50 text-yellow-700',
    failed: 'bg-red-50 text-red-700',
    refunded: 'bg-blue-50 text-blue-700',
    approved: 'bg-green-50 text-green-700',
    rejected: 'bg-red-50 text-red-700',
    expired: 'bg-slate-50 text-slate-700',
  }
  return <span className={`chip ${map[status.toLowerCase()] || map.completed}`}>{status}</span>
}

function TxIcon({ type }: { type: string }) {
  if (type === 'fare') return <div className="w-10 h-10 rounded-2xl bg-blue-50 flex items-center justify-center shrink-0"><Bus size={18} className="text-blue-600" /></div>
  if (type === 'topup') return <div className="w-10 h-10 rounded-2xl bg-green-50 flex items-center justify-center shrink-0"><ArrowDownLeft size={18} className="text-green-600" /></div>
  return <div className="w-10 h-10 rounded-2xl bg-orange-50 flex items-center justify-center shrink-0"><RotateCcw size={18} className="text-orange-500" /></div>
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
  const [mobile, setMobile] = useState('')
  const [pass, setPass] = useState('')
  const [show, setShow] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const submit = async () => {
    setLoading(true)
    setError('')
    try {
      await authService.login({ mobileNumber: mobile, password: pass })
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
      await authService.register({
        firstName: form.first,
        lastName: form.last,
        mobileNumber: form.mobile,
        password: form.pass,
        roleName: 'Passenger'
      })
      // OTP disabled for testing - go directly to home
      go('home')
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

// ── HOME ──────────────────────────────────────────────────────────────────────

function HomeScreen({ go }: { go: (s: Screen) => void }) {
  const balance = 476.50
  const [showBal, setShowBal] = useState(true)

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
        <h2 className="font-poppins text-2xl font-bold text-white mt-0.5">Juan Dela Cruz 👋</h2>
      </div>

      {/* Balance card */}
      <div className="mx-4 -mt-14 bg-white rounded-3xl shadow-lg p-5 relative z-10">
        <div className="flex items-start justify-between mb-1">
          <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Wallet Balance</p>
          <button onClick={() => setShowBal(!showBal)} className="text-slate-400">
            {showBal ? <Eye size={16} /> : <EyeOff size={16} />}
          </button>
        </div>
        <p className="font-poppins text-3xl font-bold text-slate-800 mt-1">
          {showBal ? `₱${balance.toFixed(2)}` : '₱ ••••••'}
        </p>
        <p className="text-xs text-slate-400 mt-1 font-mono">Card: •••• •••• 4821</p>
        <div className="flex gap-2 mt-4">
          <Btn variant="primary" size="sm" className="flex-1 !rounded-xl" onClick={() => go('topup')}>
            <Plus size={14} /> Top Up
          </Btn>
          <Btn variant="ghost" size="sm" className="!rounded-xl px-3 bg-blue-50" onClick={() => go('qr')}>
            <QrCode size={14} />
          </Btn>
        </div>
      </div>

      {/* Quick Actions */}
      <div className="px-4 mt-5">
        <p className="font-poppins font-semibold text-slate-800 text-sm mb-3">Quick Actions</p>
        <div className="grid grid-cols-4 gap-2">
          {[
            { icon: QrCode, label: 'My QR', screen: 'qr' as Screen, color: 'bg-blue-50 text-blue-600' },
            { icon: Wallet, label: 'Wallet', screen: 'wallet' as Screen, color: 'bg-green-50 text-green-600' },
            { icon: FileText, label: 'Discounts', screen: 'discounts' as Screen, color: 'bg-purple-50 text-purple-600' },
            { icon: Clock, label: 'History', screen: 'wallet' as Screen, color: 'bg-orange-50 text-orange-600' },
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

      {/* Recent transactions */}
      <div className="px-4 mt-5 mb-4">
        <div className="flex items-center justify-between mb-3">
          <p className="font-poppins font-semibold text-slate-800 text-sm">Recent Transactions</p>
          <button onClick={() => go('wallet')} className="text-xs text-blue-600 font-medium flex items-center gap-0.5">
            See all <ChevronRight size={12} />
          </button>
        </div>
        <div className="flex flex-col gap-2">
          {txHistory.slice(0, 4).map(tx => (
            <div key={tx.id} className="bg-white rounded-2xl p-3.5 flex items-center gap-3 shadow-sm">
              <TxIcon type={tx.type} />
              <div className="flex-1 min-w-0">
                <p className="text-sm font-semibold text-slate-800 truncate">{tx.desc}</p>
                <div className="flex items-center gap-2 mt-0.5">
                  <p className="text-xs text-slate-400 font-mono">{tx.date}</p>
                  <StatusChip status={tx.status} />
                </div>
              </div>
              <p className={`text-sm font-bold shrink-0 ${tx.amount > 0 ? 'text-green-600' : 'text-slate-800'}`}>
                {tx.amount > 0 ? '+' : ''}₱{Math.abs(tx.amount).toFixed(2)}
              </p>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

// ── WALLET ────────────────────────────────────────────────────────────────────

function WalletScreen({ go }: { go: (s: Screen) => void }) {
  const [filter, setFilter] = useState('all')
  const filtered = filter === 'all' ? txHistory : txHistory.filter(t => t.type === filter)

  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF] overflow-y-auto mobile-scroll">
      <div className="bg-blue-gradient px-5 pt-10 pb-20 relative overflow-hidden">
        <div className="absolute top-[-40px] right-[-40px] w-40 h-40 rounded-full bg-white/10" />
        <h2 className="font-poppins text-xl font-bold text-white">My Wallet</h2>
      </div>

      {/* Balance */}
      <div className="mx-4 -mt-12 bg-card-gradient rounded-3xl shadow-lg p-5 relative z-10">
        <p className="text-blue-200 text-xs uppercase tracking-wider font-semibold">Available Balance</p>
        <p className="font-poppins text-4xl font-bold text-white mt-1">₱476.50</p>
        <div className="flex gap-3 mt-4">
          <Btn variant="primary" size="sm" className="flex-1 bg-white/20 hover:bg-white/30 !text-white border-white/30" onClick={() => go('topup')}>
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
          <p className="font-poppins text-xl font-bold text-slate-800">₱800.00</p>
          <p className="text-xs text-slate-400">This month</p>
        </div>
        <div className="bg-white rounded-2xl p-4 shadow-sm">
          <div className="flex items-center gap-2 mb-1">
            <ArrowUpRight size={14} className="text-blue-500" />
            <span className="text-xs text-slate-500 font-medium">Total Spent</span>
          </div>
          <p className="font-poppins text-xl font-bold text-slate-800">₱346.50</p>
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
        {filtered.map(tx => (
          <div key={tx.id} className="bg-white rounded-2xl p-3.5 flex items-center gap-3 shadow-sm">
            <TxIcon type={tx.type} />
            <div className="flex-1 min-w-0">
              <p className="text-sm font-semibold text-slate-800 truncate">{tx.desc}</p>
              <div className="flex items-center gap-2 mt-0.5">
                <p className="text-xs text-slate-400 font-mono">{tx.date}</p>
                <StatusChip status={tx.status} />
              </div>
            </div>
            <p className={`text-sm font-bold shrink-0 ${tx.amount > 0 ? 'text-green-600' : 'text-slate-800'}`}>
              {tx.amount > 0 ? '+' : ''}₱{Math.abs(tx.amount).toFixed(2)}
            </p>
          </div>
        ))}
      </div>
    </div>
  )
}

// ── TOP UP ────────────────────────────────────────────────────────────────────

function TopUpScreen({ go }: { go: (s: Screen) => void }) {
  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF] overflow-y-auto mobile-scroll">
      <div className="bg-blue-gradient px-5 pt-10 pb-14">
        <button onClick={() => go('wallet')} className="text-white/80 mb-4 flex items-center gap-1"><ArrowLeft size={18} /> Back</button>
        <h2 className="font-poppins text-xl font-bold text-white">Top Up Wallet</h2>
        <p className="text-blue-100 text-sm mt-1">Current Balance: <span className="font-bold text-white">₱476.50</span></p>
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

function QRScreen({ go }: { go: (s: Screen) => void }) {
  const [qrData, setQrData] = useState('')
  const [qrSignature, setQrSignature] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [copied, setCopied] = useState(false)

  useEffect(() => {
    const fetchQR = async () => {
      setLoading(true)
      setError('')
      try {
        // Use card ID 1 for testing (would come from auth context in production)
        const ticket = await paymentService.getQR(1)
        setQrData(ticket.data)
        setQrSignature(ticket.signature)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to get QR')
      } finally {
        setLoading(false)
      }
    }
    fetchQR()
  }, [])

  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF] overflow-y-auto mobile-scroll">
      <div className="bg-blue-gradient px-5 pt-10 pb-14">
        <button onClick={() => go('home')} className="text-white/80 mb-4 flex items-center gap-1"><ArrowLeft size={18} /> Back</button>
        <h2 className="font-poppins text-xl font-bold text-white">My QR Code</h2>
        <p className="text-blue-100 text-sm mt-1">Show this to the driver for payment</p>
      </div>
      <div className="-mt-6 bg-white rounded-t-3xl flex-1 px-5 pt-6 pb-6 flex flex-col items-center gap-5">
        {/* QR card */}
        <div className="bg-white rounded-3xl shadow-xl p-6 w-full flex flex-col items-center gap-4 border border-slate-100">
          <div className="flex items-center gap-2">
            <div className="w-6 h-6 rounded-lg bg-blue-600 flex items-center justify-center">
              <Bus size={12} className="text-white" />
            </div>
            <span className="font-poppins font-bold text-blue-700 text-sm">TransitPay</span>
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
            ) : (
              <QRCodeSVG value={qrData} size={200} level="M" bgColor="#ffffff" fgColor="#1565C0" />
            )}
          </div>
          <div className="text-center">
            <p className="font-poppins font-bold text-slate-800">Juan Dela Cruz</p>
            <p className="text-xs text-slate-400 font-mono mt-0.5">ID: USR-4821</p>
          </div>
          <div className="w-full bg-blue-50 rounded-2xl px-4 py-3 flex items-center justify-between">
            <div>
              <p className="text-xs text-slate-500">Wallet Balance</p>
              <p className="font-poppins font-bold text-blue-700">₱476.50</p>
            </div>
            <StatusChip status="completed" />
          </div>
        </div>

        <div className="bg-yellow-50 border border-yellow-200 rounded-2xl p-3.5 w-full flex items-start gap-2">
          <AlertCircle size={15} className="text-yellow-600 shrink-0 mt-0.5" />
          <p className="text-xs text-slate-600 leading-relaxed">
            This is your permanent TransitPay QR code. It uniquely identifies your card — it does not change per trip. Do not share it with others.
          </p>
        </div>

        <div className="flex gap-2 w-full">
          <Btn variant="secondary" size="md" className="flex-1" onClick={() => { setCopied(true); setTimeout(() => setCopied(false), 1500) }}>
            {copied ? <><CheckCircle size={14} /> Copied!</> : <><Copy size={14} /> Copy ID</>}
          </Btn>
        </div>
      </div>
    </div>
  )
}

// ── DISCOUNTS ─────────────────────────────────────────────────────────────────

function DiscountsScreen({ go }: { go: (s: Screen) => void }) {
  const [applications, setApplications] = useState<DiscountApplication[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    loadApplications()
  }, [])

  const loadApplications = async () => {
    try {
      const apps = await discountService.getMyApplications(1) // Card ID 1 for testing
      setApplications(apps)
    } catch (error) {
      console.error('Failed to load applications:', error)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF] overflow-y-auto mobile-scroll">
      <div className="bg-blue-gradient px-5 pt-10 pb-16 relative overflow-hidden">
        <div className="absolute top-[-40px] right-[-40px] w-40 h-40 rounded-full bg-white/10" />
        <h2 className="font-poppins text-xl font-bold text-white">My Discounts</h2>
        <p className="text-blue-100 text-sm mt-1">View and apply for discounts</p>
      </div>

      <div className="-mt-6 bg-[#F0F4FF] rounded-t-3xl pt-4">
        <div className="px-4 mb-4">
          <Btn variant="primary" size="lg" onClick={() => go('apply-discount')}>
            <Plus size={18} /> Apply for New Discount
          </Btn>
        </div>

        <div className="px-4 flex flex-col gap-2 pb-4">
          <p className="font-poppins font-semibold text-sm text-slate-700">My Applications</p>
          {loading ? (
            <div className="flex items-center justify-center py-8">
              <RefreshCw size={24} className="text-blue-400 animate-spin" />
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

function ApplyDiscountScreen({ go }: { go: (s: Screen) => void }) {
  const [discountTypes, setDiscountTypes] = useState<DiscountType[]>([])
  const [selectedType, setSelectedType] = useState<DiscountType | null>(null)
  const [document, setDocument] = useState('')
  const [loading, setLoading] = useState(false)
  const [submitted, setSubmitted] = useState(false)

  useEffect(() => {
    loadDiscountTypes()
  }, [])

  const loadDiscountTypes = async () => {
    try {
      const types = await discountService.getDiscountTypes()
      setDiscountTypes(types.filter(t => t.isActive))
    } catch (error) {
      console.error('Failed to load discount types:', error)
    }
  }

  const handleSubmit = async () => {
    if (!selectedType) return
    setLoading(true)
    try {
      await discountService.applyForDiscount(1, selectedType.discountTypeId, document)
      setSubmitted(true)
    } catch (error) {
      alert(error instanceof Error ? error.message : 'Failed to apply for discount')
    } finally {
      setLoading(false)
    }
  }

  if (submitted) {
    return (
      <div className="flex-1 flex flex-col items-center justify-center bg-[#F0F4FF] px-6 gap-4">
        <div className="w-20 h-20 rounded-full bg-green-100 flex items-center justify-center">
          <CheckCircle size={40} className="text-green-600" />
        </div>
        <div className="text-center">
          <h2 className="font-poppins text-xl font-bold text-slate-800">Application Submitted!</h2>
          <p className="text-sm text-slate-500 mt-1">Your discount application is pending approval</p>
        </div>
        <Btn variant="primary" size="lg" onClick={() => go('discount-status')}>
          View My Applications
        </Btn>
        <Btn variant="ghost" size="lg" onClick={() => go('home')}>
          Back to Home
        </Btn>
      </div>
    )
  }

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
        <div className="flex flex-col gap-2">
          <p className="text-sm font-semibold text-slate-700">Discount Type</p>
          {discountTypes.map(type => (
            <button
              key={type.discountTypeId}
              onClick={() => setSelectedType(type)}
              className={`p-4 rounded-2xl border-2 text-left transition-all ${selectedType?.discountTypeId === type.discountTypeId ? 'border-blue-500 bg-blue-50' : 'border-slate-200 bg-white'}`}
            >
              <div className="flex items-center justify-between">
                <div>
                  <p className="font-semibold text-slate-800">{type.name}</p>
                  <p className="text-xs text-slate-500 mt-0.5">{type.description}</p>
                  <p className="text-xs text-blue-600 font-semibold mt-1">{type.discountPercentage}% discount</p>
                </div>
                {selectedType?.discountTypeId === type.discountTypeId && (
                  <CheckCircle size={20} className="text-blue-600" />
                )}
              </div>
            </button>
          ))}
        </div>

        <div className="flex flex-col gap-2">
          <label className="text-sm font-semibold text-slate-700">Document / ID Number (Optional)</label>
          <Input
            placeholder="Enter your ID number or document reference"
            value={document}
            onChange={setDocument}
            icon={<FileText size={16} />}
          />
          <p className="text-xs text-slate-500">Provide your discount ID or document number for verification</p>
        </div>

        <Btn variant="primary" size="lg" onClick={handleSubmit} disabled={!selectedType || loading}>
          {loading ? <><RefreshCw size={16} className="animate-spin" /> Submitting...</> : 'Submit Application'}
        </Btn>
      </div>
    </div>
  )
}

// ── PROFILE ───────────────────────────────────────────────────────────────────

function ProfileScreen({ go }: { go: (s: Screen) => void }) {
  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF] overflow-y-auto mobile-scroll">
      <div className="bg-blue-gradient px-5 pt-10 pb-20 relative overflow-hidden">
        <div className="absolute top-[-40px] right-[-40px] w-40 h-40 rounded-full bg-white/10" />
        <h2 className="font-poppins text-xl font-bold text-white">Profile</h2>
      </div>
      {/* Avatar */}
      <div className="flex flex-col items-center -mt-14 relative z-10">
        <div className="w-24 h-24 rounded-full bg-blue-700 flex items-center justify-center border-4 border-white shadow-lg">
          <span className="font-poppins text-3xl font-bold text-white">JD</span>
        </div>
        <p className="font-poppins font-bold text-xl text-slate-800 mt-3">Juan Dela Cruz</p>
        <p className="text-sm text-slate-500">+63 917 123 4567</p>
        <div className="mt-1"><StatusChip status="completed" /></div>
      </div>

      {/* Balance card */}
      <div className="mx-4 mt-4 bg-blue-gradient rounded-2xl p-4 flex justify-between items-center">
        <div>
          <p className="text-blue-100 text-xs">Wallet Balance</p>
          <p className="font-poppins text-2xl font-bold text-white">₱476.50</p>
        </div>
        <Btn variant="primary" size="sm" className="bg-white/20 !text-white" onClick={() => go('topup')}>
          <Plus size={14} /> Top Up
        </Btn>
      </div>

      {/* Menu items */}
      <div className="mx-4 mt-4 bg-white rounded-2xl overflow-hidden shadow-sm">
        {[
          { icon: QrCode, label: 'My QR Code', sub: 'Show to driver', screen: 'qr' as Screen },
          { icon: FileText, label: 'Discounts', sub: 'Apply and view status', screen: 'discounts' as Screen },
          { icon: CreditCard, label: 'Linked Card', sub: '•••• 4821', },
          { icon: Lock, label: 'Change Password', sub: 'Update your security', },
          { icon: Bell, label: 'Notifications', sub: 'Manage alerts', },
          { icon: HelpCircle, label: 'Help & Support', sub: 'FAQs and contact', },
          { icon: Shield, label: 'Privacy & Security', sub: 'Manage permissions', },
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
        <button onClick={() => go('welcome')}
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
    { screen: 'wallet' as Screen, icon: Wallet, label: 'Wallet' },
    { screen: 'qr' as Screen, icon: QrCode, label: 'QR Pay' },
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

// ── MAIN ──────────────────────────────────────────────────────────────────────

const showNav: Screen[] = ['home', 'wallet', 'qr', 'profile', 'discounts']

export default function PassengerApp() {
  const [screen, setScreen] = useState<Screen>('splash')
  const go = (s: Screen) => setScreen(s)

  return (
    <div className="flex flex-col h-full overflow-hidden">
      <div className="flex-1 flex flex-col overflow-hidden">
        {screen === 'splash' && <SplashScreen next={() => go('welcome')} />}
        {screen === 'welcome' && <WelcomeScreen go={go} />}
        {screen === 'login' && <LoginScreen go={go} />}
        {screen === 'register' && <RegisterScreen go={go} />}
        {screen === 'forgot' && <ForgotScreen go={go} />}
        {screen === 'otp' && <OTPScreen go={go} />}
        {screen === 'home' && <HomeScreen go={go} />}
        {screen === 'wallet' && <WalletScreen go={go} />}
        {screen === 'topup' && <TopUpScreen go={go} />}
        {screen === 'qr' && <QRScreen go={go} />}
        {screen === 'profile' && <ProfileScreen go={go} />}
        {screen === 'discounts' && <DiscountsScreen go={go} />}
        {screen === 'apply-discount' && <ApplyDiscountScreen go={go} />}
      </div>
      {showNav.includes(screen) && <BottomNav current={screen} go={go} />}
    </div>
  )
}