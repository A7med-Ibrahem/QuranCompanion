import axios, { AxiosError } from "axios";
import type { AuthResponse } from "@/types/auth";

const baseURL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5080/api";
// The refresh token itself lives only in an httpOnly cookie set by the API
// (see AuthController.SetRefreshCookie) - the browser JS never touches it.
export const apiClient = axios.create({
  baseURL,
  withCredentials: true
});

let accessToken: string | null = null;
let onUnauthorized: (() => void) | null = null;

export function setAccessToken(token: string | null) {
  accessToken = token;
}

export function setUnauthorizedHandler(handler: () => void) {
  onUnauthorized = handler;
}

apiClient.interceptors.request.use((config) => {
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  return config;
});

let refreshPromise: Promise<AuthResponse | null> | null = null;

/**
 * The single source of truth for refreshing the session. Both AuthContext's
 * initial page-load check AND the 401-retry interceptor below call this same
 * function, sharing one in-flight promise if they happen at the same time.
 *
 * This matters because our refresh tokens rotate (each successful refresh
 * revokes the old one and issues a new one) - two *separate* concurrent calls
 * to /auth/refresh would race: the second one arrives after the first has
 * already revoked its token, gets a 401, and would otherwise incorrectly log
 * the person out. That race is exactly what caused "refreshing the page
 * sometimes kicks me back to login" - most visibly in dev, where React's
 * StrictMode intentionally double-invokes effects (like AuthContext's mount
 * effect), which used to fire two independent refresh calls at once.
 */
export async function silentRefresh(): Promise<AuthResponse | null> {
  refreshPromise ??= (async () => {
    try {
      const { data } = await apiClient.post<{ success: boolean; data: AuthResponse }>("/auth/refresh");
      setAccessToken(data.data.accessToken);
      return data.data;
    } catch {
      setAccessToken(null);
      return null;
    }
  })().finally(() => {
    refreshPromise = null;
  });

  return refreshPromise;
}

apiClient.interceptors.response.use(
  (res) => res,
  async (error: AxiosError) => {
    const original = error.config as (typeof error.config & { _retried?: boolean }) | undefined;

    if (error.response?.status === 401 && original && !original._retried && !original.url?.includes("/auth/refresh")) {
      original._retried = true;
      const result = await silentRefresh();

      if (result) {
        original.headers = original.headers ?? {};
        original.headers.Authorization = `Bearer ${result.accessToken}`;
        return apiClient(original);
      }

      onUnauthorized?.();
    }

    return Promise.reject(error);
  }
);

export function extractApiError(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const message = error.response?.data?.error?.message;
    if (message) return message as string;
  }
  return "حدث خطأ غير متوقع. حاول مرة أخرى.";
}
