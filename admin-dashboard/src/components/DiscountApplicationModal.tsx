/**
 * Confirmation dialog for approving or rejecting a discount application.
 *
 * In `approve` mode it shows the consequence of approval; in `reject` mode it
 * requires a reason, which is relayed to the applicant.
 */
import { useState } from 'react'
import { X } from 'lucide-react'
import { Btn } from '../AdminApp'

type DiscountApplicationModalProps = {
  isOpen: boolean
  onClose: () => void
  onSubmit: (rejectionReason?: string) => void
  loading?: boolean
  mode: 'approve' | 'reject'
}

export function DiscountApplicationModal({ isOpen, onClose, onSubmit, loading, mode }: DiscountApplicationModalProps) {
  const [rejectionReason, setRejectionReason] = useState('')

  if (!isOpen) return null

  const handleSubmit = () => {
    if (mode === 'reject' && !rejectionReason.trim()) {
      alert('Please provide a rejection reason')
      return
    }
    onSubmit(mode === 'reject' ? rejectionReason.trim() : undefined)
    setRejectionReason('')
  }

  const handleClose = () => {
    setRejectionReason('')
    onClose()
  }

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-3xl shadow-2xl w-full max-w-md overflow-hidden">
        {/* Header */}
        <div className="px-6 py-5 border-b border-slate-100 flex items-center justify-between">
          <div>
            <h2 className="font-poppins font-bold text-slate-800 text-lg">
              {mode === 'approve' ? 'Approve Application' : 'Reject Application'}
            </h2>
            <p className="text-xs text-slate-500 mt-0.5">
              {mode === 'approve' 
                ? 'This will approve the discount application' 
                : 'Please provide a reason for rejection'}
            </p>
          </div>
          <button onClick={handleClose} className="p-2 rounded-xl hover:bg-slate-100 text-slate-400 hover:text-slate-600 transition-colors">
            <X size={20} />
          </button>
        </div>

        {/* Body */}
        <div className="p-6 flex flex-col gap-4">
          {mode === 'reject' && (
            <div className="flex flex-col gap-1.5">
              <label className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Rejection Reason *</label>
              <textarea
                value={rejectionReason}
                onChange={e => setRejectionReason(e.target.value)}
                placeholder="Please explain why this application is being rejected..."
                rows={4}
                className="w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:outline-none focus:border-blue-400 transition-all resize-none"
              />
              <p className="text-xs text-slate-400">This reason will be visible to the applicant</p>
            </div>
          )}

          {mode === 'approve' && (
            <div className="bg-green-50 border border-green-200 rounded-2xl p-4">
              <p className="text-sm text-green-800">
                By approving this application, the passenger will be granted the discount benefits immediately.
              </p>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-slate-100 flex gap-2 justify-end">
          <Btn variant="ghost" size="md" onClick={handleClose} disabled={loading}>Cancel</Btn>
          <Btn 
            variant={mode === 'approve' ? 'primary' : 'danger'} 
            size="md" 
            onClick={handleSubmit} 
            disabled={loading}
          >
            {loading ? 'Processing...' : mode === 'approve' ? 'Approve' : 'Reject'}
          </Btn>
        </div>
      </div>
    </div>
  )
}