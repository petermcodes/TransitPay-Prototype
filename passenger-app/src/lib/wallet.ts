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
}

export interface Transaction {
  transactionId: number;
  cardId: number;
  stationId?: number;
  amount: number;
  transactionType: string;
  transactionName: string;
  createdAt: string;
}

export const walletService = {
  async getWallet(cardId: number): Promise<Wallet> {
    const token = authService.getToken();
    const response = await api.get<{ success: boolean; data: Wallet; message?: string }>(
      `/api/wallet/${cardId}`,
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to get wallet');
    }
    return response.data;
  },

  async topUp(cardId: number, amount: number): Promise<Wallet> {
    const token = authService.getToken();
    const response = await api.post<{ success: boolean; data: Wallet; message?: string }>(
      '/api/wallet/topup',
      { cardId, amount },
      token || undefined
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
    const token = authService.getToken();
    const response = await api.get<{ success: boolean; data: Transaction[]; pagination: { page: number; pageSize: number; total: number; totalPages: number }; message?: string }>(
      `/api/transactions/${cardId}?page=${page}&pageSize=${pageSize}`,
      token || undefined
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