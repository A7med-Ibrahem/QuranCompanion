import { apiClient } from "./client";
import type { AuthResponse, UserProfile } from "@/types/auth";

interface Envelope<T> { success: boolean; data: T; }

export const authApi = {
  register: (payload: { email: string; password: string; displayName: string }) =>
    apiClient.post<Envelope<AuthResponse>>("/auth/register", payload).then((r) => r.data.data),

  login: (payload: { email: string; password: string }) =>
    apiClient.post<Envelope<AuthResponse>>("/auth/login", payload).then((r) => r.data.data),

  googleLogin: (idToken: string) =>
    apiClient.post<Envelope<AuthResponse>>("/auth/google", { idToken }).then((r) => r.data.data),

  refresh: () =>
    apiClient.post<Envelope<AuthResponse>>("/auth/refresh").then((r) => r.data.data),

  logout: () => apiClient.post("/auth/logout"),

  forgotPassword: (email: string) =>
    apiClient.post("/auth/forgot-password", { email }),

  resetPassword: (payload: { email: string; code: string; newPassword: string }) =>
    apiClient.post("/auth/reset-password", payload),

  me: () => apiClient.get<Envelope<UserProfile>>("/auth/me").then((r) => r.data.data),

  updateProfile: (payload: { displayName: string }) =>
    apiClient.put<Envelope<UserProfile>>("/auth/me", payload).then((r) => r.data.data)
};
