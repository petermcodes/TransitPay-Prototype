import { api, getBlob } from './api';
import { authService } from './auth';

/**
 * A user account shown in the admin users table.
 */
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

/**
 * A driver account shown in the admin drivers table.
 */
export interface Driver {
  userId: number;
  username: string;
  firstName: string;
  lastName: string;
  mobileNumber: string;
  isActive: boolean;
}

/**
 * A bus terminal/station.
 */
export interface Terminal {
  terminalId: number;
  terminalName: string;
  isActive: boolean;
  terminalCount: number;
}

/**
 * A fare matrix entry (route + vehicle/passenger type → fare).
 */
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

/**
 * A financial transaction record shown in the admin transactions table.
 */
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

/**
 * A driver trip (journey) monitored from the admin dashboard.
 */
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

/**
 * A configurable discount type (e.g., Student, Senior).
 */
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

/**
 * A passenger's discount application (approval workflow).
 */
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

/**
 * Aggregate counters for the admin dashboard report summary.
 */
export interface ReportSummary {
  totalPassengers: number;
  totalDrivers: number;
  totalTerminals: number;
  totalTransactions: number;
  totalRevenue: number;
}

/**
 * A discount assigned to a passenger's card (derived from an approved application).
 */
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

/**
 * Admin API client.
 *
 * Thin wrapper over `./api` that adds JWT auth headers and normalizes server
 * responses: every method resolves with typed data or throws an `Error` with
 * the server-provided message. Covers user/driver accounts, terminals, fare
 * rules, transactions, reports, trip monitoring and the discount workflow.
 */
export const adminService = {
  /**
   * Retrieves a paginated list of all user accounts.
   */
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

  /**
   * Re-activates a user account (sets IsActive = true).
   */
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

  /**
   * Deactivates a user account (sets IsActive = false). Deactivated users cannot log in.
   */
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

  /**
   * Resets a user's password (applies the server-side password policy).
   */
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

  /**
   * Lists all driver accounts.
   */
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

  /**
   * Creates a new driver account. When no password is supplied, the server uses the
   * generated Driver ID as the default password.
   */
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

  /**
   * Adds funds (`amount`) to a card's wallet (admin top-up).
   */
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

  /**
   * Fetches a user's transit card (masked number) for wallet/discount operations.
   */
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

  /**
   * Lists all non-deleted terminals.
   */
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

  /**
   * Creates a new terminal.
   */
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

  /**
   * Renames a terminal.
   */
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

  /**
   * Deletes a terminal. When the terminal has fare rules, the server responds with a
   * `warning` + `requiresConfirmation` rather than an error so the UI can confirm first.
   */
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

  /**
   * Lists all fare rules.
   */
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

  /**
   * Creates a fare matrix entry for a route.
   */
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

  /**
   * Updates an existing fare matrix entry (route + amount + effective date).
   */
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

  /**
   * Deletes a fare matrix entry.
   */
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

  /**
   * Retrieves a paginated list of financial transactions.
   */
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

  /**
   * Fetches aggregate report counters (passengers, drivers, terminals,
   * transactions, revenue) for the dashboard summary tiles.
   */
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
  /**
   * Retrieves a paginated list of driver trips for monitoring.
   */
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

  /**
   * Fetches a single trip (with route, status, passenger count and revenue).
   */
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
  /**
   * Lists all discount types (active and inactive).
   */
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

  /**
   * Creates a discount type (e.g., Student 20%). `requiresApproval` controls
   * whether passengers must submit an application before it can be used.
   */
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

  /**
   * Updates an existing discount type's name, percentage or approval policy.
   */
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

  /**
   * Permanently deletes a discount type. Prefer deactivate for soft disable.
   */
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

  /**
   * Reactivates a discount type so it can be selected by passengers again.
   */
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

  /**
   * Soft-disables a discount type without deleting it.
   */
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
  /**
   * Lists discount applications waiting for admin review (status = Pending).
   */
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

  /**
   * Lists all discount applications in every status (Pending/Approved/Rejected/Expired).
   */
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

  /**
   * Approves a pending discount application. On the server this activates the
   * corresponding passenger discount.
   */
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

  /**
   * Rejects a pending discount application with an optional reason that is
   * shown to the passenger.
   */
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

  /**
   * Downloads the supporting document uploaded with a discount application
   * (e.g., student ID) as a binary blob for preview/download.
   */
  async getApplicationDocument(applicationId: number): Promise<Blob> {
    const token = authService.getToken();
    return getBlob(
      `/api/discount/applications/${applicationId}/document`,
      token || undefined
    );
  },

  // Passenger Discount Management (using Discount Applications)
  /**
   * Lists the passenger discounts currently in effect, derived client-side
   * from approved discount applications (there is no dedicated endpoint).
   */
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

  /**
   * Lists all passenger discounts in every status, mapped from the
   * underlying discount applications (no dedicated endpoint exists).
   */
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

  /**
   * Manually grants a passenger discount from the admin dashboard.
   *
   * Implemented as a two-step server flow because discounts are always
   * backed by applications: 1) create an application, 2) approve it
   * immediately so the discount takes effect without passenger action.
   */
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

  /**
   * Revokes a passenger discount. Because discounts are backed by discount
   * applications, removal is performed by rejecting the application with a
   * fixed reason rather than deleting a record.
   */
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