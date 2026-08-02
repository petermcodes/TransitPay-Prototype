import PassengerApp from './PassengerApp'

function PhoneFrame({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen flex flex-col items-center justify-center px-4 py-8"
      style={{ background: 'linear-gradient(135deg, #1565C0 0%, #1976D2 40%, #0288D1 100%)' }}>
      {/* Phone shell */}
      <div className="relative w-[390px] bg-slate-800 rounded-[48px] shadow-2xl overflow-hidden"
        style={{ height: '844px', padding: '10px', boxShadow: '0 30px 80px rgba(0,0,0,0.35), 0 0 0 1px rgba(255,255,255,0.08)' }}>
        {/* Screen */}
        <div className="relative w-full h-full rounded-[40px] overflow-hidden bg-[#F0F4FF] flex flex-col"
          style={{ boxShadow: 'inset 0 0 0 1px rgba(255,255,255,0.1)' }}>
          {/* Status bar */}
          <div className="shrink-0 h-11 bg-transparent flex items-center justify-between px-7 relative z-50"
            style={{ background: 'rgba(0,0,0,0)' }}>
            <span className="text-[11px] font-bold text-slate-800" style={{ fontFamily: 'system-ui' }}>9:41</span>
            <div className="absolute left-1/2 -translate-x-1/2 top-1 w-28 h-7 rounded-full bg-black" />
            <div className="flex items-center gap-1">
              <div className="flex gap-0.5 items-end">
                {[2,3,4,5].map(h => <div key={h} className="w-1 rounded-sm bg-slate-800" style={{ height: h }} />)}
              </div>
              <div className="w-6 h-3 rounded-sm border border-slate-800 flex items-center px-0.5">
                <div className="h-2 bg-slate-800 rounded-sm" style={{ width: '75%' }} />
              </div>
            </div>
          </div>
          {/* App content */}
          <div className="flex-1 overflow-hidden flex flex-col">
            {children}
          </div>
          {/* Home indicator */}
          <div className="absolute bottom-2 left-1/2 -translate-x-1/2 w-28 h-1 rounded-full bg-black/20" />
        </div>
        {/* Side buttons */}
        <div className="absolute right-[-3px] top-24 w-[3px] h-12 rounded-r bg-slate-600" />
        <div className="absolute right-[-3px] top-44 w-[3px] h-8 rounded-r bg-slate-600" />
        <div className="absolute left-[-3px] top-20 w-[3px] h-8 rounded-l bg-slate-600" />
        <div className="absolute left-[-3px] top-32 w-[3px] h-14 rounded-l bg-slate-600" />
        <div className="absolute left-[-3px] top-50 w-[3px] h-14 rounded-l bg-slate-600" />
      </div>
      <p className="text-white/40 text-xs mt-6">TransitPay · Capstone Project · 2026</p>
    </div>
  )
}

export default function App() {
  return (
    <PhoneFrame>
      <PassengerApp />
    </PhoneFrame>
  )
}