import { useState, useEffect } from 'react'
import {
  ArrowLeft, Eye, EyeOff, RefreshCw, CheckCircle,
  Bus, QrCode, Clock, TrendingUp, User, AlertCircle,
  Play, Square, ChevronRight, Shield, Phone, Lock,
  Wifi, Battery, Signal, MapPin, CreditCard
} from 'lucide-react'
import { authService } from './lib/auth'
import { cardService, type ScanReceipt } from './lib/cards'
import { tripService, type Station, type Trip } from './lib/tripService'

type DScreen = 'login' | 'home' | 'start-trip' | 'select-destination' | 'scanner' | 'scan-result' | 'pay-success' | 'trip-history'

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
  const [error, setError] = useState('')

  const submit = async () => {
    setLoading(true)
    setError('')
    try {
      await authService.login({ mobileNumber: id, password: pass })
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
  const [activeTrip, setActiveTrip] = useState<Trip | null>(null)
  const [loading, setLoading] = useState(true)
  const [stats, setStats] = useState({ earnings: 0, trips: 0 })
  const [recentTrips, setRecentTrips] = useState<any[]>([])

  useEffect(() => {
    loadActiveTrip()
  }, [])

  const loadActiveTrip = async () => {
    try {
      const trip = await tripService.resumeActiveTrip()
      if (trip && trip.tripStatus === 'Active') {
        setActiveTrip(trip)
        setTripActive(true)
      }
    } catch (error) {
      console.error('Failed to load active trip:', error)
    } finally {
      setLoading(false)
    }
  }

  const handleStartTrip = async () => {
    try {
      // Show station selection (in real app, fetch stations from backend)
      const originStationId = prompt('Enter origin station ID:')
      if (!originStationId) return

      const response = await tripService.startTrip(parseInt(originStationId))
      if (response.success && response.data) {
        setActiveTrip(response.data)
        setTripActive(true)
        go('select-destination')
      }
    } catch (error) {
      alert(error instanceof Error ? error.message : 'Failed to start trip')
    }
  }

  const handleEndTrip = async () => {
    if (!activeTrip) return
    if (!confirm('Are you sure you want to end this trip?')) return

    try {
      const response = await tripService.endTrip(activeTrip.tripId)
      if (response.success) {
        setActiveTrip(null)
        setTripActive(false)
        tripService.clearActiveTrip()
      }
    } catch (error) {
      alert(error instanceof Error ? error.message : 'Failed to end trip')
    }
  }

  if (loading) {
    return (
      <div className="flex-1 flex items-center justify-center bg-[#F0F4FF]">
        <RefreshCw size={32} className="text-blue-400 animate-spin" />
      </div>
    )
  }

  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF] overflow-y-auto mobile-scroll">
      {/* Header */}
      <div className="bg-blue-gradient px-5 pt-10 pb-20 relative overflow-hidden">
        <div className="absolute top-[-60px] right-[-60px] w-52 h-52 rounded-full bg-white/10" />
        <div className="flex items-center justify-between">
          <div>
            <p className="text-blue-100 text-sm">Welcome back,</p>
            <h2 className="font-poppins text-2xl font-bold text-white mt-0.5">Driver</h2>
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
          <p className="font-poppins text-2xl font-bold text-slate-800">₱{stats.earnings.toFixed(2)}</p>
        </div>
        <div className="bg-white rounded-2xl p-4 shadow-sm">
          <div className="flex items-center gap-1.5 mb-1">
            <Bus size={14} className="text-blue-500" />
            <p className="text-xs text-slate-500 font-medium">Trips Today</p>
          </div>
          <p className="font-poppins text-2xl font-bold text-slate-800">{stats.trips}</p>
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
            From: {activeTrip.originStation?.stationName || `Station ${activeTrip.originStationId}`}
          </p>
          {activeTrip.finalDestinationStation && (
            <p className="text-xs text-slate-600">
              To: {activeTrip.finalDestinationStation.stationName}
            </p>
          )}
          <p className="text-xs text-slate-500 mt-1">
            Passengers: {activeTrip.passengerCount} · Revenue: ₱{activeTrip.totalRevenue.toFixed(2)}
          </p>
        </div>
      )}

      {/* Action buttons */}
      <div className="mx-4 mt-3 grid grid-cols-2 gap-3">
        {!tripActive ? (
          <button onClick={handleStartTrip}
            className="flex flex-col items-center justify-center gap-2 py-5 rounded-2xl bg-green-50 text-green-700 border border-green-100 font-poppins font-semibold text-sm shadow-sm">
            <Play size={24} />
            Start Trip
          </button>
        ) : (
          <button onClick={handleEndTrip}
            className="flex flex-col items-center justify-center gap-2 py-5 rounded-2xl bg-red-50 text-red-600 border border-red-100 font-poppins font-semibold text-sm shadow-sm">
            <Square size={24} />
            End Trip
          </button>
        )}
        <button onClick={() => tripActive ? go('select-destination') : alert('Please start a trip first')}
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
        {recentTrips.length === 0 ? (
          <div className="bg-white rounded-2xl p-6 text-center">
            <p className="text-sm text-slate-400">No recent trips</p>
          </div>
        ) : (
          <div className="flex flex-col gap-2">
            {recentTrips.map((t: any) => (
              <div key={t.id} className="bg-white rounded-2xl p-3.5 flex items-center gap-3 shadow-sm">
                <div className="w-10 h-10 rounded-2xl bg-blue-50 flex items-center justify-center shrink-0">
                  <User size={18} className="text-blue-600" />
                </div>
                <div className="flex-1">
                  <p className="text-sm font-semibold text-slate-800">{t.passengerName}</p>
                  <p className="text-xs text-slate-400 font-mono">{t.time}</p>
                </div>
                <div className="text-right">
                  <p className="text-sm font-bold text-slate-800">₱{t.fare.toFixed(2)}</p>
                  <StatusChip status={t.status} />
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

// ── START TRIP ────────────────────────────────────────────────────────────────

function StartTripScreen({ go }: { go: (s: DScreen) => void }) {
  const [stations, setStations] = useState<Station[]>([])
  const [selectedStation, setSelectedStation] = useState<Station | null>(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    loadStations()
  }, [])

  const loadStations = async () => {
    // In real app, fetch from backend
    // For now, use mock data
    setStations([
      { stationId: 1, stationName: 'Central Station', townId: 1, isActive: true },
      { stationId: 2, stationName: 'Airport Station', townId: 1, isActive: true },
      { stationId: 3, stationName: 'Cubao Station', townId: 1, isActive: true },
    ])
  }

  const handleStartTrip = async () => {
    if (!selectedStation) return
    setLoading(true)
    try {
      const response = await tripService.startTrip(selectedStation.stationId)
      if (response.success) {
        go('select-destination')
      }
    } catch (error) {
      alert(error instanceof Error ? error.message : 'Failed to start trip')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF]">
      <div className="bg-blue-gradient px-5 pt-10 pb-14">
        <button onClick={() => go('home')} className="text-white/80 mb-4 flex items-center gap-1">
          <ArrowLeft size={18} /> Back
        </button>
        <h2 className="font-poppins text-xl font-bold text-white">Start Trip</h2>
        <p className="text-blue-100 text-sm mt-0.5">Select your origin station</p>
      </div>
      <div className="-mt-6 bg-white rounded-t-3xl flex-1 px-5 pt-6 pb-6 flex flex-col gap-4">
        <div className="flex flex-col gap-2">
          <p className="text-sm font-semibold text-slate-700">Origin Station</p>
          {stations.map(station => (
            <button
              key={station.stationId}
              onClick={() => setSelectedStation(station)}
              className={`p-4 rounded-2xl border-2 text-left transition-all ${selectedStation?.stationId === station.stationId ? 'border-blue-500 bg-blue-50' : 'border-slate-200 bg-white'}`}
            >
              <div className="flex items-center gap-3">
                <MapPin size={20} className="text-blue-600" />
                <div>
                  <p className="font-semibold text-slate-800">{station.stationName}</p>
                  <p className="text-xs text-slate-500">Station #{station.stationId}</p>
                </div>
              </div>
            </button>
          ))}
        </div>
        <Btn variant="primary" size="lg" onClick={handleStartTrip} disabled={!selectedStation || loading}>
          {loading ? <><RefreshCw size={16} className="animate-spin" /> Starting...</> : 'Start Trip'}
        </Btn>
      </div>
    </div>
  )
}

// ── SELECT DESTINATION ────────────────────────────────────────────────────────

function SelectDestinationScreen({ go }: { go: (s: DScreen) => void }) {
  const [stations, setStations] = useState<Station[]>([])
  const [selectedDestination, setSelectedDestination] = useState<Station | null>(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    loadStations()
  }, [])

  const loadStations = async () => {
    // In real app, fetch from backend
    setStations([
      { stationId: 1, stationName: 'Central Station', townId: 1, isActive: true },
      { stationId: 2, stationName: 'Airport Station', townId: 1, isActive: true },
      { stationId: 3, stationName: 'Cubao Station', townId: 1, isActive: true },
    ])
  }

  const handleSelectDestination = () => {
    if (!selectedDestination) return
    // Store selected destination and go to scanner
    go('scanner')
  }

  return (
    <div className="flex-1 flex flex-col bg-[#F0F4FF]">
      <div className="bg-blue-gradient px-5 pt-10 pb-14">
        <button onClick={() => go('home')} className="text-white/80 mb-4 flex items-center gap-1">
          <ArrowLeft size={18} /> Back
        </button>
        <h2 className="font-poppins text-xl font-bold text-white">Select Destination</h2>
        <p className="text-blue-100 text-sm mt-0.5">Choose where the passenger is going</p>
      </div>
      <div className="-mt-6 bg-white rounded-t-3xl flex-1 px-5 pt-6 pb-6 flex flex-col gap-4">
        <div className="flex flex-col gap-2">
          <p className="text-sm font-semibold text-slate-700">Destination Station</p>
          {stations.map(station => (
            <button
              key={station.stationId}
              onClick={() => setSelectedDestination(station)}
              className={`p-4 rounded-2xl border-2 text-left transition-all ${selectedDestination?.stationId === station.stationId ? 'border-blue-500 bg-blue-50' : 'border-slate-200 bg-white'}`}
            >
              <div className="flex items-center gap-3">
                <MapPin size={20} className="text-blue-600" />
                <div>
                  <p className="font-semibold text-slate-800">{station.stationName}</p>
                  <p className="text-xs text-slate-500">Station #{station.stationId}</p>
                </div>
              </div>
            </button>
          ))}
        </div>
        <Btn variant="primary" size="lg" onClick={handleSelectDestination} disabled={!selectedDestination}>
          Continue to Scan
        </Btn>
      </div>
    </div>
  )
}

// ── QR SCANNER ────────────────────────────────────────────────────────────────

function QRScanner({ go, destination }: { go: (s: DScreen) => void, destination: Station | null }) {
  const [scanning, setScanning] = useState(true)
  const [countdown, setCountdown] = useState(3)
  const [scanMethod, setScanMethod] = useState<'qr' | 'card'>('qr')
  const [cardNumber, setCardNumber] = useState('')

  useEffect(() => {
    if (!scanning) return
    const t = setTimeout(() => {
      setScanning(false)
      handleScan()
    }, 3000)
    const interval = setInterval(() => setCountdown(c => Math.max(0, c - 1)), 1000)
    return () => { clearTimeout(t); clearInterval(interval) }
  }, [scanning, scanMethod, cardNumber, destination])

  const handleScan = async () => {
    if (!destination) {
      alert('Please select a destination first')
      go('select-destination')
      return
    }

    try {
      let receipt: ScanReceipt | null = null

      if (scanMethod === 'qr') {
        // Simulate QR scan (in real app, get from scanner)
        const qrData = btoa(JSON.stringify({ QRVersion: 1, CardId: 1, CardNumber: '4111111111111111', Token: 'test-token', CreatedAt: new Date().toISOString() }))
        const signature = 'test-signature'
        const result = await cardService.processConductorPayment(qrData, signature, destination.stationId)
        if (result.success && result.data) {
          receipt = result.data
        }
      } else {
        // Physical card scan
        if (!cardNumber) {
          alert('Please enter card number')
          return
        }
        const result = await cardService.scanPhysicalCard(cardNumber, destination.stationId)
        if (result.success && result.data) {
          receipt = result.data
        }
      }

      if (receipt) {
        // Store receipt for the result screen
        sessionStorage.setItem('lastReceipt', JSON.stringify(receipt))
        go('scan-result')
      }
    } catch (error) {
      alert(error instanceof Error ? error.message : 'Scan failed')
      go('home')
    }
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

        <div className="relative w-64 h-64">
          {[['top-0 left-0', 'rounded-tl-2xl'], ['top-0 right-0', 'rounded-tr-2xl'], ['bottom-0 left-0', 'rounded-bl-2xl'], ['bottom-0 right-0', 'rounded-br-2xl']].map(([pos, r]) => (
            <div key={pos} className={`absolute ${pos} w-8 h-8 border-4 border-blue-400 ${r}`} />
          ))}

          {scanning && (
            <div className="scan-line absolute left-2 right-2 h-0.5 bg-blue-400 shadow-[0_0_8px_rgba(96,165,250,0.8)]" />
          )}

          <div className="absolute inset-4 flex items-center justify-center">
            {scanning ? (
              <div className="opacity-20">
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

        {scanning && (
          <div className="absolute bottom-24 left-0 right-0 flex justify-center">
            <div className="bg-blue-600/80 backdrop-blur px-6 py-2 rounded-full flex items-center gap-2">
              <RefreshCw size={14} className="text-white animate-spin" />
              <p className="text-white text-sm font-mono">Auto-scanning... {countdown}s</p>
            </div>
          </div>
        )}
      </div>

      {/* Scan method toggle */}
      <div className="bg-slate-900 p-4 flex gap-2">
        <button
          onClick={() => setScanMethod('qr')}
          className={`flex-1 py-3 rounded-xl font-semibold text-sm ${scanMethod === 'qr' ? 'bg-blue-600 text-white' : 'bg-slate-800 text-slate-300'}`}
        >
          <QrCode size={18} className="inline mr-2" />
          QR Code
        </button>
        <button
          onClick={() => setScanMethod('card')}
          className={`flex-1 py-3 rounded-xl font-semibold text-sm ${scanMethod === 'card' ? 'bg-blue-600 text-white' : 'bg-slate-800 text-slate-300'}`}
        >
          <CreditCard size={18} className="inline mr-2" />
          Physical Card
        </button>
      </div>

      {/* Physical card input */}
      {scanMethod === 'card' && (
        <div className="bg-slate-900 p-4">
          <input
            type="text"
            value={cardNumber}
            onChange={e => setCardNumber(e.target.value)}
            placeholder="Enter card number"
            className="w-full px-4 py-3 rounded-xl bg-slate-800 text-white placeholder:text-slate-400"
          />
        </div>
      )}
    </div>
  )
}

// ── SCAN RESULT ───────────────────────────────────────────────────────────────

function ScanResult({ go }: { go: (s: DScreen) => void }) {
  // In real app, get receipt from context or state management
  const [receipt, setReceipt] = useState<ScanReceipt | null>(null)

  useEffect(() => {
    // Retrieve receipt from session storage or context
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
            <span className="font-semibold text-slate-800">{receipt.originStationName || `Station ${receipt.originStationId}`}</span>
          </div>
          <div className="flex justify-between items-center">
            <span className="text-sm text-slate-500">Destination</span>
            <span className="font-semibold text-slate-800">{receipt.destinationStationName || `Station ${receipt.destinationStationId}`}</span>
          </div>
        </div>

        {/* Fare (from backend - locked) */}
        <div className="bg-blue-600 rounded-2xl p-5 text-center">
          <p className="text-blue-200 text-xs uppercase tracking-wider font-semibold mb-1">Fare Charged</p>
          <p className="font-poppins text-4xl font-bold text-white">₱{receipt.lockedFare.toFixed(2)}</p>
          <p className="text-blue-200 text-xs mt-1">{receipt.originStationName} → {receipt.destinationStationName}</p>
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

        <Btn variant="primary" size="lg" onClick={() => go('scanner')}>Scan Next Passenger</Btn>
        <Btn variant="ghost" size="lg" onClick={() => go('home')}>Back to Dashboard</Btn>
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
  const [selectedDestination, setSelectedDestination] = useState<Station | null>(null)
  const go = (s: DScreen) => setScreen(s)

  return (
    <div className="flex flex-col h-full overflow-hidden">
      <div className="flex-1 flex flex-col overflow-hidden">
        {screen === 'login' && <DriverLogin go={go} />}
        {screen === 'home' && <DriverHome go={go} />}
        {screen === 'start-trip' && <StartTripScreen go={go} />}
        {screen === 'select-destination' && <SelectDestinationScreen go={go} />}
        {screen === 'scanner' && <QRScanner go={go} destination={selectedDestination} />}
        {screen === 'scan-result' && <ScanResult go={go} />}
        {screen === 'pay-success' && <DriverPaySuccess go={go} />}
        {screen === 'trip-history' && <TripHistory go={go} />}
      </div>
      {showNav.includes(screen) && <DriverNav current={screen} go={go} />}
    </div>
  )
}