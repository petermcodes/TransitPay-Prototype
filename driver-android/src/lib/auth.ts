/**
 * Authentication service for the driver app.
 *
 * Wraps the `/api/auth/*` endpoints (login, refresh, logout) and persists
 * the JWT access token, refresh token and cached user profile in
 * localStorage through the secure* helpers below. Storage keys are
 * driver-specific so the driver app never collides with passenger storage.
 */
import { api } from './api';

/** Authenticated driver profile as returned by the API. */
export interface User {
  userId: number;
  firstName: string;
  lastName: string;
  mobileNumber: string;
  roleId: number;
  roleName?: string;
}

/** Successful login/refresh payload: the token pair plus the user profile. */
export interface LoginResponse {
  token: string;
  refreshToken: string;
  user: User;
}

/** Credentials accepted by the login endpoint (drivers log in with a username). */
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
  /**
   * Authenticates a driver and persists the returned token pair plus the
   * user profile. Returns the full login payload on success.
   */
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

  /**
   * Exchanges a refresh token for a new token pair and persists it.
   * Called by the API client's automatic 401-recovery path.
   */
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
   * Revokes the server-side session (best effort) and clears all locally
   * stored auth state, even if the server call fails.
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

  /** Returns the persisted JWT access token, or null when logged out. */
  async getToken(): Promise<string | null> {
    return await secureGet(TOKEN_KEY);
  },

  /** Returns the persisted refresh token, or null when logged out. */
  async getRefreshToken(): Promise<string | null> {
    return await secureGet(REFRESH_TOKEN_KEY);
  },

  /** Returns the cached user profile, or null when absent or unparseable. */
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

  /** True when an access token is present locally (no server-side validation). */
  async isAuthenticated(): Promise<boolean> {
    const token = await this.getToken();
    return token !== null && token.length > 0;
  },
};