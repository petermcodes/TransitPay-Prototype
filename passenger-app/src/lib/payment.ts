import { api } from './api';
import { authService } from './auth';

export interface CreatePaymentSessionRequest {
  cardId: number;
  originStationId: number;
  destinationStationId: number;
}

export interface PaymentSessionData {
  paymentSessionId: string;
  cardId: number;
  userId: number;
  originStationId: number;
  destinationStationId: number;
  originStationName?: string;
  destinationStationName?: string;
  lockedFare: number;
  status: string;
  createdAt: string;
  updatedAt?: string;
  expiresAt: string;
}

export interface PaymentSessionResult {
  success: boolean;
  message: string;
  data?: PaymentSessionData;
}

export interface QRTicket {
  data: string;
  signature: string;
  cardId: number;
  cardNumber?: string;
}

export interface PaymentResult {
  paymentSessionId: string;
  cardId: number;
  passengerName?: string;
  maskedCardNumber?: string;
  originStationId: number;
  destinationStationId: number;
  originStationName?: string;
  destinationStationName?: string;
  lockedFare: number;
  remainingBalance: number;
  transactionReferenceNumber?: string;
  paymentTimestamp: string;
  driverId?: number;
  transactionName?: string;
}

export const paymentService = {
  /**
   * Get the active payment session for a card.
   * Kept for backward compatibility but not used in the new conductor-initiated flow.
   */
  async getActiveSession(cardId: number): Promise<PaymentSessionResult> {
    const token = authService.getToken();
    const response = await api.get<PaymentSessionResult>(
      `/api/payment/session/${cardId}`,
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to get active session');
    }
    return response;
  },

  /**
   * Get the permanent QR code for a card.
   * This QR code is shown to the driver for scanning.
   */
  async getQR(cardId: number): Promise<QRTicket> {
    const token = authService.getToken();
    const response = await api.get<{ success: boolean; data: QRTicket }>(
      `/api/payment/qr/${cardId}`,
      token || undefined
    );
    if (!response.success) {
      throw new Error('Failed to get QR code');
    }
    return response.data;
  },
};
