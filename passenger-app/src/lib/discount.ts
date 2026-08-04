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
  discountTypeId: number;
  discountTypeName?: string;
  discountPercentage?: number;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Expired';
  approvedBy?: number;
  approvedAt?: string;
  rejectedAt?: string;
  rejectionReason?: string;
  discountDocument?: string;
  createdAt: string;
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
};