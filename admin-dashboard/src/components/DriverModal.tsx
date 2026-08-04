import { useState, useEffect } from 'react'
import { Modal } from './Modal'
import { Spinner } from './Spinner'

interface DriverModalProps {
  isOpen: boolean
  onClose: () => void
  onSubmit: (data: {
    firstName: string
    lastName: string
    mobileNumber: string
    vehicle: string
    plateNumber: string
  }) => Promise<void>
  loading?: boolean
}

export function DriverModal({ isOpen, onClose, onSubmit, loading = false }: DriverModalProps) {
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [mobileNumber, setMobileNumber] = useState('')
  const [vehicle, setVehicle] = useState('')
  const [plateNumber, setPlateNumber] = useState('')
  const [errors, setErrors] = useState<{
    firstName?: string
    lastName?: string
    mobileNumber?: string
    vehicle?: string
    plateNumber?: string
  }>({})

  useEffect(() => {
    if (isOpen) {
      setFirstName('')
      setLastName('')
      setMobileNumber('')
      setVehicle('')
      setPlateNumber('')
      setErrors({})
    }
  }, [isOpen])

  const validate = (): boolean => {
    const newErrors: typeof errors = {}

    if (!firstName.trim()) {
      newErrors.firstName = 'First name is required'
    }

    if (!lastName.trim()) {
      newErrors.lastName = 'Last name is required'
    }

    if (!mobileNumber.trim()) {
      newErrors.mobileNumber = 'Mobile number is required'
    } else if (!/^[0-9]{11}$/.test(mobileNumber.replace(/\s/g, ''))) {
      newErrors.mobileNumber = 'Mobile number must be 11 digits'
    }

    if (!vehicle.trim()) {
      newErrors.vehicle = 'Vehicle is required'
    }

    if (!plateNumber.trim()) {
      newErrors.plateNumber = 'Plate number is required'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validate()) return

    try {
      await onSubmit({ firstName, lastName, mobileNumber, vehicle, plateNumber })
      onClose()
    } catch (error) {
      // Error handling is done in parent
    }
  }

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Add New Driver" size="lg">
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-semibold text-slate-700 mb-2">
              First Name <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              value={firstName}
              onChange={e => setFirstName(e.target.value)}
              placeholder="Enter first name"
              className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:outline-none focus:border-blue-400 focus:ring-2 focus:ring-blue-100 transition-all"
            />
            {errors.firstName && <p className="text-red-500 text-xs mt-1">{errors.firstName}</p>}
          </div>

          <div>
            <label className="block text-sm font-semibold text-slate-700 mb-2">
              Last Name <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              value={lastName}
              onChange={e => setLastName(e.target.value)}
              placeholder="Enter last name"
              className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:outline-none focus:border-blue-400 focus:ring-2 focus:ring-blue-100 transition-all"
            />
            {errors.lastName && <p className="text-red-500 text-xs mt-1">{errors.lastName}</p>}
          </div>
        </div>

        <div>
          <label className="block text-sm font-semibold text-slate-700 mb-2">
            Mobile Number <span className="text-red-500">*</span>
          </label>
          <input
            type="tel"
            value={mobileNumber}
            onChange={e => setMobileNumber(e.target.value)}
            placeholder="09171234567"
            maxLength={11}
            className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:outline-none focus:border-blue-400 focus:ring-2 focus:ring-blue-100 transition-all"
          />
          {errors.mobileNumber && <p className="text-red-500 text-xs mt-1">{errors.mobileNumber}</p>}
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-semibold text-slate-700 mb-2">
              Vehicle <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              value={vehicle}
              onChange={e => setVehicle(e.target.value)}
              placeholder="Bus #42"
              className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:outline-none focus:border-blue-400 focus:ring-2 focus:ring-blue-100 transition-all"
            />
            {errors.vehicle && <p className="text-red-500 text-xs mt-1">{errors.vehicle}</p>}
          </div>

          <div>
            <label className="block text-sm font-semibold text-slate-700 mb-2">
              Plate Number <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              value={plateNumber}
              onChange={e => setPlateNumber(e.target.value)}
              placeholder="ABC-1234"
              className="w-full px-4 py-2.5 rounded-xl border border-slate-200 focus:outline-none focus:border-blue-400 focus:ring-2 focus:ring-blue-100 transition-all"
            />
            {errors.plateNumber && <p className="text-red-500 text-xs mt-1">{errors.plateNumber}</p>}
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
            {loading ? 'Adding...' : 'Add Driver'}
          </button>
        </div>
      </form>
    </Modal>
  )
}