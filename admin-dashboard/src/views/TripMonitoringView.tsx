import { useState, useEffect } from 'react'
import { Activity, MapPin, Users, DollarSign, Clock, RefreshCw } from 'lucide-react'
import { adminService } from '../lib/admin'
import { Btn, Chip } from '../AdminApp'
import type { Trip } from '../lib/admin'

export function TripMonitoringView() {
  const [activeTrips, setActiveTrips] = useState<Trip[]>([])
  const [loading, setLoading] = useState(true)
  const [autoRefresh, setAutoRefresh] = useState(true)

  useEffect(() => {
    loadActiveTrips()
  }, [])

  useEffect(() => {
    if (!autoRefresh) return
    const interval = setInterval(loadActiveTrips, 30000) // Refresh every 30 seconds
    return () => clearInterval(interval)
  }, [autoRefresh])

  const loadActiveTrips = async () => {
    setLoading(true)
    try {
      const result = await adminService.getTrips(1, 100)
      const active = result.data.filter(trip => trip.tripStatus === 'Active')
      setActiveTrips(active)
    } catch (err) {
      console.error('Failed to load active trips:', err)
    } finally {
      setLoading(false)
    }
  }

  const totalPassengers = activeTrips.reduce((sum, trip) => sum + trip.passengerCount, 0)
  const totalRevenue = activeTrips.reduce((sum, trip) => sum + trip.totalRevenue, 0)

  return (
    <div className="flex flex-col gap-4">
      {/* Header with stats */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
        <div className="bg-white rounded-2xl p-5 shadow-sm border border-slate-100">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-2xl bg-blue-50 flex items-center justify-center">
              <Activity size={20} className="text-blue-600" />
            </div>
            <div>
              <p className="text-xs text-slate-500 font-medium">Active Trips</p>
              <p className="font-poppins text-2xl font-bold text-slate-800">{activeTrips.length}</p>
            </div>
          </div>
        </div>
        <div className="bg-white rounded-2xl p-5 shadow-sm border border-slate-100">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-2xl bg-green-50 flex items-center justify-center">
              <Users size={20} className="text-green-600" />
            </div>
            <div>
              <p className="text-xs text-slate-500 font-medium">Total Passengers</p>
              <p className="font-poppins text-2xl font-bold text-slate-800">{totalPassengers}</p>
            </div>
          </div>
        </div>
        <div className="bg-white rounded-2xl p-5 shadow-sm border border-slate-100">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-2xl bg-purple-50 flex items-center justify-center">
              <DollarSign size={20} className="text-purple-600" />
            </div>
            <div>
              <p className="text-xs text-slate-500 font-medium">Total Revenue</p>
              <p className="font-poppins text-2xl font-bold text-slate-800">₱{totalRevenue.toFixed(2)}</p>
            </div>
          </div>
        </div>
      </div>

      {/* Controls */}
      <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 flex justify-between items-center">
        <div className="flex items-center gap-2">
          <Activity size={18} className="text-green-600" />
          <p className="font-poppins font-bold text-slate-800">Live Trip Monitoring</p>
          {autoRefresh && (
            <span className="flex items-center gap-1 text-xs text-green-600 font-semibold">
              <span className="w-2 h-2 rounded-full bg-green-600 animate-pulse" />
              Live
            </span>
          )}
        </div>
        <div className="flex gap-2">
          <button
            onClick={() => setAutoRefresh(!autoRefresh)}
            className={`px-4 py-2 rounded-xl text-sm font-semibold transition-all flex items-center gap-2 ${
              autoRefresh 
                ? 'bg-green-50 text-green-700 border border-green-200' 
                : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
            }`}>
            <RefreshCw size={14} className={autoRefresh ? 'animate-spin' : ''} />
            {autoRefresh ? 'Auto-Refresh ON' : 'Auto-Refresh OFF'}
          </button>
          <Btn variant="primary" size="md" onClick={loadActiveTrips}>
            <RefreshCw size={14} /> Refresh
          </Btn>
        </div>
      </div>

      {/* Active Trips Grid */}
      {loading ? (
        <div className="bg-white rounded-2xl p-8 text-center text-slate-500">Loading active trips...</div>
      ) : activeTrips.length === 0 ? (
        <div className="bg-white rounded-2xl p-8 text-center text-slate-500">
          <Activity size={48} className="mx-auto mb-3 text-slate-300" />
          <p className="font-semibold">No active trips</p>
          <p className="text-sm mt-1">All trips are currently completed or cancelled</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {activeTrips.map(trip => (
            <div key={trip.tripId} className="bg-white rounded-2xl p-5 shadow-sm border border-slate-100 card-hover">
              <div className="flex items-start justify-between mb-4">
                <div className="flex items-center gap-2">
                  <div className="w-10 h-10 rounded-2xl bg-blue-600 flex items-center justify-center">
                    <span className="text-sm font-bold text-white">{trip.driverName[0]}</span>
                  </div>
                  <div>
                    <p className="font-semibold text-slate-800 text-sm">{trip.driverName}</p>
                    <p className="text-xs text-slate-400 font-mono">TRP-{trip.tripId.toString().padStart(6, '0')}</p>
                  </div>
                </div>
                <Chip label="Active" variant="success" />
              </div>

              <div className="flex flex-col gap-3">
                <div className="flex items-start gap-2">
                  <MapPin size={14} className="text-slate-400 mt-0.5 shrink-0" />
                  <div className="flex-1">
                    <p className="text-xs text-slate-500">Route</p>
                    <p className="text-sm font-medium text-slate-800">{trip.routeName}</p>
                  </div>
                </div>

                <div className="flex items-center gap-2">
                  <Clock size={14} className="text-slate-400 shrink-0" />
                  <div className="flex-1">
                    <p className="text-xs text-slate-500">Started</p>
                    <p className="text-sm font-medium text-slate-800">
                      {trip.startedAt ? new Date(trip.startedAt).toLocaleTimeString('en-US', { 
                        hour: '2-digit', 
                        minute: '2-digit' 
                      }) : '-'}
                    </p>
                  </div>
                </div>

                <div className="pt-3 border-t border-slate-100 grid grid-cols-2 gap-3">
                  <div>
                    <p className="text-xs text-slate-500 mb-1">Passengers</p>
                    <div className="flex items-center gap-1">
                      <Users size={14} className="text-blue-600" />
                      <p className="font-poppins font-bold text-slate-800">{trip.passengerCount}</p>
                    </div>
                  </div>
                  <div>
                    <p className="text-xs text-slate-500 mb-1">Revenue</p>
                    <div className="flex items-center gap-1">
                      <DollarSign size={14} className="text-green-600" />
                      <p className="font-poppins font-bold text-slate-800">₱{trip.totalRevenue.toFixed(2)}</p>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}