import { useState } from 'react'
import {
  LayoutDashboard, Users, Bus, MapPin, Map,
  Grid3X3, CreditCard, BarChart3, Settings,
  Search, Plus, Edit2, Trash2, CheckCircle,
  XCircle, AlertCircle, TrendingUp, TrendingDown,
  DollarSign, ArrowDownLeft,
  Bell, LogOut, Menu, X, Eye, Download
} from 'lucide-react'

type AdminSection =
  | 'dashboard' | 'users' | 'drivers' | 'stations' | 'towns'
  | 'fare-matrix' | 'transactions' | 'reports' | 'settings'

// ── Shared ─────────────────────────────────────────────────────────────────────

function Chip({ label, variant = 'default' }: { label: string; variant?: 'success' | 'warning' | 'danger' | 'info' | 'default' }) {
  const map = {
    success: 'bg-green-50 text-green-700 border border-green-200',
    warning: 'bg-yellow-50 text-yellow-700 border border-yellow-200',
    danger: 'bg-red-50 text-red-700 border border-red-200',
    info: 'bg-blue-50 text-blue-700 border border-blue-200',
    default: 'bg-slate-100 text-slate-600',
  }
  return <span className={`chip ${map[variant]}`}>{label}</span>
}

function Btn({ children, variant = 'primary', size = 'md', onClick, className = '' }: {
  children: React.ReactNode; variant?: 'primary' | 'secondary' | 'ghost' | 'danger'
  size?: 'sm' | 'md' | 'lg'; onClick?: () => void; className?: string
}) {
  const sizes = { sm: 'px-3 py-1.5 text-xs', md: 'px-4 py-2 text-sm', lg: 'px-5 py-2.5 text-sm' }
  const variants = {
    primary: 'bg-blue-gradient text-white shadow-sm hover:shadow-md hover:brightness-105',
    secondary: 'bg-white text-blue-700 border-2 border-blue-600 hover:bg-blue-50',
    ghost: 'text-blue-700 hover:bg-blue-50 border border-transparent',
    danger: 'bg-red-500 text-white hover:bg-red-600',
  }
  return (
    <button onClick={onClick}
      className={`inline-flex items-center gap-1.5 font-semibold rounded-xl transition-all font-poppins ${sizes[size]} ${variants[variant]} ${className}`}>
      {children}
    </button>
  )
}

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

