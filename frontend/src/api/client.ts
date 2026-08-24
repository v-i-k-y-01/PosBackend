/**
 * The base API URL loaded from Vite environment variables.
 */
const API_URL = import.meta.env.VITE_API_URL ?? '';

/**
 * Custom error class representing API communication and status code errors.
 */
export class ApiError extends Error {
  /**
   * Initializes a new instance of the ApiError.
   * @param message - The error message.
   * @param status - The HTTP status code.
   */
  constructor(message: string, public status: number) {
    super(message);
    this.name = 'ApiError';
  }
}

/**
 * Executes an HTTP fetch request against the backend API.
 * Automatically injects the stored JWT token and content headers.
 * @param path - The sub-route path of the API.
 * @param options - Additional RequestInit parameters.
 * @returns The resolved response payload.
 */
export async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const accessToken = localStorage.getItem('pos.accessToken');
  
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
    ...options.headers as Record<string, string>
  };

  const response = await fetch(`${API_URL}${path}`, {
    ...options,
    headers
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new ApiError(body?.error ?? 'Something went wrong. Please try again.', response.status);
  }

  // HTTP status 204 represents NoContent, return undefined.
  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}
