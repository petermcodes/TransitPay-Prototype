import { useState, useEffect } from 'react'
import {
  ArrowLeft, Eye, EyeOff, RefreshCw, CheckCircle,
  Bus, QrCode, Clock, TrendingUp, User, AlertCircle,
  Play, Square, ChevronRight, Shield, Phone, Lock,
  Wifi, Battery, Signal
} from 'lucide-react'

type DScreen = 'login' | 'home' | 'scanner' | 'scan-result' | 'pay-success' | 'trip-history'

function Btn({ children, variant = 'primary', className = '', onClick, disabled, size = 'md' }: {
  children: React.ReactNode; variant?: 'primary' | 'secondary' | 'ghost' | 'danger'
  className?: string; onClick?: () => void; disabled?: boolean; size?: 'sm' | 'md' | 'lg'
}) {
  const base = 'inline-flex items-center justify-center gap-2 font-semibold rounded-2xl transition-all active:scale-[0.97] cursor-pointer select-none font-poppins'
  const sizes = { sm: 'px-4 py-2 text-sm', md: 'px-5 py-3 text-sm', lg: 'px-6 py-4 text-base w-full' }
  const variants = {
    primary: 'bg-blue-gradient text-white shadow-md hover:shadow-lg hover:brightness-105 disabled:opacity-50',
    secondary: 'bg-white text-[#1976D2] border-2 border-[#1976D2] hover:bg-blue-50 disabled:opacity-50',
    ghost: 'text-[#1976D2] hover:bg-blue-50 disabled:opacity-50',
    danger: 'bg-red-500 text-white hover:bg-red-600 disabled:opacity-50',
  }
  return (
    <button disabled={disabled} onClick={onClick}
      className={`${base} ${sizes[size]} ${variants[variant]} ${className}`}>
      {children}
    </button>
  )
}

function StatusChip({ status }: { status: string }) {
  const map: Record<string, string> = {
    completed: 'bg-green-50 text-green-700',
    pending: 'bg-yellow-50 text-yellow-700',
    failed: 'bg-red-50 text-red-700',
  }
  return <span className={`chip ${map[status] || map.completed}`}>{status}</span>
}

// ── LOGIN ─────────────────────────────────────────────────────────────────────

function DriverLogin({ go }: { go: (s: DScreen) => void }) {
  const [id, setId] = useState('')
  const [pass, setPass] = useState('')
  const [show, setShow] = useState(false)
  const [loading, setLoading] = useState(false)

  return (
    <div className="flex-1 flex flex-col min-h-full bg-[#F0F4FF]">
      <div className="bg-blue-gradient px-6 pt-12 pb-20 relative overflow-hidden flex flex-col items-center">
        <div className="absolute top-[-60px] right-[-60px] w-52 h-52 rounded-full bg-white/10" />
        <div className="absolute bottom-[-40px] left-[-40px] w-40 h-40 rounded-full bg-white/10" />
        <div className="w-20 h-20 rounded-3xl bg-white/20 backdrop-blur flex items-center justify-center shadow-xl mb-4">
          <Bus size={38} className="text-white" />
        </div>
        <h1 className="font-poppins text-2xl font-bold text-white">Driver Portal</h1>
        <p className="text-blue-100 text-sm mt-1">TransitPay Fleet Management</p>
      </div>
      <div className="-mt-6 bg-white rounded-t-3xl flex-1 px-6 pt-8 pb-6 flex flex-col gap-5">
        <div className="flex flex-col gap-1.5">
          <label className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Driver ID</label>
          <div className="relative">
            <span className="absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400"><User size={16} /></span>
            <input value={id} onChange={e => setId(e.target.value)} placeholder="DRV-XXXX"
              className="tp-input w-full rounded-2xl border border-slate-200 bg-white px-4 pl-10 py-3.5 text-sm text-slate-800 placeholder:text-slate-400 transition-all" />
          </div>
        </div>
        <div className="flex flex-col gap-1.5">
          <label className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Password</label>
          <div className="relative">
            <span className="absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400"><Lock size={16} /></span>
            <input type={show ? 'text' : 'password'} value={pass} onChange={e => setPass(e.target.value)} placeholder="Enter password"
              className="tp-input w-full rounded-2xl border border-slate-200 bg-white px-4 pl-10 pr-12 py-3.5 text-sm text-slate-800 placeholder:text-slate-400 transition-all" />
            <button onClick={() => setShow(!show)} className="absolute right-3.5 top-1/2 -translate-y-1/2 text-slate-400">
              {show ? <EyeOff size={16} /> : <Eye size={16} />}
            </button>
          </div>
        </div>
        <Btn variant="primary" size="lg" onClick={() => { setLoading(true); setTimeout(() => { setLoading(false); go('home') }, 1200) }} disabled={loading}>
          {loading ? <><RefreshCw size={16} className="animate-spin" /> Signing in...</> : 'Sign In'}
        </Btn>
        <div className="bg-blue-50 rounded-2xl p-3.5 flex items-start gap-2.5">
          <Shield size={15} className="text-blue-600 shrink-0 mt-0.5" />
          <p className="text-xs text-slate-600 leading-relaxed">
            Your account must be approved by an administrator before you can log in. Contact your fleet manager for assistance.
          </p>
        </div>
      </div>
    </div>
  )
}

