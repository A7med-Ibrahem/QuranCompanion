import { apiClient } from "./client";
import type { ReadingProgress } from "@/types/quran";

interface Envelope<T> { success: boolean; data: T; }

export const readingProgressApi = {
  getMine: () =>
    apiClient.get<Envelope<ReadingProgress | null>>("/reading-progress/me").then((r) => r.data.data),

  updateMine: (surahNumber: number, ayahNumber: number) =>
    apiClient
      .put<Envelope<ReadingProgress>>("/reading-progress/me", { surahNumber, ayahNumber })
      .then((r) => r.data.data)
};
