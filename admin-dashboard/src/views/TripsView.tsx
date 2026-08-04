import { useState, useEffect } from 'react'
import { Search, Plus, Eye, Edit2, Trash2, CheckCircle, XCircle, AlertCircle, Bus } from 'lucide-react'
import { adminService } from '../lib/admin'
import { Btn, Chip } from '../AdminApp'
import type { Trip } from '../lib/admin'

type StatusFilter = 'all' | 'Pending' | 'Active' | 'Completed' | 'Cancelled'

export function TripsView({ onAddTrip }: { onAddTrip: () => void }) {
  const [trips, setTrips] = useState<Trip[]>([])
  const [loading, setLoading] = useState(true)
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all')
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)

  useEffect(() => {
    loadTrips()
  }, [page])

  const loadTrips = async () => {
    setLoading(true)
    try {
      const result = await adminService.getTrips(page, 20)
      setTrips(result.data)
      setTotalPages(result.pagination.totalPages)
    } catch (err) {
      console.error('Failed to load trips:', err)
    } finally {
      setLoading(false)
    }
  }

  const handleEndTrip = async (tripId: number) => {
    if (!confirm('Are you sure you want to end this trip?')) return
    try {
      await adminService.endTrip(tripId)
      alert('Trip ended successfully')
      loadTrips()
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Failed to end trip')
    }
  }

  const handleCancelTrip = async (tripId: number) => {
    if (!confirm('Are you sure you want to cancel this trip?')) return
    try {
      await adminService.cancelTrip(tripId)
      alert('Trip cancelled successfully')
      loadTrips()
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Failed to cancel trip')
    }
  }

  const filteredTrips = trips.filter(trip => {
    const matchesSearch = trip.driverName.toLowerCase().includes(search.toLowerCase()) ||
                         trip.routeName.toLowerCase().includes(search.toLowerCase()) ||
                         trip.tripId.toString().includes(search)
    const matchesStatus = statusFilter === 'all' || trip.tripStatus === statusFilter
    return matchesSearch && matchesStatus
  })

  const getStatusVariant = (status: string) => {
    switch (status) {
      case 'Active': return 'success'
      case 'Pending': return 'warning'
      case 'Completed': return 'info'
      case 'Cancelled': return 'danger'
      default: return 'default'
    }
  }

  return (
    <div className="flex flex-col gap-4">
      {/* Controls */}
      <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 flex flex-wrap gap-3 items-center justify-between">
        <div className="flex gap-2 flex-wrap">
          {(['all', 'Pending', 'Active', 'Completed', 'Cancelled'] as StatusFilter[]).map(status => (
            <button key={status} onClick={() => setStatusFilter(status)}
              className={`px-4 py-1.5 rounded-xl text-sm font-semibold transition-all ${statusFilter === status ? 'bg-blue-600 text-white shadow-sm' : 'text-slate-600 bg-slate-100 hover:bg-slate-200'}`}>
              {status.charAt(0).toUpperCase() + status.slice(1)}
            </button>
          ))}
        </div>
        <div className="flex gap-2 items-center">
          <div className="relative">
            <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
            <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search trips..."
              className="pl-9 pr-4 py-2 text-sm rounded-xl border border-slate-200 w-44 focus:outline-none focus:border-blue-400 transition-all" />
          </div>
          <Btn variant="primary" size="md" onClick={onAddTrip}><Plus size={14} /> Add Trip</Btn>
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        {loading ? (
          <div className="p-8 text-center text-slate-500">Loading trips...</div>
        ) : filteredTrips.length === 0 ? (
          <div className="p-8 text-center text-slate-500">No trips found</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead><tr className="border-b border-slate-100 bg-slate-50">
                {['Trip ID', 'Driver', 'Route', 'Status', 'Passengers', 'Revenue', 'Started', 'Actions'].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider whitespace-nowrap">{h}</th>
                ))}
              </tr></thead>
              <tbody>
                {filteredTrips.map((trip, i, arr) => (
                  <tr key={trip.tripId} className={`border-b border-slate-100 hover:bg-blue-50/40 transition-colors ${i === arr.length - 1 ? 'border-0' : ''}`}>
                    <td className="px-4 py-3.5 font-mono text-xs text-blue-600 font-semibold">TRP-{trip.tripId.toString().padStart(6, '0')}</td>
                    <td className="px-4 py-3.5">
                      <div className="flex items-center gap-2.5">
                        <div className="w-8 h-8 rounded-xl bg-blue-600 flex items-center justify-center shrink-0">
                          <span className="text-xs font-bold text-white">{trip.driverName[0]}</span>
                        </div>
                        <span className="font-medium text-slate-800 whitespace-nowrap">{trip.driverName}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3.5">
                      <div className="flex flex-col">
                        <span className="font-medium text-slate-800 text-xs">{trip.originStationName}</span>
                        <span className="text-slate-400 text-xs">→ {trip.finalDestinationStationName}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3.5 whitespace-nowrap">
                      <Chip label={trip.tripStatus} variant={getStatusVariant(trip.tripStatus) as any} />
                    </td>
                    <td className="px-4 py-3.5 text-slate-600 font-semibold">{trip.passengerCount}</td>
                    <td className="px-4 py-3.5 font-semibold text-slate-800">₱{trip.totalRevenue.toFixed(2)}</td>
                    <td className="px-4 py-3.5 text-slate-400 text-xs whitespace-nowrap">
                      {trip.startedAt ? new Date(trip.startedAt).toLocaleString('en-US', { 
                        month: 'short', 
                        day: 'numeric', 
                        hour: '2-digit', 
                        minute: '2-digit' 
                      }) : '-'}
                    </td>
                    <td className="px-4 py-3.5">
                      <div className="flex items-center gap-1">
                        <button className="p-1.5 rounded-lg hover:bg-blue-50 text-slate-400 hover:text-blue-600 transition-colors"><Eye size={14} /></button>
                        {(trip.tripStatus === 'Active' || trip.tripStatus === 'Pending') && (
                          <>
                            <button 
                              onClick={() => handleEndTrip(trip.tripId)}
                              className="p-1.5 rounded-lg hover:bg-green-50 text-slate-400 hover:text-green-600 transition-colors"
                              title="End Trip">
                              <CheckCircle size={14} />
                            </button>
                            <button 
                              onClick={() => handleCancelTrip(trip.tripId)}
                              className="p-1.5 rounded-lg hover:bg-red-50 text-slate-400 hover:text-red-500 transition-colors"
                              title="Cancel Trip">
                              <XCircle size={14} />
                            </button>
                          </>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between">
          <button 
            onClick={() => setPage(p => Math.max(1, p - 1))}
            disabled={page === 1}
            className="px-4 py-2 rounded-xl text-sm font-semibold bg-white border border-slate-200 hover:bg-slate-50 disabled:opacity-50 disabled:cursor-not-allowed">
            Previous
          </button>
          <span className="text-sm text-slate-600">Page {page} of {totalPages}</span>
          <button 
            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
            disabled={page === totalPages}
            className="px-4 py-2 rounded-xl text-sm font-semibold bg-white border border-slate-200 hover:bg-slate-50 disabled:opacity-50 disabled:cursor-not-allowed">
            Next
          </button>
        </div>
      )}
    </div>
  )
}