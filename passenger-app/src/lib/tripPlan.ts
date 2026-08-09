import { api } from './api';
import { authService } from './auth';

export interface TripPlan {
  planId: number;
  cardId: number;
  originTerminalId: number;
  originTerminalName: string;
  destinationTerminalId: number;
  destinationTerminalName: string;
  status: string;
  createdAt: string;
  expiresAt: string | null;
  usedAt: string | null;
  normalFare: number;
  discountAmount: number | null;
  discountPercentage: number | null;
  finalFarePrice: number;
}

export interface FareCalculation {
  normalFare: number;
  discountPercentage: number | null;
  discountAmount: number | null;
  finalFare: number;
}

export interface CreateTripPlanRequest {
  originTerminalId: number;
  destinationTerminalId: number;
}

export interface UpdateTripPlanDestinationRequest {
  newDestinationTerminalId: number;
}

export const tripPlanService = {
  /**
   * Creates a new trip plan for the authenticated passenger.
   * If an active plan exists, it will be cancelled and replaced.
   */
  async createTripPlan(originTerminalId: number, destinationTerminalId: number): Promise<TripPlan> {
    const token = authService.getToken();
    if (!token) {
      throw new Error('Not authenticated');
    }

    const response = await api.post<{ success: boolean; data: TripPlan }>(
      '/api/trip-plan',
      { originTerminalId, destinationTerminalId },
      token
    );

    if (!response.success || !response.data) {
      throw new Error('Failed to create trip plan');
    }

    return response.data;
  },

  /**
   * Gets the authenticated passenger's active trip plan.
   */
  async getActiveTripPlan(): Promise<TripPlan | null> {
    const token = authService.getToken();
    if (!token) {
      return null;
    }

    try {
      const response = await api.get<{ success: boolean; data: TripPlan }>(
        '/api/trip-plan/active',
        token
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
   * Gets a specific trip plan by ID.
   */
  async getTripPlanById(planId: number): Promise<TripPlan | null> {
    const token = authService.getToken();
    if (!token) {
      return null;
    }

    try {
      const response = await api.get<{ success: boolean; data: TripPlan }>(
        `/api/trip-plan/${planId}`,
        token
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
   * Gets the authenticated passenger's trip plan history.
   */
  async updateTripPlanDestination(planId: number, newDestinationTerminalId: number): Promise<TripPlan> {
    const token = authService.getToken();
    if (!token) {
      throw new Error('Not authenticated');
    }

    const response = await api.put<{ success: boolean; data: TripPlan }>(
      `/api/trip-plan/${planId}`,
      { newDestinationTerminalId },
      token
    );

    if (!response.success || !response.data) {
      throw new Error('Failed to update trip plan');
    }

    return response.data;
  },

  /**
   * Cancels an active trip plan.
   */
  async cancelTripPlan(planId: number): Promise<void> {
    const token = authService.getToken();
    if (!token) {
      throw new Error('Not authenticated');
    }

    const response = await api.delete<{ success: boolean; message: string }>(
      `/api/trip-plan/${planId}`,
      token
    );

    if (!response.success) {
      throw new Error('Failed to cancel trip plan');
    }
  },

  /**
   * Gets the authenticated passenger's trip plan history.
   */
  async getTripPlanHistory(): Promise<TripPlan[]> {
    const token = authService.getToken();
    if (!token) {
      return [];
    }

    try {
      const response = await api.get<{ success: boolean; data: TripPlan[] }>(
        '/api/trip-plan/history',
        token
      );

      if (response.success && response.data) {
        return response.data;
      }

      return [];
    } catch (error) {
      return [];
    }
  },

  /**
   * Calculates fare for a given route and card.
   */
  async calculateFare(originTerminalId: number, destinationTerminalId: number, cardId: number): Promise<FareCalculation> {
    const token = authService.getToken();
    if (!token) {
      throw new Error('Not authenticated');
    }

    const response = await api.get<{ success: boolean; data: FareCalculation }>(
      `/api/fare/calculate?originTerminalId=${originTerminalId}&destinationTerminalId=${destinationTerminalId}&cardId=${cardId}`,
      token
    );

    if (!response.success || !response.data) {
      throw new Error('Failed to calculate fare');
    }

    return response.data;
  }
};
