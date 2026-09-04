/**
 * Admin dashboard root application.
 *
 * Single-file shell composing the admin UI: shared primitives (Chip, Btn,
 * KpiCard), sidebar/topbar navigation, and the per-section views (dashboard,
 * passengers, drivers, terminals, fare matrix, transactions, reports,
 * settings, trips, discount management, live trip monitoring). The root
 * `AdminApp` component (bottom) owns the login gate and the data-mutating
 * handlers (terminal/driver/fare-rule CRUD) that back the child views.
 */
import { useState, useEffect } from 'react'
import {
  LayoutDashboard, Users, Bus, Map,
  Grid3X3, CreditCard, BarChart3, Settings,
  Search, Plus, Edit2, CheckCircle, Trash2, Eye,
  XCircle, AlertCircle, TrendingUp, TrendingDown,
  DollarSign,
  Bell, LogOut, Menu, X, RefreshCw,
  Tag, FileText, Activity, User, Lock, Wallet
} from 'lucide-react'
import { adminService, type Terminal, type User as AppUser, type Driver, type FareRule, type Transaction, type ReportSummary } from './lib/admin'
import { authService } from './lib/auth'
import { ToastContainer, useToast } from './components/Toast'
import { DriverModal } from './components/DriverModal'
import { TerminalModal } from './components/TerminalModal'
import { FareRuleModal } from './components/FareRuleModal'
import { DiscountTypeModal } from './components/DiscountTypeModal'
import { TripsView } from './views/TripsView'
import { DiscountTypesView } from './views/DiscountTypesView'
import { DiscountApplicationsView } from './views/DiscountApplicationsView'
import { TripMonitoringView } from './views/TripMonitoringView'
import { PassengerDiscountsView } from './views/PassengerDiscountsView'

/** Top-level navigation sections selectable in the sidebar. */
type AdminSection =
  | 'dashboard' | 'users' | 'drivers' | 'terminals'
  | 'fare-matrix' | 'transactions' | 'reports' | 'settings'
  | 'trips' | 'discount-types' | 'discount-applications' | 'passenger-discounts' | 'trip-monitoring'

// ── Shared ─────────────────────────────────────────────────────────────────────

/** Small pill-shaped status/label badge used across all admin views. */
export function Chip({ label, variant = 'default' }: { label: string; variant?: 'success' | 'warning' | 'danger' | 'info' | 'default' }) {
  const map = {
    success: 'bg-green-50 text-green-700 border border-green-200',
    warning: 'bg-yellow-50 text-yellow-700 border border-yellow-200',
    danger: 'bg-red-50 text-red-700 border border-red-200',
    info: 'bg-blue-50 text-blue-700 border border-blue-200',
    default: 'bg-slate-100 text-slate-600',
  }
  return <span className={`chip ${map[variant]}`}>{label}</span>
}

/** Shared button component with size/variant styles used by every admin view. */
export function Btn({ children, variant = 'primary', size = 'md', onClick, disabled, className = '' }: {
  children: React.ReactNode; variant?: 'primary' | 'secondary' | 'ghost' | 'danger'
  size?: 'sm' | 'md' | 'lg'; onClick?: () => void; disabled?: boolean; className?: string
}) {
  const sizes = { sm: 'px-3 py-1.5 text-xs', md: 'px-4 py-2 text-sm', lg: 'px-5 py-2.5 text-sm' }
  const variants = {
    primary: 'bg-blue-gradient text-white shadow-sm hover:shadow-md hover:brightness-105',
    secondary: 'bg-white text-blue-700 border-2 border-blue-600 hover:bg-blue-50',
    ghost: 'text-blue-700 hover:bg-blue-50 border border-transparent',
    danger: 'bg-red-500 text-white hover:bg-red-600',
  }
  return (
    <button onClick={onClick} disabled={disabled}
      className={`inline-flex items-center gap-1.5 font-semibold rounded-xl transition-all font-poppins ${sizes[size]} ${variants[variant]} ${className} ${disabled ? 'opacity-50 cursor-not-allowed' : ''}`}>
      {children}
    </button>
  )
}

/** Dashboard stat card showing an icon, headline value and optional trend. */
function KpiCard({ icon: Icon, label, value, sub, trend, color = 'blue' }: {
  icon: React.ElementType; label: string; value: string; sub?: string; trend?: string; color?: string
}) {
  const colors: Record<string, string> = {
    blue: 'bg-blue-50 text-blue-600',
    green: 'bg-green-50 text-green-600',
    orange: 'bg-orange-50 text-orange-600',
    purple: 'bg-purple-50 text-purple-600',
  }
  return (
    <div className="bg-white rounded-2xl p-5 shadow-sm border border-slate-100 card-hover">
      <div className="flex items-start justify-between mb-3">
        <div className={`w-10 h-10 rounded-2xl ${colors[color]} flex items-center justify-center`}>
          <Icon size={20} />
        </div>
        {trend && (
          <span className={`text-xs font-semibold flex items-center gap-0.5 ${trend.startsWith('+') ? 'text-green-600' : 'text-red-500'}`}>
            {trend.startsWith('+') ? <TrendingUp size={12} /> : <TrendingDown size={12} />} {trend}
          </span>
        )}
      </div>
      <p className="font-poppins text-2xl font-bold text-slate-800">{value}</p>
      <p className="text-sm font-medium text-slate-500 mt-0.5">{label}</p>
      {sub && <p className="text-xs text-slate-400 mt-0.5">{sub}</p>}
    </div>
  )
}

// ── SIDEBAR ────────────────────────────────────────────────────────────────────

