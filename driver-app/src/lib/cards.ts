import { api } from './api';
import { authService } from './auth';

export interface ScanReceipt {
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

export interface ScanQRResponse {
  success: boolean;
  message: string;
  data?: ScanReceipt;
}

export interface ProcessConductorPaymentRequest {
  QRData: string;
  Signature: string;
  DestinationStationId: number;
}

export interface ProcessConductorPaymentResponse {
  success: boolean;
  message: string;
  data?: ScanReceipt;
}

export interface ScanPhysicalCardRequest {
  cardNumber: string;
  destinationStationId: number;
}

export interface ScanPhysicalCardResponse {
  success: boolean;
  message: string;
  data?: ScanReceipt;
}

export const cardService = {
  /**
   * Scans a QR code and processes payment using the passenger's payment session.
   * This is the legacy flow where passenger selects route beforehand.
   */
  async scanQR(qrData: string, signature: string): Promise<ScanQRResponse> {
    const token = authService.getToken();
    const response = await api.post<ScanQRResponse>(
      '/api/payment/scan',
      { QRData: qrData, Signature: signature },
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'QR scan failed');
    }
    return response;
  },

  /**
   * Processes a conductor-initiated payment where driver scans QR and selects destination.
   * Backend calculates fare based on trip origin, destination, and card's passenger type.
   */
  async processConductorPayment(qrData: string, signature: string, destinationStationId: number): Promise<ProcessConductorPaymentResponse> {
    const token = authService.getToken();
    const response = await api.post<ProcessConductorPaymentResponse>(
      '/api/payment/process-conductor',
      { QRData: qrData, Signature: signature, DestinationStationId: destinationStationId },
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Payment processing failed');
    }
    return response;
  },

  /**
   * Scans a physical card (by card number) and processes payment.
   * The driver enters the card number manually or via NFC.
   */
  async scanPhysicalCard(cardNumber: string, destinationStationId: number): Promise<ScanPhysicalCardResponse> {
    const token = authService.getToken();
    const response = await api.post<ScanPhysicalCardResponse>(
      '/api/payment/scan-physical',
      { CardNumber: cardNumber, DestinationStationId: destinationStationId },
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Physical card scan failed');
    }
    return response;
  },
};