// ── HOME ──────────────────────────────────────────────────────────────────────

function DriverHome({ go }: { go: (s: DScreen) => void }) {
  const [tripActive, setTripActive] = useState(false)

  const trips = [
    { id: 'T-001', pax: 'Juan D.', fare: 23, time: '10:14', status: 'completed' },
    { id: 'T-002', pax: 'Maria S.', fare: 18, time: '09:52', status: 'completed' },
    { id: 'T-003', pax: 'Pedro R.', fare: 28, time: '09:30', status: 'completed' },
  ]

  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF] overflow-y-auto mobile-scroll">
      {/* Header */}
      <div className="bg-blue-gradient px-5 pt-10 pb-20 relative overflow-hidden">
        <div className="absolute top-[-60px] right-[-60px] w-52 h-52 rounded-full bg-white/10" />
        <div className="flex items-center justify-between">
          <div>
            <p className="text-blue-100 text-sm">Welcome back,</p>
            <h2 className="font-poppins text-2xl font-bold text-white mt-0.5">Pedro Santos</h2>
            <div className="flex items-center gap-2 mt-1">
              <div className={`w-2 h-2 rounded-full ${tripActive ? 'bg-green-400 animate-pulse' : 'bg-slate-400'}`} />
              <span className="text-blue-100 text-xs">{tripActive ? 'On Duty' : 'Off Duty'}</span>
            </div>
          </div>
          <div className="w-14 h-14 rounded-2xl bg-white/20 flex items-center justify-center">
            <Bus size={28} className="text-white" />
          </div>
        </div>
      </div>

      {/* Stats */}
      <div className="mx-4 -mt-14 grid grid-cols-2 gap-3 relative z-10">
        <div className="bg-white rounded-2xl p-4 shadow-sm">
          <div className="flex items-center gap-1.5 mb-1">
            <TrendingUp size={14} className="text-green-500" />
            <p className="text-xs text-slate-500 font-medium">Today's Earnings</p>
          </div>
          <p className="font-poppins text-2xl font-bold text-slate-800">₱846</p>
          <p className="text-xs text-green-600 font-medium mt-0.5">↑ 12% vs yesterday</p>
        </div>
        <div className="bg-white rounded-2xl p-4 shadow-sm">
          <div className="flex items-center gap-1.5 mb-1">
            <Bus size={14} className="text-blue-500" />
            <p className="text-xs text-slate-500 font-medium">Trips Today</p>
          </div>
          <p className="font-poppins text-2xl font-bold text-slate-800">34</p>
          <p className="text-xs text-blue-600 font-medium mt-0.5">+6 this hour</p>
        </div>
      </div>

      {/* Vehicle info */}
      <div className="mx-4 mt-3 bg-white rounded-2xl p-4 shadow-sm">
        <p className="text-xs text-slate-500 font-semibold uppercase tracking-wider mb-3">Current Vehicle</p>
        <div className="flex items-center gap-3">
          <div className="w-12 h-12 rounded-2xl bg-blue-50 flex items-center justify-center shrink-0">
            <Bus size={24} className="text-blue-600" />
          </div>
          <div className="flex-1">
            <p className="font-poppins font-bold text-slate-800">Bus Unit #42</p>
            <p className="text-sm text-slate-500">Route: Cubao → Ortigas Loop</p>
          </div>
          <div className="text-right">
            <p className="font-mono font-bold text-slate-800">ABC-1234</p>
            <span className="chip bg-green-50 text-green-700">Active</span>
          </div>
        </div>
      </div>

      {/* Action buttons */}
      <div className="mx-4 mt-3 grid grid-cols-2 gap-3">
        <button onClick={() => setTripActive(!tripActive)}
          className={`flex flex-col items-center justify-center gap-2 py-5 rounded-2xl transition-all font-poppins font-semibold text-sm shadow-sm ${tripActive ? 'bg-red-50 text-red-600 border border-red-100' : 'bg-green-50 text-green-700 border border-green-100'}`}>
          {tripActive ? <><Square size={24} /> End Trip</> : <><Play size={24} /> Start Trip</>}
        </button>
        <button onClick={() => go('scanner')}
          className="flex flex-col items-center justify-center gap-2 py-5 rounded-2xl bg-blue-gradient text-white font-poppins font-semibold text-sm shadow-md">
          <QrCode size={24} />
          Scan QR
        </button>
      </div>

      {/* Recent */}
      <div className="mx-4 mt-4 mb-4">
        <div className="flex items-center justify-between mb-3">
          <p className="font-poppins font-semibold text-sm text-slate-800">Recent Passengers</p>
          <button onClick={() => go('trip-history')} className="text-xs text-blue-600 font-medium flex items-center gap-0.5">
            See all <ChevronRight size={12} />
          </button>
        </div>
        <div className="flex flex-col gap-2">
          {trips.map(t => (
            <div key={t.id} className="bg-white rounded-2xl p-3.5 flex items-center gap-3 shadow-sm">
              <div className="w-10 h-10 rounded-2xl bg-blue-50 flex items-center justify-center shrink-0">
                <User size={18} className="text-blue-600" />
              </div>
              <div className="flex-1">
                <p className="text-sm font-semibold text-slate-800">{t.pax}</p>
                <p className="text-xs text-slate-400 font-mono">{t.time} AM</p>
              </div>
              <div className="text-right">
                <p className="text-sm font-bold text-slate-800">₱{t.fare}.00</p>
                <StatusChip status={t.status} />
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

// ── QR SCANNER ────────────────────────────────────────────────────────────────

function QRScanner({ go }: { go: (s: DScreen) => void }) {
  const [scanning, setScanning] = useState(true)
  const [countdown, setCountdown] = useState(3)

  useEffect(() => {
    if (!scanning) return
    const t = setTimeout(() => {
      setScanning(false)
      go('scan-result')
    }, 3000)
    const interval = setInterval(() => setCountdown(c => Math.max(0, c - 1)), 1000)
    return () => { clearTimeout(t); clearInterval(interval) }
  }, [scanning, go])

  return (
    <div className="flex-1 flex flex-col bg-black">
      {/* Fake camera view */}
      <div className="flex-1 relative flex items-center justify-center"
        style={{ background: 'linear-gradient(135deg, #0f172a 0%, #1e293b 100%)' }}>
        {/* Camera grid */}
        <div className="absolute inset-0 opacity-10"
          style={{ backgroundImage: 'linear-gradient(rgba(255,255,255,0.1) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,0.1) 1px, transparent 1px)', backgroundSize: '30px 30px' }} />

        <button onClick={() => go('home')} className="absolute top-12 left-4 text-white/80 flex items-center gap-1 z-20">
          <ArrowLeft size={18} /> Back
        </button>

        {/* Top label */}
        <div className="absolute top-20 left-0 right-0 flex justify-center">
          <div className="bg-black/60 backdrop-blur px-4 py-2 rounded-full">
            <p className="text-white text-sm font-semibold font-poppins">
              {scanning ? 'Position QR code in frame' : 'QR Detected!'}
            </p>
          </div>
        </div>

        {/* Scan frame */}
        <div className="relative w-64 h-64">
          {/* Corners */}
          {[['top-0 left-0', 'rounded-tl-2xl'], ['top-0 right-0', 'rounded-tr-2xl'], ['bottom-0 left-0', 'rounded-bl-2xl'], ['bottom-0 right-0', 'rounded-br-2xl']].map(([pos, r]) => (
            <div key={pos} className={`absolute ${pos} w-8 h-8 border-4 border-blue-400 ${r}`} />
          ))}

          {/* Scan line */}
          {scanning && (
            <div className="scan-line absolute left-2 right-2 h-0.5 bg-blue-400 shadow-[0_0_8px_rgba(96,165,250,0.8)]" />
          )}

          {/* Center content */}
          <div className="absolute inset-4 flex items-center justify-center">
            {scanning ? (
              <div className="opacity-20">
                {/* Simulated QR dots pattern */}
                <div className="grid grid-cols-8 gap-1">
                  {Array.from({ length: 64 }).map((_, i) => (
                    <div key={i} className={`w-4 h-4 rounded-sm ${Math.random() > 0.5 ? 'bg-white' : ''}`} />
                  ))}
                </div>
              </div>
            ) : (
              <CheckCircle size={64} className="text-green-400" />
            )}
          </div>
        </div>

        {/* Countdown */}
        {scanning && (
          <div className="absolute bottom-24 left-0 right-0 flex justify-center">
            <div className="bg-blue-600/80 backdrop-blur px-6 py-2 rounded-full flex items-center gap-2">
              <RefreshCw size={14} className="text-white animate-spin" />
              <p className="text-white text-sm font-mono">Auto-scanning... {countdown}s</p>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

// ── SCAN RESULT ───────────────────────────────────────────────────────────────

function ScanResult({ go }: { go: (s: DScreen) => void }) {
  const [loading, setLoading] = useState(false)

  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF]">
      <div className="bg-blue-gradient px-5 pt-10 pb-14">
        <button onClick={() => go('scanner')} className="text-white/80 mb-4 flex items-center gap-1"><ArrowLeft size={18} /> Rescan</button>
        <h2 className="font-poppins text-xl font-bold text-white">Passenger Details</h2>
        <p className="text-blue-100 text-sm mt-1">Review and confirm fare payment</p>
      </div>
      <div className="-mt-6 bg-white rounded-t-3xl flex-1 px-5 pt-6 pb-6 flex flex-col gap-4">
        {/* Passenger card */}
        <div className="bg-blue-50 rounded-2xl p-4 flex items-center gap-3 border border-blue-100">
          <div className="w-14 h-14 rounded-2xl bg-blue-600 flex items-center justify-center shrink-0">
            <span className="font-poppins text-xl font-bold text-white">JD</span>
          </div>
          <div className="flex-1">
            <p className="font-poppins font-bold text-slate-800">Juan Dela Cruz</p>
            <p className="text-xs text-slate-500 font-mono">ID: USR-4821</p>
            <p className="text-xs text-slate-500">+63 917 123 4567</p>
          </div>
          <div>
            <span className="chip bg-green-50 text-green-700">Verified</span>
          </div>
        </div>

        {/* Wallet status */}
        <div className="bg-slate-50 rounded-2xl p-4 flex flex-col gap-3">
          <div className="flex justify-between items-center">
            <span className="text-sm text-slate-500">Wallet Status</span>
            <span className="chip bg-green-50 text-green-700">Active</span>
          </div>
          <div className="flex justify-between items-center">
            <span className="text-sm text-slate-500">Current Balance</span>
            <span className="font-poppins font-bold text-slate-800">₱476.50</span>
          </div>
        </div>

        {/* Fare */}
        <div className="bg-blue-600 rounded-2xl p-5 text-center">
          <p className="text-blue-200 text-xs uppercase tracking-wider font-semibold mb-1">Fare Amount</p>
          <p className="font-poppins text-4xl font-bold text-white">₱23.00</p>
          <p className="text-blue-200 text-xs mt-1">Cubao → Ortigas</p>
        </div>

        {/* After payment */}
        <div className="flex justify-between items-center bg-slate-50 rounded-2xl px-4 py-3">
          <span className="text-sm text-slate-500">Remaining Balance</span>
          <span className="font-poppins font-bold text-green-600">₱453.50</span>
        </div>

        <Btn variant="primary" size="lg"
          onClick={() => { setLoading(true); setTimeout(() => { setLoading(false); go('pay-success') }, 1200) }}
          disabled={loading}>
          {loading ? <><RefreshCw size={16} className="animate-spin" /> Processing...</> : 'Confirm Payment ₱23.00'}
        </Btn>
        <Btn variant="ghost" size="lg" onClick={() => go('scanner')}>Cancel</Btn>
      </div>
    </div>
  )
}

// ── PAYMENT SUCCESS ───────────────────────────────────────────────────────────

function DriverPaySuccess({ go }: { go: (s: DScreen) => void }) {
  return (
    <div className="flex-1 flex flex-col items-center justify-center bg-[#F0F4FF] px-6 gap-5 fade-in">
      <div className="relative">
        <div className="w-24 h-24 rounded-full bg-green-100 flex items-center justify-center shadow-lg">
          <CheckCircle size={48} className="text-green-500" />
        </div>
        <div className="absolute inset-0 rounded-full bg-green-100 pulse-ring" />
      </div>
      <div className="text-center">
        <h2 className="font-poppins text-2xl font-bold text-slate-800">Fare Collected!</h2>
        <p className="text-slate-500 text-sm mt-1">Payment processed successfully</p>
      </div>
      <div className="bg-white rounded-3xl p-5 w-full shadow-sm flex flex-col gap-3">
        {[['Passenger', 'Juan Dela Cruz'], ['Fare Collected', '₱23.00'], ['Route', 'Cubao → Ortigas'], ['Remaining Balance', '₱453.50'], ['Reference', 'TPDR-20260802-0034']].map(([k, v]) => (
          <div key={k} className="flex justify-between items-center">
            <span className="text-sm text-slate-500">{k}</span>
            <span className={`text-sm font-semibold ${k === 'Fare Collected' ? 'text-green-600 font-bold text-base' : 'text-slate-800'}`}>{v}</span>
          </div>
        ))}
      </div>
      <Btn variant="primary" size="lg" onClick={() => go('scanner')}>Scan Next Passenger</Btn>
      <Btn variant="ghost" size="lg" onClick={() => go('home')}>Back to Dashboard</Btn>
    </div>
  )
}

// ── TRIP HISTORY ──────────────────────────────────────────────────────────────

function TripHistory({ go }: { go: (s: DScreen) => void }) {
  const trips = [
    { id: 'T-034', pax: 'Juan Dela Cruz', fare: 23, route: 'Cubao → Ortigas', time: '10:14 AM', status: 'completed' },
    { id: 'T-033', pax: 'Maria Santos', fare: 18, route: 'Marikina → Cubao', time: '09:52 AM', status: 'completed' },
    { id: 'T-032', pax: 'Pedro Reyes', fare: 28, route: 'Lawton → Shaw', time: '09:30 AM', status: 'completed' },
    { id: 'T-031', pax: 'Anna Cruz', fare: 13, route: 'Pioneer → Boni', time: '09:15 AM', status: 'completed' },
    { id: 'T-030', pax: 'Carlo Tan', fare: 33, route: 'Sucat → Ortigas', time: '09:00 AM', status: 'completed' },
    { id: 'T-029', pax: 'Liza Garcia', fare: 23, route: 'Quiapo → Cubao', time: '08:42 AM', status: 'completed' },
  ]

  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF] overflow-y-auto mobile-scroll">
      <div className="bg-blue-gradient px-5 pt-10 pb-16">
        <button onClick={() => go('home')} className="text-white/80 mb-4 flex items-center gap-1"><ArrowLeft size={18} /> Back</button>
        <h2 className="font-poppins text-xl font-bold text-white">Trip History</h2>
        <p className="text-blue-100 text-sm mt-0.5">Today: 34 trips · ₱846.00</p>
      </div>

      <div className="-mt-6 bg-[#F0F4FF] rounded-t-3xl pt-4">
        {/* Summary */}
        <div className="mx-4 grid grid-cols-3 gap-2 mb-4">
          {[['Total Trips', '34'], ['Total Fare', '₱846'], ['Avg Fare', '₱24.9']].map(([k, v]) => (
            <div key={k} className="bg-white rounded-2xl p-3 text-center shadow-sm">
              <p className="font-poppins font-bold text-slate-800 text-lg">{v}</p>
              <p className="text-[10px] text-slate-400 mt-0.5">{k}</p>
            </div>
          ))}
        </div>

        <div className="px-4 flex flex-col gap-2 pb-4">
          {trips.map(t => (
            <div key={t.id} className="bg-white rounded-2xl p-3.5 flex items-center gap-3 shadow-sm">
              <div className="w-10 h-10 rounded-2xl bg-blue-50 flex items-center justify-center shrink-0">
                <User size={18} className="text-blue-600" />
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-semibold text-slate-800">{t.pax}</p>
                <p className="text-xs text-slate-400">{t.route}</p>
                <p className="text-[10px] text-slate-400 font-mono">{t.time}</p>
              </div>
              <div className="text-right shrink-0">
                <p className="text-sm font-bold text-slate-800">₱{t.fare}.00</p>
                <StatusChip status={t.status} />
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

// ── BOTTOM NAV ────────────────────────────────────────────────────────────────

function DriverNav({ current, go }: { current: DScreen; go: (s: DScreen) => void }) {
  return (
    <div className="bg-white border-t border-slate-100 shadow-[0_-4px_16px_rgba(0,0,0,0.06)]">
      <div className="flex">
        {[
          { screen: 'home' as DScreen, icon: Bus, label: 'Dashboard' },
          { screen: 'scanner' as DScreen, icon: QrCode, label: 'Scan QR' },
          { screen: 'trip-history' as DScreen, icon: Clock, label: 'History' },
        ].map(({ screen, icon: Icon, label }) => {
          const active = current === screen
          const isCenter = screen === 'scanner'
          return (
            <button key={screen} onClick={() => go(screen)} className={`bnav-item ${active ? 'active' : ''}`}>
              {isCenter ? (
                <div className="w-12 h-12 rounded-2xl bg-blue-gradient flex items-center justify-center shadow-md -mt-4">
                  <Icon size={22} className="text-white" />
                </div>
              ) : <Icon size={22} />}
              <span className={`text-[10px] font-semibold ${isCenter ? 'mt-1' : ''}`}>{label}</span>
            </button>
          )
        })}
      </div>
    </div>
  )
}

// ── MAIN ──────────────────────────────────────────────────────────────────────

const showNav: DScreen[] = ['home', 'trip-history', 'pay-success']

export default function DriverApp() {
  const [screen, setScreen] = useState<DScreen>('login')
  const go = (s: DScreen) => setScreen(s)

  return (
    <div className="flex flex-col h-full overflow-hidden">
      <div className="flex-1 flex flex-col overflow-hidden">
        {screen === 'login' && <DriverLogin go={go} />}
        {screen === 'home' && <DriverHome go={go} />}
        {screen === 'scanner' && <QRScanner go={go} />}
        {screen === 'scan-result' && <ScanResult go={go} />}
        {screen === 'pay-success' && <DriverPaySuccess go={go} />}
        {screen === 'trip-history' && <TripHistory go={go} />}
      </div>
      {showNav.includes(screen) && <DriverNav current={screen} go={go} />}
    </div>
  )
}
