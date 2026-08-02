import { api } from './api';
import { authService } from './auth';

export interface User {
  userId: number;
  username: string;
  firstName: string;
  lastName: string;
  mobileNumber: string;
  isActive: boolean;
  roleName: string;
}

export interface Driver {
  userId: number;
  username: string;
  firstName: string;
  lastName: string;
  mobileNumber: string;
  isActive: boolean;
}

export interface Station {
  stationId: number;
  stationName: string;
  isActive: boolean;
  townName: string;
}

export interface Town {
  townId: number;
  townName: string;
  isActive: boolean;
  stationCount: number;
}

export interface FareRule {
  fareId: number;
  originStationName: string;
  destinationStationName: string;
  vehicleType: string;
  passengerType: string;
  fareAmount: number;
  effectiveDate: string;
  isActive: boolean;
}

export interface Transaction {
  transactionId: number;
  cardNumber: string;
  amount: number;
  transactionType: string;
  transactionName: string;
  createdAt: string;
}

export interface ReportSummary {
  totalUsers: number;
  totalDrivers: number;
  totalStations: number;
  totalTowns: number;
  totalTransactions: number;
  totalRevenue: number;
}

type ApiResponseWithMessage<T> = { success: boolean; data: T; message?: string };
type PaginatedResponse<T> = { success: boolean; data: T; pagination: { page: number; pageSize: number; total: number; totalPages: number }; message?: string };

export const adminService = {
  async getUsers(page = 1, pageSize = 20): Promise<{
    data: User[];
    pagination: { page: number; pageSize: number; total: number; totalPages: number };
  }> {
    const token = authService.getToken();
    const response = await api.get<PaginatedResponse<User[]>>(
      `/api/admin/users?page=${page}&pageSize=${pageSize}`,
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to get users');
    }
    return {
      data: response.data,
      pagination: response.pagination,
    };
  },

  async getDrivers(page = 1, pageSize = 20): Promise<{
    data: Driver[];
    pagination: { page: number; pageSize: number; total: number; totalPages: number };
  }> {
    const token = authService.getToken();
    const response = await api.get<PaginatedResponse<Driver[]>>(
      `/api/admin/drivers?page=${page}&pageSize=${pageSize}`,
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to get drivers');
    }
    return {
      data: response.data,
      pagination: response.pagination,
    };
  },

  async getStations(): Promise<Station[]> {
    const token = authService.getToken();
    const response = await api.get<ApiResponseWithMessage<Station[]>>(
      '/api/admin/stations',
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to get stations');
    }
    return response.data;
  },

  async createStation(data: { townId: number; stationName: string }): Promise<Station> {
    const token = authService.getToken();
    const response = await api.post<ApiResponseWithMessage<Station>>(
      '/api/admin/stations',
      data,
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to create station');
    }
    return response.data;
  },

  async getTowns(): Promise<Town[]> {
    const token = authService.getToken();
    const response = await api.get<ApiResponseWithMessage<Town[]>>(
      '/api/admin/towns',
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to get towns');
    }
    return response.data;
  },

  async createTown(data: { townName: string }): Promise<Town> {
    const token = authService.getToken();
    const response = await api.post<ApiResponseWithMessage<Town>>(
      '/api/admin/towns',
      data,
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to create town');
    }
    return response.data;
  },

  async getFareRules(): Promise<FareRule[]> {
    const token = authService.getToken();
    const response = await api.get<ApiResponseWithMessage<FareRule[]>>(
      '/api/admin/fare-rules',
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to get fare rules');
    }
    return response.data;
  },

  async createFareRule(data: {
    originStationId: number;
    destinationStationId: number;
    vehicleType: string;
    passengerType: string;
    fareAmount: number;
    effectiveDate: string;
  }): Promise<FareRule> {
    const token = authService.getToken();
    const response = await api.post<ApiResponseWithMessage<FareRule>>(
      '/api/admin/fare-rules',
      data,
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to create fare rule');
    }
    return response.data;
  },

  async getTransactions(page = 1, pageSize = 20): Promise<{
    data: Transaction[];
    pagination: { page: number; pageSize: number; total: number; totalPages: number };
  }> {
    const token = authService.getToken();
    const response = await api.get<PaginatedResponse<Transaction[]>>(
      `/api/admin/transactions?page=${page}&pageSize=${pageSize}`,
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

  async getReportSummary(): Promise<ReportSummary> {
    const token = authService.getToken();
    const response = await api.get<ApiResponseWithMessage<ReportSummary>>(
      '/api/admin/reports/summary',
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to get report summary');
    }
    return response.data;
  },
};