import { useState, useEffect } from 'react'
import { Search, Plus, Eye, Trash2, User, AlertCircle } from 'lucide-react'
import { adminService } from '../lib/admin'
import { Btn, Chip } from '../AdminApp'
import type { PassengerDiscount } from '../lib/admin'

type TabView = 'active' | 'all'

export function PassengerDiscountsView() {
  const [activeTab, setActiveTab] = useState<TabView>('active')
  const [activeDiscounts, setActiveDiscounts] = useState<PassengerDiscount[]>([])
  const [allDiscounts, setAllDiscounts] = useState<PassengerDiscount[]>([])
  const [loading, setLoading] = useState(true)
  const [search, setSearch] = useState('')
  const [showAssignModal, setShowAssignModal] = useState(false)
  const [discountTypes, setDiscountTypes] = useState<{ discountTypeId: number; name: string; discountPercentage: number }[]>([])
  const [selectedCardId, setSelectedCardId] = useState('')
  const [selectedDiscountType, setSelectedDiscountType] = useState<number | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    loadDiscounts()
  }, [activeTab])

  const loadDiscounts = async () => {
    setLoading(true)
    try {
      if (activeTab === 'active') {
        const data = await adminService.getActivePassengerDiscounts()
        setActiveDiscounts(data)
      } else {
        const data = await adminService.getAllPassengerDiscounts()
        setAllDiscounts(data)
      }
    } catch (err) {
      console.error('Failed to load passenger discounts:', err)
    } finally {
      setLoading(false)
    }
  }

  const loadDiscountTypes = async () => {
    try {
      const types = await adminService.getDiscountTypes()
      setDiscountTypes(types.filter(t => t.isActive).map(t => ({
        discountTypeId: t.discountTypeId,
        name: t.name,
        discountPercentage: t.discountPercentage
      })))
    } catch (err) {
      console.error('Failed to load discount types:', err)
    }
  }

  const handleAssignClick = async () => {
    await loadDiscountTypes()
    setShowAssignModal(true)
  }

  const handleAssign = async () => {
    if (!selectedCardId || !selectedDiscountType) return
    setSubmitting(true)
    try {
      await adminService.assignPassengerDiscount(parseInt(selectedCardId), selectedDiscountType)
      alert('Discount assigned successfully!')
      setShowAssignModal(false)
      setSelectedCardId('')
      setSelectedDiscountType(null)
      loadDiscounts()
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Failed to assign discount')
    } finally {
      setSubmitting(false)
    }
  }

  const handleRemove = async (discountId: number) => {
    if (!confirm('Are you sure you want to remove this discount? This action cannot be undone.')) return
    try {
      await adminService.removePassengerDiscount(discountId)
      alert('Discount removed successfully!')
      loadDiscounts()
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Failed to remove discount')
    }
  }

  const currentDiscounts = activeTab === 'active' ? activeDiscounts : allDiscounts

  const filteredDiscounts = currentDiscounts.filter(d => {
      const matchesSearch = d.maskedCardNumber?.toLowerCase().includes(search.toLowerCase()) ||
                         d.passengerName?.toLowerCase().includes(search.toLowerCase()) ||
                         d.discountTypeName?.toLowerCase().includes(search.toLowerCase())
    return matchesSearch
  })

  const getStatusVariant = (status: string) => {
    switch (status) {
      case 'Active': return 'success'
      case 'Expired': return 'default'
      case 'Revoked': return 'danger'
      default: return 'default'
    }
  }

  return (
    <div className="flex flex-col gap-4">
      {/* Controls */}
      <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 flex flex-wrap gap-3 items-center justify-between">
        <div className="flex gap-2 flex-wrap">
          <button
            onClick={() => setActiveTab('active')}
            className={`px-4 py-2 rounded-xl text-sm font-semibold transition-all flex items-center gap-2 ${activeTab === 'active' ? 'bg-blue-600 text-white shadow-sm' : 'text-slate-600 bg-slate-100 hover:bg-slate-200'}`}>
            <User size={16} />
            Active Discounts
            {activeDiscounts.length > 0 && (
              <span className={`px-2 py-0.5 rounded-full text-xs font-bold ${activeTab === 'active' ? 'bg-white/30 text-white' : 'bg-blue-100 text-blue-600'}`}>
                {activeDiscounts.length}
              </span>
            )}
          </button>
          <button
            onClick={() => setActiveTab('all')}
            className={`px-4 py-2 rounded-xl text-sm font-semibold transition-all ${activeTab === 'all' ? 'bg-blue-600 text-white shadow-sm' : 'text-slate-600 bg-slate-100 hover:bg-slate-200'}`}>
            All Discounts
          </button>
        </div>
        <div className="flex gap-2 items-center">
          {activeTab === 'all' && (
            <div className="relative">
              <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
              <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by card or name..."
                className="pl-9 pr-4 py-2 text-sm rounded-xl border border-slate-200 w-64 focus:outline-none focus:border-blue-400 transition-all" />
            </div>
          )}
          <Btn variant="primary" size="md" onClick={handleAssignClick}>
            <Plus size={14} /> Assign Discount
          </Btn>
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        {loading ? (
          <div className="p-8 text-center text-slate-500">Loading passenger discounts...</div>
        ) : filteredDiscounts.length === 0 ? (
          <div className="p-8 text-center text-slate-500">
            <User size={48} className="mx-auto mb-3 text-slate-300" />
            <p className="font-semibold">No passenger discounts found</p>
            <p className="text-sm mt-1">Assign a discount to a passenger to get started</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead><tr className="border-b border-slate-100 bg-slate-50">
                {['ID', 'Card Number', 'Passenger', 'Discount Type', 'Discount %', 'Status', 'Assigned', 'Actions'].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider whitespace-nowrap">{h}</th>
                ))}
              </tr></thead>
              <tbody>
                {filteredDiscounts.map((discount, i, arr) => (
                  <tr key={discount.passengerDiscountId} className={`border-b border-slate-100 hover:bg-blue-50/40 transition-colors ${i === arr.length - 1 ? 'border-0' : ''}`}>
                    <td className="px-4 py-3.5 font-mono text-xs text-blue-600 font-semibold">
                      PD-{discount.passengerDiscountId.toString().padStart(6, '0')}
                    </td>
                    <td className="px-4 py-3.5 font-mono text-xs text-slate-600">{discount.maskedCardNumber}</td>
                    <td className="px-4 py-3.5">
                      <div className="flex items-center gap-2">
                        <div className="w-8 h-8 rounded-xl bg-blue-600 flex items-center justify-center shrink-0">
                          <span className="text-xs font-bold text-white">{discount.passengerName?.[0] || 'P'}</span>
                        </div>
                        <span className="font-medium text-slate-800">{discount.passengerName || 'Unknown'}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3.5 font-medium text-slate-800">{discount.discountTypeName}</td>
                    <td className="px-4 py-3.5">
                      <span className="font-poppins font-bold text-slate-800">{discount.discountPercentage}%</span>
                    </td>
                    <td className="px-4 py-3.5 whitespace-nowrap">
                      <Chip label={discount.status} variant={getStatusVariant(discount.status) as any} />
                    </td>
                    <td className="px-4 py-3.5 text-slate-400 text-xs whitespace-nowrap">
                      {new Date(discount.assignedAt).toLocaleDateString('en-US', { 
                        month: 'short', 
                        day: 'numeric', 
                        year: 'numeric'
                      })}
                    </td>
                    <td className="px-4 py-3.5">
                      <div className="flex items-center gap-1">
                        <button className="p-1.5 rounded-lg hover:bg-blue-50 text-slate-400 hover:text-blue-600 transition-colors">
                          <Eye size={14} />
                        </button>
                        {discount.status === 'Active' && (
                          <button 
                            onClick={() => handleRemove(discount.passengerDiscountId)}
                            className="p-1.5 rounded-lg hover:bg-red-50 text-slate-400 hover:text-red-500 transition-colors"
                            title="Remove Discount">
                            <Trash2 size={14} />
                          </button>
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

      {/* Assign Modal */}
      {showAssignModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-3xl shadow-xl max-w-md w-full p-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="font-poppins font-bold text-xl text-slate-800">Assign Discount</h3>
              <button onClick={() => setShowAssignModal(false)} className="text-slate-400 hover:text-slate-600">
                <Trash2 size={20} />
              </button>
            </div>

            <div className="flex flex-col gap-4">
              <div>
                <label className="text-sm font-semibold text-slate-700 mb-2 block">Card Number</label>
                <input
                  type="text"
                  value={selectedCardId}
                  onChange={e => setSelectedCardId(e.target.value)}
                  placeholder="Enter card number"
                  className="w-full px-4 py-3 rounded-xl border border-slate-200 focus:outline-none focus:border-blue-400"
                />
                <p className="text-xs text-slate-500 mt-1">Enter the passenger's card number</p>
              </div>

              <div>
                <label className="text-sm font-semibold text-slate-700 mb-2 block">Discount Type</label>
                <select
                  value={selectedDiscountType || ''}
                  onChange={e => setSelectedDiscountType(e.target.value ? parseInt(e.target.value) : null)}
                  className="w-full px-4 py-3 rounded-xl border border-slate-200 focus:outline-none focus:border-blue-400">
                  <option value="">Select discount type...</option>
                  {discountTypes.map(type => (
                    <option key={type.discountTypeId} value={type.discountTypeId}>
                      {type.name} ({type.discountPercentage}% off)
                    </option>
                  ))}
                </select>
              </div>

              <div className="bg-blue-50 border border-blue-200 rounded-xl p-3 flex items-start gap-2">
                <AlertCircle size={16} className="text-blue-600 shrink-0 mt-0.5" />
                <p className="text-xs text-slate-600">
                  This will assign the selected discount to the passenger's card. The discount will be automatically applied during fare calculation.
                </p>
              </div>

              <div className="flex gap-2 mt-2">
                <Btn variant="secondary" size="lg" className="flex-1" onClick={() => setShowAssignModal(false)}>
                  Cancel
                </Btn>
                <Btn variant="primary" size="lg" className="flex-1" onClick={handleAssign} disabled={!selectedCardId || !selectedDiscountType || submitting}>
                  {submitting ? 'Assigning...' : 'Assign Discount'}
                </Btn>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}