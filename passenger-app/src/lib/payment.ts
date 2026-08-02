import { api } from './api';
import { authService } from './auth';

export interface PaymentRequest {
  cardId: number;
  stationId: number;
  amount: number;
}

export interface FarePreview {
  cardId: number;
  stationId: number;
  fareAmount: number;
}

export const paymentService = {
  async previewFare(cardId: number, stationId: number): Promise<FarePreview> {
    const token = authService.getToken();
    const response = await api.get<{ success: boolean; data: FarePreview }>(
      `/api/payment/fare/${cardId}/${stationId}`,
      token || undefined
    );
    if (!response.success) {
      throw new Error('Failed to preview fare');
    }
    return response.data;
  },

  async payFare(data: PaymentRequest): Promise<{ success: boolean; message: string; data: unknown }> {
    const token = authService.getToken();
    const response = await api.post<{ success: boolean; message: string; data: unknown }>(
      '/api/payment/fare',
      data,
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Payment failed');
    }
    return response;
  },
};