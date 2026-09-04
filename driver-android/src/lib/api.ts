/**
 * HTTP client for the driver app.
 *
 * Thin fetch-based wrapper around the TransitPay API that automatically
 * attaches the driver's JWT access token and transparently handles token
 * refresh: on a 401 it attempts a refresh-token grant and retries the
 * original request once; if that still fails the session is cleared and the
 * user is redirected to the login screen.
 */
import { authService } from './auth';

const API_BASE = import.meta.env.VITE_API_URL || '';

/**
 * Performs an HTTP request against the TransitPay API with auth handling.
 *
 * Flow: attach `Authorization: Bearer <token>` when a token exists → on 401
 * attempt a refresh-token grant and retry once → if still unauthorized,
 * clear the session and hard-redirect to /login. Non-OK responses throw an
 * Error carrying the API's `message` field when available.
 */
async function request<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const url = `${API_BASE}${endpoint}`;
  let token = await authService.getToken();

  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    ...options.headers,
  };

  if (token) {
    (headers as Record<string, string>)['Authorization'] = `Bearer ${token}`;
  }

  let response = await fetch(url, {
    ...options,
    headers,
  });

  // Handle 401 - try to refresh token
  if (response.status === 401) {
    const refreshToken = await authService.getRefreshToken();

    if (refreshToken) {
      try {
        const refreshResponse = await fetch(`${API_BASE}/api/auth/refresh`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            userId: (await authService.getUser())?.userId,
            refreshToken,
          }),
        });

        if (refreshResponse.ok) {
          const refreshData = await refreshResponse.json();
          if (refreshData.success && refreshData.data) {
            // Store new tokens
            await authService.refreshToken(
              refreshData.data.user.userId,
              refreshData.data.refreshToken
            );

            // Retry original request with new token
            token = await authService.getToken();
            const newHeaders: HeadersInit = {
              'Content-Type': 'application/json',
              ...options.headers,
            };

            if (token) {
              (newHeaders as Record<string, string>)['Authorization'] = `Bearer ${token}`;
            }

            response = await fetch(url, {
              ...options,
              headers: newHeaders,
            });
          }
        }
      } catch (error) {
        console.error('Token refresh failed:', error);
      }
    }

    // If still 401 or refresh failed, logout
    if (response.status === 401) {
      await authService.logout();
      // Redirect to login
      window.location.href = '/login';
      throw new Error('Session expired');
    }
  }

  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: 'Network error' }));
    throw new Error(error.message || `HTTP ${response.status}`);
  }

  return response.json();
}

/**
 * Convenience HTTP verbs bound to {@link request}. Each call resolves with
 * the parsed JSON body typed as `T` (the API's response envelope).
 */
export const api = {
  /** GET request. */
  get: <T>(endpoint: string) =>
    request<T>(endpoint, { method: 'GET' }),

  /** POST request with a JSON-serialized body. */
  post: <T>(endpoint: string, data: unknown) =>
    request<T>(endpoint, {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  /** PUT request with a JSON-serialized body. */
  put: <T>(endpoint: string, data: unknown) =>
    request<T>(endpoint, {
      method: 'PUT',
      body: JSON.stringify(data),
    }),

  /** DELETE request. */
  delete: <T>(endpoint: string) =>
    request<T>(endpoint, { method: 'DELETE' }),
};

/**
 * Standard response envelope returned by every TransitPay API endpoint:
 * `success` flags the business-level outcome, `message` carries
 * user-facing error text, and `data` holds the typed payload.
 */
export type ApiResponse<T> = {
  success: boolean;
  message: string;
  data: T;
};