import { useState, useEffect, useRef } from 'react'
import { Html5Qrcode } from 'html5-qrcode'
import {
  ArrowLeft, Eye, EyeOff, RefreshCw, CheckCircle,
  Bus, QrCode, Clock, TrendingUp, User, AlertCircle,
  Play, Square, ChevronRight, Shield, Phone, Lock
} from 'lucide-react'
import { authService } from './lib/auth'
import { cardService, type ScanReceipt, type DriverTransaction } from './lib/cards'
import { tripService, type Terminal, type Trip } from './lib/tripService'

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
    active: 'bg-green-50 text-green-700',
    cancelled: 'bg-red-50 text-red-700',
  }
  return <span className={`chip ${map[status.toLowerCase()] || map.completed}`}>{status}</span>
}

function formatTripTime(iso?: string): string {
  if (!iso) return '-'
  return new Date(iso).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })
}

// ── LOGIN ─────────────────────────────────────────────────────────────────────

function DriverLogin({ go, onTripStatusChecked }: { go: (s: DScreen) => void; onTripStatusChecked?: (hasActiveTrip: boolean) => void }) {
  const [id, setId] = useState('')
  const [pass, setPass] = useState('')
  const [show, setShow] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  // Load last logged-in driver ID on mount
  useEffect(() => {
    const lastDriverId = localStorage.getItem('lastDriverId')
    if (lastDriverId) {
      setId(lastDriverId)
    }
  }, [])

  const submit = async () => {
    setLoading(true)
    setError('')
    try {
      await authService.login({ username: id, password: pass })
      // Save driver ID for next login
      localStorage.setItem('lastDriverId', id)
      // Check for active trip after login to restore workflow
      const activeTrip = await tripService.getActiveTrip()
      const hasActiveTrip = activeTrip && activeTrip.tripStatus === 'Active'
      // Notify parent about trip status
      onTripStatusChecked?.(hasActiveTrip ?? false)
      // Always go to home - no auto-redirect to scanner
      go('home')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed')
    } finally {
      setLoading(false)
    }
  }

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
            <input value={id} onChange={e => setId(e.target.value)} placeholder="DRV-000010"
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
        {error && (
          <div className="bg-red-50 border border-red-200 rounded-2xl p-3 flex items-start gap-2">
            <AlertCircle size={15} className="text-red-600 shrink-0 mt-0.5" />
            <p className="text-xs text-red-600">{error}</p>
          </div>
        )}
        <Btn variant="primary" size="lg" onClick={submit} disabled={loading}>
          {loading ? <><RefreshCw size={16} className="animate-spin" /> Signing in...</> : 'Sign In'}
        </Btn>
        <div className="bg-blue-50 rounded-2xl p-3.5 flex items-start gap-2.5">
          <Shield size={15} className="text-blue-600 shrink-0 mt-0.5" />
          <p className="text-xs text-slate-600 leading-relaxed">
            Your account is ready to use. You can log in immediately with your credentials.
          </p>
        </div>
      </div>
    </div>
  )
}

// ── HOME ──────────────────────────────────────────────────────────────────────

