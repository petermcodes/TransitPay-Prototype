/**
 * Base URL for all API calls. Empty string in development so Vite's dev-server
 * proxy handles `/api` requests; set via `VITE_API_URL` in production builds.
 */
const API_BASE = import.meta.env.VITE_API_URL || '';

/**
 * Core fetch wrapper for the admin dashboard.
 *
 * Adds JSON headers, attaches the JWT as `Authorization: Bearer <token>` when
 * provided, and throws an `Error` (with the server's `message` field when
 * available) for non-2xx responses.
 */
async function request<T>(
  endpoint: string,
  options: RequestInit = {},
  token?: string
): Promise<T> {
  const url = `${API_BASE}${endpoint}`;
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    ...options.headers,
  };

  if (token) {
    (headers as Record<string, string>)['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(url, {
    ...options,
    headers,
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: 'Network error' }));
    throw new Error(error.message || `HTTP ${response.status}`);
  }

  return response.json();
}

/**
 * REST helper object used by the feature services (`auth`, `admin`, etc.).
 * Every call returns the parsed JSON body; HTTP failures throw.
 */
export const api = {
  /** Performs a GET request. `token` is optional for public endpoints. */
  get: <T>(endpoint: string, token?: string) =>
    request<T>(endpoint, { method: 'GET' }, token),

  /** Performs a POST request with a JSON-serialized body. */
  post: <T>(endpoint: string, data: unknown, token?: string) =>
    request<T>(endpoint, {
      method: 'POST',
      body: JSON.stringify(data),
    }, token),

  /** Performs a PUT request with a JSON-serialized body. */
  put: <T>(endpoint: string, data: unknown, token?: string) =>
    request<T>(endpoint, {
      method: 'PUT',
      body: JSON.stringify(data),
    }, token),

  /** Performs a DELETE request. */
  delete: <T>(endpoint: string, token?: string) =>
    request<T>(endpoint, { method: 'DELETE' }, token),
};

// Specialized blob fetcher for file downloads
// The generic api.get<T> uses response.json() which fails on binary responses
/**
 * Fetches an endpoint and returns the raw response body as a {@link Blob}
 * (used for binary downloads such as discount-application documents).
 */
export async function getBlob(endpoint: string, token?: string): Promise<Blob> {
  const url = `${API_BASE}${endpoint}`;
  const headers: HeadersInit = {
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };

  const response = await fetch(url, {
    method: 'GET',
    headers,
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: 'Network error' }));
    throw new Error(error.message || `HTTP ${response.status}`);
  }

  return response.blob();
}

/**
 * Standard envelope returned by TransitPay API endpoints:
 * `success` indicates the business outcome, `message` carries a
 * human-readable error/success text, and `data` holds the payload.
 */
export type ApiResponse<T> = {
  success: boolean;
  message: string;
  data: T;
};
