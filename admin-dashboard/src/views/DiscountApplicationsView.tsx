/**
 * Discount application review screen (admin approval workflow).
 *
 * Shows two tabs — Pending (awaiting review) and All — with search and an
 * expandable detail row per application. Admins can approve, reject (with a
 * reason) and preview/download the applicant's supporting document
 * (inline preview for images, file download otherwise).
 */
import { useState, useEffect, Fragment } from 'react'
import { Search, Eye, Check, X, AlertCircle, Download, ChevronDown, ChevronUp } from 'lucide-react'
import { adminService } from '../lib/admin'
import type { DiscountApplication } from '../lib/admin'

type TabView = 'pending' | 'all'

export function DiscountApplicationsView() {
  const [activeTab, setActiveTab] = useState<TabView>('pending')
  const [pendingApplications, setPendingApplications] = useState<DiscountApplication[]>([])
  const [allApplications, setAllApplications] = useState<DiscountApplication[]>([])
  const [loading, setLoading] = useState(true)
  const [search, setSearch] = useState('')
  const [previewDocument, setPreviewDocument] = useState<string | null>(null)
  const [expandedId, setExpandedId] = useState<number | null>(null)

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
        const data = await adminService.getAllApplications()
        setAllApplications(data)
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
      const matchesSearch = app.passengerName.toLowerCase().includes(search.toLowerCase()) ||
                         app.discountTypeName.toLowerCase().includes(search.toLowerCase())
    return matchesSearch
  })

  // Maps backend status to display label and colored dot
  // Backend serializes DiscountApplicationStatus enum as strings: 'Pending', 'Approved', 'Rejected', 'Expired'
  const getStatusInfo = (status: string): { label: string; color: string } => {
    switch (status) {
      case 'Pending': return { label: 'Pending', color: '#F59E0B' }      // 🟡 yellow
      case 'Approved': return { label: 'Active', color: '#10B981' }       // 🟢 green
      case 'Rejected': return { label: 'Rejected', color: '#EF4444' }     // 🔴 red
      case 'Expired': return { label: 'Expired', color: '#EF4444' }      // 🔴 red
      default: return { label: 'Unknown', color: '#94A3B8' }     // grey fallback
    }
  }

  const handleViewDocument = async (doc: string | null, applicationId: number) => {
    if (!doc) return
    
    try {
      const blob = await adminService.getApplicationDocument(applicationId)
      const url = URL.createObjectURL(blob)
      
      if (doc.startsWith('data:image')) {
        setPreviewDocument(url)
      } else {
        const a = document.createElement('a')
        a.href = url
        a.download = `document_${applicationId}.txt`
        document.body.appendChild(a)
        a.click()
        document.body.removeChild(a)
      }
      
      URL.revokeObjectURL(url)
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Failed to download document')
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
            <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by passenger name or discount type..."
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
                {['Application ID', 'Passenger Name', 'Discount Type', 'Discount %', 'Document', 'Status', 'Submitted', 'Actions'].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider whitespace-nowrap">{h}</th>
                ))}
              </tr></thead>
              <tbody>
                {filteredApplications.map((app, i, arr) => (
                  <Fragment key={app.discountApplicationId}>
                  <tr className={`border-b border-slate-100 hover:bg-blue-50/40 transition-colors ${i === arr.length - 1 ? 'border-0' : ''}`}>
                    <td className="px-4 py-3.5">
                      <button
                        onClick={() => setExpandedId(expandedId === app.discountApplicationId ? null : app.discountApplicationId)}
                        className="flex items-center gap-1 font-mono text-xs text-blue-600 font-semibold hover:text-blue-800 hover:underline transition-colors cursor-pointer"
                        title="Click to view full details">
                        APP-{app.discountApplicationId.toString().padStart(6, '0')}
                        {expandedId === app.discountApplicationId ? <ChevronUp size={12} /> : <ChevronDown size={12} />}
                      </button>
                    </td>
                    <td className="px-4 py-3.5 font-medium text-slate-800">{app.passengerName}</td>
                    <td className="px-4 py-3.5 font-medium text-slate-800">{app.discountTypeName}</td>
                    <td className="px-4 py-3.5">
                      <span className="font-poppins font-bold text-slate-800">
                        {app.discountPercentage !== null ? `${app.discountPercentage}%` : '-'}
                      </span>
                    </td>
                    <td className="px-4 py-3.5">
                      {app.discountDocument ? (
                        <div className="flex gap-1">
                          <button 
                            onClick={() => handleViewDocument(app.discountDocument, app.discountApplicationId)}
                            className="flex items-center gap-1 text-blue-600 hover:text-blue-700 transition-colors"
                            title="View Document">
                            <Eye size={14} />
                            <span className="text-xs">View</span>
                          </button>
                          <button 
                            onClick={() => handleViewDocument(app.discountDocument, app.discountApplicationId)}
                            className="flex items-center gap-1 text-green-600 hover:text-green-700 transition-colors"
                            title="Download Document">
                            <Download size={14} />
                          </button>
                        </div>
                      ) : (
                        <span className="text-xs text-slate-400">No document</span>
                      )}
                    </td>
                    <td className="px-4 py-3.5 whitespace-nowrap">
                      {(() => {
                        const info = getStatusInfo(app.status)
                        return (
                          <div className="flex items-center gap-2">
                            <span className="inline-block w-2.5 h-2.5 rounded-full" style={{ backgroundColor: info.color }} />
                            <span className={`text-xs font-semibold ${info.color === '#10B981' ? 'text-green-600' : info.color === '#F59E0B' ? 'text-yellow-600' : info.color === '#EF4444' ? 'text-red-500' : 'text-slate-500'}`}>
                              {info.label}
                            </span>
                          </div>
                        )
                      })()}
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
                        {app.status === 'Pending' ? (
                          <>
                            <button 
                              onClick={() => handleApprove(app.discountApplicationId)}
                              className="p-1.5 rounded-lg hover:bg-green-50 text-slate-400 hover:text-green-600 transition-colors"
                              title="Approve">
                              <Check size={14} />
                            </button>
                            <button 
                              onClick={() => handleReject(app.discountApplicationId)}
                              className="p-1.5 rounded-lg hover:bg-red-50 text-slate-400 hover:text-red-500 transition-colors"
                              title="Reject">
                              <X size={14} />
                            </button>
                          </>
                        ) : (
                          <span className="text-xs text-slate-300">—</span>
                        )}
                      </div>
                    </td>
                  </tr>
                  {expandedId === app.discountApplicationId && (
                    <tr className="bg-slate-50/80 border-b border-slate-100">
                      <td colSpan={8} className="px-4 py-4">
                        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4 text-sm">
                          <div>
                            <p className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-1">Masked Card</p>
                            <p className="font-medium text-slate-800">{app.maskedCardNumber || '-'}</p>
                          </div>
                          <div>
                            <p className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-1">User ID</p>
                            <p className="font-medium text-slate-800">{app.userId}</p>
                          </div>
                          <div>
                            <p className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-1">Discount Type ID</p>
                            <p className="font-medium text-slate-800">{app.discountTypeId}</p>
                          </div>
                          <div>
                            <p className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-1">Discount %</p>
                            <p className="font-medium text-slate-800">{app.discountPercentage !== null ? `${app.discountPercentage}%` : '-'}</p>
                          </div>
                          <div>
                            <p className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-1">Status</p>
                            {(() => {
                              const info = getStatusInfo(app.status)
                              return (
                                <div className="flex items-center gap-2">
                                  <span className="inline-block w-2.5 h-2.5 rounded-full" style={{ backgroundColor: info.color }} />
                                  <span className={`text-xs font-semibold ${info.color === '#10B981' ? 'text-green-600' : info.color === '#F59E0B' ? 'text-yellow-600' : info.color === '#EF4444' ? 'text-red-500' : 'text-slate-500'}`}>
                                    {info.label}
                                  </span>
                                </div>
                              )
                            })()}
                          </div>
                          <div>
                            <p className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-1">Submitted</p>
                            <p className="font-medium text-slate-800">
                              {new Date(app.createdAt).toLocaleString('en-US', { month: 'short', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' })}
                            </p>
                          </div>
                          <div>
                            <p className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-1">Approved By</p>
                            <p className="font-medium text-slate-800">{app.approvedBy ?? '-'}</p>
                          </div>
                          <div>
                            <p className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-1">Approved At</p>
                            <p className="font-medium text-slate-800">
                              {app.approvedAt ? new Date(app.approvedAt).toLocaleString('en-US', { month: 'short', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' }) : '-'}
                            </p>
                          </div>
                          <div>
                            <p className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-1">Rejected At</p>
                            <p className="font-medium text-slate-800">
                              {app.rejectedAt ? new Date(app.rejectedAt).toLocaleString('en-US', { month: 'short', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' }) : '-'}
                            </p>
                          </div>
                          <div>
                            <p className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-1">Rejection Reason</p>
                            <p className="font-medium text-slate-800">{app.rejectionReason || '-'}</p>
                          </div>
                          <div>
                            <p className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-1">Document</p>
                            {app.discountDocument ? (
                              <div className="flex gap-2">
                                <button 
                                  onClick={() => handleViewDocument(app.discountDocument, app.discountApplicationId)}
                                  className="flex items-center gap-1 text-blue-600 hover:text-blue-700 transition-colors"
                                  title="View Document">
                                  <Eye size={14} />
                                  <span className="text-xs">View</span>
                                </button>
                                <button 
                                  onClick={() => handleViewDocument(app.discountDocument, app.discountApplicationId)}
                                  className="flex items-center gap-1 text-green-600 hover:text-green-700 transition-colors"
                                  title="Download Document">
                                  <Download size={14} />
                                </button>
                              </div>
                            ) : (
                              <span className="text-xs text-slate-400">No document</span>
                            )}
                          </div>
                        </div>
                      </td>
                    </tr>
                  )}
                  </Fragment>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Document Preview Modal */}
      {previewDocument && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4" onClick={() => { setPreviewDocument(null); URL.revokeObjectURL(previewDocument) }}>
          <div className="bg-white rounded-2xl shadow-2xl max-w-4xl max-h-[90vh] overflow-hidden" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between p-4 border-b border-slate-200">
              <h3 className="text-lg font-bold text-slate-800">Document Preview</h3>
              <button 
                onClick={() => { setPreviewDocument(null); URL.revokeObjectURL(previewDocument) }}
                className="p-2 hover:bg-slate-100 rounded-lg transition-colors">
                <X size={20} className="text-slate-400" />
              </button>
            </div>
            <div className="p-4 overflow-auto max-h-[calc(90vh-80px)]">
              <img src={previewDocument} alt="Document preview" className="max-w-full h-auto rounded-lg" />
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
