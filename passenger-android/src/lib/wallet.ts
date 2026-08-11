import { api } from './api';
import { authService } from './auth';

export interface Wallet {
  walletId: number;
  cardId: number;
  balance: number;
  status: string;
  createdAt: string;
  updatedAt?: string;
}

export interface TopUpRequest {
  cardId: number;
  amount: number;
  paymentMode?: string;
}

export interface Transaction {
  transactionId: number;
  cardId: number;
  stationId?: number;
  amount: number;
  transactionType: string;
  transactionName: string;
  status?: string;
  transactionReferenceNumber?: string;
  originTerminalId?: number;
  originTerminalName?: string;
  terminalId?: number;
  destinationTerminalName?: string;
  finalFare?: number;
  remainingBalance?: number;
  paymentMode?: string;
  driverName?: string;
  maskedCardNumber?: string;
  createdAt: string;
}

export interface WalletStats {
  totalTopUp: number;
  totalSpent: number;
}

/**
 * Computes wallet statistics (Total Top Up / Total Spent) from a list of
 * transactions. Values are derived from the live API data — never fabricated.
 */
export function computeWalletStats(transactions: Transaction[]): WalletStats {
  const now = new Date();
  const startOfMonth = new Date(now.getFullYear(), now.getMonth(), 1);

  let totalTopUp = 0;
  let totalSpent = 0;

  for (const tx of transactions) {
    const createdAt = new Date(tx.createdAt);
    if (createdAt < startOfMonth) continue;

    const type = tx.transactionType.toLowerCase();
    if (type === 'top_up' || type === 'topup') {
      totalTopUp += tx.amount;
    } else if (type === 'payment' || type === 'fare') {
      totalSpent += tx.amount;
    }
  }

  return { totalTopUp, totalSpent };
}

export const walletService = {
  async getWallet(cardId: number): Promise<Wallet> {
    const token = await authService.getToken();
    const response = await api.get<{ success: boolean; data: Wallet; message?: string }>(
      `/api/wallet/${cardId}`
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to get wallet');
    }
    return response.data;
  },

  async topUp(cardId: number, amount: number, paymentMode?: string): Promise<Wallet> {
    const token = await authService.getToken();
    const response = await api.post<{ success: boolean; data: Wallet; message?: string }>(
      '/api/wallet/topup',
      { cardId, amount, paymentMode }
    );
    if (!response.success) {
      throw new Error(response.message || 'Top-up failed');
    }
    return response.data;
  },

  async getTransactions(cardId: number, page = 1, pageSize = 20): Promise<{
    data: Transaction[];
    pagination: { page: number; pageSize: number; total: number; totalPages: number };
  }> {
    const token = await authService.getToken();
    const response = await api.get<{ success: boolean; data: Transaction[]; pagination: { page: number; pageSize: number; total: number; totalPages: number }; message?: string }>(
      `/api/transactions/${cardId}?page=${page}&pageSize=${pageSize}`
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to get transactions');
    }
    return {
      data: response.data,
      pagination: response.pagination,
    };
  },
};