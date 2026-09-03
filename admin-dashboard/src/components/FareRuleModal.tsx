/**
 * Create/edit form for fare matrix entries (origin → destination fare).
 *
 * Used both for adding a new rule and editing an existing one (via
 * `initialData`). Enforces distinct origin/destination and a positive amount.
 */
import { useState, useEffect } from 'react'
import { Modal } from './Modal'
import { Spinner } from './Spinner'
import type { Terminal } from '../lib/admin'

interface FareRuleModalProps {
  isOpen: boolean
  onClose: () => void
  onSubmit: (data: {
    originTerminalId: number
    destinationTerminalId: number
    fareAmount: number
    effectiveDate: string
  }) => Promise<void>
  terminals: Terminal[]
  loading?: boolean
  initialData?: {
    originTerminalId: number
    destinationTerminalId: number
    fareAmount: number
    effectiveDate: string
  }
}

export function FareRuleModal({ isOpen, onClose, onSubmit, terminals, loading = false, initialData }: FareRuleModalProps) {
  const [originTerminalId, setOriginTerminalId] = useState<number>(0)
  const [destinationTerminalId, setDestinationTerminalId] = useState<number>(0)
  const [fareAmount, setFareAmount] = useState('')
  const [effectiveDate, setEffectiveDate] = useState('')
  const [errors, setErrors] = useState<{
    originTerminalId?: string
    destinationTerminalId?: string
    fareAmount?: string
    effectiveDate?: string
  }>({})

  useEffect(() => {
    if (isOpen) {
      setOriginTerminalId(initialData?.originTerminalId || terminals[0]?.terminalId || 0)
      setDestinationTerminalId(initialData?.destinationTerminalId || terminals[1]?.terminalId || 0)
      setFareAmount(initialData ? String(initialData.fareAmount) : '')
      setEffectiveDate(initialData?.effectiveDate?.split('T')[0] || new Date().toISOString().split('T')[0])
      setErrors({})
    }
  }, [isOpen, terminals, initialData])

  const validate = (): boolean => {
    const newErrors: typeof errors = {}

    if (!originTerminalId) {
      newErrors.originTerminalId = 'Please select origin terminal'
    }

    if (!destinationTerminalId) {
      newErrors.destinationTerminalId = 'Please select destination terminal'
    }

    if (originTerminalId && destinationTerminalId && originTerminalId === destinationTerminalId) {
      newErrors.destinationTerminalId = 'Origin and destination must be different'
    }

    if (!fareAmount || Number(fareAmount) <= 0) {
      newErrors.fareAmount = 'Fare amount must be greater than 0'
    }

    if (!effectiveDate) {
      newErrors.effectiveDate = 'Effective date is required'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validate()) return

    try {
      await onSubmit({
        originTerminalId,
        destinationTerminalId,
        fareAmount: Number(fareAmount),
        effectiveDate
      })
      onClose()
    } catch (error) {
      // Error handling is done in parent
    }
  }

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={initialData ? 'Edit Fare Rule' : 'Add New Fare Rule'} size="lg">
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-semibold text-slate-700 mb-2">
              Origin Terminal <span className="text-red-500">*</span>
            </label>
            <select
              value={originTerminalId}
              onChange={e => setOriginTerminalId(Number(e.target.value))}
              className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:outline-none focus:border-blue-400 focus:ring-2 focus:ring-blue-100 transition-all"
            >
              <option value="">Select origin</option>
              {terminals.map(terminal => (
                <option key={terminal.terminalId} value={terminal.terminalId}>
                  {terminal.terminalName}
                </option>
              ))}
            </select>
            {errors.originTerminalId && <p className="text-red-500 text-xs mt-1">{errors.originTerminalId}</p>}
          </div>

          <div>
            <label className="block text-sm font-semibold text-slate-700 mb-2">
              Destination Terminal <span className="text-red-500">*</span>
            </label>
            <select
              value={destinationTerminalId}
              onChange={e => setDestinationTerminalId(Number(e.target.value))}
              className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:outline-none focus:border-blue-400 focus:ring-2 focus:ring-blue-100 transition-all"
            >
              <option value="">Select destination</option>
              {terminals.map(terminal => (
                <option key={terminal.terminalId} value={terminal.terminalId}>
                  {terminal.terminalName}
                </option>
              ))}
            </select>
            {errors.destinationTerminalId && <p className="text-red-500 text-xs mt-1">{errors.destinationTerminalId}</p>}
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-semibold text-slate-700 mb-2">
              Fare Amount (₱) <span className="text-red-500">*</span>
            </label>
            <input
              type="number"
              value={fareAmount}
              onChange={e => setFareAmount(e.target.value)}
              placeholder="0.00"
              step="0.01"
              min="0"
              className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:outline-none focus:border-blue-400 focus:ring-2 focus:ring-blue-100 transition-all"
            />
            {errors.fareAmount && <p className="text-red-500 text-xs mt-1">{errors.fareAmount}</p>}
          </div>

          <div>
            <label className="block text-sm font-semibold text-slate-700 mb-2">
              Effective Date <span className="text-red-500">*</span>
            </label>
            <input
              type="date"
              value={effectiveDate}
              onChange={e => setEffectiveDate(e.target.value)}
              className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:outline-none focus:border-blue-400 focus:ring-2 focus:ring-blue-100 transition-all"
            />
            {errors.effectiveDate && <p className="text-red-500 text-xs mt-1">{errors.effectiveDate}</p>}
          </div>
        </div>

        <div className="flex gap-3 pt-4">
          <button
            type="button"
            onClick={onClose}
            disabled={loading}
            className="flex-1 px-4 py-2.5 rounded-xl border-2 border-slate-200 text-slate-700 font-semibold hover:bg-slate-50 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={loading}
            className="flex-1 px-4 py-2.5 rounded-xl bg-blue-gradient text-white font-semibold hover:shadow-md transition-all disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
          >
            {loading && <Spinner size="sm" />}
            {loading ? 'Saving...' : initialData ? 'Save Changes' : 'Add Fare Rule'}
          </button>
        </div>
      </form>
    </Modal>
  )
}
