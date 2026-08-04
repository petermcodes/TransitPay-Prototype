import { useState, useEffect } from 'react'
import { Modal } from './Modal'
import { Spinner } from './Spinner'
import type { Town } from '../lib/admin'

interface StationModalProps {
  isOpen: boolean
  onClose: () => void
  onSubmit: (data: { townId: number; stationName: string }) => Promise<void>
  towns: Town[]
  loading?: boolean
}

export function StationModal({ isOpen, onClose, onSubmit, towns, loading = false }: StationModalProps) {
  const [stationName, setStationName] = useState('')
  const [townId, setTownId] = useState<number>(towns[0]?.townId || 0)
  const [errors, setErrors] = useState<{ stationName?: string; townId?: string }>({})

  useEffect(() => {
    if (isOpen) {
      setStationName('')
      setTownId(towns[0]?.townId || 0)
      setErrors({})
    }
  }, [isOpen, towns])

  const validate = (): boolean => {
    const newErrors: { stationName?: string; townId?: string } = {}

    if (!stationName.trim()) {
      newErrors.stationName = 'Station name is required'
    }

    if (!townId) {
      newErrors.townId = 'Please select a town'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validate()) return

    try {
      await onSubmit({ townId, stationName })
      onClose()
    } catch (error) {
      // Error handling is done in parent
    }
  }

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Add New Station" size="md">
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <div>
          <label className="block text-sm font-semibold text-slate-700 mb-2">
            Town <span className="text-red-500">*</span>
          </label>
          <select
            value={townId}
            onChange={e => setTownId(Number(e.target.value))}
            className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:outline-none focus:border-blue-400 focus:ring-2 focus:ring-blue-100 transition-all"
          >
            <option value="">Select a town</option>
            {towns.map(town => (
              <option key={town.townId} value={town.townId}>
                {town.townName}
              </option>
            ))}
          </select>
          {errors.townId && <p className="text-red-500 text-xs mt-1">{errors.townId}</p>}
        </div>

        <div>
          <label className="block text-sm font-semibold text-slate-700 mb-2">
            Station Name <span className="text-red-500">*</span>
          </label>
          <input
            type="text"
            value={stationName}
            onChange={e => setStationName(e.target.value)}
            placeholder="Enter station name"
            className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:outline-none focus:border-blue-400 focus:ring-2 focus:ring-blue-100 transition-all"
          />
          {errors.stationName && <p className="text-red-500 text-xs mt-1">{errors.stationName}</p>}
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
            {loading ? 'Adding...' : 'Add Station'}
          </button>
        </div>
      </form>
    </Modal>
  )
}