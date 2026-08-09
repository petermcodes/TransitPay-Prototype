import { api } from './api';
import { authService } from './auth';

export interface DiscountType {
  discountTypeId: number;
  name: string;
  description?: string;
  discountPercentage: number;
  isActive: boolean;
  requiresApproval: boolean;
}

export interface DiscountApplication {
  discountApplicationId: number;
  cardId: number;
  userId?: number;
  discountTypeId: number;
  discountTypeName?: string;
  discountPercentage?: number;
  status: string; // 'Pending', 'Approved', 'Rejected', 'Expired'
  approvedBy?: number;
  approvedAt?: string;
  rejectedAt?: string;
  rejectionReason?: string;
  discountDocument?: string;
  createdAt: string;
}

/**
 * Maps discount application status string values to their display names.
 * The backend serializes the DiscountApplicationStatus enum as strings.
 */
export const DISCOUNT_STATUS: Record<string, string> = {
  Pending: 'Pending',
  Approved: 'Approved',
  Rejected: 'Rejected',
  Expired: 'Expired',
};

/**
 * Converts a discount application status string to its display name.
 * Falls back to 'Pending' for unknown values.
 */
export function getDiscountStatusName(status: string): string {
  return DISCOUNT_STATUS[status] || 'Pending';
}

export interface DiscountApplicationRequest {
  cardId: number;
  discountTypeId: number;
  discountDocument?: string;
}

export const discountService = {
  /**
   * Get all available discount types
   */
  async getDiscountTypes(): Promise<DiscountType[]> {
    const token = authService.getToken();
    const response = await api.get<{ success: boolean; data: DiscountType[] }>(
      '/api/Discount/types',
      token || undefined
    );
    if (!response.success || !response.data) {
      throw new Error('Failed to get discount types');
    }
    return response.data;
  },

  /**
   * Apply for a discount
   */
  async applyForDiscount(cardId: number, discountTypeId: number, discountDocument?: string): Promise<DiscountApplication> {
    const token = authService.getToken();
    const response = await api.post<{ success: boolean; data: DiscountApplication; message?: string }>(
      '/api/Discount/apply',
      { cardId, discountTypeId, discountDocument },
      token || undefined
    );
    if (!response.success || !response.data) {
      throw new Error(response.message || 'Failed to apply for discount');
    }
    return response.data;
  },

  /**
   * Get my discount applications
   */
  async getMyApplications(cardId: number): Promise<DiscountApplication[]> {
    const token = authService.getToken();
    const response = await api.get<{ success: boolean; data: DiscountApplication[] }>(
      `/api/Discount/applications/card/${cardId}`,
      token || undefined
    );
    if (!response.success || !response.data) {
      throw new Error('Failed to get applications');
    }
    return response.data;
  },

  /**
   * Get the user's current active discount type (if any approved application exists)
   * Queries the PassengerDiscounts table directly for the materialized active discount.
   */
  async getCurrentDiscountType(cardId: number): Promise<DiscountType | null> {
    try {
      const token = authService.getToken();
      const response = await api.get<{ 
        success: boolean; 
        data: { 
          discountTypeName?: string; 
          discountPercentage?: number;
          status?: string;  // 'Active', 'Expired', 'Revoked'
        } | null 
      }>(
        `/api/Discount/active/${cardId}`,
        token || undefined
      );
      
      if (!response.success || !response.data || !response.data.discountTypeName) {
        return null;
      }

      // Validate that the status is Active
      // PassengerDiscountStatus enum serialized as string: 'Active', 'Expired', 'Revoked'
      const status = response.data.status;
      if (status !== undefined && status !== 'Active') {
        // Discount exists but is not active (expired or revoked)
        return null;
      }

      // Return a DiscountType-like object with the discount program name
      return {
        discountTypeId: 0, // Not needed for theme lookup
        name: response.data.discountTypeName,
        discountPercentage: response.data.discountPercentage || 0,
        isActive: true,
        requiresApproval: false
      };
    } catch {
      return null;
    }
  },
};

/**
 * Returns the card theme colors based on the passenger's discount type.
 * Checks discount type name first, then falls back to passenger type.
 * Regular passengers always get the blue card.
 */
export function getCardTheme(passengerType?: string | number, discountTypeName?: string): {
  from: string;
  to: string;
  label: string;
} {
  // First check discount type name (takes priority)
  if (discountTypeName) {
    const discountLower = discountTypeName.toLowerCase();
    if (discountLower.includes('student')) {
      return { from: '#059669', to: '#10B981', label: 'Student' };
    }
    if (discountLower.includes('senior')) {
      return { from: '#6D28D9', to: '#8B5CF6', label: 'Senior Citizen' };
    }
    if (discountLower.includes('disabled') || discountLower.includes('pwd')) {
      return { from: '#EA580C', to: '#F59E0B', label: 'PWD' };
    }
  }

  // Convert enum integer to string name if needed
  // Backend PassengerType enum: 0=Passenger, 1=Student, 2=Senior, 3=DISABLED
  const PassengerTypeMap: Record<number, string> = {
    0: 'passenger',
    1: 'student',
    2: 'senior',
    3: 'disabled'
  };
  
  let passengerTypeStr: string | undefined;
  if (typeof passengerType === 'number') {
    passengerTypeStr = PassengerTypeMap[passengerType]?.toLowerCase();
  } else if (typeof passengerType === 'string') {
    passengerTypeStr = passengerType.toLowerCase();
  }

  // Fall back to passenger type
  switch (passengerTypeStr) {
    case 'student':
      return { from: '#059669', to: '#10B981', label: 'Student' };
    case 'senior':
      return { from: '#6D28D9', to: '#8B5CF6', label: 'Senior Citizen' };
    case 'disabled':
      return { from: '#EA580C', to: '#F59E0B', label: 'PWD' };
    default:
      return { from: '#1E3A8A', to: '#2563EB', label: 'Regular' };
  }
}