const navItems: { id: AdminSection; label: string; icon: React.ElementType; badge?: number }[] = [
  { id: 'dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { id: 'users', label: 'Passengers', icon: Users, badge: 12 },
  { id: 'drivers', label: 'Drivers', icon: Bus, badge: 3 },
  { id: 'stations', label: 'Stations', icon: MapPin },
  { id: 'towns', label: 'Towns', icon: Map },
  { id: 'fare-matrix', label: 'Fare Matrix', icon: Grid3X3 },
  { id: 'transactions', label: 'Transactions', icon: CreditCard },
  { id: 'reports', label: 'Reports', icon: BarChart3 },
  { id: 'settings', label: 'Settings', icon: Settings },
]

function Sidebar({ active, setActive, open, setOpen }: {
  active: AdminSection; setActive: (s: AdminSection) => void; open: boolean; setOpen: (v: boolean) => void
}) {
  return (
    <>
      {/* Overlay */}
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
              {item.badge && (
                <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold ${active === item.id ? 'bg-white/30 text-white' : 'bg-blue-100 text-blue-600'}`}>
                  {item.badge}
                </span>
              )}
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
              <span className="font-poppins text-sm font-bold text-white">AD</span>
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-semibold text-slate-800">Admin User</p>
              <p className="text-xs text-slate-400 truncate">admin@transitpay.ph</p>
            </div>
            <button className="text-slate-400 hover:text-red-500 transition-colors"><LogOut size={16} /></button>
          </div>
        </div>
      </aside>
    </>
  )
}

// ── TOPBAR ────────────────────────────────────────────────────────────────────

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
          <span className="absolute top-1.5 right-1.5 w-2 h-2 rounded-full bg-red-500" />
        </button>
        <div className="w-9 h-9 rounded-xl bg-blue-600 flex items-center justify-center">
          <span className="font-poppins text-xs font-bold text-white">AD</span>
        </div>
      </div>
    </header>
  )
}

// ── DASHBOARD ─────────────────────────────────────────────────────────────────

function RevenueChart() {
  const days = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']
  const values = [8200, 9400, 7800, 11200, 10800, 13400, 12100]
  const max = Math.max(...values)

  return (
    <div className="bg-white rounded-2xl p-5 shadow-sm border border-slate-100">
      <div className="flex items-center justify-between mb-5">
        <div>
          <p className="font-poppins font-bold text-slate-800">Revenue Overview</p>
          <p className="text-xs text-slate-500 mt-0.5">This week's fare collection</p>
        </div>
        <div className="flex gap-1">
          {['Week', 'Month', 'Year'].map((t, i) => (
            <button key={t} className={`px-3 py-1 rounded-lg text-xs font-semibold transition-all ${i === 0 ? 'bg-blue-600 text-white shadow-sm' : 'text-slate-500 hover:bg-slate-100'}`}>{t}</button>
          ))}
        </div>
      </div>
      <div className="flex items-end gap-2 h-40">
        {values.map((v, i) => (
          <div key={i} className="flex-1 flex flex-col items-center gap-1.5">
            <span className="text-[10px] text-slate-400">₱{(v / 1000).toFixed(1)}k</span>
            <div className="w-full rounded-t-lg transition-all hover:opacity-80" style={{
              height: `${(v / max) * 100}%`,
              background: i === 5 ? 'linear-gradient(135deg, #1565C0, #2196F3)' : '#DBEAFE',
            }} />
            <span className="text-[10px] text-slate-500">{days[i]}</span>
          </div>
        ))}
      </div>
      <div className="mt-4 pt-4 border-t border-slate-100 grid grid-cols-3 gap-4">
        {[['Total', '₱72,900'], ['Average', '₱10,414'], ['Best Day', 'Sat ₱13,400']].map(([k, v]) => (
          <div key={k}>
            <p className="text-xs text-slate-400">{k}</p>
            <p className="font-poppins font-bold text-slate-800 text-sm mt-0.5">{v}</p>
          </div>
        ))}
      </div>
    </div>
  )
}

const recentTx = [
  { id: 'TX-8821', passenger: 'Juan Dela Cruz', type: 'Fare', amount: 23, status: 'completed', date: 'Aug 2, 10:14 AM' },
  { id: 'TX-8820', passenger: 'Maria Santos', type: 'Top Up', amount: 500, status: 'completed', date: 'Aug 2, 10:02 AM' },
  { id: 'TX-8819', passenger: 'Pedro Reyes', type: 'Fare', amount: 18, status: 'completed', date: 'Aug 2, 09:55 AM' },
  { id: 'TX-8818', passenger: 'Anna Cruz', type: 'Refund', amount: 23, status: 'completed', date: 'Aug 2, 09:40 AM' },
  { id: 'TX-8817', passenger: 'Carlo Tan', type: 'Fare', amount: 28, status: 'pending', date: 'Aug 2, 09:30 AM' },
]

function DashboardView() {
  return (
    <div className="flex flex-col gap-5">
      {/* KPIs */}
      <div className="grid grid-cols-2 lg:grid-cols-3 xl:grid-cols-6 gap-3">
        <KpiCard icon={Users} label="Total Passengers" value="18,421" sub="Registered" trend="+142" color="blue" />
        <KpiCard icon={Bus} label="Total Drivers" value="284" sub="Active drivers" trend="+8" color="green" />
        <KpiCard icon={MapPin} label="Active Stations" value="47" sub="Across 7 towns" color="orange" />
        <KpiCard icon={DollarSign} label="Daily Revenue" value="₱12,845" sub="Aug 2, 2026" trend="+12%" color="green" />
        <KpiCard icon={TrendingUp} label="Monthly Revenue" value="₱284,920" sub="August 2026" trend="+8%" color="purple" />
        <KpiCard icon={CreditCard} label="Transactions" value="1,042" sub="Today" trend="+5%" color="blue" />
      </div>

      {/* Chart + stats */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-5">
        <div className="xl:col-span-2"><RevenueChart /></div>
        <div className="flex flex-col gap-3">
          <div className="bg-blue-gradient rounded-2xl p-5 text-white">
            <p className="text-blue-100 text-xs font-semibold uppercase tracking-wider mb-1">Today's Collection</p>
            <p className="font-poppins text-3xl font-bold">₱12,845</p>
            <p className="text-blue-200 text-sm mt-1">1,042 transactions</p>
            <div className="mt-3 pt-3 border-t border-white/20 flex justify-between">
              <div><p className="text-blue-100 text-xs">Avg Fare</p><p className="font-poppins font-bold">₱23.40</p></div>
              <div><p className="text-blue-100 text-xs">Peak Hour</p><p className="font-poppins font-bold">8–9 AM</p></div>
              <div><p className="text-blue-100 text-xs">Top Route</p><p className="font-poppins font-bold">R-42</p></div>
            </div>
          </div>
          {[['Pending Approvals', '3 drivers', 'warning'], ['Failed Transactions', '12 today', 'danger'], ['Active Routes', '18 of 22', 'success']].map(([k, v, c]) => (
            <div key={k} className="bg-white rounded-2xl p-4 flex items-center justify-between shadow-sm border border-slate-100">
              <div>
                <p className="text-sm font-semibold text-slate-700">{k}</p>
                <p className="text-xs text-slate-400">{v}</p>
              </div>
              <Chip label={c === 'warning' ? 'Review' : c === 'danger' ? 'Alert' : 'Normal'} variant={c as any} />
            </div>
          ))}
        </div>
      </div>

      {/* Recent transactions */}
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="px-5 py-4 border-b border-slate-100 flex items-center justify-between">
          <p className="font-poppins font-bold text-slate-800">Recent Transactions</p>
          <Btn variant="ghost" size="sm"><Eye size={13} /> View All</Btn>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead><tr className="border-b border-slate-100 bg-slate-50">
              {['Transaction ID', 'Passenger', 'Type', 'Amount', 'Status', 'Date'].map(h => (
                <th key={h} className="px-5 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider whitespace-nowrap">{h}</th>
              ))}
            </tr></thead>
            <tbody>
              {recentTx.map((t, i) => (
                <tr key={t.id} className={`border-b border-slate-100 hover:bg-blue-50/40 transition-colors ${i === recentTx.length - 1 ? 'border-0' : ''}`}>
                  <td className="px-5 py-3.5 font-mono text-xs text-blue-600 font-semibold whitespace-nowrap">{t.id}</td>
                  <td className="px-5 py-3.5 font-medium text-slate-800 whitespace-nowrap">{t.passenger}</td>
                  <td className="px-5 py-3.5 whitespace-nowrap">
                    <Chip label={t.type} variant={t.type === 'Fare' ? 'info' : t.type === 'Top Up' ? 'success' : 'warning'} />
                  </td>
                  <td className={`px-5 py-3.5 font-bold whitespace-nowrap ${t.type === 'Fare' ? 'text-slate-800' : 'text-green-600'}`}>
                    {t.type === 'Fare' ? '-' : '+'}₱{t.amount}
                  </td>
                  <td className="px-5 py-3.5 whitespace-nowrap">
                    <Chip label={t.status} variant={t.status === 'completed' ? 'success' : 'warning'} />
                  </td>
                  <td className="px-5 py-3.5 text-slate-400 text-xs whitespace-nowrap">{t.date}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}

// ── USERS ─────────────────────────────────────────────────────────────────────

const passengers = [
  { id: 'USR-4821', name: 'Juan Dela Cruz', mobile: '09171234567', balance: 476.50, trips: 312, status: 'active', joined: 'Mar 2025' },
  { id: 'USR-4822', name: 'Maria Santos', mobile: '09281234567', balance: 210.00, trips: 88, status: 'active', joined: 'Apr 2025' },
  { id: 'USR-4823', name: 'Pedro Reyes', mobile: '09091234567', balance: 55.25, trips: 420, status: 'active', joined: 'Jan 2025' },
  { id: 'USR-4824', name: 'Anna Cruz', mobile: '09181234567', balance: 890.75, trips: 156, status: 'suspended', joined: 'Jun 2025' },
  { id: 'USR-4825', name: 'Carlo Tan', mobile: '09271234567', balance: 123.00, trips: 67, status: 'active', joined: 'Jul 2025' },
]

function UsersView() {
  const [search, setSearch] = useState('')
  const [filter, setFilter] = useState('all')

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
            <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search users..."
              className="pl-9 pr-4 py-2 text-sm rounded-xl border border-slate-200 w-44 focus:outline-none focus:border-blue-400 transition-all" />
          </div>
          <Btn variant="primary" size="md"><Plus size={14} /> Add User</Btn>
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead><tr className="border-b border-slate-100 bg-slate-50">
              {['User ID', 'Name', 'Mobile', 'Balance', 'Trips', 'Status', 'Joined', 'Actions'].map(h => (
                <th key={h} className="px-4 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider whitespace-nowrap">{h}</th>
              ))}
            </tr></thead>
            <tbody>
              {passengers.filter(p => p.name.toLowerCase().includes(search.toLowerCase()) && (filter === 'all' || p.status === filter)).map((p, i, arr) => (
                <tr key={p.id} className={`border-b border-slate-100 hover:bg-blue-50/40 transition-colors ${i === arr.length - 1 ? 'border-0' : ''}`}>
                  <td className="px-4 py-3.5 font-mono text-xs text-blue-600 font-semibold">{p.id}</td>
                  <td className="px-4 py-3.5">
                    <div className="flex items-center gap-2.5">
                      <div className="w-8 h-8 rounded-xl bg-blue-600 flex items-center justify-center shrink-0">
                        <span className="text-xs font-bold text-white">{p.name[0]}</span>
                      </div>
                      <span className="font-medium text-slate-800 whitespace-nowrap">{p.name}</span>
                    </div>
                  </td>
                  <td className="px-4 py-3.5 text-slate-500 font-mono text-xs whitespace-nowrap">{p.mobile}</td>
                  <td className="px-4 py-3.5 font-semibold text-slate-800 whitespace-nowrap">₱{p.balance.toFixed(2)}</td>
                  <td className="px-4 py-3.5 text-slate-600">{p.trips}</td>
                  <td className="px-4 py-3.5 whitespace-nowrap">
                    <Chip label={p.status} variant={p.status === 'active' ? 'success' : 'danger'} />
                  </td>
                  <td className="px-4 py-3.5 text-slate-400 text-xs whitespace-nowrap">{p.joined}</td>
                  <td className="px-4 py-3.5">
                    <div className="flex items-center gap-1">
                      <button className="p-1.5 rounded-lg hover:bg-blue-50 text-slate-400 hover:text-blue-600 transition-colors"><Eye size={14} /></button>
                      <button className="p-1.5 rounded-lg hover:bg-blue-50 text-slate-400 hover:text-blue-600 transition-colors"><Edit2 size={14} /></button>
                      <button className={`p-1.5 rounded-lg transition-colors ${p.status === 'active' ? 'hover:bg-red-50 text-slate-400 hover:text-red-500' : 'hover:bg-green-50 text-slate-400 hover:text-green-600'}`}>
                        {p.status === 'active' ? <XCircle size={14} /> : <CheckCircle size={14} />}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}

// ── DRIVERS ───────────────────────────────────────────────────────────────────

const drivers = [
  { id: 'DRV-001', name: 'Pedro Santos', mobile: '09171111111', vehicle: 'Bus #42', plate: 'ABC-1234', trips: 1840, earnings: '₱42,800', status: 'active', approval: 'approved' },
  { id: 'DRV-002', name: 'Carlos Rivera', mobile: '09282222222', vehicle: 'Bus #07', plate: 'XYZ-5678', trips: 1102, earnings: '₱28,400', status: 'active', approval: 'approved' },
  { id: 'DRV-003', name: 'Jose Mendoza', mobile: '09093333333', vehicle: 'Bus #15', plate: 'DEF-9012', trips: 0, earnings: '₱0', status: 'pending', approval: 'pending' },
  { id: 'DRV-004', name: 'Rico Barretto', mobile: '09184444444', vehicle: 'Bus #31', plate: 'GHI-3456', trips: 2240, earnings: '₱58,200', status: 'active', approval: 'approved' },
  { id: 'DRV-005', name: 'Leo Villanueva', mobile: '09275555555', vehicle: 'N/A', plate: 'N/A', trips: 0, earnings: '₱0', status: 'pending', approval: 'pending' },
]

function DriversView() {
  return (
    <div className="flex flex-col gap-4">
      {/* Pending section */}
      <div className="bg-yellow-50 border border-yellow-200 rounded-2xl p-4">
        <div className="flex items-center gap-2 mb-3">
          <AlertCircle size={16} className="text-yellow-600" />
          <p className="font-poppins font-semibold text-slate-800 text-sm">Pending Driver Approvals (2)</p>
        </div>
        <div className="flex flex-col gap-2">
          {drivers.filter(d => d.approval === 'pending').map(d => (
            <div key={d.id} className="bg-white rounded-xl p-3.5 flex items-center gap-3 shadow-sm">
              <div className="w-10 h-10 rounded-xl bg-yellow-100 flex items-center justify-center shrink-0">
                <span className="font-bold text-yellow-700 text-sm">{d.name[0]}</span>
              </div>
              <div className="flex-1">
                <p className="font-semibold text-slate-800 text-sm">{d.name}</p>
                <p className="text-xs text-slate-400">{d.mobile}</p>
              </div>
              <div className="flex gap-2">
                <Btn variant="primary" size="sm"><CheckCircle size={13} /> Approve</Btn>
                <Btn variant="danger" size="sm"><XCircle size={13} /> Reject</Btn>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* All drivers table */}
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="px-5 py-4 border-b border-slate-100 flex items-center justify-between">
          <p className="font-poppins font-bold text-slate-800">All Drivers</p>
          <div className="flex gap-2">
            <div className="relative">
              <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
              <input placeholder="Search drivers..." className="pl-9 pr-4 py-1.5 text-sm rounded-xl border border-slate-200 w-40 focus:outline-none focus:border-blue-400" />
            </div>
            <Btn variant="primary" size="sm"><Plus size={13} /> Add Driver</Btn>
          </div>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead><tr className="border-b border-slate-100 bg-slate-50">
              {['Driver ID', 'Name', 'Vehicle', 'Plate No.', 'Total Trips', 'Earnings', 'Status', 'Actions'].map(h => (
                <th key={h} className="px-4 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider whitespace-nowrap">{h}</th>
              ))}
            </tr></thead>
            <tbody>
              {drivers.map((d, i, arr) => (
                <tr key={d.id} className={`border-b border-slate-100 hover:bg-blue-50/40 transition-colors ${i === arr.length - 1 ? 'border-0' : ''}`}>
                  <td className="px-4 py-3.5 font-mono text-xs text-blue-600 font-semibold">{d.id}</td>
                  <td className="px-4 py-3.5">
                    <div className="flex items-center gap-2.5">
                      <div className="w-8 h-8 rounded-xl bg-blue-600 flex items-center justify-center shrink-0">
                        <span className="text-xs font-bold text-white">{d.name[0]}</span>
                      </div>
                      <span className="font-medium text-slate-800 whitespace-nowrap">{d.name}</span>
                    </div>
                  </td>
                  <td className="px-4 py-3.5 text-slate-600 whitespace-nowrap">{d.vehicle}</td>
                  <td className="px-4 py-3.5 font-mono text-xs text-slate-600 whitespace-nowrap">{d.plate}</td>
                  <td className="px-4 py-3.5 text-slate-600">{d.trips}</td>
                  <td className="px-4 py-3.5 font-semibold text-slate-800 whitespace-nowrap">{d.earnings}</td>
                  <td className="px-4 py-3.5 whitespace-nowrap">
                    <Chip label={d.approval === 'pending' ? 'Pending' : d.status} variant={d.approval === 'pending' ? 'warning' : 'success'} />
                  </td>
                  <td className="px-4 py-3.5">
                    <div className="flex items-center gap-1">
                      <button className="p-1.5 rounded-lg hover:bg-blue-50 text-slate-400 hover:text-blue-600 transition-colors"><Eye size={14} /></button>
                      <button className="p-1.5 rounded-lg hover:bg-blue-50 text-slate-400 hover:text-blue-600 transition-colors"><Edit2 size={14} /></button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}

// ── TOWNS & STATIONS ──────────────────────────────────────────────────────────

const towns = [
  { id: 'TWN-01', name: 'Quezon City', stations: 4, status: 'active' },
  { id: 'TWN-02', name: 'Marikina', stations: 3, status: 'active' },
  { id: 'TWN-03', name: 'Pasig', stations: 3, status: 'active' },
  { id: 'TWN-04', name: 'Mandaluyong', stations: 3, status: 'active' },
  { id: 'TWN-05', name: 'Manila', stations: 4, status: 'active' },
  { id: 'TWN-06', name: 'Parañaque', stations: 3, status: 'inactive' },
]

function TownsView() {
  return (
    <div className="flex flex-col gap-4">
      <div className="flex justify-between items-center">
        <div />
        <Btn variant="primary"><Plus size={14} /> Add Town</Btn>
      </div>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
        {towns.map(t => (
          <div key={t.id} className="bg-white rounded-2xl p-5 shadow-sm border border-slate-100 card-hover">
            <div className="flex items-start justify-between mb-3">
              <div className="w-10 h-10 rounded-2xl bg-blue-50 flex items-center justify-center">
                <Map size={18} className="text-blue-600" />
              </div>
              <Chip label={t.status} variant={t.status === 'active' ? 'success' : 'default'} />
            </div>
            <p className="font-poppins font-bold text-slate-800">{t.name}</p>
            <p className="text-xs text-slate-400 mt-1">{t.id} · {t.stations} stations</p>
            <div className="flex gap-2 mt-4">
              <Btn variant="ghost" size="sm" className="flex-1"><Edit2 size={12} /> Edit</Btn>
              <Btn variant="danger" size="sm" className="flex-1"><Trash2 size={12} /> Delete</Btn>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

function StationsView() {
  const stations = [
    { id: 'STN-01', name: 'Cubao Station', town: 'Quezon City', status: 'active' },
    { id: 'STN-02', name: 'Commonwealth', town: 'Quezon City', status: 'active' },
    { id: 'STN-03', name: 'Fairview Terminal', town: 'Quezon City', status: 'active' },
    { id: 'STN-04', name: 'Marikina Station', town: 'Marikina', status: 'active' },
    { id: 'STN-05', name: 'Ortigas Station', town: 'Pasig', status: 'active' },
    { id: 'STN-06', name: 'Shaw Station', town: 'Pasig', status: 'inactive' },
  ]
  return (
    <div className="flex flex-col gap-4">
      <div className="flex justify-between items-center">
        <div className="relative">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
          <input placeholder="Search stations..." className="pl-9 pr-4 py-2 text-sm rounded-xl border border-slate-200 w-48 focus:outline-none focus:border-blue-400" />
        </div>
        <Btn variant="primary"><Plus size={14} /> Add Station</Btn>
      </div>
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <table className="w-full text-sm">
          <thead><tr className="border-b border-slate-100 bg-slate-50">
            {['Station ID', 'Station Name', 'Town', 'Status', 'Actions'].map(h => (
              <th key={h} className="px-5 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider">{h}</th>
            ))}
          </tr></thead>
          <tbody>
            {stations.map((s, i, arr) => (
              <tr key={s.id} className={`border-b border-slate-100 hover:bg-blue-50/40 transition-colors ${i === arr.length - 1 ? 'border-0' : ''}`}>
                <td className="px-5 py-3.5 font-mono text-xs text-blue-600 font-semibold">{s.id}</td>
                <td className="px-5 py-3.5 font-medium text-slate-800">{s.name}</td>
                <td className="px-5 py-3.5 text-slate-500">{s.town}</td>
                <td className="px-5 py-3.5"><Chip label={s.status} variant={s.status === 'active' ? 'success' : 'default'} /></td>
                <td className="px-5 py-3.5">
                  <div className="flex items-center gap-1">
                    <button className="p-1.5 rounded-lg hover:bg-blue-50 text-slate-400 hover:text-blue-600 transition-colors"><Edit2 size={14} /></button>
                    <button className="p-1.5 rounded-lg hover:bg-red-50 text-slate-400 hover:text-red-500 transition-colors"><Trash2 size={14} /></button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

// ── FARE MATRIX ───────────────────────────────────────────────────────────────

const fares = [
  { id: 'FM-01', origin: 'Cubao Station', dest: 'Ortigas Station', vehicle: 'Bus', type: 'Regular', amount: 23, effective: 'Jan 1, 2026', status: 'active' },
  { id: 'FM-02', origin: 'Cubao Station', dest: 'Ortigas Station', vehicle: 'Bus', type: 'Student', amount: 16, effective: 'Jan 1, 2026', status: 'active' },
  { id: 'FM-03', origin: 'Cubao Station', dest: 'Airport Link', vehicle: 'Bus', type: 'Regular', amount: 38, effective: 'Jan 1, 2026', status: 'active' },
  { id: 'FM-04', origin: 'Marikina Station', dest: 'Cubao Station', vehicle: 'Bus', type: 'Regular', amount: 18, effective: 'Jan 1, 2026', status: 'active' },
  { id: 'FM-05', origin: 'Lawton Station', dest: 'Shaw Station', vehicle: 'Bus', type: 'PWD', amount: 11, effective: 'Jan 1, 2026', status: 'active' },
]

function FareMatrixView() {
  return (
    <div className="flex flex-col gap-4">
      <div className="flex justify-end">
        <Btn variant="primary"><Plus size={14} /> Add Fare Rule</Btn>
      </div>
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead><tr className="border-b border-slate-100 bg-slate-50">
              {['ID', 'Origin Station', 'Destination', 'Vehicle', 'Passenger Type', 'Fare', 'Effective', 'Status', 'Actions'].map(h => (
                <th key={h} className="px-4 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider whitespace-nowrap">{h}</th>
              ))}
            </tr></thead>
            <tbody>
              {fares.map((f, i, arr) => (
                <tr key={f.id} className={`border-b border-slate-100 hover:bg-blue-50/40 transition-colors ${i === arr.length - 1 ? 'border-0' : ''}`}>
                  <td className="px-4 py-3.5 font-mono text-xs text-blue-600">{f.id}</td>
                  <td className="px-4 py-3.5 font-medium text-slate-800 whitespace-nowrap">{f.origin}</td>
                  <td className="px-4 py-3.5 text-slate-600 whitespace-nowrap">{f.dest}</td>
                  <td className="px-4 py-3.5 text-slate-600">{f.vehicle}</td>
                  <td className="px-4 py-3.5"><Chip label={f.type} variant={f.type === 'Regular' ? 'default' : f.type === 'Student' ? 'info' : 'success'} /></td>
                  <td className="px-4 py-3.5 font-poppins font-bold text-slate-800">₱{f.amount}.00</td>
                  <td className="px-4 py-3.5 text-slate-400 text-xs whitespace-nowrap">{f.effective}</td>
                  <td className="px-4 py-3.5"><Chip label={f.status} variant="success" /></td>
                  <td className="px-4 py-3.5">
                    <div className="flex gap-1">
                      <button className="p-1.5 rounded-lg hover:bg-blue-50 text-slate-400 hover:text-blue-600 transition-colors"><Edit2 size={14} /></button>
                      <button className="p-1.5 rounded-lg hover:bg-red-50 text-slate-400 hover:text-red-500 transition-colors"><XCircle size={14} /></button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}

// ── TRANSACTIONS ──────────────────────────────────────────────────────────────

const allTx = [
  { id: 'TX-8821', passenger: 'Juan Dela Cruz', type: 'Fare', amount: -23, status: 'completed', date: 'Aug 2, 10:14 AM' },
  { id: 'TX-8820', passenger: 'Maria Santos', type: 'Top Up', amount: 500, status: 'completed', date: 'Aug 2, 10:02 AM' },
  { id: 'TX-8819', passenger: 'Pedro Reyes', type: 'Fare', amount: -18, status: 'completed', date: 'Aug 2, 09:55 AM' },
  { id: 'TX-8818', passenger: 'Anna Cruz', type: 'Refund', amount: 23, status: 'completed', date: 'Aug 2, 09:40 AM' },
  { id: 'TX-8817', passenger: 'Carlo Tan', type: 'Fare', amount: -28, status: 'pending', date: 'Aug 2, 09:30 AM' },
  { id: 'TX-8816', passenger: 'Juan Dela Cruz', type: 'Top Up', amount: 300, status: 'completed', date: 'Aug 1, 08:10 PM' },
  { id: 'TX-8815', passenger: 'Maria Santos', type: 'Fare', amount: -13, status: 'failed', date: 'Aug 1, 07:55 PM' },
]

function TransactionsView() {
  const [typeFilter, setTypeFilter] = useState('all')
  const [statusFilter, setStatusFilter] = useState('all')

  const filtered = allTx.filter(t =>
    (typeFilter === 'all' || t.type.toLowerCase() === typeFilter) &&
    (statusFilter === 'all' || t.status === statusFilter)
  )

  return (
    <div className="flex flex-col gap-4">
      <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 flex flex-wrap gap-3 items-center justify-between">
        <div className="flex gap-2 flex-wrap">
          <div className="flex items-center gap-2">
            <span className="text-xs text-slate-500 font-semibold">Type:</span>
            {['all', 'fare', 'top up', 'refund'].map(f => (
              <button key={f} onClick={() => setTypeFilter(f)}
                className={`px-3 py-1.5 rounded-xl text-xs font-semibold transition-all ${typeFilter === f ? 'bg-blue-600 text-white' : 'text-slate-600 bg-slate-100 hover:bg-slate-200'}`}>
                {f.charAt(0).toUpperCase() + f.slice(1)}
              </button>
            ))}
          </div>
          <div className="flex items-center gap-2">
            <span className="text-xs text-slate-500 font-semibold">Status:</span>
            {['all', 'completed', 'pending', 'failed'].map(f => (
              <button key={f} onClick={() => setStatusFilter(f)}
                className={`px-3 py-1.5 rounded-xl text-xs font-semibold transition-all ${statusFilter === f ? 'bg-blue-600 text-white' : 'text-slate-600 bg-slate-100 hover:bg-slate-200'}`}>
                {f.charAt(0).toUpperCase() + f.slice(1)}
              </button>
            ))}
          </div>
        </div>
        <div className="relative">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
          <input placeholder="Search..." className="pl-9 pr-4 py-1.5 text-sm rounded-xl border border-slate-200 w-40 focus:outline-none focus:border-blue-400" />
        </div>
      </div>

      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead><tr className="border-b border-slate-100 bg-slate-50">
              {['TX ID', 'Passenger', 'Type', 'Amount', 'Status', 'Date', 'Actions'].map(h => (
                <th key={h} className="px-4 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider whitespace-nowrap">{h}</th>
              ))}
            </tr></thead>
            <tbody>
              {filtered.map((t, i, arr) => (
                <tr key={t.id} className={`border-b border-slate-100 hover:bg-blue-50/40 transition-colors ${i === arr.length - 1 ? 'border-0' : ''}`}>
                  <td className="px-4 py-3.5 font-mono text-xs text-blue-600 font-semibold">{t.id}</td>
                  <td className="px-4 py-3.5 font-medium text-slate-800 whitespace-nowrap">{t.passenger}</td>
                  <td className="px-4 py-3.5 whitespace-nowrap">
                    <Chip label={t.type} variant={t.type === 'Fare' ? 'info' : t.type === 'Top Up' ? 'success' : 'warning'} />
                  </td>
                  <td className={`px-4 py-3.5 font-bold whitespace-nowrap ${t.amount > 0 ? 'text-green-600' : 'text-slate-800'}`}>
                    {t.amount > 0 ? '+' : ''}₱{Math.abs(t.amount)}
                  </td>
                  <td className="px-4 py-3.5 whitespace-nowrap">
                    <Chip label={t.status} variant={t.status === 'completed' ? 'success' : t.status === 'pending' ? 'warning' : 'danger'} />
                  </td>
                  <td className="px-4 py-3.5 text-slate-400 text-xs whitespace-nowrap">{t.date}</td>
                  <td className="px-4 py-3.5">
                    <div className="flex items-center gap-1">
                      <button className="p-1.5 rounded-lg hover:bg-blue-50 text-slate-400 hover:text-blue-600 transition-colors"><Eye size={14} /></button>
                      {t.type === 'Fare' && t.status === 'completed' && (
                        <button className="p-1.5 rounded-lg hover:bg-orange-50 text-slate-400 hover:text-orange-500 transition-colors"><ArrowDownLeft size={14} /></button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}

// ── REPORTS ───────────────────────────────────────────────────────────────────

function ReportsView() {
  const [period, setPeriod] = useState('daily')

  const data = {
    daily: [{ date: 'Aug 2', revenue: 12845, trips: 1042 }, { date: 'Aug 1', revenue: 11200, trips: 962 }, { date: 'Jul 31', revenue: 13400, trips: 1124 }],
    weekly: [{ date: 'Week 31', revenue: 84200, trips: 7128 }, { date: 'Week 30', revenue: 78400, trips: 6640 }, { date: 'Week 29', revenue: 81900, trips: 6942 }],
    monthly: [{ date: 'August', revenue: 284920, trips: 24190 }, { date: 'July', revenue: 298400, trips: 25280 }, { date: 'June', revenue: 271000, trips: 22980 }],
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 flex items-center justify-between flex-wrap gap-3">
        <div className="flex gap-2">
          {['daily', 'weekly', 'monthly'].map(p => (
            <button key={p} onClick={() => setPeriod(p)}
              className={`px-4 py-2 rounded-xl text-sm font-semibold transition-all ${period === p ? 'bg-blue-600 text-white shadow-sm' : 'text-slate-600 bg-slate-100 hover:bg-slate-200'}`}>
              {p.charAt(0).toUpperCase() + p.slice(1)}
            </button>
          ))}
        </div>
        <div className="flex gap-2">
          <Btn variant="secondary" size="md"><Download size={14} /> Export PDF</Btn>
          <Btn variant="primary" size="md"><Download size={14} /> Export Excel</Btn>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-3">
        {[['Total Revenue', `₱${(data[period as keyof typeof data][0].revenue).toLocaleString()}`, '+12%', 'green'],
          ['Total Trips', data[period as keyof typeof data][0].trips.toLocaleString(), '+5%', 'blue'],
          ['Avg per Trip', `₱${(data[period as keyof typeof data][0].revenue / data[period as keyof typeof data][0].trips).toFixed(2)}`, '+7%', 'purple']].map(([k, v, t, c]) => (
          <div key={k} className="bg-white rounded-2xl p-5 shadow-sm border border-slate-100">
            <p className="text-sm text-slate-500 font-medium">{k}</p>
            <p className="font-poppins text-2xl font-bold text-slate-800 mt-1">{v}</p>
            <p className={`text-xs font-semibold mt-1 ${c === 'green' ? 'text-green-600' : c === 'blue' ? 'text-blue-600' : 'text-purple-600'}`}>{t} vs prev</p>
          </div>
        ))}
      </div>

      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="px-5 py-4 border-b border-slate-100">
          <p className="font-poppins font-bold text-slate-800">Revenue Report</p>
          <p className="text-xs text-slate-400 mt-0.5">{period.charAt(0).toUpperCase() + period.slice(1)} breakdown</p>
        </div>
        <table className="w-full text-sm">
          <thead><tr className="border-b border-slate-100 bg-slate-50">
            {['Period', 'Revenue', 'Trips', 'Avg Fare', 'Growth'].map(h => (
              <th key={h} className="px-5 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider">{h}</th>
            ))}
          </tr></thead>
          <tbody>
            {data[period as keyof typeof data].map((r, i, arr) => (
              <tr key={r.date} className={`border-b border-slate-100 hover:bg-blue-50/40 ${i === arr.length - 1 ? 'border-0' : ''}`}>
                <td className="px-5 py-4 font-semibold text-slate-800">{r.date}</td>
                <td className="px-5 py-4 font-poppins font-bold text-slate-800">₱{r.revenue.toLocaleString()}</td>
                <td className="px-5 py-4 text-slate-600">{r.trips.toLocaleString()}</td>
                <td className="px-5 py-4 text-slate-600">₱{(r.revenue / r.trips).toFixed(2)}</td>
                <td className="px-5 py-4"><Chip label="+12%" variant="success" /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

// ── SETTINGS ──────────────────────────────────────────────────────────────────

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

export default function AdminApp() {
  const [section, setSection] = useState<AdminSection>('dashboard')
  const [sidebarOpen, setSidebarOpen] = useState(false)

  return (
    <div className="flex h-full bg-[#F0F4FF] overflow-hidden">
      <Sidebar active={section} setActive={setSection} open={sidebarOpen} setOpen={setSidebarOpen} />

      <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
        <Topbar section={section} sidebarOpen={sidebarOpen} setSidebarOpen={setSidebarOpen} />

        <main className="flex-1 overflow-y-auto p-4 lg:p-6">
          {section === 'dashboard' && <DashboardView />}
          {section === 'users' && <UsersView />}
          {section === 'drivers' && <DriversView />}
          {section === 'stations' && <StationsView />}
          {section === 'towns' && <TownsView />}
          {section === 'fare-matrix' && <FareMatrixView />}
          {section === 'transactions' && <TransactionsView />}
          {section === 'reports' && <ReportsView />}
          {section === 'settings' && <SettingsView />}
        </main>
      </div>
    </div>
  )
}