/** Sidebar navigation model — one entry per AdminSection. */
const navItems: { id: AdminSection; label: string; icon: React.ElementType }[] = [
  { id: 'dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { id: 'users', label: 'Passengers', icon: Users },
  { id: 'drivers', label: 'Drivers', icon: Bus },
  { id: 'terminals', label: 'Terminals', icon: Map },
  { id: 'fare-matrix', label: 'Fare Matrix', icon: Grid3X3 },
  { id: 'transactions', label: 'Transactions', icon: CreditCard },
  { id: 'reports', label: 'Reports', icon: BarChart3 },
  { id: 'settings', label: 'Settings', icon: Settings },
  { id: 'trips', label: 'Trips', icon: Bus },
  { id: 'discount-types', label: 'Discount Programs', icon: Tag },
  { id: 'discount-applications', label: 'Discount Apps', icon: FileText },
  { id: 'passenger-discounts', label: 'Passenger Discounts', icon: User },
  { id: 'trip-monitoring', label: 'Trip Monitor', icon: Activity },
]

/**
 * Collapsible left navigation. `open`/`setOpen` control the mobile drawer;
 * `onLogout` is wired to the authService-backed logout in AdminApp.
 */
function Sidebar({ active, setActive, open, setOpen, onLogout }: {
  active: AdminSection; setActive: (s: AdminSection) => void; open: boolean; setOpen: (v: boolean) => void; onLogout: () => void
}) {
  const user = authService.getUser()
  const displayName = user ? `${user.firstName} ${user.lastName}`.trim() : 'Admin User'
  const initials = user ? `${user.firstName[0] || ''}${user.lastName[0] || ''}`.toUpperCase() : 'AD'

  return (
    <>
      {open && <div className="fixed inset-0 bg-black/40 z-20 lg:hidden" onClick={() => setOpen(false)} />}

      <aside className={`fixed lg:relative top-0 left-0 h-full z-30 flex flex-col bg-white border-r border-slate-100 shadow-lg lg:shadow-none transition-all duration-300
        ${open ? 'w-64 translate-x-0' : 'w-64 -translate-x-full lg:translate-x-0 lg:w-64'}`}
        style={{ minWidth: 256 }}>
        {/* Logo */}
        <div className="px-5 py-5 border-b border-slate-100">
          <div className="flex items-center gap-2.5">
            <div className="w-9 h-9 rounded-xl bg-blue-gradient flex items-center justify-center shadow-sm">
              <Bus size={18} className="text-white" />
            </div>
            <div>
              <p className="font-poppins font-bold text-slate-800 leading-tight">TransitPay</p>
              <p className="text-[10px] text-slate-400">Admin Console</p>
            </div>
          </div>
        </div>

        {/* Nav */}
        <nav className="flex-1 px-3 py-4 overflow-y-auto">
          <p className="text-[10px] font-bold text-slate-400 uppercase tracking-widest px-3 mb-2">Main Menu</p>
          {navItems.slice(0, 7).map(item => (
            <button key={item.id} onClick={() => { setActive(item.id); setOpen(false) }}
              className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-xl mb-0.5 transition-all text-sm font-semibold
                ${active === item.id ? 'bg-blue-gradient text-white shadow-sm' : 'text-slate-600 hover:bg-slate-100 hover:text-slate-800'}`}>
              <item.icon size={18} />
              <span className="flex-1 text-left">{item.label}</span>
            </button>
          ))}
          <p className="text-[10px] font-bold text-slate-400 uppercase tracking-widest px-3 mb-2 mt-4">Management</p>
          {navItems.slice(7).map(item => (
            <button key={item.id} onClick={() => { setActive(item.id); setOpen(false) }}
              className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-xl mb-0.5 transition-all text-sm font-semibold
                ${active === item.id ? 'bg-blue-gradient text-white shadow-sm' : 'text-slate-600 hover:bg-slate-100 hover:text-slate-800'}`}>
              <item.icon size={18} />
              <span>{item.label}</span>
            </button>
          ))}
        </nav>

        {/* User */}
        <div className="px-4 py-4 border-t border-slate-100">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-blue-600 flex items-center justify-center">
              <span className="font-poppins text-sm font-bold text-white">{initials}</span>
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-semibold text-slate-800">{displayName}</p>
              <p className="text-xs text-slate-400 truncate">{user?.username || 'admin'}</p>
            </div>
            <button onClick={onLogout} className="text-slate-400 hover:text-red-500 transition-colors"><LogOut size={16} /></button>
          </div>
        </div>
      </aside>
    </>
  )
}

// ── TOPBAR ────────────────────────────────────────────────────────────────────

/** Sticky header showing the current section title and the mobile menu toggle. */
function Topbar({ section, sidebarOpen, setSidebarOpen }: { section: AdminSection; sidebarOpen: boolean; setSidebarOpen: (v: boolean) => void }) {
  const label = navItems.find(n => n.id === section)?.label || 'Dashboard'
  return (
    <header className="h-16 bg-white border-b border-slate-100 px-4 lg:px-6 flex items-center justify-between shrink-0 shadow-sm">
      <div className="flex items-center gap-3">
        <button onClick={() => setSidebarOpen(!sidebarOpen)} className="lg:hidden text-slate-600 hover:text-blue-600 transition-colors">
          {sidebarOpen ? <X size={22} /> : <Menu size={22} />}
        </button>
        <div>
          <p className="font-poppins font-bold text-slate-800 text-base">{label}</p>
          <p className="text-xs text-slate-400 hidden sm:block">TransitPay Management Portal</p>
        </div>
      </div>
      <div className="flex items-center gap-2">
        <div className="relative hidden md:block">
          <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
          <input placeholder="Search..." className="pl-9 pr-4 py-2 text-sm rounded-xl border border-slate-200 bg-slate-50 w-48 focus:outline-none focus:border-blue-400 focus:bg-white transition-all" />
        </div>
        <button className="relative w-9 h-9 rounded-xl border border-slate-200 flex items-center justify-center text-slate-600 hover:bg-blue-50 hover:text-blue-600 transition-colors">
          <Bell size={16} />
        </button>
        <div className="w-9 h-9 rounded-xl bg-blue-600 flex items-center justify-center">
          <span className="font-poppins text-xs font-bold text-white">AD</span>
        </div>
      </div>
    </header>
  )
}

// ── DASHBOARD ─────────────────────────────────────────────────────────────────

