import { api } from './api';
import { authService } from './auth';

export interface Trip {
  tripId: number;
  driverId: number;
  originTerminalId: number;
  finalDestinationTerminalId?: number;
  currentBoardingOriginTerminalId?: number;
  currentBoardingOriginTerminalName?: string;
  boardingOriginUpdatedAt?: string;
  tripStatus: 'Pending' | 'Active' | 'Completed' | 'Cancelled';
  passengerCount: number;
  totalRevenue: number;
  startTime?: string;
  endTime?: string;
  createdAt: string;
  updatedAt?: string;
  originTerminal?: Terminal;
  finalDestinationTerminal?: Terminal;
  originTerminalName?: string;
  finalDestinationTerminalName?: string;
  routeName?: string;
  startedAt?: string;
  endedAt?: string;
}

export interface Terminal {
  terminalId: number;
  terminalName: string;
  isActive: boolean;
}

export interface StartTripRequest {
  originTerminalId?: number;
  finalDestinationTerminalId?: number;
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

export interface TripHistoryResponse {
  success: boolean;
  message: string;
  data: Trip[];
  pagination: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

export const tripService = {
  /**
   * Starts a new trip for the authenticated driver.
   * Origin and destination are optional — the trip can be started immediately
   * and the driver selects them afterward for scanning.
   */
  async startTrip(originTerminalId?: number, finalDestinationTerminalId?: number): Promise<StartTripResponse> {
    const token = await authService.getToken();
    if (!token) {
      throw new Error('Not authenticated');
    }

    const response = await api.post<StartTripResponse>(
      '/api/Trip/start',
      { 
        OriginTerminalId: originTerminalId, 
        FinalDestinationTerminalId: finalDestinationTerminalId 
      }
    );

    return response;
  },

  /**
   * Ends the active trip.
   */
  async endTrip(tripId: number): Promise<EndTripResponse> {
    const token = await authService.getToken();
    if (!token) {
      throw new Error('Not authenticated');
    }

    const response = await api.post<EndTripResponse>(
      `/api/Trip/${tripId}/end`,
      {}
    );

    return response;
  },

  /**
   * Gets the active trip for the authenticated driver.
   * The backend route is GET /api/Trip/active (driver ID comes from JWT).
   */
  async getActiveTrip(): Promise<Trip | null> {
    const token = await authService.getToken();
    if (!token) {
      return null;
    }

    try {
      const response = await api.get<{ success: boolean; data: Trip }>(
        '/api/Trip/active'
      );

      if (response.success && response.data) {
        return response.data;
      }

      return null;
    } catch (error) {
      return null;
    }
  },

  /**
   * Resumes the active trip from the backend.
   * The backend is the source of truth — no localStorage state.
   */
  async resumeActiveTrip(): Promise<Trip | null> {
    return await this.getActiveTrip();
  },

  /**
   * Retrieves trip history for the authenticated driver with pagination.
   */
  async getTripHistory(page = 1, pageSize = 20): Promise<{
    data: Trip[];
    pagination: { page: number; pageSize: number; totalCount: number; totalPages: number };
  }> {
    const token = await authService.getToken();
    if (!token) {
      throw new Error('Not authenticated');
    }

    const response = await api.get<TripHistoryResponse>(
      `/api/Trip/history?page=${page}&pageSize=${pageSize}`
    );

    if (!response.success) {
      throw new Error(response.message || 'Failed to get trip history');
    }

    return {
      data: response.data,
      pagination: response.pagination,
    };
  },

  /**
   * Fetches the list of terminals for trip origin/destination selection.
   * The backend TerminalController allows Driver and Admin roles.
   */
  async getTerminals(): Promise<Terminal[]> {
    const token = await authService.getToken();
    if (!token) {
      throw new Error('Not authenticated');
    }

    const response = await api.get<{ success: boolean; data: Array<{ terminalId: number; terminalName: string; isActive: boolean }>; message?: string }>(
      '/api/terminal'
    );

    if (!response.success) {
      throw new Error(response.message || 'Failed to get terminals');
    }

    // Map Terminal response to Terminal interface (backend returns camelCase)
    return response.data.map(terminal => ({
      terminalId: terminal.terminalId,
      terminalName: terminal.terminalName,
      isActive: terminal.isActive
    }));
  },

  /**
   * Updates the current boarding origin for an active trip.
   * Only called when the conductor explicitly changes the boarding terminal.
   */
  async updateBoardingOrigin(tripId: number, originTerminalId: number): Promise<{ success: boolean; message: string }> {
    const token = await authService.getToken();
    if (!token) {
      throw new Error('Not authenticated');
    }

    const response = await api.put<{ success: boolean; message: string }>(
      `/api/Trip/${tripId}/boarding-origin`,
      { OriginTerminalId: originTerminalId }
    );

    return response;
  },
};