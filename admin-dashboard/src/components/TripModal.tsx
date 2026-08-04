import { useState, useEffect } from 'react'
import { X, MapPin } from 'lucide-react'
import { Btn } from '../AdminApp'

type TripModalProps = {
  isOpen: boolean
  onClose: () => void
  onSubmit: (data: {
    driverId: number
    originStationId: number
    finalDestinationStationId: number
  }) => void
  loading?: boolean
  initialData?: {
    driverId: number
    originStationId: number
    finalDestinationStationId: number
  }
}

export function TripModal({ isOpen, onClose, onSubmit, loading, initialData }: TripModalProps) {
  const [driverId, setDriverId] = useState('')
  const [originStationId, setOriginStationId] = useState('')
  const [finalDestinationStationId, setFinalDestinationStationId] = useState('')
  const [errors, setErrors] = useState<Record<string, string>>({})

  // Mock data - in real app, these would be fetched from API
  const mockDrivers = [
    { id: 1, name: 'Pedro Santos' },
    { id: 2, name: 'Carlos Rivera' },
    { id: 3, name: 'Jose Mendoza' },
  ]

  const mockStations = [
    { id: 1, name: 'Cubao Station' },
    { id: 2, name: 'Ortigas Station' },
    { id: 3, name: 'Marikina Station' },
    { id: 4, name: 'Fairview Terminal' },
    { id: 5, name: 'Airport Link' },
  ]

  useEffect(() => {
    if (isOpen && initialData) {
      setDriverId(initialData.driverId.toString())
      setOriginStationId(initialData.originStationId.toString())
      setFinalDestinationStationId(initialData.finalDestinationStationId.toString())
    } else if (!isOpen) {
      resetForm()
    }
  }, [isOpen, initialData])

  const resetForm = () => {
    setDriverId('')
    setOriginStationId('')
    setFinalDestinationStationId('')
    setErrors({})
  }

  const validate = () => {
    const newErrors: Record<string, string> = {}
    if (!driverId) newErrors.driverId = 'Driver is required'
    if (!originStationId) newErrors.originStationId = 'Origin station is required'
    if (!finalDestinationStationId) newErrors.finalDestinationStationId = 'Destination station is required'
    if (originStationId && finalDestinationStationId && originStationId === finalDestinationStationId) {
      newErrors.finalDestinationStationId = 'Destination must be different from origin'
    }
    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = () => {
    if (!validate()) return
    onSubmit({
      driverId: parseInt(driverId),
      originStationId: parseInt(originStationId),
      finalDestinationStationId: parseInt(finalDestinationStationId),
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
              {initialData ? 'Edit Trip' : 'Create New Trip'}
            </h2>
            <p className="text-xs text-slate-500 mt-0.5">
              {initialData ? 'Update trip details' : 'Assign a driver to a new trip'}
            </p>
          </div>
          <button onClick={onClose} className="p-2 rounded-xl hover:bg-slate-100 text-slate-400 hover:text-slate-600 transition-colors">
            <X size={20} />
          </button>
        </div>

        {/* Body */}
        <div className="p-6 flex flex-col gap-4">
          <div className="flex flex-col gap-1.5">
            <label className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Driver *</label>
            <select
              value={driverId}
              onChange={e => setDriverId(e.target.value)}
              className={`w-full rounded-xl border ${errors.driverId ? 'border-red-300 bg-red-50' : 'border-slate-200 bg-white'} px-4 py-2.5 text-sm focus:outline-none focus:border-blue-400 transition-all`}
            >
              <option value="">Select a driver</option>
              {mockDrivers.map(driver => (
                <option key={driver.id} value={driver.id}>{driver.name}</option>
              ))}
            </select>
            {errors.driverId && <p className="text-xs text-red-600">{errors.driverId}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <label className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Origin Station *</label>
            <div className="relative">
              <MapPin size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
              <select
                value={originStationId}
                onChange={e => setOriginStationId(e.target.value)}
                className={`w-full rounded-xl border ${errors.originStationId ? 'border-red-300 bg-red-50' : 'border-slate-200 bg-white'} px-4 py-2.5 text-sm focus:outline-none focus:border-blue-400 transition-all pl-10`}
              >
                <option value="">Select origin station</option>
                {mockStations.map(station => (
                  <option key={station.id} value={station.id}>{station.name}</option>
                ))}
              </select>
            </div>
            {errors.originStationId && <p className="text-xs text-red-600">{errors.originStationId}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <label className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Final Destination *</label>
            <div className="relative">
              <MapPin size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
              <select
                value={finalDestinationStationId}
                onChange={e => setFinalDestinationStationId(e.target.value)}
                className={`w-full rounded-xl border ${errors.finalDestinationStationId ? 'border-red-300 bg-red-50' : 'border-slate-200 bg-white'} px-4 py-2.5 text-sm focus:outline-none focus:border-blue-400 transition-all pl-10`}
              >
                <option value="">Select destination station</option>
                {mockStations.map(station => (
                  <option key={station.id} value={station.id}>{station.name}</option>
                ))}
              </select>
            </div>
            {errors.finalDestinationStationId && <p className="text-xs text-red-600">{errors.finalDestinationStationId}</p>}
          </div>
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-slate-100 flex gap-2 justify-end">
          <Btn variant="ghost" size="md" onClick={onClose} disabled={loading}>Cancel</Btn>
          <Btn variant="primary" size="md" onClick={handleSubmit} disabled={loading}>
            {loading ? 'Creating...' : initialData ? 'Update' : 'Create Trip'}
          </Btn>
        </div>
      </div>
    </div>
  )
}