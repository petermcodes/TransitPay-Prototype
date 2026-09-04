import { Capacitor } from '@capacitor/core';
import { api } from './api';

/** Authenticated passenger profile as returned by the auth endpoints. */
export interface User {
  userId: number;
  firstName: string;
  lastName: string;
  mobileNumber: string;
  roleId: number;
  roleName?: string;
}

/** Successful login/refresh payload containing both tokens plus the user. */
export interface LoginResponse {
  token: string;
  refreshToken: string;
  user: User;
}

/** Payload for creating a new passenger account. */
export interface RegisterRequest {
  username: string;
  firstName: string;
  lastName: string;
  mobileNumber: string;
  password: string;
}

/** Payload for the username/password login endpoint. */
export interface LoginRequest {
  username: string;
  password: string;
}

/** Payload for exchanging a refresh token for a new token pair. */
export interface RefreshTokenRequest {
  userId: number;
  refreshToken: string;
}

const TOKEN_KEY = 'transitpay_token';
const REFRESH_TOKEN_KEY = 'transitpay_refresh_token';
const USER_KEY = 'transitpay_user';

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

/**
 * Authentication/session service for the passenger app.
 *
 * Handles registration, login, token refresh and logout against the backend,
 * persisting the token pair and user profile in storage (localStorage today;
 * see the TODO above about secure storage for production).
 */
export const authService = {
  /** Creates a new passenger account. Does not start a session. */
  async register(data: RegisterRequest) {
    return api.post<{ success: boolean; message: string; data: { userId: number; role: string } }>(
      '/api/auth/register',
      data
    );
  },

  /** Logs in and persists the token pair and user profile to storage. */
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

  /** Exchanges a refresh token for a new token pair and persists it. */
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

  /**
   * Logs out: best-effort server-side token revocation, then clears all
   * locally stored session data (token, refresh token, user).
   */
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

  /** Returns the stored access token, or null when logged out. */
  async getToken(): Promise<string | null> {
    return await secureGet(TOKEN_KEY);
  },

  /** Returns the stored refresh token, or null when logged out. */
  async getRefreshToken(): Promise<string | null> {
    return await secureGet(REFRESH_TOKEN_KEY);
  },

  /** Returns the persisted user profile, or null if absent/corrupt. */
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

  /** True when an access token is present (does not validate it server-side). */
  async isAuthenticated(): Promise<boolean> {
    const token = await this.getToken();
    return token !== null && token.length > 0;
  },
};