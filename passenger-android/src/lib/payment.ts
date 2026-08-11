import { api } from './api';
import { authService } from './auth';

export interface QRTicket {
  data: string;
  signature: string;
  cardId: number;
  maskedCardNumber?: string;
}

export const qrService = {
  /**
   * Get the permanent QR code for a card.
   * This QR code is shown to the driver for scanning.
   */
  async getQR(cardId: number): Promise<QRTicket> {
    const token = await authService.getToken();
    const response = await api.get<{ success: boolean; data: QRTicket }>(
      `/api/payment/qr/${cardId}`
    );
    if (!response.success) {
      throw new Error('Failed to get QR code');
    }
    return response.data;
  },
};
