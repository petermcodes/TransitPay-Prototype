import { useState, useEffect } from 'react'
import { Modal } from './Modal'
import { Spinner } from './Spinner'
import type { Terminal } from '../lib/admin'

interface TerminalModalProps {
  isOpen: boolean
  onClose: () => void
  onSubmit: (data: { terminalId: number; terminalName: string }) => Promise<void>
  terminals: Terminal[]
  loading?: boolean
  initialData?: { terminalId: number; terminalName: string }
}

export function TerminalModal({ isOpen, onClose, onSubmit, terminals, loading = false, initialData }: TerminalModalProps) {
  const [terminalName, setTerminalName] = useState('')
  const [terminalId, setTerminalId] = useState<number>(terminals[0]?.terminalId || 0)
  const [errors, setErrors] = useState<{ terminalName?: string; terminalId?: string }>({})

  useEffect(() => {
    if (isOpen) {
      setTerminalName(initialData?.terminalName || '')
      setTerminalId(initialData?.terminalId || terminals[0]?.terminalId || 0)
      setErrors({})
    }
  }, [isOpen, terminals, initialData])

  const validate = (): boolean => {
    const newErrors: { terminalName?: string; terminalId?: string } = {}

    if (!terminalName.trim()) {
      newErrors.terminalName = 'Terminal name is required'
    }

    // Only require terminalId when editing
    if (initialData && !terminalId) {
      newErrors.terminalId = 'Please select a terminal'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validate()) return

    try {
      await onSubmit({ terminalId, terminalName })
      onClose()
    } catch (error) {
      // Error handling is done in parent
    }
  }

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={initialData ? 'Edit Terminal' : 'Add New Terminal'} size="md">
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        {/* Only show terminal selector when editing */}
        {initialData && (
          <div>
            <label className="block text-sm font-semibold text-slate-700 mb-2">
              Terminal <span className="text-red-500">*</span>
            </label>
            <select
              value={terminalId}
              onChange={e => setTerminalId(Number(e.target.value))}
              className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:outline-none focus:border-blue-400 focus:ring-2 focus:ring-blue-100 transition-all"
            >
              <option value="">Select a terminal</option>
              {terminals.map(terminal => (
                <option key={terminal.terminalId} value={terminal.terminalId}>
                  {terminal.terminalName}
                </option>
              ))}
            </select>
            {errors.terminalId && <p className="text-red-500 text-xs mt-1">{errors.terminalId}</p>}
          </div>
        )}

        <div>
          <label className="block text-sm font-semibold text-slate-700 mb-2">
            Terminal Name <span className="text-red-500">*</span>
          </label>
          <input
            type="text"
            value={terminalName}
            onChange={e => setTerminalName(e.target.value)}
            placeholder="Enter terminal name"
            className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:outline-none focus:border-blue-400 focus:ring-2 focus:ring-blue-100 transition-all"
          />
          {errors.terminalName && <p className="text-red-500 text-xs mt-1">{errors.terminalName}</p>}
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
            {loading ? 'Saving...' : initialData ? 'Save Changes' : 'Add Terminal'}
          </button>
        </div>
      </form>
    </Modal>
  )
}