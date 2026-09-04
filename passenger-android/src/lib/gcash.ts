/**
 * Simulated GCash top-up service for the passenger app.
 *
 * Mirrors a real GCash redirect checkout: the app asks the backend for a checkout
 * session (payment intent), the user "authenticates" in the simulated GCash screen
 * (mobile number + sandbox OTP), and confirmation credits the wallet server-side.
 * The whole flow runs against the TransitPay API in sandbox mode — no real money
 * moves. Swapping in a real payment gateway later only changes backend endpoints.
 */
import { api } from './api';

/** A GCash top-up checkout session created by the backend (payment intent). */
export interface GcashTopUpSession {
  sessionId: string;
  cardId: number;
  amount: number;
  transactionReferenceNumber?: string;
  status: string;
  expiresAt: string;
  gcashReference?: string;
}

/** Outcome of confirming a simulated GCash payment. */
export interface GcashTopUpConfirmResult {
  success: boolean;
  message: string;
  sessionStatus: string;
  attemptsRemaining: number;
  transactionReferenceNumber?: string;
  gcashReference?: string;
  newBalance?: number;
}

/** GCash top-up operations against the sandbox gateway endpoints. */
export const gcashService = {
  /**
   * Creates a checkout session for the given card and amount. The linked
   * TOP_UP transaction is created PENDING server-side.
   */
  async initiate(cardId: number, amount: number): Promise<GcashTopUpSession> {
    const response = await api.post<{ success: boolean; data: GcashTopUpSession; message?: string }>(
      '/api/topup/gcash/initiate',
      { cardId, amount }
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to start GCash payment');
    }
    return response.data;
  },

  /**
   * Confirms the payment with the (sandbox) OTP. Business outcomes (wrong code,
   * failed payment) are returned in the result — only transport/session errors throw.
   */
  async confirm(sessionId: string, otp: string): Promise<GcashTopUpConfirmResult> {
    const response = await api.post<{ success: boolean; data: GcashTopUpConfirmResult; message?: string }>(
      '/api/topup/gcash/confirm',
      { sessionId, otp }
    );
    if (!response.data) {
      throw new Error(response.message || 'GCash payment failed');
    }
    return response.data;
  },

  /** Voids an open checkout session (pending transaction is CANCELLED server-side). */
  async cancel(sessionId: string): Promise<GcashTopUpSession> {
    const response = await api.post<{ success: boolean; data: GcashTopUpSession; message?: string }>(
      '/api/topup/gcash/cancel',
      { sessionId }
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to cancel GCash payment');
    }
    return response.data;
  },

  /** Fetches the current state of a checkout session (used for polling/refresh). */
  async getStatus(sessionId: string): Promise<GcashTopUpSession> {
    const response = await api.get<{ success: boolean; data: GcashTopUpSession; message?: string }>(
      `/api/topup/gcash/status/${sessionId}`
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to get payment status');
    }
    return response.data;
  },
};
