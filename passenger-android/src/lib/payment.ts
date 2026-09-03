/**
 * QR-based payment service for the passenger app.
 *
 * The passenger's permanent card QR code is the payment instrument: it is
 * generated once per card (server-side signed) and presented to the driver
 * for scanning when boarding.
 */
import { api } from './api';
import { authService } from './auth';

/**
 * Server-signed QR ticket payload for a card.
 * `data` is the QR content, `signature` proves backend authenticity so
 * drivers can reject forged codes.
 */
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
   * Uses POST to generate QR if it doesn't exist, or retrieve existing one.
   */
  async getQR(cardId: number): Promise<QRTicket> {
    const token = await authService.getToken();
    const response = await api.post<{ success: boolean; data: QRTicket }>(
      '/api/payment/qr',
      { cardId }
    );
    if (!response.success) {
      throw new Error('Failed to get QR code');
    }
    return response.data;
  },
};
