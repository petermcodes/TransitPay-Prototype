import { api } from './api';

/**
 * An authenticated user as returned by the auth endpoints.
 */
export interface User {
  userId: number;
  username: string;
  firstName: string;
  lastName: string;
  mobileNumber: string;
  roleId: number;
  roleName?: string;
}

/**
 * Payload returned by a successful login or token refresh: a short-lived JWT,
 * a long-lived refresh token, and the authenticated user.
 */
export interface LoginResponse {
  token: string;
  refreshToken: string;
  user: User;
}

/**
 * Credentials accepted by the login endpoint.
 */
export interface LoginRequest {
  username: string;
  password: string;
}

// localStorage keys — prefixed per app so multiple TransitPay frontends on the
// same origin (dev) do not overwrite each other's sessions.
const TOKEN_KEY = 'transitpay_admin_token';
const REFRESH_TOKEN_KEY = 'transitpay_admin_refresh_token';
const USER_KEY = 'transitpay_admin_user';

/**
 * Admin-dashboard auth service.
 *
 * Persists the JWT, refresh token and user profile in localStorage and
 * exposes helpers for login/logout, token validation and refresh. All other
 * services read the token from here to build `Authorization` headers.
 */
export const authService = {
  /**
   * Authenticates against `/api/auth/login` and, on success, stores the
   * token, refresh token and user in localStorage.
   */
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

  /**
   * Verifies the stored token against `/api/auth/validate`. On success the
   * stored user profile is refreshed; on failure all auth state is cleared.
   * Used at app startup to restore an existing session.
   */
  async validateToken(): Promise<boolean> {
    const token = this.getToken();
    if (!token) {
      return false;
    }
    try {
      const response = await api.get<{ success: boolean; data: User }>(
        '/api/auth/validate',
        token
      );
      if (response.success && response.data) {
        // Update stored user info with fresh data from server
        localStorage.setItem(USER_KEY, JSON.stringify(response.data));
        return true;
      }
      return false;
    } catch {
      // Token is invalid or expired — clear all auth state
      this.clearAuth();
      return false;
    }
  },

  /**
   * Removes all stored auth state (token, refresh token, user).
   */
  clearAuth(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  },

  /**
   * Exchanges a refresh token for a new token pair and persists both.
   * The old refresh token is revoked server-side (rotation).
   */
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

  /**
   * Revokes the server-side session (best effort) and clears local auth state.
   */
  async logout(): Promise<void> {
    const userId = this.getUser()?.userId;
    const token = this.getToken();
    if (userId && token) {
      try {
        await api.post('/api/auth/logout', {}, token);
      } catch {
        // Server-side token revocation is best-effort; clear local state regardless.
      }
    }
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  },

  /** Returns the stored JWT, or `null` when logged out. */
  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  },

  /** Returns the stored refresh token, or `null` when logged out. */
  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  },

  /** Returns the stored user profile, or `null` when absent/corrupted. */
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

  /** `true` when a token is present locally (without server validation). */
  isAuthenticated(): boolean {
    return !!this.getToken();
  },
};