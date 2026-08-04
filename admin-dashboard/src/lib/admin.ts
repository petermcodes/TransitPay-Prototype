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
  transactionReferenceNumber?: string;
  referenceNumber?: string;
  createdAt: string;
}

export interface Trip {
  tripId: number;
  driverId: number;
  driverName: string;
  originStationId: number;
  originStationName: string;
  finalDestinationStationId: number;
  finalDestinationStationName: string;
  routeName: string;
  tripStatus: string;
  startedAt: string | null;
  endedAt: string | null;
  passengerCount: number;
  totalRevenue: number;
  createdAt: string;
}

export interface DiscountType {
  discountTypeId: number;
  name: string;
  description: string | null;
  discountPercentage: number;
  isActive: boolean;
  requiresApproval: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface DiscountApplication {
  discountApplicationId: number;
  cardId: number;
  cardNumber: string;
  discountTypeId: number;
  discountTypeName: string;
  discountPercentage: number | null;
  status: string;
  discountDocument: string | null;
  approvedBy: number | null;
  approvedAt: string | null;
  rejectedAt: string | null;
  rejectionReason: string | null;
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

  async getDrivers(): Promise<Driver[]> {
    const token = authService.getToken();
    const response = await api.get<ApiResponseWithMessage<Driver[]>>(
      `/api/driver`,
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to get drivers');
    }
    return response.data;
  },

  async createDriver(data: {
    firstName: string;
    lastName: string;
    mobileNumber: string;
    password: string;
  }): Promise<Driver> {
    const token = authService.getToken();
    const response = await api.post<ApiResponseWithMessage<Driver>>(
      '/api/driver',
      data,
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to create driver');
    }
    return response.data;
  },

  async approveDriver(driverId: number): Promise<void> {
    const token = authService.getToken();
    const response = await api.put<{ success: boolean; message?: string }>(
      `/api/driver/${driverId}/approve`,
      {},
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to approve driver');
    }
  },

  async rejectDriver(driverId: number): Promise<void> {
    const token = authService.getToken();
    const response = await api.put<{ success: boolean; message?: string }>(
      `/api/driver/${driverId}/reject`,
      {},
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to reject driver');
    }
  },

  async topUpWallet(cardId: number, amount: number): Promise<{ balance: number }> {
    const token = authService.getToken();
    const response = await api.post<ApiResponseWithMessage<{ balance: number }>>(
      '/api/wallet/topup',
      { cardId, amount },
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to top up wallet');
    }
    return response.data;
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

  // Trip Management
  async getTrips(page = 1, pageSize = 20): Promise<{
    data: Trip[];
    pagination: { page: number; pageSize: number; total: number; totalPages: number };
  }> {
    const token = authService.getToken();
    const response = await api.get<PaginatedResponse<Trip[]>>(
      `/api/admin/trips?page=${page}&pageSize=${pageSize}`,
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to get trips');
    }
    return {
      data: response.data,
      pagination: response.pagination,
    };
  },

  async getTripById(tripId: number): Promise<Trip> {
    const token = authService.getToken();
    const response = await api.get<ApiResponseWithMessage<Trip>>(
      `/api/admin/trips/${tripId}`,
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to get trip');
    }
    return response.data;
  },

  async endTrip(tripId: number): Promise<void> {
    const token = authService.getToken();
    const response = await api.post<{ success: boolean; message?: string }>(
      `/api/admin/trips/${tripId}/end`,
      {},
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to end trip');
    }
  },

  async cancelTrip(tripId: number): Promise<void> {
    const token = authService.getToken();
    const response = await api.post<{ success: boolean; message?: string }>(
      `/api/admin/trips/${tripId}/cancel`,
      {},
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to cancel trip');
    }
  },

  // Discount Type Management
  async getDiscountTypes(): Promise<DiscountType[]> {
    const token = authService.getToken();
    const response = await api.get<ApiResponseWithMessage<DiscountType[]>>(
      '/api/discount/types',
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to get discount types');
    }
    return response.data;
  },

  async createDiscountType(data: {
    name: string;
    description?: string;
    discountPercentage: number;
    requiresApproval: boolean;
  }): Promise<DiscountType> {
    const token = authService.getToken();
    const response = await api.post<ApiResponseWithMessage<DiscountType>>(
      '/api/discount/types',
      data,
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to create discount type');
    }
    return response.data;
  },

  async updateDiscountType(discountTypeId: number, data: {
    name: string;
    description?: string;
    discountPercentage: number;
    requiresApproval: boolean;
  }): Promise<DiscountType> {
    const token = authService.getToken();
    const response = await api.put<ApiResponseWithMessage<DiscountType>>(
      `/api/discount/types/${discountTypeId}`,
      data,
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to update discount type');
    }
    return response.data;
  },

  async deleteDiscountType(discountTypeId: number): Promise<void> {
    const token = authService.getToken();
    const response = await api.delete<{ success: boolean; message?: string }>(
      `/api/discount/types/${discountTypeId}`,
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to delete discount type');
    }
  },

  async activateDiscountType(discountTypeId: number): Promise<void> {
    const token = authService.getToken();
    const response = await api.post<{ success: boolean; message?: string }>(
      `/api/discount/types/${discountTypeId}/activate`,
      {},
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to activate discount type');
    }
  },

  async deactivateDiscountType(discountTypeId: number): Promise<void> {
    const token = authService.getToken();
    const response = await api.post<{ success: boolean; message?: string }>(
      `/api/discount/types/${discountTypeId}/deactivate`,
      {},
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to deactivate discount type');
    }
  },

  // Discount Application Management
  async getPendingApplications(): Promise<DiscountApplication[]> {
    const token = authService.getToken();
    const response = await api.get<ApiResponseWithMessage<DiscountApplication[]>>(
      '/api/discount/applications/pending',
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to get pending applications');
    }
    return response.data;
  },

  async getAllApplications(page = 1, pageSize = 20): Promise<{
    data: DiscountApplication[];
    pagination: { page: number; pageSize: number; total: number; totalPages: number };
  }> {
    const token = authService.getToken();
    const response = await api.get<PaginatedResponse<DiscountApplication[]>>(
      `/api/discount/applications?page=${page}&pageSize=${pageSize}`,
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to get applications');
    }
    return {
      data: response.data,
      pagination: response.pagination,
    };
  },

  async approveApplication(applicationId: number): Promise<void> {
    const token = authService.getToken();
    const response = await api.post<{ success: boolean; message?: string }>(
      `/api/discount/applications/${applicationId}/approve`,
      {},
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to approve application');
    }
  },

  async rejectApplication(applicationId: number, rejectionReason?: string): Promise<void> {
    const token = authService.getToken();
    const response = await api.post<{ success: boolean; message?: string }>(
      `/api/discount/applications/${applicationId}/reject`,
      { rejectionReason },
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to reject application');
    }
  },
};