function DriverHome({ go, activeTrip, onTripChanged, tripActive, setTripActive, selectedOrigin, setSelectedOrigin }: {
  go: (s: DScreen) => void; activeTrip: Trip | null; onTripChanged: (trip: Trip | null) => void
  tripActive: boolean; setTripActive: (active: boolean) => void
  selectedOrigin: Terminal | null; setSelectedOrigin: (s: Terminal | null) => void
}) {
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [stats, setStats] = useState({ earnings: 0, trips: 0 })
  const [terminals, setTerminals] = useState<Terminal[]>([])
  const [starting, setStarting] = useState(false)
  const [ending, setEnding] = useState(false)
  const [recentPassengers, setRecentPassengers] = useState<DriverTransaction[]>([])
  const [loadingPassengers, setLoadingPassengers] = useState(false)
  const user = authService.getUser()

  useEffect(() => {
    loadTerminals()
    loadStats()
    loadRecentPassengers()
  }, [])

  const loadTerminals = async () => {
    try {
      const data = await tripService.getTerminals()
      setTerminals(data)
    } catch (err) {
      console.error('Failed to load terminals:', err)
    }
  }

  const loadStats = async () => {
    setLoading(true)
    setError('')
    try {
      // Derive today's stats from trip history
      const history = await tripService.getTripHistory(1, 100)
      const today = new Date()
      const todayStart = new Date(today.getFullYear(), today.getMonth(), today.getDate())
      const todayTrips = history.data.filter(t => {
        const started = t.startedAt ? new Date(t.startedAt) : null
        return started && started >= todayStart && t.tripStatus === 'Completed'
      })
      const earnings = todayTrips.reduce((sum, t) => sum + t.totalRevenue, 0)
      setStats({ earnings, trips: todayTrips.length })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load trip data')
    } finally {
      setLoading(false)
    }
  }

  const loadRecentPassengers = async () => {
    setLoadingPassengers(true)
    try {
      const result = await cardService.getDriverTransactions(1, 10)
      if (result.success) {
        setRecentPassengers(result.data)
      }
    } catch (err) {
      console.error('Failed to load recent passengers:', err)
    } finally {
      setLoadingPassengers(false)
    }
  }

  const handleStartTrip = async () => {
    // Pre-flight check: verify no active trip exists before starting
    try {
      const existingTrip = await tripService.getActiveTrip()
      if (existingTrip && existingTrip.tripStatus === 'Active') {
        setError('You already have an active trip. Please end it first.')
        onTripChanged(existingTrip)
        return
      }
      
      setStarting(true)
      setError('')
      try {
        // Start the trip immediately without origin/destination
        const response = await tripService.startTrip()
        if (response.success && response.data) {
          onTripChanged(response.data)
          setTripActive(true)
          // Stay on home screen - button will change to "End Trip"
        } else {
          setError(response.message || 'Failed to start trip')
        }
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Failed to start trip'
        // If the error indicates an active trip already exists, fetch and resume it
        if (errorMessage.includes('already have an active trip')) {
          try {
            const activeTrip = await tripService.getActiveTrip()
            if (activeTrip && activeTrip.tripStatus === 'Active') {
              onTripChanged(activeTrip)
              setTripActive(true)
              return
            }
          } catch (resumeErr) {
            console.error('Failed to resume active trip:', resumeErr)
          }
        }
        setError(errorMessage)
      } finally {
        setStarting(false)
      }
    } catch (err) {
      setError('Failed to verify trip status. Please try again.')
    }
  }

  const handleEndTrip = async () => {
    if (!activeTrip) return
    setEnding(true)
    setError('')
    try {
      const response = await tripService.endTrip(activeTrip.tripId)
      if (response.success) {
        onTripChanged(null)
        setTripActive(false)
        setSelectedOrigin(null)
        loadStats()
      } else {
        setError(response.message || 'Failed to end trip')
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to end trip')
    } finally {
      setEnding(false)
    }
  }

  const handleScanQR = () => {
    if (!tripActive) {
      alert('Please start a trip first')
      return
    }
    go('scanner')
  }

  if (loading) {
    return (
      <div className="flex-1 flex items-center justify-center bg-[#F0F4FF]">
        <RefreshCw size={32} className="text-blue-400 animate-spin" />
      </div>
    )
  }

  const displayName = user ? `${user.firstName} ${user.lastName}`.trim() : 'Driver'

  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF] overflow-y-auto mobile-scroll">
      {/* Header */}
      <div className="bg-blue-gradient px-5 pt-10 pb-20 relative overflow-hidden">
        <div className="absolute top-[-60px] right-[-60px] w-52 h-52 rounded-full bg-white/10" />
        <div className="flex items-center justify-between">
          <div>
            <p className="text-blue-100 text-sm">Welcome back,</p>
            <h2 className="font-poppins text-2xl font-bold text-white mt-0.5">{displayName}</h2>
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

      {error && (
        <div className="mx-4 mt-3 bg-red-50 border border-red-200 rounded-2xl p-3 flex items-start gap-2">
          <AlertCircle size={15} className="text-red-600 shrink-0 mt-0.5" />
          <p className="text-xs text-red-600">{error}</p>
        </div>
      )}

      {/* Stats */}
      <div className="mx-4 -mt-14 grid grid-cols-2 gap-3 relative z-10">
        <div className="bg-white rounded-2xl p-4 shadow-sm">
          <div className="flex items-center gap-1.5 mb-1">
            <TrendingUp size={14} className="text-green-500" />
            <p className="text-xs text-slate-500 font-medium">Today's Earnings</p>
          </div>
          <p className="font-poppins text-2xl font-bold text-slate-800">₱{(stats.earnings || 0).toFixed(2)}</p>
        </div>
        <div className="bg-white rounded-2xl p-4 shadow-sm">
          <div className="flex items-center gap-1.5 mb-1">
            <Bus size={14} className="text-blue-500" />
            <p className="text-xs text-slate-500 font-medium">Trips Today</p>
          </div>
          <p className="font-poppins text-2xl font-bold text-slate-800">{stats.trips || 0}</p>
        </div>
      </div>

      {/* Active Trip Banner */}
      {tripActive && activeTrip && (
        <div className="mx-4 mt-3 bg-green-50 border border-green-200 rounded-2xl p-4">
          <div className="flex items-center gap-2 mb-2">
            <div className="w-2 h-2 rounded-full bg-green-500 animate-pulse" />
            <p className="text-sm font-semibold text-green-700">Active Trip</p>
          </div>
          <p className="text-xs text-slate-600">
            From: {activeTrip.originTerminalName || activeTrip.originTerminal?.terminalName || `Terminal ${activeTrip.originTerminalId}`}
          </p>
          {activeTrip.finalDestinationTerminalName && (
            <p className="text-xs text-slate-600">
              To: {activeTrip.finalDestinationTerminalName}
            </p>
          )}
          <p className="text-xs text-slate-500 mt-1">
            Passengers: {activeTrip.passengerCount} · Revenue: ₱{(activeTrip.totalRevenue || 0).toFixed(2)}
          </p>
        </div>
      )}

      {/* Action buttons */}
      <div className="mx-4 mt-3 grid grid-cols-2 gap-3">
        {!tripActive ? (
          <button onClick={handleStartTrip} disabled={starting}
            className="flex flex-col items-center justify-center gap-2 py-5 rounded-2xl bg-green-50 text-green-700 border border-green-100 font-poppins font-semibold text-sm shadow-sm disabled:opacity-50">
            {starting ? <RefreshCw size={24} className="animate-spin" /> : <Play size={24} />}
            {starting ? 'Starting...' : 'Start Trip'}
          </button>
        ) : (
          <button onClick={handleEndTrip} disabled={ending}
            className="flex flex-col items-center justify-center gap-2 py-5 rounded-2xl bg-red-50 text-red-600 border border-red-100 font-poppins font-semibold text-sm shadow-sm disabled:opacity-50">
            {ending ? <RefreshCw size={24} className="animate-spin" /> : <Square size={24} />}
            {ending ? 'Ending...' : 'End Trip'}
          </button>
        )}
        <button onClick={handleScanQR}
          className="flex flex-col items-center justify-center gap-2 py-5 rounded-2xl bg-blue-gradient text-white font-poppins font-semibold text-sm shadow-md">
          <QrCode size={24} />
          Scan QR
        </button>
      </div>

      {/* Origin & Destination selectors removed - handled in passenger app */}

      {/* Recent Passengers */}
      <div className="mx-4 mt-4 mb-4">
        <div className="flex items-center justify-between mb-3">
          <p className="font-poppins font-semibold text-sm text-slate-800">Recent Passengers</p>
          <button onClick={() => go('trip-history')} className="text-xs text-blue-600 font-medium flex items-center gap-0.5">
            See all <ChevronRight size={12} />
          </button>
        </div>
        {loadingPassengers ? (
          <div className="bg-white rounded-2xl p-6 text-center">
            <RefreshCw size={24} className="text-blue-400 animate-spin mx-auto" />
          </div>
        ) : recentPassengers.length === 0 ? (
          <div className="bg-white rounded-2xl p-6 text-center">
            <User size={32} className="text-slate-300 mx-auto mb-2" />
            <p className="text-sm text-slate-400">No recent passengers</p>
            <p className="text-xs text-slate-400 mt-1">Passenger details will appear here after scanning</p>
          </div>
        ) : (
          <div className="bg-white rounded-2xl overflow-hidden">
            {recentPassengers.slice(0, 5).map((transaction, index) => (
              <div key={transaction.transactionId} className={`p-3.5 flex items-center gap-3 ${index > 0 ? 'border-t border-slate-100' : ''}`}>
                <div className="w-10 h-10 rounded-2xl bg-blue-50 flex items-center justify-center shrink-0">
                  <User size={18} className="text-blue-600" />
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-semibold text-slate-800 truncate">{transaction.passengerName}</p>
                  <p className="text-xs text-slate-400 font-mono">{transaction.maskedCardNumber || `Card: ${transaction.cardId}`}</p>
                  <p className="text-[10px] text-slate-400">
                    {transaction.originTerminalName || `Terminal ${transaction.originTerminalId}`} → {transaction.destinationTerminalName || `Terminal ${transaction.terminalId}`}
                  </p>
                </div>
                <div className="text-right shrink-0">
                  <p className="text-sm font-bold text-slate-800">₱{transaction.finalFare.toFixed(2)}</p>
                  <p className="text-[10px] text-slate-400 font-mono">{formatTripTime(transaction.createdAt)}</p>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

    </div>
  )
}

// ── QR SCANNER ────────────────────────────────────────────────────────────────

function QRScanner({ go, activeTrip }: { go: (s: DScreen) => void, activeTrip: Trip | null }) {
  const [scanning, setScanning] = useState(true)
  const [error, setError] = useState('')
  const [scanError, setScanError] = useState<string>('')
  const scannerRef = useRef<Html5Qrcode | null>(null)

  const startScanner = async () => {
    try {
      const scanner = new Html5Qrcode('qr-reader')
      scannerRef.current = scanner
      await scanner.start(
        { facingMode: 'environment' },
        { fps: 10, qrbox: { width: 250, height: 250 } },
        async (decodedText) => {
          // QR code detected - stop scanning and process payment
          setScanning(false)
          await scanner.stop()
          await handleScannedQR(decodedText)
        },
        () => {
          // No QR code detected - keep scanning
        }
      )
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to start camera')
      setScanning(false)
    }
  }

  // Start camera-based QR scanning when component mounts
  useEffect(() => {
    startScanner()

    // Cleanup: stop scanner when component unmounts
    return () => {
      if (scannerRef.current) {
        const state = scannerRef.current.getState()
        // Only stop if the scanner is actually running (2 = SCANNING) or paused (3 = PAUSED)
        if (state === 2 || state === 3) {
          scannerRef.current.stop().catch(() => {})
        }
      }
    }
  }, [])

  const handleScannedQR = async (decodedText: string) => {
    setScanError('') // Clear previous error
    try {
      // Parse the QR data - it should contain the payment session data
      // The QR format is: base64Data.signature
      // Use lastIndexOf to handle any edge cases where the data might contain dots
      const trimmed = decodedText.trim()
      const dotIndex = trimmed.lastIndexOf('.')
      const qrData = dotIndex > 0 ? trimmed.substring(0, dotIndex) : ''
      const qrSignature = dotIndex > 0 ? trimmed.substring(dotIndex + 1) : ''
      
      if (!qrData || !qrSignature) {
        setScanError('Invalid QR code format. Please try scanning again.')
        setScanning(true)
        startScanner()
        return
      }

      const result = await cardService.processConductorPayment(qrData, qrSignature)
      if (result.success && result.data) {
        sessionStorage.setItem('lastReceipt', JSON.stringify(result.data))
        go('scan-result')
      } else {
        // Show error but STAY on scanner
        setScanError(result.message || 'Payment processing failed')
        setScanning(true)
        startScanner() // Restart scanning
      }
    } catch (error) {
      // Show error but STAY on scanner
      setScanError(error instanceof Error ? error.message : 'Scan failed')
      setScanning(true)
      startScanner() // Restart scanning
    }
  }

  const dismissError = () => {
    setScanError('')
  }

  return (
    <div className="flex-1 flex flex-col bg-black">
      <div className="flex-1 relative flex items-center justify-center"
        style={{ background: 'linear-gradient(135deg, #0f172a 0%, #1e293b 100%)' }}>
        <div className="absolute inset-0 opacity-10"
          style={{ backgroundImage: 'linear-gradient(rgba(255,255,255,0.1) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,0.1) 1px, transparent 1px)', backgroundSize: '30px 30px' }} />

        <button onClick={() => go('home')} className="absolute top-12 left-4 text-white/80 flex items-center gap-1 z-20">
          <ArrowLeft size={18} /> Back
        </button>

        <div className="absolute top-20 left-0 right-0 flex justify-center">
          <div className="bg-black/60 backdrop-blur px-4 py-2 rounded-full">
            <p className="text-white text-sm font-semibold font-poppins">
              {scanning ? 'Position QR code in frame' : 'QR Detected!'}
            </p>
          </div>
        </div>

        {/* Camera viewfinder */}
        <div className="relative w-64 h-64">
          <div id="qr-reader" className="w-full h-full" />
          {[['top-0 left-0', 'rounded-tl-2xl'], ['top-0 right-0', 'rounded-tr-2xl'], ['bottom-0 left-0', 'rounded-bl-2xl'], ['bottom-0 right-0', 'rounded-br-2xl']].map(([pos, r]) => (
            <div key={pos} className={`absolute ${pos} w-8 h-8 border-4 border-blue-400 ${r} z-10`} />
          ))}

          {scanning && (
            <div className="scan-line absolute left-2 right-2 h-0.5 bg-blue-400 shadow-[0_0_8px_rgba(96,165,250,0.8)] z-10" />
          )}
        </div>

        {error && (
          <div className="absolute bottom-24 left-0 right-0 flex justify-center px-4">
            <div className="bg-red-600/80 backdrop-blur px-6 py-2 rounded-full">
              <p className="text-white text-sm">{error}</p>
            </div>
          </div>
        )}

        {scanError && (
          <div className="absolute bottom-24 left-0 right-0 flex justify-center px-4 z-30">
            <div className="bg-red-600/90 backdrop-blur px-6 py-3 rounded-2xl shadow-lg max-w-sm" onClick={dismissError}>
              <p className="text-white text-sm text-center font-semibold">{scanError}</p>
              <p className="text-white/80 text-xs text-center mt-1">Tap to dismiss</p>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

// ── SCAN RESULT ───────────────────────────────────────────────────────────────

function ScanResult({ go }: { go: (s: DScreen) => void }) {
  const [receipt, setReceipt] = useState<ScanReceipt | null>(null)

  useEffect(() => {
    const receiptData = sessionStorage.getItem('lastReceipt')
    if (receiptData) {
      setReceipt(JSON.parse(receiptData))
    }
  }, [])

  if (!receipt) {
    return (
      <div className="flex-1 flex flex-col bg-[#F0F4FF]">
        <div className="bg-blue-gradient px-5 pt-10 pb-14">
          <button onClick={() => go('scanner')} className="text-white/80 mb-4 flex items-center gap-1">
            <ArrowLeft size={18} /> Back
          </button>
          <h2 className="font-poppins text-xl font-bold text-white">Scan Result</h2>
        </div>
        <div className="-mt-6 bg-white rounded-t-3xl flex-1 px-5 pt-6 pb-6 flex flex-col items-center justify-center gap-4">
          <AlertCircle size={48} className="text-red-400" />
          <p className="text-sm text-red-600 text-center">No receipt data available</p>
          <Btn variant="primary" size="lg" onClick={() => go('scanner')}>Scan Again</Btn>
        </div>
      </div>
    )
  }

  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF]">
      <div className="bg-blue-gradient px-5 pt-10 pb-14">
        <button onClick={() => go('scanner')} className="text-white/80 mb-4 flex items-center gap-1">
          <ArrowLeft size={18} /> Back
        </button>
        <h2 className="font-poppins text-xl font-bold text-white">Passenger Verified</h2>
        <p className="text-blue-100 text-sm mt-1">Payment processed successfully</p>
      </div>
      <div className="-mt-6 bg-white rounded-t-3xl flex-1 px-5 pt-6 pb-6 flex flex-col gap-4">
        {/* Passenger header */}
        <div className="bg-blue-50 rounded-2xl p-4 flex items-center gap-3 border border-blue-100">
          <div className="w-14 h-14 rounded-2xl bg-blue-600 flex items-center justify-center shrink-0">
            <span className="font-poppins text-xl font-bold text-white">{receipt.passengerName?.charAt(0) || 'P'}</span>
          </div>
          <div className="flex-1">
            <p className="font-poppins font-bold text-slate-800">{receipt.passengerName || 'Passenger'}</p>
            <p className="text-xs text-slate-500 font-mono">{receipt.maskedCardNumber || `Card: ${receipt.cardId}`}</p>
          </div>
          <div>
            <span className="chip bg-green-50 text-green-700">Verified</span>
          </div>
        </div>

        {/* Trip details */}
        <div className="bg-slate-50 rounded-2xl p-4 flex flex-col gap-3">
          <div className="flex justify-between items-center">
            <span className="text-sm text-slate-500">Origin</span>
            <span className="font-semibold text-slate-800">{receipt.originTerminalName || `Terminal ${receipt.originTerminalId}`}</span>
          </div>
          <div className="flex justify-between items-center">
            <span className="text-sm text-slate-500">Destination</span>
            <span className="font-semibold text-slate-800">{receipt.destinationTerminalName || `Terminal ${receipt.destinationTerminalId}`}</span>
          </div>
        </div>

        {/* Fare (from backend - locked) */}
        <div className="bg-blue-600 rounded-2xl p-5 text-center">
          <p className="text-blue-200 text-xs uppercase tracking-wider font-semibold mb-1">Fare Charged</p>
          <p className="font-poppins text-4xl font-bold text-white">₱{receipt.lockedFare.toFixed(2)}</p>
          <p className="text-blue-200 text-xs mt-1">{receipt.originTerminalName} → {receipt.destinationTerminalName}</p>
        </div>

        {/* Receipt details */}
        <div className="flex justify-between items-center bg-slate-50 rounded-2xl px-4 py-3">
          <span className="text-sm text-slate-500">Remaining Balance</span>
          <span className="font-poppins font-bold text-green-600">₱{receipt.remainingBalance.toFixed(2)}</span>
        </div>
        {receipt.transactionReferenceNumber && (
          <div className="flex justify-between items-center bg-slate-50 rounded-2xl px-4 py-3">
            <span className="text-sm text-slate-500">Reference No.</span>
            <span className="font-mono text-xs text-blue-600 font-semibold">{receipt.transactionReferenceNumber}</span>
          </div>
        )}
        <div className="flex justify-between items-center bg-slate-50 rounded-2xl px-4 py-3">
          <span className="text-sm text-slate-500">Payment Time</span>
          <span className="text-xs font-mono text-slate-600">{new Date(receipt.paymentTimestamp).toLocaleString()}</span>
        </div>

        <Btn variant="primary" size="lg" onClick={() => go('scanner')}>Close</Btn>
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
      <Btn variant="primary" size="lg" onClick={() => go('scanner')}>Close</Btn>
    </div>
  )
}

// ── TRIP HISTORY ──────────────────────────────────────────────────────────────

function TripHistory({ go }: { go: (s: DScreen) => void }) {
  const [trips, setTrips] = useState<Trip[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [timePeriod, setTimePeriod] = useState<'today' | 'week' | 'month' | 'year'>('today')
  const [expandedTripId, setExpandedTripId] = useState<number | null>(null)
  const [tripPassengers, setTripPassengers] = useState<DriverTransaction[]>([])
  const [loadingPassengers, setLoadingPassengers] = useState(false)

  useEffect(() => {
    loadTrips()
  }, [])

  const loadTrips = async () => {
    setLoading(true)
    setError('')
    try {
      const result = await tripService.getTripHistory(1, 100)
      setTrips(result.data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load trip history')
    } finally {
      setLoading(false)
    }
  }

  const loadTripPassengers = async (trip: Trip) => {
    setLoadingPassengers(true)
    try {
      const result = await cardService.getDriverTransactions(1, 100)
      if (result.success) {
        const started = new Date(trip.startedAt || trip.createdAt)
        const ended = trip.endedAt ? new Date(trip.endedAt) : new Date()

        const passengers = result.data.filter(tx => {
          const txTime = new Date(tx.createdAt)
          return txTime >= started && txTime <= ended
        })

        setTripPassengers(passengers)
      }
    } catch (err) {
      console.error('Failed to load trip passengers:', err)
    } finally {
      setLoadingPassengers(false)
    }
  }

  const handleTripClick = async (trip: Trip) => {
    if (expandedTripId === trip.tripId) {
      setExpandedTripId(null)
      setTripPassengers([])
    } else {
      setExpandedTripId(trip.tripId)
      await loadTripPassengers(trip)
    }
  }

  const filterTripsByPeriod = (trips: Trip[], period: 'today' | 'week' | 'month' | 'year'): Trip[] => {
    const now = new Date()
    const todayStart = new Date(now.getFullYear(), now.getMonth(), now.getDate())

    return trips.filter(t => {
      if (!t.startedAt) return false
      const started = new Date(t.startedAt)

      switch (period) {
        case 'today':
          return started >= todayStart
        case 'week': {
          const weekStart = new Date(todayStart)
          weekStart.setDate(weekStart.getDate() - 7)
          return started >= weekStart
        }
        case 'month': {
          const monthStart = new Date(todayStart)
          monthStart.setMonth(monthStart.getMonth() - 1)
          return started >= monthStart
        }
        case 'year': {
          const yearStart = new Date(todayStart)
          yearStart.setFullYear(yearStart.getFullYear() - 1)
          return started >= yearStart
        }
        default:
          return false
      }
    })
  }

  const filteredTrips = filterTripsByPeriod(trips, timePeriod)
  const totalTrips = filteredTrips.length
  const totalEarnings = filteredTrips.reduce((sum, t) => sum + t.totalRevenue, 0)
  const avgFare = totalTrips > 0 ? totalEarnings / totalTrips : 0

  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF] overflow-y-auto mobile-scroll">
      <div className="bg-blue-gradient px-5 pt-10 pb-16">
        <button onClick={() => go('home')} className="text-white/80 mb-4 flex items-center gap-1">
          <ArrowLeft size={18} /> Back
        </button>
        <h2 className="font-poppins text-xl font-bold text-white">Trip History</h2>
      </div>

      <div className="-mt-6 bg-[#F0F4FF] rounded-t-3xl pt-4">
        {/* Time Period Filter Tabs */}
        <div className="mx-4 mb-3">
          <div className="bg-white rounded-2xl p-1 shadow-sm flex">
            {[
              { period: 'today' as const, label: 'Today' },
              { period: 'week' as const, label: 'This Week' },
              { period: 'month' as const, label: 'This Month' },
              { period: 'year' as const, label: 'This Year' },
            ].map(({ period, label }) => (
              <button
                key={period}
                onClick={() => setTimePeriod(period)}
                className={`flex-1 py-2 px-2 rounded-xl text-xs font-semibold transition-all ${
                  timePeriod === period
                    ? 'bg-blue-gradient text-white shadow-sm'
                    : 'text-slate-600 hover:bg-slate-50'
                }`}>
                {label}
              </button>
            ))}
          </div>
        </div>

        {/* Stats Cards */}
        <div className="mx-4 grid grid-cols-3 gap-2 mb-4">
          <div className="bg-white rounded-2xl p-3 text-center shadow-sm">
            <p className="font-poppins font-bold text-slate-800 text-lg">{totalTrips}</p>
            <p className="text-[10px] text-slate-400 mt-0.5">Total Trips</p>
          </div>
          <div className="bg-white rounded-2xl p-3 text-center shadow-sm">
            <p className="font-poppins font-bold text-slate-800 text-lg">₱{avgFare.toFixed(0)}</p>
            <p className="text-[10px] text-slate-400 mt-0.5">Average (Fare per Trip)</p>
          </div>
          <div className="bg-white rounded-2xl p-3 text-center shadow-sm">
            <p className="font-poppins font-bold text-slate-800 text-lg">₱{totalEarnings.toFixed(0)}</p>
            <p className="text-[10px] text-slate-400 mt-0.5">Total Earnings</p>
          </div>
        </div>

        {error && (
          <div className="mx-4 mb-3 bg-red-50 border border-red-200 rounded-2xl p-3 flex items-start gap-2">
            <AlertCircle size={15} className="text-red-600 shrink-0 mt-0.5" />
            <p className="text-xs text-red-600">{error}</p>
          </div>
        )}

        {/* Trip List */}
        <div className="px-4 flex flex-col gap-2 pb-4">
          {loading ? (
            <div className="flex items-center justify-center py-8">
              <RefreshCw size={24} className="text-blue-400 animate-spin" />
            </div>
          ) : filteredTrips.length === 0 ? (
            <div className="bg-white rounded-2xl p-6 text-center">
              <Clock size={32} className="text-slate-300 mx-auto mb-2" />
              <p className="text-sm text-slate-400">No trips found for this period</p>
            </div>
          ) : (
            filteredTrips.map(t => {
              const tripDate = t.startedAt
                ? new Date(t.startedAt).toLocaleDateString('en-US', {
                    month: 'short',
                    day: 'numeric',
                    year: 'numeric'
                  })
                : new Date(t.createdAt).toLocaleDateString('en-US', {
                    month: 'short',
                    day: 'numeric',
                    year: 'numeric'
                  })

              const isExpanded = expandedTripId === t.tripId

              return (
                <div key={t.tripId} className="bg-white rounded-2xl shadow-sm overflow-hidden">
                  <div
                    className="p-4 cursor-pointer hover:bg-blue-50 transition-colors"
                    onClick={() => handleTripClick(t)}>
                    <div className="flex justify-between items-start">
                      <div>
                        <p className="text-sm font-semibold text-slate-800">Trip #{t.tripId}</p>
                        <p className="text-xs text-slate-500 mt-1">{tripDate}</p>
                      </div>
                      <div className="text-right">
                        <p className="text-sm font-bold text-slate-800">₱{t.totalRevenue.toFixed(2)}</p>
                        <StatusChip status={String(t.tripStatus)} />
                      </div>
                    </div>
                  </div>

                  {isExpanded && (
                    <div className="px-4 pb-4 pt-2 border-t border-slate-100">
                      {loadingPassengers ? (
                        <div className="flex justify-center py-4">
                          <RefreshCw size={20} className="text-blue-400 animate-spin" />
                        </div>
                      ) : tripPassengers.length === 0 ? (
                        <p className="text-xs text-slate-400 text-center py-2">No passengers found</p>
                      ) : (
                        <div className="flex flex-col gap-2">
                          {tripPassengers.map(passenger => (
                            <div key={passenger.transactionId} className="flex justify-between items-start">
                              <div>
                                <p className="text-sm font-semibold text-slate-800">{passenger.passengerName}</p>
                                <p className="text-xs text-slate-500">
                                  {passenger.originTerminalName || `Terminal ${passenger.originTerminalId}`} → {passenger.destinationTerminalName || '—'}
                                </p>
                              </div>
                              <div className="text-right">
                                <p className="text-sm font-bold text-slate-800">₱{passenger.finalFare.toFixed(2)}</p>
                                <p className="text-[10px] text-slate-400">
                                  {new Date(passenger.createdAt).toLocaleTimeString('en-US', {
                                    hour: '2-digit',
                                    minute: '2-digit'
                                  })}
                                </p>
                              </div>
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                  )}
                </div>
              )
            })
          )}
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
          { screen: 'scanner' as DScreen, icon: QrCode, label: 'Scan' },
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

const showNav: DScreen[] = ['home', 'trip-history']

export default function DriverApp() {
  const [screen, setScreen] = useState<DScreen>('login')
  const [selectedOrigin, setSelectedOrigin] = useState<Terminal | null>(null)
  const [activeTrip, setActiveTrip] = useState<Trip | null>(null)
  const [tripActive, setTripActive] = useState(false)
  const [checkingTrip, setCheckingTrip] = useState(false)
  
  // Centralized function to check and sync active trip status
  // Checks database: driver_id = logged-in driver AND trip_status = Active (1)
  const checkActiveTrip = async () => {
    setCheckingTrip(true)
    try {
      const trip = await tripService.getActiveTrip()
      const isActive = trip && trip.tripStatus === 'Active'
      if (isActive) {
        setActiveTrip(trip)
        setTripActive(true)
        return true
      } else {
        setActiveTrip(null)
        setTripActive(false)
        return false
      }
    } catch (err) {
      console.error('Failed to check active trip:', err)
      setActiveTrip(null)
      setTripActive(false)
      return false
    } finally {
      setCheckingTrip(false)
    }
  }
  
  const go = async (s: DScreen) => {
    // When navigating to home, wait for DB check before rendering
    if (s === 'home') {
      await checkActiveTrip()
    }
    
    // Prevent navigating to scanner without an active trip
    if (s === 'scanner' && !tripActive) {
      alert('Please start a trip first')
      return
    }
    
    setScreen(s)
  }

  // Restore active trip on app load (workflow persistence)
  useEffect(() => {
    const restoreActiveTrip = async () => {
      // Only restore if user is already authenticated (has token)
      if (authService.isAuthenticated()) {
        await checkActiveTrip()
        // No auto-redirect - just sync trip state
      }
    }
    restoreActiveTrip()
  }, [])

  return (
    <div className="flex flex-col h-full overflow-hidden">
      <div className="flex-1 flex flex-col overflow-hidden">
        {screen === 'login' && <DriverLogin 
          go={go} 
          onTripStatusChecked={(hasActiveTrip) => {
            setTripActive(hasActiveTrip)
          }}
        />}
        {screen === 'home' && (checkingTrip ? (
          <div className="flex-1 flex items-center justify-center bg-[#F0F4FF]">
            <RefreshCw size={32} className="text-blue-400 animate-spin" />
          </div>
        ) : (
          <DriverHome
            go={go}
            activeTrip={activeTrip}
            onTripChanged={setActiveTrip}
            tripActive={tripActive}
            setTripActive={setTripActive}
            selectedOrigin={selectedOrigin}
            setSelectedOrigin={setSelectedOrigin}
          />
        ))}
        {/* Start-trip and select-destination screens removed - trip starts directly from home */}
        {screen === 'scanner' && <QRScanner go={go} activeTrip={activeTrip} />}
        {screen === 'scan-result' && <ScanResult go={go} />}
        {screen === 'pay-success' && <DriverPaySuccess go={go} />}
        {screen === 'trip-history' && <TripHistory go={go} />}
      </div>
      {showNav.includes(screen) && <DriverNav current={screen} go={go} />}
    </div>
  )
}
