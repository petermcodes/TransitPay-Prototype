import { api } from './api';
import { authService } from './auth';

export interface CardValidation {
  cardId: number;
  cardNumber: string;
  status: string;
  balance: number;
}

export const cardService = {
  async validateCard(cardNumber: string): Promise<CardValidation> {
    const token = authService.getToken();
    const response = await api.get<{ success: boolean; data: CardValidation; message?: string }>(
      `/api/cards/validate/${encodeURIComponent(cardNumber)}`,
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Card validation failed');
    }
    return response.data;
  },
};