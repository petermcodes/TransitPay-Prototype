import { api, getBlob } from './api';
import { authService } from './auth';

export interface User {
  userId: number;
  username: string;
  firstName: string;
  lastName: string;
  mobileNumber: string;
  isActive: boolean;
  roleName: string;
  createdAt?: string;
}

export interface Driver {
  userId: number;
  username: string;
  firstName: string;
  lastName: string;
  mobileNumber: string;
  isActive: boolean;
}

export interface Terminal {
  terminalId: number;
  terminalName: string;
  isActive: boolean;
  terminalCount: number;
}

export interface FareRule {
  fareId: number;
  originTerminalId?: number;
  destinationTerminalId?: number;
  originTerminalName: string;
  destinationTerminalName: string;
  fareAmount: number;
  effectiveDate: string;
  isActive: boolean;
}

export interface Transaction {
  transactionId: number;
  passengerName?: string;
  originTerminalName?: string;
  destinationTerminalName?: string;
  maskedCardNumber: string;
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
  originTerminalId: number;
  originTerminalName: string;
  finalDestinationTerminalId: number;
  finalDestinationTerminalName: string;
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
  userId: number;
  maskedCardNumber: string;
  passengerName: string;
  discountTypeId: number;
  discountTypeName: string;
  discountPercentage: number | null;
  status: string; // 'Pending', 'Approved', 'Rejected', 'Expired'
  discountDocument: string | null;
  approvedBy: number | null;
  approvedAt: string | null;
  rejectedAt: string | null;
  rejectionReason: string | null;
  createdAt: string;
}

export interface ReportSummary {
  totalPassengers: number;
  totalDrivers: number;
  totalTerminals: number;
  totalTransactions: number;
  totalRevenue: number;
}

export interface PassengerDiscount {
  passengerDiscountId: number;
  cardId: number;
  maskedCardNumber: string;
  passengerName?: string;
  discountTypeId: number;
  discountTypeName: string;
  discountPercentage: number;
  status: string;
  assignedAt: string;
  expiresAt?: string;
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

  async activateUser(userId: number): Promise<void> {
    const token = authService.getToken();
    const response = await api.put<{ success: boolean; message?: string }>(
      `/api/admin/users/${userId}/activate`,
      {},
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to activate user');
    }
  },

  async deactivateUser(userId: number): Promise<void> {
    const token = authService.getToken();
    const response = await api.put<{ success: boolean; message?: string }>(
      `/api/admin/users/${userId}/deactivate`,
      {},
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to deactivate user');
    }
  },

