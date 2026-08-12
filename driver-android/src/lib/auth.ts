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
  username: string;
  password: string;
}

const TOKEN_KEY = 'transitpay_driver_token';
const REFRESH_TOKEN_KEY = 'transitpay_driver_refresh_token';
const USER_KEY = 'transitpay_driver_user';

// Storage helpers - uses localStorage for now
// TODO: Add @capacitor-community/secure-storage for production
function secureSet(key: string, value: string): void {
  localStorage.setItem(key, value);
}

function secureGet(key: string): string | null {
  return localStorage.getItem(key);
}

function secureRemove(key: string): void {
  localStorage.removeItem(key);
}

export const authService = {
  async login(data: LoginRequest): Promise<LoginResponse> {
    const response = await api.post<{ success: boolean; data: LoginResponse }>(
      '/api/auth/login',
      data
    );
    if (response.success && response.data) {
      await secureSet(TOKEN_KEY, response.data.token);
      await secureSet(REFRESH_TOKEN_KEY, response.data.refreshToken);
      await secureSet(USER_KEY, JSON.stringify(response.data.user));
    }
    return response.data;
  },

  async refreshToken(userId: number, refreshToken: string): Promise<LoginResponse> {
    const response = await api.post<{ success: boolean; data: LoginResponse }>(
      '/api/auth/refresh',
      { userId, refreshToken }
    );
    if (response.success && response.data) {
      await secureSet(TOKEN_KEY, response.data.token);
      await secureSet(REFRESH_TOKEN_KEY, response.data.refreshToken);
    }
    return response.data;
  },

  async logout(): Promise<void> {
    const user = await this.getUser();
    const token = await this.getToken();
    if (user?.userId && token) {
      try {
        await api.post('/api/auth/logout', {});
      } catch {
        // Server-side token revocation is best-effort; clear local state regardless.
      }
    }
    await secureRemove(TOKEN_KEY);
    await secureRemove(REFRESH_TOKEN_KEY);
    await secureRemove(USER_KEY);
  },

  async getToken(): Promise<string | null> {
    return await secureGet(TOKEN_KEY);
  },

  async getRefreshToken(): Promise<string | null> {
    return await secureGet(REFRESH_TOKEN_KEY);
  },

  async getUser(): Promise<User | null> {
    const userStr = await secureGet(USER_KEY);
    if (userStr) {
      try {
        return JSON.parse(userStr);
      } catch {
        return null;
      }
    }
    return null;
  },

  async isAuthenticated(): Promise<boolean> {
    const token = await this.getToken();
    return token !== null && token.length > 0;
  },
};