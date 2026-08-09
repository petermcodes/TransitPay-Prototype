import { useState, useEffect } from 'react'
import { Search, Plus, Eye, Edit2, Trash2, ToggleLeft, ToggleRight } from 'lucide-react'
import { adminService } from '../lib/admin'
import { Btn, Chip } from '../AdminApp'
import type { DiscountType } from '../lib/admin'

export function DiscountTypesView({ onAddDiscountType }: { onAddDiscountType: () => void }) {
  const [discountTypes, setDiscountTypes] = useState<DiscountType[]>([])
  const [loading, setLoading] = useState(true)
  const [search, setSearch] = useState('')
  const [filter, setFilter] = useState<'all' | 'active' | 'inactive'>('all')

  useEffect(() => {
    loadDiscountTypes()
  }, [])

  const loadDiscountTypes = async () => {
    setLoading(true)
    try {
      const data = await adminService.getDiscountTypes()
      setDiscountTypes(data)
    } catch (err) {
      console.error('Failed to load discount types:', err)
    } finally {
      setLoading(false)
    }
  }

  const handleActivate = async (discountTypeId: number) => {
    if (!confirm('Are you sure you want to activate this discount type?')) return
    try {
      await adminService.activateDiscountType(discountTypeId)
      alert('Discount type activated successfully')
      loadDiscountTypes()
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Failed to activate discount type')
    }
  }

  const handleDeactivate = async (discountTypeId: number) => {
    if (!confirm('Are you sure you want to deactivate this discount type?')) return
    try {
      await adminService.deactivateDiscountType(discountTypeId)
      alert('Discount type deactivated successfully')
      loadDiscountTypes()
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Failed to deactivate discount type')
    }
  }

  const handleDelete = async (discountTypeId: number) => {
    if (!confirm('Are you sure you want to delete this discount type? This action cannot be undone.')) return
    try {
      await adminService.deleteDiscountType(discountTypeId)
      alert('Discount type deleted successfully')
      loadDiscountTypes()
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Failed to delete discount type')
    }
  }

  const filteredTypes = discountTypes.filter(dt => {
    const matchesSearch = dt.name.toLowerCase().includes(search.toLowerCase()) ||
                         dt.description?.toLowerCase().includes(search.toLowerCase())
    const matchesFilter = filter === 'all' || (filter === 'active' ? dt.isActive : !dt.isActive)
    return matchesSearch && matchesFilter
  })

  return (
    <div className="flex flex-col gap-4">
      {/* Controls */}
      <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 flex flex-wrap gap-3 items-center justify-between">
        <div className="flex gap-2 flex-wrap">
          {(['all', 'active', 'inactive'] as const).map(status => (
            <button key={status} onClick={() => setFilter(status)}
              className={`px-4 py-1.5 rounded-xl text-sm font-semibold transition-all ${filter === status ? 'bg-blue-600 text-white shadow-sm' : 'text-slate-600 bg-slate-100 hover:bg-slate-200'}`}>
              {status.charAt(0).toUpperCase() + status.slice(1)}
            </button>
          ))}
        </div>
        <div className="flex gap-2 items-center">
          <div className="relative">
            <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
            <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search discount types..."
              className="pl-9 pr-4 py-2 text-sm rounded-xl border border-slate-200 w-44 focus:outline-none focus:border-blue-400 transition-all" />
          </div>
          <Btn variant="primary" size="md" onClick={onAddDiscountType}><Plus size={14} /> Add Discount Type</Btn>
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        {loading ? (
          <div className="p-8 text-center text-slate-500">Loading discount types...</div>
        ) : filteredTypes.length === 0 ? (
          <div className="p-8 text-center text-slate-500">No discount types found</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead><tr className="border-b border-slate-100 bg-slate-50">
                {['ID', 'Name', 'Description', 'Discount %', 'Requires Approval', 'Status', 'Created', 'Actions'].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-bold text-slate-500 uppercase tracking-wider whitespace-nowrap">{h}</th>
                ))}
              </tr></thead>
              <tbody>
                {filteredTypes.map((dt, i, arr) => (
                  <tr key={dt.discountTypeId} className={`border-b border-slate-100 hover:bg-blue-50/40 transition-colors ${i === arr.length - 1 ? 'border-0' : ''}`}>
                    <td className="px-4 py-3.5 font-mono text-xs text-blue-600 font-semibold">DSC-{dt.discountTypeId.toString().padStart(4, '0')}</td>
                    <td className="px-4 py-3.5 font-medium text-slate-800 whitespace-nowrap">{dt.name}</td>
                    <td className="px-4 py-3.5 text-slate-600 text-xs max-w-xs truncate">{dt.description || '-'}</td>
                    <td className="px-4 py-3.5">
                      <span className="font-poppins font-bold text-slate-800">{dt.discountPercentage}%</span>
                    </td>
                    <td className="px-4 py-3.5 whitespace-nowrap">
                      <Chip label={dt.requiresApproval ? 'Yes' : 'No'} variant={dt.requiresApproval ? 'warning' : 'default'} />
                    </td>
                    <td className="px-4 py-3.5 whitespace-nowrap">
                      <Chip label={dt.isActive ? 'Active' : 'Inactive'} variant={dt.isActive ? 'success' : 'default'} />
                    </td>
                    <td className="px-4 py-3.5 text-slate-400 text-xs whitespace-nowrap">
                      {new Date(dt.createdAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}
                    </td>
                    <td className="px-4 py-3.5">
                      <div className="flex items-center gap-1">
                        <button className="p-1.5 rounded-lg hover:bg-blue-50 text-slate-400 hover:text-blue-600 transition-colors"><Eye size={14} /></button>
                        <button className="p-1.5 rounded-lg hover:bg-blue-50 text-slate-400 hover:text-blue-600 transition-colors"><Edit2 size={14} /></button>
                        {dt.isActive ? (
                          <button 
                            onClick={() => handleDeactivate(dt.discountTypeId)}
                            className="p-1.5 rounded-lg hover:bg-orange-50 text-slate-400 hover:text-orange-600 transition-colors"
                            title="Deactivate">
                            <ToggleRight size={14} />
                          </button>
                        ) : (
                          <button 
                            onClick={() => handleActivate(dt.discountTypeId)}
                            className="p-1.5 rounded-lg hover:bg-green-50 text-slate-400 hover:text-green-600 transition-colors"
                            title="Activate">
                            <ToggleLeft size={14} />
                          </button>
                        )}
                        <button 
                          onClick={() => handleDelete(dt.discountTypeId)}
                          className="p-1.5 rounded-lg hover:bg-red-50 text-slate-400 hover:text-red-500 transition-colors">
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