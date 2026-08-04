import { useState, useEffect } from 'react'
import { X } from 'lucide-react'
import { Btn } from '../AdminApp'

type DiscountTypeModalProps = {
  isOpen: boolean
  onClose: () => void
  onSubmit: (data: {
    name: string
    description?: string
    discountPercentage: number
    requiresApproval: boolean
  }) => void
  loading?: boolean
  initialData?: {
    name: string
    description?: string
    discountPercentage: number
    requiresApproval: boolean
  }
}

export function DiscountTypeModal({ isOpen, onClose, onSubmit, loading, initialData }: DiscountTypeModalProps) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [discountPercentage, setDiscountPercentage] = useState(0)
  const [requiresApproval, setRequiresApproval] = useState(true)
  const [errors, setErrors] = useState<Record<string, string>>({})

  useEffect(() => {
    if (isOpen && initialData) {
      setName(initialData.name)
      setDescription(initialData.description || '')
      setDiscountPercentage(initialData.discountPercentage)
      setRequiresApproval(initialData.requiresApproval)
    } else if (!isOpen) {
      resetForm()
    }
  }, [isOpen, initialData])

  const resetForm = () => {
    setName('')
    setDescription('')
    setDiscountPercentage(0)
    setRequiresApproval(true)
    setErrors({})
  }

  const validate = () => {
    const newErrors: Record<string, string> = {}
    if (!name.trim()) newErrors.name = 'Name is required'
    if (name.length > 100) newErrors.name = 'Name must be 100 characters or less'
    if (discountPercentage < 0 || discountPercentage > 100) {
      newErrors.discountPercentage = 'Discount must be between 0 and 100'
    }
    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = () => {
    if (!validate()) return
    onSubmit({
      name: name.trim(),
      description: description.trim() || undefined,
      discountPercentage,
      requiresApproval,
    })
  }

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-3xl shadow-2xl w-full max-w-md overflow-hidden">
        {/* Header */}
        <div className="px-6 py-5 border-b border-slate-100 flex items-center justify-between">
          <div>
            <h2 className="font-poppins font-bold text-slate-800 text-lg">
              {initialData ? 'Edit Discount Type' : 'Add Discount Type'}
            </h2>
            <p className="text-xs text-slate-500 mt-0.5">
              {initialData ? 'Update discount type details' : 'Create a new discount type'}
            </p>
          </div>
          <button onClick={onClose} className="p-2 rounded-xl hover:bg-slate-100 text-slate-400 hover:text-slate-600 transition-colors">
            <X size={20} />
          </button>
        </div>

        {/* Body */}
        <div className="p-6 flex flex-col gap-4">
          <div className="flex flex-col gap-1.5">
            <label className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Name *</label>
            <input
              value={name}
              onChange={e => setName(e.target.value)}
              placeholder="e.g., Student, Senior, PWD"
              className={`w-full rounded-xl border ${errors.name ? 'border-red-300 bg-red-50' : 'border-slate-200 bg-white'} px-4 py-2.5 text-sm focus:outline-none focus:border-blue-400 transition-all`}
            />
            {errors.name && <p className="text-xs text-red-600">{errors.name}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <label className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Description</label>
            <textarea
              value={description}
              onChange={e => setDescription(e.target.value)}
              placeholder="Describe the discount eligibility and terms..."
              rows={3}
              className="w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:outline-none focus:border-blue-400 transition-all resize-none"
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <label className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Discount Percentage *</label>
            <div className="relative">
              <input
                type="number"
                value={discountPercentage}
                onChange={e => setDiscountPercentage(parseFloat(e.target.value) || 0)}
                min="0"
                max="100"
                step="0.01"
                className={`w-full rounded-xl border ${errors.discountPercentage ? 'border-red-300 bg-red-50' : 'border-slate-200 bg-white'} px-4 py-2.5 text-sm focus:outline-none focus:border-blue-400 transition-all pr-10`}
              />
              <span className="absolute right-4 top-1/2 -translate-y-1/2 text-slate-400 text-sm font-semibold">%</span>
            </div>
            {errors.discountPercentage && <p className="text-xs text-red-600">{errors.discountPercentage}</p>}
          </div>

          <div className="flex items-center justify-between py-2">
            <div>
              <p className="text-sm font-semibold text-slate-700">Requires Approval</p>
              <p className="text-xs text-slate-400 mt-0.5">Applications need admin review</p>
            </div>
            <button
              onClick={() => setRequiresApproval(!requiresApproval)}
              className={`relative w-12 h-7 rounded-full transition-colors ${requiresApproval ? 'bg-blue-600' : 'bg-slate-200'}`}>
              <div className={`absolute top-1 left-1 w-5 h-5 rounded-full bg-white shadow-sm transition-transform ${requiresApproval ? 'translate-x-5' : ''}`} />
            </button>
          </div>
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-slate-100 flex gap-2 justify-end">
          <Btn variant="ghost" size="md" onClick={onClose} disabled={loading}>Cancel</Btn>
          <Btn variant="primary" size="md" onClick={handleSubmit} disabled={loading}>
            {loading ? 'Saving...' : initialData ? 'Update' : 'Create'}
          </Btn>
        </div>
      </div>
    </div>
  )
}