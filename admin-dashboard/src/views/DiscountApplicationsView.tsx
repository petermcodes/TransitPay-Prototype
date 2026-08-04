import { useState, useEffect } from 'react'
import { Search, Eye, CheckCircle, XCircle, AlertCircle, FileText } from 'lucide-react'
import { adminService } from '../lib/admin'
import { Btn, Chip } from '../AdminApp'
import type { DiscountApplication } from '../lib/admin'

type TabView = 'pending' | 'all'

export function DiscountApplicationsView() {
  const [activeTab, setActiveTab] = useState<TabView>('pending')
  const [pendingApplications, setPendingApplications] = useState<DiscountApplication[]>([])
  const [allApplications, setAllApplications] = useState<DiscountApplication[]>([])
  const [loading, setLoading] = useState(true)
  const [search, setSearch] = useState('')

  useEffect(() => {
    loadApplications()
  }, [activeTab])

  const loadApplications = async () => {
    setLoading(true)
    try {
      if (activeTab === 'pending') {
        const data = await adminService.getPendingApplications()
        setPendingApplications(data)
      } else {
        const result = await adminService.getAllApplications(1, 50)
        setAllApplications(result.data)
      }
    } catch (err) {
      console.error('Failed to load applications:', err)
    } finally {
      setLoading(false)
    }
  }

  const handleApprove = async (applicationId: number) => {
    if (!confirm('Are you sure you want to approve this discount application?')) return
    try {
      await adminService.approveApplication(applicationId)
      alert('Application approved successfully')
      loadApplications()
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Failed to approve application')
    }
  }

  const handleReject = async (applicationId: number) => {
    const reason = prompt('Please provide a reason for rejection:')
    if (reason === null) return // User cancelled
    try {
      await adminService.rejectApplication(applicationId, reason || undefined)
      alert('Application rejected successfully')
      loadApplications()
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Failed to reject application')
    }
  }

  const currentApplications = activeTab === 'pending' ? pendingApplications : allApplications

  const filteredApplications = currentApplications.filter(app => {
    const matchesSearch = app.cardNumber.toLowerCase().includes(search.toLowerCase()) ||
                         app.discountTypeName.toLowerCase().includes(search.toLowerCase())
    return matchesSearch
  })

  const getStatusVariant = (status: string) => {
    switch (status) {
      case 'Approved': return 'success'
      case 'Pending': return 'warning'
      case 'Rejected': return 'danger'
      case 'Expired': return 'default'
      default: return 'default'
    }
  }

  return (
    <div className="flex flex-col gap-4">
      {/* Tabs */}
      <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100">
        <div className="flex gap-2">
          <button
            onClick={() => setActiveTab('pending')}
            className={`px-4 py-2 rounded-xl text-sm font-semibold transition-all flex items-center gap-2 ${activeTab === 'pending' ? 'bg-blue-600 text-white shadow-sm' : 'text-slate-600 bg-slate-100 hover:bg-slate-200'}`}>
            <AlertCircle size={16} />
            Pending Approvals
            {pendingApplications.length > 0 && (
              <span className={`px-2 py-0.5 rounded-full text-xs font-bold ${activeTab === 'pending' ? 'bg-white/30 text-white' : 'bg-red-100 text-red-600'}`}>
                {pendingApplications.length}
              </span>
            )}
          </button>
          <button
            onClick={() => setActiveTab('all')}
            className={`px-4 py-2 rounded-xl text-sm font-semibold transition-all ${activeTab === 'all' ? 'bg-blue-600 text-white shadow-sm' : 'text-slate-600 bg-slate-100 hover:bg-slate-200'}`}>
            All Applications
          </button>
        </div>
      </div>

      {/* Controls */}
      {activeTab === 'all' && (
        <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 flex justify-end">
          <div className="relative">
            <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
            <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by card number or discount type..."
              className="pl-9 pr-4 py-2 text-sm rounded-xl border border-slate-200 w-64 focus:outline-none focus:border-blue-400 transition-all" />
          </div>
        </div>
      )}

      {/* Table */}
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        {loading ? (
          <div className="p-8 text-center text-slate-500">Loading applications...</div>
        ) : filteredApplications.length === 0 ? (
          <div className="p-8 text-center text-slate-500">
            {activeTab === 'pending' ? 'No pending applications' : 'No applications found'}
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead><tr className="border-b border-slate-100 bg-slate-50">
                {['Application ID', 'Card Number', 'Discount Type', 'Discount %', 'Document', 'Status', 'Submitted', 'Actions'].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider whitespace-nowrap">{h}</th>
                ))}
              </tr></thead>
              <tbody>
                {filteredApplications.map((app, i, arr) => (
                  <tr key={app.discountApplicationId} className={`border-b border-slate-100 hover:bg-blue-50/40 transition-colors ${i === arr.length - 1 ? 'border-0' : ''}`}>
                    <td className="px-4 py-3.5 font-mono text-xs text-blue-600 font-semibold">
                      APP-{app.discountApplicationId.toString().padStart(6, '0')}
                    </td>
                    <td className="px-4 py-3.5 font-mono text-xs text-slate-600">{app.cardNumber}</td>
                    <td className="px-4 py-3.5 font-medium text-slate-800">{app.discountTypeName}</td>
                    <td className="px-4 py-3.5">
                      <span className="font-poppins font-bold text-slate-800">
                        {app.discountPercentage !== null ? `${app.discountPercentage}%` : '-'}
                      </span>
                    </td>
                    <td className="px-4 py-3.5">
                      {app.discountDocument ? (
                        <button className="flex items-center gap-1 text-blue-600 hover:text-blue-700 transition-colors">
                          <FileText size={14} />
                          <span className="text-xs">View</span>
                        </button>
                      ) : (
                        <span className="text-xs text-slate-400">No document</span>
                      )}
                    </td>
                    <td className="px-4 py-3.5 whitespace-nowrap">
                      <Chip label={app.status} variant={getStatusVariant(app.status) as any} />
                    </td>
                    <td className="px-4 py-3.5 text-slate-400 text-xs whitespace-nowrap">
                      {new Date(app.createdAt).toLocaleDateString('en-US', { 
                        month: 'short', 
                        day: 'numeric', 
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit'
                      })}
                    </td>
                    <td className="px-4 py-3.5">
                      <div className="flex items-center gap-1">
                        <button className="p-1.5 rounded-lg hover:bg-blue-50 text-slate-400 hover:text-blue-600 transition-colors">
                          <Eye size={14} />
                        </button>
                        {app.status === 'Pending' && (
                          <>
                            <button 
                              onClick={() => handleApprove(app.discountApplicationId)}
                              className="p-1.5 rounded-lg hover:bg-green-50 text-slate-400 hover:text-green-600 transition-colors"
                              title="Approve">
                              <CheckCircle size={14} />
                            </button>
                            <button 
                              onClick={() => handleReject(app.discountApplicationId)}
                              className="p-1.5 rounded-lg hover:bg-red-50 text-slate-400 hover:text-red-500 transition-colors"
                              title="Reject">
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
    </div>
  )
}