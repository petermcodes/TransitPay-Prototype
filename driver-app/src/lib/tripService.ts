import { api } from './api';
import { authService } from './auth';

export interface Trip {
  tripId: number;
  driverId: number;
  originStationId: number;
  finalDestinationStationId?: number;
  tripStatus: 'Pending' | 'Active' | 'Completed' | 'Cancelled';
  passengerCount: number;
  totalRevenue: number;
  startTime?: string;
  endTime?: string;
  createdAt: string;
  updatedAt?: string;
  originStation?: Station;
  finalDestinationStation?: Station;
}

export interface Station {
  stationId: number;
  stationName: string;
  townId: number;
  isActive: boolean;
}

export interface StartTripRequest {
  originStationId: number;
}

export interface StartTripResponse {
  success: boolean;
  message: string;
  data?: Trip;
}

export interface EndTripResponse {
  success: boolean;
  message: string;
  data?: Trip;
}

export const tripService = {
  /**
   * Starts a new trip for the authenticated driver.
   */
  async startTrip(originStationId: number): Promise<StartTripResponse> {
    const token = authService.getToken();
    if (!token) {
      throw new Error('Not authenticated');
    }

    const response = await api.post<StartTripResponse>(
      '/api/Trip/start',
      { originStationId },
      token
    );

    if (response.success && response.data) {
      // Persist active trip ID
      localStorage.setItem('activeTripId', response.data.tripId.toString());
    }

    return response;
  },

  /**
   * Ends the active trip.
   */
  async endTrip(tripId: number): Promise<EndTripResponse> {
    const token = authService.getToken();
    if (!token) {
      throw new Error('Not authenticated');
    }

    const response = await api.post<EndTripResponse>(
      `/api/Trip/${tripId}/end`,
      {},
      token
    );

    if (response.success) {
      // Clear persisted trip ID
      localStorage.removeItem('activeTripId');
    }

    return response;
  },

  /**
   * Gets the active trip for the authenticated driver.
   */
  async getActiveTrip(): Promise<Trip | null> {
    const token = authService.getToken();
    if (!token) {
      return null;
    }

    const user = authService.getUser();
    if (!user) {
      return null;
    }

    try {
      const response = await api.get<{ success: boolean; data: Trip }>(
        `/api/Trip/active/${user.userId}`,
        token
      );

      if (response.success && response.data) {
        // Update persisted trip ID
        localStorage.setItem('activeTripId', response.data.tripId.toString());
        return response.data;
      }

      return null;
    } catch (error) {
      return null;
    }
  },

  /**
   * Resumes the active trip from localStorage if it exists.
   * Call this on app startup to restore trip state.
   */
  async resumeActiveTrip(): Promise<Trip | null> {
    const activeTripId = localStorage.getItem('activeTripId');
    if (!activeTripId) {
      return null;
    }

    // Fetch the active trip from backend
    return await this.getActiveTrip();
  },

  /**
   * Clears the persisted trip state.
   */
  clearActiveTrip(): void {
    localStorage.removeItem('activeTripId');
  },

  /**
   * Checks if there's an active trip persisted.
   */
  hasActiveTrip(): boolean {
    return !!localStorage.getItem('activeTripId');
  }
};