  async resetPassword(userId: number, newPassword: string): Promise<void> {
    const token = authService.getToken();
    const response = await api.post<{ success: boolean; message?: string }>(
      `/api/admin/users/${userId}/reset-password`,
      { newPassword },
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to reset password');
    }
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
    password?: string;
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

  async getCardByUserId(userId: number): Promise<{ cardId: number; cardNumber: string }> {
    const token = authService.getToken();
    const response = await api.get<ApiResponseWithMessage<{ cardId: number; cardNumber: string }>>(
      `/api/cards/user/${userId}`,
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to get card');
    }
    return response.data;
  },

  async getTerminals(): Promise<Terminal[]> {
    const token = authService.getToken();
    const response = await api.get<ApiResponseWithMessage<Terminal[]>>(
      '/api/admin/terminals',
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to get terminals');
    }
    return response.data;
  },

  async createTerminal(data: { terminalName: string }): Promise<Terminal> {
    const token = authService.getToken();
    const response = await api.post<ApiResponseWithMessage<Terminal>>(
      '/api/admin/terminals',
      data,
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to create terminal');
    }
    return response.data;
  },

  async updateTerminal(terminalId: number, data: { terminalName: string }): Promise<Terminal> {
    const token = authService.getToken();
    const response = await api.put<ApiResponseWithMessage<Terminal>>(
      `/api/admin/terminals/${terminalId}`,
      data,
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to update terminal');
    }
    return response.data;
  },

  async deleteTerminal(terminalId: number, confirm: boolean = false): Promise<{ 
    success: boolean; 
    warning?: boolean; 
    requiresConfirmation?: boolean;
    message: string; 
    affectedFareRules?: number 
  }> {
    const token = authService.getToken();
    const response = await api.delete<{ 
      success: boolean; 
      warning?: boolean; 
      requiresConfirmation?: boolean;
      message: string; 
      affectedFareRules?: number 
    }>(
      `/api/admin/terminals/${terminalId}?confirm=${confirm}`,
      token || undefined
    );
    if (!response.success && !response.warning) {
      throw new Error(response.message || 'Failed to delete terminal');
    }
    return response;
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
    originTerminalId: number;
    destinationTerminalId: number;
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

  async updateFareRule(fareId: number, data: {
    originTerminalId: number;
    destinationTerminalId: number;
    fareAmount: number;
    effectiveDate: string;
  }): Promise<FareRule> {
    const token = authService.getToken();
    const response = await api.put<ApiResponseWithMessage<FareRule>>(
      `/api/admin/fare-rules/${fareId}`,
      data,
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to update fare rule');
    }
    return response.data;
  },

  async deleteFareRule(fareId: number): Promise<void> {
    const token = authService.getToken();
    const response = await api.delete<{ success: boolean; message?: string }>(
      `/api/admin/fare-rules/${fareId}`,
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to delete fare rule');
    }
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

  // Trip Management (Read-Only)
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

  async getAllApplications(): Promise<DiscountApplication[]> {
    const token = authService.getToken();
    const response = await api.get<ApiResponseWithMessage<DiscountApplication[]>>(
      '/api/discount/applications',
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to get applications');
    }
    return response.data;
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

  async getApplicationDocument(applicationId: number): Promise<Blob> {
    const token = authService.getToken();
    return getBlob(
      `/api/discount/applications/${applicationId}/document`,
      token || undefined
    );
  },

  // Passenger Discount Management (using Discount Applications)
  async getActivePassengerDiscounts(): Promise<PassengerDiscount[]> {
    const token = authService.getToken();
    // Get all applications and filter by Approved status (status = 1)
    const response = await api.get<ApiResponseWithMessage<DiscountApplication[]>>(
      '/api/discount/applications',
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to get active passenger discounts');
    }
    // Filter to only approved applications and map to PassengerDiscount interface
    const approvedApplications = response.data.filter(app => app.status === 'Approved');
    return approvedApplications.map(app => ({
      passengerDiscountId: app.discountApplicationId,
      cardId: app.cardId,
      maskedCardNumber: app.maskedCardNumber,
      passengerName: app.passengerName,
      discountTypeId: app.discountTypeId,
      discountTypeName: app.discountTypeName,
      discountPercentage: app.discountPercentage || 0,
      status: app.status === 'Approved' ? 'Active' : 'Inactive',
      assignedAt: app.approvedAt || app.createdAt,
      expiresAt: undefined
    }));
  },

  async getAllPassengerDiscounts(): Promise<PassengerDiscount[]> {
    const token = authService.getToken();
    const response = await api.get<ApiResponseWithMessage<DiscountApplication[]>>(
      '/api/discount/applications',
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to get passenger discounts');
    }
    // Map DiscountApplication to PassengerDiscount interface
    return response.data.map(app => ({
      passengerDiscountId: app.discountApplicationId,
      cardId: app.cardId,
      maskedCardNumber: app.maskedCardNumber,
      passengerName: app.passengerName,
      discountTypeId: app.discountTypeId,
      discountTypeName: app.discountTypeName,
      discountPercentage: app.discountPercentage || 0,
      status: app.status === 'Approved' ? 'Active' : app.status === 'Pending' ? 'Pending' : app.status === 'Rejected' ? 'Rejected' : 'Expired',
      assignedAt: app.approvedAt || app.createdAt,
      expiresAt: undefined
    }));
  },

  async assignPassengerDiscount(cardId: number, discountTypeId: number): Promise<PassengerDiscount> {
    const token = authService.getToken();
    // First, create a discount application
    const createResponse = await api.post<{ success: boolean; message: string; data: DiscountApplication }>(
      '/api/discount/apply',
      { cardId, discountTypeId },
      token || undefined
    );
    if (!createResponse.success) {
      throw new Error(createResponse.message || 'Failed to create discount application');
    }
    
    // Then approve it immediately
    const approveResponse = await api.post<{ success: boolean; message?: string }>(
      `/api/discount/applications/${createResponse.data.discountApplicationId}/approve`,
      {},
      token || undefined
    );
    if (!approveResponse.success) {
      throw new Error(approveResponse.message || 'Failed to approve discount application');
    }
    
    // Return the created application mapped to PassengerDiscount interface
    return {
      passengerDiscountId: createResponse.data.discountApplicationId,
      cardId: createResponse.data.cardId,
      maskedCardNumber: createResponse.data.maskedCardNumber,
      passengerName: createResponse.data.passengerName,
      discountTypeId: createResponse.data.discountTypeId,
      discountTypeName: createResponse.data.discountTypeName,
      discountPercentage: createResponse.data.discountPercentage || 0,
      status: 'Active',
      assignedAt: new Date().toISOString(),
      expiresAt: undefined
    };
  },

  async removePassengerDiscount(passengerDiscountId: number): Promise<void> {
    const token = authService.getToken();
    // For discount applications, we reject them instead of deleting
    const response = await api.post<{ success: boolean; message?: string }>(
      `/api/discount/applications/${passengerDiscountId}/reject`,
      { rejectionReason: 'Removed by admin' },
      token || undefined
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to remove discount');
    }
  },
};