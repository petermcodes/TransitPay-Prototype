/**
 * Thin HTTP client for the passenger app.
 *
 * Wraps `fetch` with the API base URL (from VITE_API_URL), automatic Bearer
 * token injection, transparent refresh-token retry on 401, and forced logout
 * + redirect to /login when the session cannot be recovered.
 */
import { authService } from './auth';

const API_BASE = import.meta.env.VITE_API_URL || '';

/**
 * Performs an HTTP request with JSON headers and the stored access token.
 * On a 401 it attempts one refresh-token round-trip and replays the original
 * request; if the session is still invalid the user is logged out and the
 * promise rejects with 'Session expired'.
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

export const api = {
  /** GET and parse the JSON body as `T`. */
  get: <T>(endpoint: string) =>
    request<T>(endpoint, { method: 'GET' }),

  /** POST a JSON-serialized body and parse the response as `T`. */
  post: <T>(endpoint: string, data: unknown) =>
    request<T>(endpoint, {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  /** PUT a JSON-serialized body and parse the response as `T`. */
  put: <T>(endpoint: string, data: unknown) =>
    request<T>(endpoint, {
      method: 'PUT',
      body: JSON.stringify(data),
    }),

  /** DELETE a resource and parse the response as `T`. */
  delete: <T>(endpoint: string) =>
    request<T>(endpoint, { method: 'DELETE' }),
};

/** Standard backend envelope: every endpoint returns success/message/data. */
export type ApiResponse<T> = {
  success: boolean;
  message: string;
  data: T;
};