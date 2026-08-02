import { api } from './api';

export interface User {
  userId: number;
  firstName: string;
  lastName: string;
  mobileNumber: string;
  roleId: number;
  roleName?: string;
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
  user: User;
}

export interface LoginRequest {
  mobileNumber: string;
  password: string;
}

const TOKEN_KEY = 'transitpay_admin_token';
const REFRESH_TOKEN_KEY = 'transitpay_admin_refresh_token';
const USER_KEY = 'transitpay_admin_user';

export const authService = {
  async login(data: LoginRequest): Promise<LoginResponse> {
    const response = await api.post<{ success: boolean; data: LoginResponse }>(
      '/api/auth/login',
      data
    );
    if (response.success && response.data) {
      localStorage.setItem(TOKEN_KEY, response.data.token);
      localStorage.setItem(REFRESH_TOKEN_KEY, response.data.refreshToken);
      localStorage.setItem(USER_KEY, JSON.stringify(response.data.user));
    }
    return response.data;
  },

  async refreshToken(userId: number, refreshToken: string): Promise<LoginResponse> {
    const response = await api.post<{ success: boolean; data: LoginResponse }>(
      '/api/auth/refresh',
      { userId, refreshToken }
    );
    if (response.success && response.data) {
      localStorage.setItem(TOKEN_KEY, response.data.token);
      localStorage.setItem(REFRESH_TOKEN_KEY, response.data.refreshToken);
    }
    return response.data;
  },

  logout() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  },

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  },

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  },

  getUser(): User | null {
    const userStr = localStorage.getItem(USER_KEY);
    if (userStr) {
      try {
        return JSON.parse(userStr);
      } catch {
        return null;
      }
    }
    return null;
  },

  isAuthenticated(): boolean {
    return !!this.getToken();
  },
};