/** Overview screen: KPI summary (ridership, revenue, cards) plus recent transactions. */
function DashboardView() {
  const [summary, setSummary] = useState<ReportSummary | null>(null)
  const [recentTx, setRecentTx] = useState<Transaction[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    const load = async () => {
      setLoading(true)
      setError('')
      try {
        const [s, txs] = await Promise.all([
          adminService.getReportSummary(),
          adminService.getTransactions(1, 5),
        ])
        setSummary(s)
        setRecentTx(txs.data)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load dashboard')
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [])

  if (loading) {
    return (
      <div className="flex items-center justify-center py-16">
        <RefreshCw size={32} className="text-blue-400 animate-spin" />
      </div>
    )
  }

  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-2xl p-4 flex items-start gap-2">
        <AlertCircle size={15} className="text-red-600 shrink-0 mt-0.5" />
        <p className="text-xs text-red-600">{error}</p>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-5">
      {/* KPIs */}
      <div className="grid grid-cols-2 lg:grid-cols-3 xl:grid-cols-6 gap-3">
        <KpiCard icon={Users} label="Total Passengers" value={summary?.totalPassengers?.toLocaleString() || '0'} sub="Registered" color="blue" />
        <KpiCard icon={Bus} label="Total Drivers" value={summary?.totalDrivers?.toLocaleString() || '0'} sub="Registered" color="green" />
        <KpiCard icon={Map} label="Terminals" value={summary?.totalTerminals?.toLocaleString() || '0'} sub="Registered" color="purple" />
        <KpiCard icon={DollarSign} label="Total Revenue" value={`₱${(summary?.totalRevenue || 0).toLocaleString()}`} sub="All time" color="green" />
        <KpiCard icon={CreditCard} label="Transactions" value={summary?.totalTransactions?.toLocaleString() || '0'} sub="All time" color="blue" />
      </div>

      {/* Recent transactions */}
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="px-5 py-4 border-b border-slate-100 flex items-center justify-between">
          <p className="font-poppins font-bold text-slate-800">Recent Transactions</p>
        </div>
        {recentTx.length === 0 ? (
          <div className="p-8 text-center text-slate-500">
            <CreditCard size={48} className="mx-auto mb-3 text-slate-300" />
            <p className="font-semibold">No transactions yet</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead><tr className="border-b border-slate-100 bg-slate-50">
                {['Passenger', 'Type', 'Amount', 'Date'].map(h => (
                  <th key={h} className="px-5 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider whitespace-nowrap">{h}</th>
                ))}
              </tr></thead>
              <tbody>
                {recentTx.map((t, i) => (
                  <tr key={t.transactionId} className={`border-b border-slate-100 hover:bg-blue-50/40 transition-colors ${i === recentTx.length - 1 ? 'border-0' : ''}`}>
                    <td className="px-5 py-3.5 font-medium text-slate-800 whitespace-nowrap">{t.passengerName || '—'}</td>
                    <td className="px-5 py-3.5 whitespace-nowrap">
                      <Chip label={t.transactionType} variant={t.transactionType === 'PAYMENT' ? 'info' : t.transactionType === 'TOP_UP' ? 'success' : 'warning'} />
                    </td>
                    <td className={`px-5 py-3.5 font-bold whitespace-nowrap ${t.transactionType === 'TOP_UP' ? 'text-green-600' : 'text-slate-800'}`}>
                      {t.transactionType === 'TOP_UP' ? '+' : '-'}₱{Math.abs(t.amount).toFixed(2)}
                    </td>
                    <td className="px-5 py-3.5 text-slate-400 text-xs whitespace-nowrap">
                      {new Date(t.createdAt).toLocaleString('en-US', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}

// ── PASSENGERS (formerly Users) ────────────────────────────────────────────────

/**
 * Passenger account management: list, search, filter, activate/deactivate and
 * manual wallet crediting (resolves the passenger's card, then tops it up).
 */
function PassengersView() {
  const [users, setUsers] = useState<AppUser[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [search, setSearch] = useState('')
  const [filter, setFilter] = useState('all')
  const [addCreditUser, setAddCreditUser] = useState<AppUser | null>(null)
  const [creditAmount, setCreditAmount] = useState('')
  const [creditLoading, setCreditLoading] = useState(false)
  const [creditError, setCreditError] = useState('')

  useEffect(() => {
    loadUsers()
  }, [])

  /** Fetches the passenger list and mirrors server errors to the UI. */
  const loadUsers = async () => {
    setLoading(true)
    setError('')
    try {
      const result = await adminService.getUsers(1, 50)
      setUsers(result.data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load passengers')
    } finally {
      setLoading(false)
    }
  }

  /** Activates or deactivates a passenger account, then refreshes the list. */
  const handleToggleStatus = async (userId: number, isActive: boolean) => {
    try {
      if (isActive) {
        await adminService.deactivateUser(userId)
      } else {
        await adminService.activateUser(userId)
      }
      await loadUsers()
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Failed to update status')
    }
  }

  /** Credits a passenger's wallet manually; amount comes from the credit dialog. */
  const handleAddCredit = async () => {
    if (!addCreditUser || !creditAmount) return
    setCreditLoading(true)
    setCreditError('')
    try {
      const card = await adminService.getCardByUserId(addCreditUser.userId)
      await adminService.topUpWallet(card.cardId, parseFloat(creditAmount))
      alert(`₱${parseFloat(creditAmount).toFixed(2)} added to ${addCreditUser.firstName} ${addCreditUser.lastName}'s wallet!`)
      setAddCreditUser(null)
      setCreditAmount('')
    } catch (err) {
      setCreditError(err instanceof Error ? err.message : 'Failed to add credit')
    } finally {
      setCreditLoading(false)
    }
  }

  const filtered = users.filter(u => {
    const name = `${u.firstName} ${u.lastName}`.toLowerCase()
    const matchesSearch = name.includes(search.toLowerCase()) || u.username.toLowerCase().includes(search.toLowerCase())
    const matchesFilter = filter === 'all' || (filter === 'active' ? u.isActive : !u.isActive)
    return matchesSearch && matchesFilter
  })

  return (
    <div className="flex flex-col gap-4">
      {/* Controls */}
      <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 flex flex-wrap gap-3 items-center justify-between">
        <div className="flex gap-2 flex-wrap">
          {['all', 'active', 'suspended'].map(f => (
            <button key={f} onClick={() => setFilter(f)}
              className={`px-4 py-1.5 rounded-xl text-sm font-semibold transition-all ${filter === f ? 'bg-blue-600 text-white shadow-sm' : 'text-slate-600 bg-slate-100 hover:bg-slate-200'}`}>
              {f.charAt(0).toUpperCase() + f.slice(1)}
            </button>
          ))}
        </div>
        <div className="flex gap-2 items-center">
          <div className="relative">
            <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
            <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search passengers..."
              className="pl-9 pr-4 py-2 text-sm rounded-xl border border-slate-200 w-44 focus:outline-none focus:border-blue-400 transition-all" />
          </div>
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        {loading ? (
          <div className="p-8 text-center text-slate-500">
            <RefreshCw size={24} className="mx-auto mb-3 text-blue-400 animate-spin" />
            Loading passengers...
          </div>
        ) : error ? (
          <div className="p-8 text-center text-red-500">{error}</div>
        ) : filtered.length === 0 ? (
          <div className="p-8 text-center text-slate-500">
            <Users size={48} className="mx-auto mb-3 text-slate-300" />
            <p className="font-semibold">No passengers found</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead><tr className="border-b border-slate-100 bg-slate-50">
                {['Passenger Name', 'Username', 'Status', 'Actions'].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider whitespace-nowrap">{h}</th>
                ))}
              </tr></thead>
              <tbody>
                {filtered.map((u, i, arr) => (
                  <tr key={u.userId} className={`border-b border-slate-100 hover:bg-blue-50/40 transition-colors ${i === arr.length - 1 ? 'border-0' : ''}`}>
                    <td className="px-4 py-3.5">
                      <div className="flex items-center gap-2.5">
                        <div className="w-8 h-8 rounded-xl bg-blue-600 flex items-center justify-center shrink-0">
                          <span className="text-xs font-bold text-white">{u.firstName[0]}</span>
                        </div>
                        <span className="font-medium text-slate-800 whitespace-nowrap">{u.firstName} {u.lastName}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3.5 text-slate-500 font-mono text-xs whitespace-nowrap">{u.username}</td>
                    <td className="px-4 py-3.5 whitespace-nowrap">
                      <Chip label={u.isActive ? 'Active' : 'Suspended'} variant={u.isActive ? 'success' : 'danger'} />
                    </td>
                    <td className="px-4 py-3.5">
                      <div className="flex items-center gap-1">
                        <button onClick={() => handleToggleStatus(u.userId, u.isActive)}
                          className="p-1.5 rounded-lg hover:bg-blue-50 text-slate-400 hover:text-blue-600 transition-colors"
                          title={u.isActive ? 'Suspend' : 'Reactivate'}>
                          {u.isActive ? <XCircle size={14} /> : <CheckCircle size={14} />}
                        </button>
                        <button onClick={() => { setAddCreditUser(u); setCreditAmount(''); setCreditError('') }}
                          className="p-1.5 rounded-lg hover:bg-green-50 text-slate-400 hover:text-green-600 transition-colors"
                          title="Add Credit">
                          <Wallet size={14} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Add Credit Modal */}
      {addCreditUser && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-3xl shadow-xl max-w-md w-full p-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="font-poppins font-bold text-xl text-slate-800">Add Credit</h3>
              <button onClick={() => setAddCreditUser(null)} className="text-slate-400 hover:text-slate-600">
                <X size={20} />
              </button>
            </div>

            <div className="flex flex-col gap-4">
              <div className="bg-blue-50 border border-blue-200 rounded-xl p-3 flex items-center gap-3">
                <div className="w-10 h-10 rounded-xl bg-blue-600 flex items-center justify-center shrink-0">
                  <span className="text-sm font-bold text-white">{addCreditUser.firstName[0]}</span>
                </div>
                <div>
                  <p className="font-semibold text-slate-800">{addCreditUser.firstName} {addCreditUser.lastName}</p>
                  <p className="text-xs text-slate-500 font-mono">{addCreditUser.username}</p>
                </div>
              </div>

              <div>
                <label className="text-sm font-semibold text-slate-700 mb-2 block">Amount (₱)</label>
                <input
                  type="number"
                  min="1"
                  step="0.01"
                  value={creditAmount}
                  onChange={e => setCreditAmount(e.target.value)}
                  placeholder="Enter amount to add"
                  className="w-full px-4 py-3 rounded-xl border border-slate-200 focus:outline-none focus:border-green-400 focus:ring-2 focus:ring-green-100 transition-all"
                />
                <p className="text-xs text-slate-500 mt-1">This will be added to the passenger's wallet balance.</p>
              </div>

              {creditError && (
                <div className="bg-red-50 border border-red-200 rounded-xl p-3 flex items-start gap-2">
                  <AlertCircle size={15} className="text-red-600 shrink-0 mt-0.5" />
                  <p className="text-xs text-red-600">{creditError}</p>
                </div>
              )}

              <div className="flex gap-2 mt-2">
                <Btn variant="secondary" size="lg" className="flex-1" onClick={() => setAddCreditUser(null)}>
                  Cancel
                </Btn>
                <Btn variant="primary" size="lg" className="flex-1" onClick={handleAddCredit} disabled={!creditAmount || creditLoading}>
                  {creditLoading ? 'Adding...' : 'Add Credit'}
                </Btn>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

// ── DRIVERS ───────────────────────────────────────────────────────────────────

/**
 * Driver account management: list, search, activate/deactivate, and password
 * resets. Driver creation itself is handled by the DriverModal opened via
 * `onAddDriver` (submit handled in AdminApp).
 */
function DriversView({ onAddDriver }: { onAddDriver: () => void }) {
  const [drivers, setDrivers] = useState<Driver[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [search, setSearch] = useState('')

  useEffect(() => {
    loadDrivers()
  }, [])

  /** Fetches the driver list and mirrors server errors to the UI. */
  const loadDrivers = async () => {
    setLoading(true)
    setError('')
    try {
      const data = await adminService.getDrivers()
      setDrivers(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load drivers')
    } finally {
      setLoading(false)
    }
  }

  /** Activates or deactivates a driver account, then refreshes the list. */
  const handleToggleStatus = async (driverId: number, isActive: boolean) => {
    try {
      if (isActive) {
        await adminService.deactivateUser(driverId)
      } else {
        await adminService.activateUser(driverId)
      }
      await loadDrivers()
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Failed to update status')
    }
  }

  /** Prompts for and applies a new password for a driver account. */
  const handleResetPassword = async (driver: Driver) => {
    const newPassword = prompt(`Enter a new password for ${driver.firstName} ${driver.lastName} (${driver.username}):\n\nMinimum 8 characters.`)
    if (newPassword === null) return // User cancelled
    if (newPassword.length < 8) {
      alert('Password must be at least 8 characters.')
      return
    }
    try {
      await adminService.resetPassword(driver.userId, newPassword)
      alert(`Password changed successfully for ${driver.username}`)
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Failed to change password')
    }
  }

  const filtered = drivers.filter(d => {
    const name = `${d.firstName} ${d.lastName}`.toLowerCase()
    return name.includes(search.toLowerCase()) || d.username.toLowerCase().includes(search.toLowerCase())
  })

  return (
    <div className="flex flex-col gap-4">
      {/* All drivers table */}
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="px-5 py-4 border-b border-slate-100 flex items-center justify-between">
          <p className="font-poppins font-bold text-slate-800">All Drivers</p>
          <div className="flex gap-2">
            <div className="relative">
              <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
              <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search drivers..." className="pl-9 pr-4 py-1.5 text-sm rounded-xl border border-slate-200 w-40 focus:outline-none focus:border-blue-400" />
            </div>
            <Btn variant="primary" size="sm" onClick={onAddDriver}><Plus size={13} /> Add Driver</Btn>
          </div>
        </div>
        {loading ? (
          <div className="p-8 text-center text-slate-500">
            <RefreshCw size={24} className="mx-auto mb-3 text-blue-400 animate-spin" />
            Loading drivers...
          </div>
        ) : error ? (
          <div className="p-8 text-center text-red-500">{error}</div>
        ) : filtered.length === 0 ? (
          <div className="p-8 text-center text-slate-500">
            <Bus size={48} className="mx-auto mb-3 text-slate-300" />
            <p className="font-semibold">No drivers found</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead><tr className="border-b border-slate-100 bg-slate-50">
                {['Driver Name', 'Driver ID', 'Status', 'Actions'].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider whitespace-nowrap">{h}</th>
                ))}
              </tr></thead>
              <tbody>
                {filtered.map((d, i, arr) => (
                  <tr key={d.userId} className={`border-b border-slate-100 hover:bg-blue-50/40 transition-colors ${i === arr.length - 1 ? 'border-0' : ''}`}>
                    <td className="px-4 py-3.5">
                      <div className="flex items-center gap-2.5">
                        <div className="w-8 h-8 rounded-xl bg-blue-600 flex items-center justify-center shrink-0">
                          <span className="text-xs font-bold text-white">{d.firstName[0]}</span>
                        </div>
                        <span className="font-medium text-slate-800 whitespace-nowrap">{d.firstName} {d.lastName}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3.5 font-mono text-xs text-blue-600 font-semibold">{d.username}</td>
                    <td className="px-4 py-3.5 whitespace-nowrap">
                      <Chip label={d.isActive ? 'Active' : 'Suspended'} variant={d.isActive ? 'success' : 'danger'} />
                    </td>
                    <td className="px-4 py-3.5">
                      <div className="flex items-center gap-1">
                        <button onClick={() => handleToggleStatus(d.userId, d.isActive)}
                          className="p-1.5 rounded-lg hover:bg-blue-50 text-slate-400 hover:text-blue-600 transition-colors"
                          title={d.isActive ? 'Suspend' : 'Reactivate'}>
                          {d.isActive ? <XCircle size={14} /> : <CheckCircle size={14} />}
                        </button>
                        <button onClick={() => handleResetPassword(d)}
                          className="p-1.5 rounded-lg hover:bg-yellow-50 text-slate-400 hover:text-yellow-600 transition-colors"
                          title="Change Password">
                          <Lock size={14} />
                        </button>
                        <button className="p-1.5 rounded-lg hover:bg-blue-50 text-slate-400 hover:text-blue-600 transition-colors" title="View Details">
                          <Eye size={14} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}

// ── TERMINALS & STATIONS ──────────────────────────────────────────────────────

/**
 * Terminal list (presentation only — data is loaded by AdminApp and passed
 * in via props so the fare-rule modal can share the same terminal state).
 */
function TerminalsView({ onAddTerminal, terminals, onEdit, onDelete }: {
  onAddTerminal: () => void; terminals: Terminal[]; onEdit: (terminal: Terminal) => void; onDelete: (terminalId: number) => void
}) {
  return (
    <div className="flex flex-col gap-4">
      <div className="flex justify-between items-center">
        <div />
        <Btn variant="primary" onClick={onAddTerminal}><Plus size={14} /> Add Terminal</Btn>
      </div>
      {terminals.length === 0 ? (
        <div className="bg-white rounded-2xl p-8 text-center text-slate-500">
          <Map size={48} className="mx-auto mb-3 text-slate-300" />
          <p className="font-semibold">No terminals found</p>
        </div>
      ) : (
        <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
          <table className="w-full text-sm">
            <thead><tr className="border-b border-slate-100 bg-slate-50">
              <th className="px-5 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider">Terminal ID</th>
              <th className="px-5 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider">Terminal Name</th>
              <th className="px-5 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider">Actions</th>
            </tr></thead>
            <tbody>
              {terminals.map((t, i, arr) => (
                <tr key={t.terminalId} className={`border-b border-slate-100 hover:bg-blue-50/40 transition-colors ${i === arr.length - 1 ? 'border-0' : ''}`}>
                  <td className="px-5 py-3.5 font-mono text-xs text-blue-600 font-semibold">TRM-{t.terminalId.toString().padStart(2, '0')}</td>
                  <td className="px-5 py-3.5 font-medium text-slate-800">{t.terminalName}</td>
                  <td className="px-5 py-3.5">
                    <div className="flex items-center gap-1">
                      <button onClick={() => onEdit(t)} className="p-1.5 rounded-lg hover:bg-blue-50 text-slate-400 hover:text-blue-600 transition-colors" title="Edit Terminal">
                        <Edit2 size={14} />
                      </button>
                      <button onClick={() => onDelete(t.terminalId)} className="p-1.5 rounded-lg hover:bg-red-50 text-slate-400 hover:text-red-500 transition-colors" title="Delete Terminal">
                        <Trash2 size={14} />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}


// ── FARE MATRIX ───────────────────────────────────────────────────────────────

/**
 * Origin→destination fare table management. Loads fare rules itself; add/
 * edit/delete are delegated to AdminApp via props (modals live there).
 */
function FareMatrixView({ onAddFareRule, onEditFareRule, onDeleteFareRule }: {
  onAddFareRule: () => void; onEditFareRule: (fare: FareRule) => void; onDeleteFareRule: (fareId: number) => void
}) {
  const [fares, setFares] = useState<FareRule[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    loadFares()
  }, [])

  /** Fetches all fare rules (origin/destination pairs with amounts). */
  const loadFares = async () => {
    setLoading(true)
    setError('')
    try {
      const data = await adminService.getFareRules()
      setFares(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load fare rules')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex justify-end">
        <Btn variant="primary" onClick={onAddFareRule}><Plus size={14} /> Add Fare Rule</Btn>
      </div>
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        {loading ? (
          <div className="p-8 text-center text-slate-500">
            <RefreshCw size={24} className="mx-auto mb-3 text-blue-400 animate-spin" />
            Loading fare rules...
          </div>
        ) : error ? (
          <div className="p-8 text-center text-red-500">{error}</div>
        ) : fares.length === 0 ? (
          <div className="p-8 text-center text-slate-500">
            <Grid3X3 size={48} className="mx-auto mb-3 text-slate-300" />
            <p className="font-semibold">No fare rules found</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead><tr className="border-b border-slate-100 bg-slate-50">
                {['Origin Terminal', 'Destination', 'Fare', 'Effective', 'Status', 'Actions'].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider whitespace-nowrap">{h}</th>
                ))}
              </tr></thead>
              <tbody>
                {fares.map((f, i, arr) => (
                  <tr key={f.fareId} className={`border-b border-slate-100 hover:bg-blue-50/40 transition-colors ${i === arr.length - 1 ? 'border-0' : ''}`}>
                    <td className="px-4 py-3.5 font-medium text-slate-800 whitespace-nowrap">{f.originTerminalName}</td>
                    <td className="px-4 py-3.5 text-slate-600 whitespace-nowrap">{f.destinationTerminalName}</td>
                    <td className="px-4 py-3.5 font-poppins font-bold text-slate-800">₱{f.fareAmount.toFixed(2)}</td>
                    <td className="px-4 py-3.5 text-slate-400 text-xs whitespace-nowrap">{new Date(f.effectiveDate).toLocaleDateString()}</td>
                    <td className="px-4 py-3.5"><Chip label={f.isActive ? 'Active' : 'Inactive'} variant={f.isActive ? 'success' : 'default'} /></td>
                    <td className="px-4 py-3.5">
                      <div className="flex items-center gap-1">
                        <button onClick={() => onEditFareRule(f)} className="p-1.5 rounded-lg hover:bg-blue-50 text-slate-400 hover:text-blue-600 transition-colors" title="Edit Fare Rule">
                          <Edit2 size={14} />
                        </button>
                        <button onClick={() => onDeleteFareRule(f.fareId)} className="p-1.5 rounded-lg hover:bg-red-50 text-slate-400 hover:text-red-500 transition-colors" title="Delete Fare Rule">
                          <Trash2 size={14} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}

// ── TRANSACTIONS ──────────────────────────────────────────────────────────────

/** Searchable list of all fare transactions across the system (read-only). */
function TransactionsView() {
  const [transactions, setTransactions] = useState<Transaction[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [typeFilter, setTypeFilter] = useState('all')
  const [search, setSearch] = useState('')

  useEffect(() => {
    loadTransactions()
  }, [])

  /** Fetches the first page of system-wide transactions for the ledger table. */
  const loadTransactions = async () => {
    setLoading(true)
    setError('')
    try {
      const result = await adminService.getTransactions(1, 50)
      setTransactions(result.data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load transactions')
    } finally {
      setLoading(false)
    }
  }

  const filtered = transactions.filter(t => {
    const matchesType = typeFilter === 'all' || t.transactionType.toLowerCase() === typeFilter
    const matchesSearch = search === '' ||
      t.passengerName?.toLowerCase().includes(search.toLowerCase()) ||
      t.transactionId.toString().includes(search)
    return matchesType && matchesSearch
  })

  return (
    <div className="flex flex-col gap-4">
      <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 flex flex-wrap gap-3 items-center justify-between">
        <div className="flex gap-2 flex-wrap">
          <div className="flex items-center gap-2">
            <span className="text-xs text-slate-500 font-semibold">Type:</span>
            {['all', 'payment', 'top_up', 'refund'].map(f => (
              <button key={f} onClick={() => setTypeFilter(f)}
                className={`px-3 py-1.5 rounded-xl text-xs font-semibold transition-all ${typeFilter === f ? 'bg-blue-600 text-white' : 'text-slate-600 bg-slate-100 hover:bg-slate-200'}`}>
                {f === 'top_up' ? 'Top Up' : f.charAt(0).toUpperCase() + f.slice(1)}
              </button>
            ))}
          </div>
        </div>
        <div className="relative">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
          <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by passenger or TX ID..." className="pl-9 pr-4 py-1.5 text-sm rounded-xl border border-slate-200 w-56 focus:outline-none focus:border-blue-400" />
        </div>
      </div>

      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        {loading ? (
          <div className="p-8 text-center text-slate-500">
            <RefreshCw size={24} className="mx-auto mb-3 text-blue-400 animate-spin" />
            Loading transactions...
          </div>
        ) : error ? (
          <div className="p-8 text-center text-red-500">{error}</div>
        ) : filtered.length === 0 ? (
          <div className="p-8 text-center text-slate-500">
            <CreditCard size={48} className="mx-auto mb-3 text-slate-300" />
            <p className="font-semibold">No transactions found</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead><tr className="border-b border-slate-100 bg-slate-50">
                {['TX ID', 'Passenger Name', 'Origin', 'Destination', 'Transaction Type', 'Amount', 'Date'].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider whitespace-nowrap">{h}</th>
                ))}
              </tr></thead>
              <tbody>
                {filtered.map((t, i, arr) => (
                  <tr key={t.transactionId} className={`border-b border-slate-100 hover:bg-blue-50/40 transition-colors ${i === arr.length - 1 ? 'border-0' : ''}`}>
                    <td className="px-4 py-3.5 font-mono text-xs text-blue-600 font-semibold">TX-{t.transactionId.toString().padStart(6, '0')}</td>
                    <td className="px-4 py-3.5 font-medium text-slate-800 whitespace-nowrap">{t.passengerName || '—'}</td>
                    <td className="px-4 py-3.5 text-slate-600 whitespace-nowrap">{t.originTerminalName || '—'}</td>
                    <td className="px-4 py-3.5 text-slate-600 whitespace-nowrap">{t.destinationTerminalName || '—'}</td>
                    <td className="px-4 py-3.5 whitespace-nowrap">
                      <Chip label={t.transactionType} variant={t.transactionType === 'PAYMENT' ? 'info' : t.transactionType === 'TOP_UP' ? 'success' : 'warning'} />
                    </td>
                    <td className={`px-4 py-3.5 font-bold whitespace-nowrap ${t.transactionType === 'TOP_UP' ? 'text-green-600' : 'text-slate-800'}`}>
                      {t.transactionType === 'TOP_UP' ? '+' : '-'}₱{Math.abs(t.amount).toFixed(2)}
                    </td>
                    <td className="px-4 py-3.5 text-slate-400 text-xs whitespace-nowrap">
                      {new Date(t.createdAt).toLocaleString('en-US', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}

// ── REPORTS ───────────────────────────────────────────────────────────────────

/** Aggregated ridership/revenue report view driven by a date range. */
function ReportsView() {
  const [summary, setSummary] = useState<ReportSummary | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    const load = async () => {
      setLoading(true)
      setError('')
      try {
        const data = await adminService.getReportSummary()
        setSummary(data)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load report')
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [])

  if (loading) {
    return (
      <div className="flex items-center justify-center py-16">
        <RefreshCw size={32} className="text-blue-400 animate-spin" />
      </div>
    )
  }

  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-2xl p-4 flex items-start gap-2">
        <AlertCircle size={15} className="text-red-600 shrink-0 mt-0.5" />
        <p className="text-xs text-red-600">{error}</p>
      </div>
    )
  }

  const avgFare = summary && summary.totalTransactions > 0 ? summary.totalRevenue / summary.totalTransactions : 0

  return (
    <div className="flex flex-col gap-4">
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-3">
        {[['Total Revenue', `₱${(summary?.totalRevenue || 0).toLocaleString()}`],
          ['Total Transactions', (summary?.totalTransactions || 0).toLocaleString()],
          ['Avg per Transaction', `₱${avgFare.toFixed(2)}`]].map(([k, v]) => (
          <div key={k} className="bg-white rounded-2xl p-5 shadow-sm border border-slate-100">
            <p className="text-sm text-slate-500 font-medium">{k}</p>
            <p className="font-poppins text-2xl font-bold text-slate-800 mt-1">{v}</p>
          </div>
        ))}
      </div>

      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="px-5 py-4 border-b border-slate-100">
          <p className="font-poppins font-bold text-slate-800">System Summary</p>
          <p className="text-xs text-slate-400 mt-0.5">Live data from the backend</p>
        </div>
        <table className="w-full text-sm">
          <thead><tr className="border-b border-slate-100 bg-slate-50">
            {['Metric', 'Value'].map(h => (
              <th key={h} className="px-5 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider">{h}</th>
            ))}
          </tr></thead>
          <tbody>
            {[
              ['Total Passengers', (summary?.totalPassengers || 0).toLocaleString()],
              ['Total Drivers', (summary?.totalDrivers || 0).toLocaleString()],
              ['Total Terminals', (summary?.totalTerminals || 0).toLocaleString()],
              ['Total Transactions', (summary?.totalTransactions || 0).toLocaleString()],
              ['Total Revenue', `₱${(summary?.totalRevenue || 0).toLocaleString()}`],
            ].map(([k, v], i, arr) => (
              <tr key={k} className={`border-b border-slate-100 hover:bg-blue-50/40 ${i === arr.length - 1 ? 'border-0' : ''}`}>
                <td className="px-5 py-4 font-semibold text-slate-800">{k}</td>
                <td className="px-5 py-4 font-poppins font-bold text-slate-800">{v}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

// ── SETTINGS ──────────────────────────────────────────────────────────────────

/** Placeholder settings screen (no functional controls yet). */
function SettingsView() {
  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 max-w-4xl">
      {[
        { title: 'System Settings', items: ['App Name', 'Contact Email', 'Support Number', 'Maintenance Mode'] },
        { title: 'Fare Settings', items: ['Base Fare', 'Per KM Rate', 'Senior Discount %', 'Student Discount %'] },
        { title: 'Notification Settings', items: ['Email Notifications', 'SMS Alerts', 'Push Notifications'] },
        { title: 'Security', items: ['Two-Factor Auth', 'Session Timeout', 'Password Policy', 'Audit Logs'] },
      ].map(({ title, items }) => (
        <div key={title} className="bg-white rounded-2xl p-5 shadow-sm border border-slate-100">
          <p className="font-poppins font-bold text-slate-800 mb-4">{title}</p>
          <div className="flex flex-col gap-3">
            {items.map(item => (
              <div key={item} className="flex items-center justify-between py-2 border-b border-slate-100 last:border-0">
                <span className="text-sm text-slate-700">{item}</span>
                <Btn variant="ghost" size="sm"><Edit2 size={12} /> Edit</Btn>
              </div>
            ))}
          </div>
        </div>
      ))}
    </div>
  )
}

// ── MAIN ──────────────────────────────────────────────────────────────────────

/**
 * Root admin component. Owns the login gate (token restore + login/logout,
 * with the token validated on startup so stale tokens can't skip auth), the
 * shared terminal/driver/fare data consumed by the section views and modals,
 * and every mutation handler with toast feedback. Once authenticated it
 * renders the active section on top of the sidebar/topbar shell.
 */
export default function AdminApp() {
  const [section, setSection] = useState<AdminSection>('dashboard')
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const [authChecking, setAuthChecking] = useState(true)
  const [loginUsername, setLoginUsername] = useState('')
  const [loginPass, setLoginPass] = useState('')
  const [loginError, setLoginError] = useState('')
  const [loginLoading, setLoginLoading] = useState(false)

  // Validate token on app startup — prevents skipping login with stale tokens
  useEffect(() => {
    const checkAuth = async () => {
      const valid = await authService.validateToken()
      setIsAuthenticated(valid)
      setAuthChecking(false)
    }
    checkAuth()
  }, [])

  // Modal states
  const [isDriverModalOpen, setIsDriverModalOpen] = useState(false)
  const [isTerminalModalOpen, setIsTerminalModalOpen] = useState(false)
  const [isFareRuleModalOpen, setIsFareRuleModalOpen] = useState(false)
  const [editingTerminal, setEditingTerminal] = useState<Terminal | null>(null)
  const [editingFareRule, setEditingFareRule] = useState<FareRule | null>(null)
  const [isDiscountTypeModalOpen, setIsDiscountTypeModalOpen] = useState(false)

  // Loading states
  const [driverLoading, setDriverLoading] = useState(false)
  const [terminalLoading, setTerminalLoading] = useState(false)
  const [fareRuleLoading, setFareRuleLoading] = useState(false)

  // Data states
  const [terminals, setTerminals] = useState<Terminal[]>([])

  // Toast notifications
  const { toasts, removeToast, success, error: showError } = useToast()

  /** Submits admin credentials; on success the app switches to the main layout. */
  const handleLogin = async () => {
    setLoginLoading(true)
    setLoginError('')
    try {
      await authService.login({ username: loginUsername, password: loginPass })
      setIsAuthenticated(true)
    } catch (err) {
      setLoginError(err instanceof Error ? err.message : 'Login failed')
    } finally {
      setLoginLoading(false)
    }
  }

  /** Clears the stored session and returns to the login screen. */
  const handleLogout = () => {
    authService.logout().then(() => setIsAuthenticated(false))
  }

  // Load initial data
  useEffect(() => {
    if (!isAuthenticated) return
    loadTerminals()
    loadDrivers()
    loadFares()
  }, [isAuthenticated])

  /** Loads terminals into shared state (also consumed by the fare-rule modal). */
  const loadTerminals = async () => {
    try {
      const data = await adminService.getTerminals()
      setTerminals(data)
    } catch (err) {
      console.error('Failed to load terminals:', err)
    }
  }

  /** Refresh hook for driver data; the DriversView loads its own list too. */
  const loadDrivers = async () => {
    try {
      await adminService.getDrivers()
    } catch (err) {
      console.error('Failed to load drivers:', err)
    }
  }

  /** Refresh hook for fare-rule data; the FareMatrixView loads its own list too. */
  const loadFares = async () => {
    try {
      await adminService.getFareRules()
    } catch (err) {
      console.error('Failed to load fares:', err)
    }
  }

  // ── Handlers: Terminals ─────────────────────────────────────────────────

  /** Creates a terminal via the TerminalModal, then refreshes shared terminal state. */
  const handleAddTerminal = async (data: { terminalName: string }) => {
    setTerminalLoading(true)
    try {
      await adminService.createTerminal(data)
      success('Terminal added successfully!')
      setIsTerminalModalOpen(false)
      await loadTerminals()
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to add terminal')
    } finally {
      setTerminalLoading(false)
    }
  }

  /** Renames the terminal currently being edited (`editingTerminal`). */
  const handleEditTerminal = async (data: { terminalName: string }) => {
    if (!editingTerminal) return
    setTerminalLoading(true)
    try {
      await adminService.updateTerminal(editingTerminal.terminalId, data)
      success('Terminal updated successfully!')
      setIsTerminalModalOpen(false)
      setEditingTerminal(null)
      await loadTerminals()
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to update terminal')
    } finally {
      setTerminalLoading(false)
    }
  }

  /**
   * Two-phase delete: the first call asks the backend whether the terminal
   * is referenced by fare rules. If so, a warning response is returned and
   * the delete must be re-issued with explicit user confirmation.
   */
  const handleDeleteTerminal = async (terminalId: number) => {
    try {
      // First call to check if terminal is used in fare rules
      const response = await adminService.deleteTerminal(terminalId)
      
      // Check if backend requires confirmation (warning response)
      if (response.warning && response.requiresConfirmation) {
        const confirmed = confirm(
          `${response.message}\n\nThis will permanently delete ${response.affectedFareRules} fare rule(s). This action cannot be undone.`
        )
        
        if (!confirmed) return
        
        // User confirmed, call again with confirmation
        await adminService.deleteTerminal(terminalId, true)
        success(`Terminal and ${response.affectedFareRules} related fare rule(s) deleted successfully!`)
      } else {
        // No warning, deletion was successful
        success('Terminal deleted successfully!')
      }
      
      await loadTerminals()
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to delete terminal')
    }
  }

  // ── Handlers: Drivers ───────────────────────────────────────────────────

  /**
   * Creates a driver account from the DriverModal. Note: only identity
   * fields are sent — the backend generates the Driver ID which doubles as
   * the driver's initial password.
   */
  const handleAddDriver = async (data: {
    firstName: string
    lastName: string
    mobileNumber: string
    vehicle: string
    plateNumber: string
  }) => {
    setDriverLoading(true)
    try {
      // The backend generates the Driver ID (e.g., DRV-000010) which becomes
      // the driver's default password.
      await adminService.createDriver({
        firstName: data.firstName,
        lastName: data.lastName,
        mobileNumber: data.mobileNumber
      })
      success('Driver added successfully!')
      setIsDriverModalOpen(false)
      await loadDrivers()
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to add driver')
    } finally {
      setDriverLoading(false)
    }
  }

  // ── Handlers: Fare Rules ────────────────────────────────────────────────

  /** Creates an origin→destination fare rule via the FareRuleModal. */
  const handleAddFareRule = async (data: {
    originTerminalId: number
    destinationTerminalId: number
    fareAmount: number
    effectiveDate: string
  }) => {
    setFareRuleLoading(true)
    try {
      await adminService.createFareRule(data)
      success('Fare rule added successfully!')
      setIsFareRuleModalOpen(false)
      await loadFares()
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to add fare rule')
    } finally {
      setFareRuleLoading(false)
    }
  }

  /** Updates the fare rule currently being edited (`editingFareRule`). */
  const handleEditFareRule = async (data: {
    originTerminalId: number
    destinationTerminalId: number
    fareAmount: number
    effectiveDate: string
  }) => {
    if (!editingFareRule) return
    setFareRuleLoading(true)
    try {
      await adminService.updateFareRule(editingFareRule.fareId, data)
      success('Fare rule updated successfully!')
      setIsFareRuleModalOpen(false)
      setEditingFareRule(null)
      await loadFares()
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to update fare rule')
    } finally {
      setFareRuleLoading(false)
    }
  }

  /** Deletes a fare rule after a browser confirm dialog. */
  const handleDeleteFareRule = async (fareId: number) => {
    if (!confirm('Are you sure you want to delete this fare rule?')) return
    try {
      await adminService.deleteFareRule(fareId)
      success('Fare rule deleted successfully!')
      await loadFares()
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to delete fare rule')
    }
  }

  if (authChecking) {
    return (
      <div className="flex h-full items-center justify-center bg-[#F0F4FF]">
        <div className="flex flex-col items-center gap-4">
          <RefreshCw size={40} className="text-blue-400 animate-spin" />
          <p className="text-slate-600 font-poppins">Validating session...</p>
        </div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return (
      <div className="flex h-full items-center justify-center bg-[#F0F4FF] p-4">
        <div className="w-full max-w-md bg-white rounded-3xl shadow-xl overflow-hidden">
          <div className="bg-blue-gradient px-6 py-10 text-center relative overflow-hidden">
            <div className="absolute top-[-40px] right-[-40px] w-40 h-40 rounded-full bg-white/10" />
            <div className="absolute bottom-[-40px] left-[-40px] w-40 h-40 rounded-full bg-white/10" />
            <div className="w-16 h-16 rounded-2xl bg-white/20 backdrop-blur flex items-center justify-center mx-auto mb-4 shadow-lg">
              <Bus size={32} className="text-white" />
            </div>
            <h1 className="font-poppins text-2xl font-bold text-white">TransitPay Admin</h1>
            <p className="text-blue-100 text-sm mt-1">Sign in to manage the system</p>
          </div>
          <div className="p-6 flex flex-col gap-4">
            <div className="flex flex-col gap-1.5">
              <label htmlFor="login-username" className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Username</label>
              <input id="login-username" value={loginUsername} onChange={e => setLoginUsername(e.target.value)} placeholder="Enter username"
                className="w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-800 placeholder:text-slate-400 focus:outline-none focus:border-blue-400" />
            </div>
            <div className="flex flex-col gap-1.5">
              <label htmlFor="login-password" className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Password</label>
              <input id="login-password" type="password" value={loginPass} onChange={e => setLoginPass(e.target.value)} placeholder="Enter password"
                className="w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-800 placeholder:text-slate-400 focus:outline-none focus:border-blue-400" />
            </div>
            {loginError && (
              <div className="bg-red-50 border border-red-200 rounded-2xl p-3 flex items-start gap-2">
                <AlertCircle size={15} className="text-red-600 shrink-0 mt-0.5" />
                <p className="text-xs text-red-600">{loginError}</p>
              </div>
            )}
            <Btn variant="primary" size="lg" onClick={handleLogin} disabled={loginLoading}>
              {loginLoading ? <><RefreshCw size={16} className="animate-spin" /> Signing in...</> : 'Sign In'}
            </Btn>
            <p className="text-center text-xs text-slate-400">
              Admin credentials are set via the <span className="font-mono font-semibold">ADMIN_BOOTSTRAP_PASSWORD</span> environment variable.
            </p>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="flex h-full bg-[#F0F4FF] overflow-hidden">
      <Sidebar active={section} setActive={setSection} open={sidebarOpen} setOpen={setSidebarOpen} onLogout={handleLogout} />

      <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
        <Topbar section={section} sidebarOpen={sidebarOpen} setSidebarOpen={setSidebarOpen} />

        <main className="flex-1 overflow-y-auto p-4 lg:p-6">
          {section === 'dashboard' && <DashboardView />}
          {section === 'users' && <PassengersView />}
          {section === 'drivers' && <DriversView onAddDriver={() => setIsDriverModalOpen(true)} />}
          {/* Nav "Terminals" → shows Terminal data */}
          {section === 'terminals' && <TerminalsView
            onAddTerminal={() => { setEditingTerminal(null); setIsTerminalModalOpen(true) }}
            terminals={terminals}
            onEdit={(t) => { setEditingTerminal(t); setIsTerminalModalOpen(true) }}
            onDelete={handleDeleteTerminal}
          />}
          {section === 'fare-matrix' && <FareMatrixView
            onAddFareRule={() => { setEditingFareRule(null); setIsFareRuleModalOpen(true) }}
            onEditFareRule={(f) => { setEditingFareRule(f); setIsFareRuleModalOpen(true) }}
            onDeleteFareRule={handleDeleteFareRule}
          />}
          {section === 'transactions' && <TransactionsView />}
          {section === 'reports' && <ReportsView />}
          {section === 'settings' && <SettingsView />}
          {section === 'trips' && <TripsView />}
          {section === 'discount-types' && <DiscountTypesView onAddDiscountType={() => setIsDiscountTypeModalOpen(true)} />}
          {section === 'discount-applications' && <DiscountApplicationsView />}
          {section === 'passenger-discounts' && <PassengerDiscountsView />}
          {section === 'trip-monitoring' && <TripMonitoringView />}
        </main>
      </div>

      {/* Modals */}
      <DriverModal
        isOpen={isDriverModalOpen}
        onClose={() => setIsDriverModalOpen(false)}
        onSubmit={handleAddDriver}
        loading={driverLoading}
      />

      <TerminalModal
        isOpen={isTerminalModalOpen}
        onClose={() => { setIsTerminalModalOpen(false); setEditingTerminal(null) }}
        onSubmit={editingTerminal ? handleEditTerminal : handleAddTerminal}
        terminals={terminals}
        loading={terminalLoading}
        initialData={editingTerminal ? { terminalId: editingTerminal.terminalId, terminalName: editingTerminal.terminalName } : undefined}
      />

      <FareRuleModal
        isOpen={isFareRuleModalOpen}
        onClose={() => { setIsFareRuleModalOpen(false); setEditingFareRule(null) }}
        onSubmit={editingFareRule ? handleEditFareRule : handleAddFareRule}
        terminals={terminals}
        loading={fareRuleLoading}
        initialData={editingFareRule ? {
          originTerminalId: 0,
          destinationTerminalId: 0,
          fareAmount: editingFareRule.fareAmount,
          effectiveDate: editingFareRule.effectiveDate,
        } : undefined}
      />

      <DiscountTypeModal
        isOpen={isDiscountTypeModalOpen}
        onClose={() => setIsDiscountTypeModalOpen(false)}
        onSubmit={async (data) => {
          try {
            await adminService.createDiscountType(data)
            success('Discount type created successfully!')
            setIsDiscountTypeModalOpen(false)
          } catch (err) {
            showError(err instanceof Error ? err.message : 'Failed to create discount type')
          }
        }}
      />

      {/* Toast Notifications */}
      <ToastContainer toasts={toasts} onRemove={removeToast} />
    </div>
  )
}