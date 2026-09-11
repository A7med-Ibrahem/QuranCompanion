import { apiClient } from "./client";
import type { WirdPlan, TodayWird, WirdType } from "@/types/wird";

interface Envelope<T> { success: boolean; data: T; }

export const wirdApi = {
  getPlan: () =>
    apiClient.get<Envelope<WirdPlan | null>>("/wird/plan").then((r) => r.data.data),

  setPlan: (type: WirdType, customAyahsPerDay?: number, targetCompletionDate?: string) =>
    apiClient
      .put<Envelope<WirdPlan>>("/wird/plan", { type, customAyahsPerDay, targetCompletionDate })
      .then((r) => r.data.data),

  getToday: () =>
    apiClient.get<Envelope<TodayWird | null>>("/wird/today").then((r) => r.data.data),

  completeToday: () =>
    apiClient.post<Envelope<TodayWird>>("/wird/complete").then((r) => r.data.data),

  getMyStreak: () =>
    apiClient.get<Envelope<{ currentStreak: number }>>("/wird/streak").then((r) => r.data.data)
};
