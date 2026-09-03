import { api } from './api';
import { authService } from './auth';

/**
 * A planned journey between two terminals with its locked-in fare
 * breakdown. A plan stays Active until it is used, cancelled by the
 * passenger, or expired (24 hours after creation).
 */
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

/** Fare quote for a route including the passenger's discount breakdown. */
export interface FareCalculation {
  normalFare: number;
  discountPercentage: number | null;
  discountAmount: number | null;
  finalFare: number;
}

/** Payload for creating a trip plan between two terminals. */
export interface CreateTripPlanRequest {
  originTerminalId: number;
  destinationTerminalId: number;
}

/** Payload for changing the destination of an existing trip plan. */
export interface UpdateTripPlanDestinationRequest {
  newDestinationTerminalId: number;
}

/**
 * Trip planning service for the passenger app.
 *
 * Plans lock in the fare at creation time; only one plan can be active at
 * a time — creating a new plan cancels and replaces the previous one.
 */
export const tripPlanService = {
  /**
   * Creates a new trip plan for the authenticated passenger.
   * If an active plan exists, it will be cancelled and replaced.
   */
  async createTripPlan(originTerminalId: number, destinationTerminalId: number): Promise<TripPlan> {
    const token = await authService.getToken();
    if (!token) {
      throw new Error('Not authenticated');
    }

    const response = await api.post<{ success: boolean; data: TripPlan }>(
      '/api/trip-plan',
      { originTerminalId, destinationTerminalId }
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
    const token = await authService.getToken();
    if (!token) {
      return null;
    }

    try {
      const response = await api.get<{ success: boolean; data: TripPlan }>(
        '/api/trip-plan/active'
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
    const token = await authService.getToken();
    if (!token) {
      return null;
    }

    try {
      const response = await api.get<{ success: boolean; data: TripPlan }>(
        `/api/trip-plan/${planId}`
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
   * Changes the destination of an existing trip plan and re-quotes the fare.
   */
  async updateTripPlanDestination(planId: number, newDestinationTerminalId: number): Promise<TripPlan> {
    const token = await authService.getToken();
    if (!token) {
      throw new Error('Not authenticated');
    }

    const response = await api.put<{ success: boolean; data: TripPlan }>(
      `/api/trip-plan/${planId}`,
      { newDestinationTerminalId }
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
    const token = await authService.getToken();
    if (!token) {
      throw new Error('Not authenticated');
    }

    const response = await api.delete<{ success: boolean; message: string }>(
      `/api/trip-plan/${planId}`
    );

    if (!response.success) {
      throw new Error('Failed to cancel trip plan');
    }
  },

  /**
   * Gets the authenticated passenger's trip plan history.
   */
  async getTripPlanHistory(): Promise<TripPlan[]> {
    const token = await authService.getToken();
    if (!token) {
      return [];
    }

    try {
      const response = await api.get<{ success: boolean; data: TripPlan[] }>(
        '/api/trip-plan/history'
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
    const token = await authService.getToken();
    if (!token) {
      throw new Error('Not authenticated');
    }

    const response = await api.get<{ success: boolean; data: FareCalculation }>(
      `/api/fare/calculate?originTerminalId=${originTerminalId}&destinationTerminalId=${destinationTerminalId}&cardId=${cardId}`
    );

    if (!response.success || !response.data) {
      throw new Error('Failed to calculate fare');
    }

    return response.data;
  }
};
