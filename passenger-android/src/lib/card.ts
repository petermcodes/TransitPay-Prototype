import { api } from './api';
import { authService } from './auth';

export interface Card {
  cardId: number;
  maskedCardNumber: string;
  status: string;
  passengerType?: string;
  issueDate?: string;
  expiryDate?: string;
}

/**
 * Resolves the authenticated user's TransitPay card ID via the backend
 * GET /api/cards/me endpoint. Returns the cardId if a card is linked,
 * or null if no card exists for the user.
 */
export async function resolveCardId(userId: number): Promise<number | null> {
  const token = await authService.getToken();
  console.log('[Card] resolveCardId called for userId:', userId, 'token exists:', !!token);
  
  if (!token) {
    console.log('[Card] No token found');
    return null;
  }

  try {
    console.log('[Card] Fetching /api/cards/me...');
    const response = await api.get<{ success: boolean; data: { cardId: number; maskedCardNumber: string; status: string; passengerType: string; issueDate: string; expiryDate?: string } }>(
      '/api/cards/me'
    );
    console.log('[Card] Response:', response);
    if (!response.success || !response.data) {
      console.log('[Card] No card data in response');
      return null;
    }
    console.log('[Card] Found cardId:', response.data.cardId);
    return response.data.cardId;
  } catch (error) {
    console.error('[Card] Error fetching card:', error);
    return null;
  }
}

/**
 * Fetches the authenticated user's full card info including passenger type.
 * Used to determine the card theme based on the passenger's discount type.
 */
export async function getMyCard(userId: number): Promise<Card | null> {
  const token = await authService.getToken();
  if (!token) return null;

  try {
    const response = await api.get<{ success: boolean; data: { cardId: number; maskedCardNumber: string; status: string; passengerType: string; issueDate: string; expiryDate?: string } }>(
      '/api/cards/me'
    );
    if (!response.success || !response.data) return null;
    return {
      cardId: response.data.cardId,
      maskedCardNumber: response.data.maskedCardNumber,
      status: response.data.status,
      passengerType: response.data.passengerType,
      issueDate: response.data.issueDate,
      expiryDate: response.data.expiryDate,
    };
  } catch {
    return null;
  }
}

/**
 * Fetches a card by its card number (existing backend endpoint).
 * Used when a card number is known (e.g., from a scanned QR or manual entry).
 */
export async function getCardByNumber(cardNumber: string): Promise<Card | null> {
  const token = await authService.getToken();
  if (!token) return null;

  try {
    const response = await api.get<{ success: boolean; data: Card }>(
      `/api/cards/${cardNumber}`
    );
    if (!response.success) return null;
    return response.data;
  } catch {
    return null;
  }
}
