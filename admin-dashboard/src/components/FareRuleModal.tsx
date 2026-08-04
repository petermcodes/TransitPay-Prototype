import { useState, useEffect } from 'react'
import { Modal } from './Modal'
import { Spinner } from './Spinner'
import type { Station } from '../lib/admin'

interface FareRuleModalProps {
  isOpen: boolean
  onClose: () => void
  onSubmit: (data: {
    originStationId: number
    destinationStationId: number
    vehicleType: string
    passengerType: string
    fareAmount: number
    effectiveDate: string
  }) => Promise<void>
  stations: Station[]
  loading?: boolean
}

export function FareRuleModal({ isOpen, onClose, onSubmit, stations, loading = false }: FareRuleModalProps) {
  const [originStationId, setOriginStationId] = useState<number>(0)
  const [destinationStationId, setDestinationStationId] = useState<number>(0)
  const [vehicleType, setVehicleType] = useState('Bus')
  const [passengerType, setPassengerType] = useState('Regular')
  const [fareAmount, setFareAmount] = useState('')
  const [effectiveDate, setEffectiveDate] = useState('')
  const [errors, setErrors] = useState<{
    originStationId?: string
    destinationStationId?: string
    fareAmount?: string
    effectiveDate?: string
  }>({})

  useEffect(() => {
    if (isOpen) {
      setOriginStationId(stations[0]?.stationId || 0)
      setDestinationStationId(stations[1]?.stationId || 0)
      setVehicleType('Bus')
      setPassengerType('Regular')
      setFareAmount('')
      setEffectiveDate(new Date().toISOString().split('T')[0])
      setErrors({})
    }
  }, [isOpen, stations])

  const validate = (): boolean => {
    const newErrors: typeof errors = {}

    if (!originStationId) {
      newErrors.originStationId = 'Please select origin station'
    }

    if (!destinationStationId) {
      newErrors.destinationStationId = 'Please select destination station'
    }

    if (originStationId && destinationStationId && originStationId === destinationStationId) {
      newErrors.destinationStationId = 'Origin and destination must be different'
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
        originStationId,
        destinationStationId,
        vehicleType,
        passengerType,
        fareAmount: Number(fareAmount),
        effectiveDate
      })
      onClose()
    } catch (error) {
      // Error handling is done in parent
    }
  }

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Add New Fare Rule" size="lg">
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-semibold text-slate-700 mb-2">
              Origin Station <span className="text-red-500">*</span>
            </label>
            <select
              value={originStationId}
              onChange={e => setOriginStationId(Number(e.target.value))}
              className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:outline-none focus:border-blue-400 focus:ring-2 focus:ring-blue-100 transition-all"
            >
              <option value="">Select origin</option>
              {stations.map(station => (
                <option key={station.stationId} value={station.stationId}>
                  {station.stationName}
                </option>
              ))}
            </select>
            {errors.originStationId && <p className="text-red-500 text-xs mt-1">{errors.originStationId}</p>}
          </div>

          <div>
            <label className="block text-sm font-semibold text-slate-700 mb-2">
              Destination Station <span className="text-red-500">*</span>
            </label>
            <select
              value={destinationStationId}
              onChange={e => setDestinationStationId(Number(e.target.value))}
              className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:outline-none focus:border-blue-400 focus:ring-2 focus:ring-blue-100 transition-all"
            >
              <option value="">Select destination</option>
              {stations.map(station => (
                <option key={station.stationId} value={station.stationId}>
                  {station.stationName}
                </option>
              ))}
            </select>
            {errors.destinationStationId && <p className="text-red-500 text-xs mt-1">{errors.destinationStationId}</p>}
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-semibold text-slate-700 mb-2">
              Vehicle Type <span className="text-red-500">*</span>
            </label>
            <select
              value={vehicleType}
              onChange={e => setVehicleType(e.target.value)}
              className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:outline-none focus:border-blue-400 focus:ring-2 focus:ring-blue-100 transition-all"
            >
              <option value="Bus">Bus</option>
              <option value="Jeepney">Jeepney</option>
              <option value="UV Express">UV Express</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-semibold text-slate-700 mb-2">
              Passenger Type <span className="text-red-500">*</span>
            </label>
            <select
              value={passengerType}
              onChange={e => setPassengerType(e.target.value)}
              className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:outline-none focus:border-blue-400 focus:ring-2 focus:ring-blue-100 transition-all"
            >
              <option value="Regular">Regular</option>
              <option value="Student">Student</option>
              <option value="Senior">Senior</option>
              <option value="PWD">PWD</option>
            </select>
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
            {loading ? 'Adding...' : 'Add Fare Rule'}
          </button>
        </div>
      </form>
    </Modal>
  )
}