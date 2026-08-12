import { authService } from './auth';

const API_BASE = import.meta.env.VITE_API_URL || '';

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
  get: <T>(endpoint: string) =>
    request<T>(endpoint, { method: 'GET' }),

  post: <T>(endpoint: string, data: unknown) =>
    request<T>(endpoint, {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  put: <T>(endpoint: string, data: unknown) =>
    request<T>(endpoint, {
      method: 'PUT',
      body: JSON.stringify(data),
    }),

  delete: <T>(endpoint: string) =>
    request<T>(endpoint, { method: 'DELETE' }),
};

export type ApiResponse<T> = {
  success: boolean;
  message: string;
  data: T;
};