import { api } from './api';
import { authService } from './auth';

export interface ScanReceipt {
  cardId: number;
  passengerName?: string;
  maskedCardNumber?: string;
  originTerminalId: number;
  destinationTerminalId: number;
  originTerminalName?: string;
  destinationTerminalName?: string;
  lockedFare: number;
  remainingBalance: number;
  transactionReferenceNumber?: string;
  paymentTimestamp: string;
  driverId?: number;
  transactionName?: string;
}

export interface ProcessConductorPaymentRequest {
  QRData: string;
  Signature: string;
  OriginTerminalId: number;
  DestinationTerminalId: number;
}

export interface ProcessConductorPaymentResponse {
  success: boolean;
  message: string;
  data?: ScanReceipt;
}

export interface ScanPhysicalCardRequest {
  cardNumber: string;
  originTerminalId: number;
  destinationTerminalId: number;
}

export interface ScanPhysicalCardResponse {
  success: boolean;
  message: string;
  data?: ScanReceipt;
}

export interface DriverTransaction {
  transactionId: number;
  cardId: number;
  amount: number;
  transactionType: string;
  transactionName: string;
  status: string;
  transactionReferenceNumber?: string;
  originTerminalId: number;
  originTerminalName?: string;
  terminalId: number;
  destinationTerminalName?: string;
  finalFare: number;
  remainingBalance: number;
  paymentMode?: string;
  passengerName: string;
  maskedCardNumber?: string;
  createdAt: string;
}

export interface DriverTransactionsResponse {
  success: boolean;
  message: string;
  data: DriverTransaction[];
  pagination: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

export const cardService = {
  /**
   * Processes a conductor-initiated payment where driver scans QR.
   * Backend reads destination from passenger's active trip plan.
   */
  async processConductorPayment(qrData: string, signature: string): Promise<ProcessConductorPaymentResponse> {
    const token = authService.getToken();
    const response = await api.post<ProcessConductorPaymentResponse>(
      '/api/payment/process-conductor',
      { QRData: qrData, Signature: signature },
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Payment processing failed');
    }
    return response;
  },

  /**
   * Scans a physical card (by card number) and processes payment.
   * Backend reads destination from passenger's active trip plan.
   */
  async scanPhysicalCard(cardNumber: string): Promise<ScanPhysicalCardResponse> {
    const token = authService.getToken();
    const response = await api.post<ScanPhysicalCardResponse>(
      '/api/payment/scan-physical',
      { CardNumber: cardNumber },
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Physical card scan failed');
    }
    return response;
  },

  /**
   * Fetches recent transactions processed by the authenticated driver.
   * Used to display recent passengers in the driver home screen.
   */
  async getDriverTransactions(page = 1, pageSize = 10): Promise<DriverTransactionsResponse> {
    const token = authService.getToken();
    if (!token) {
      throw new Error('Not authenticated');
    }

    const response = await api.get<DriverTransactionsResponse>(
      `/api/Transactions/driver?page=${page}&pageSize=${pageSize}`,
      token
    );

    return response;
  },
};
