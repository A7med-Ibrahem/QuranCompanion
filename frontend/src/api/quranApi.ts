import { apiClient } from "./client";
import type { SurahSummary, SurahDetail, Ayah } from "@/types/quran";

interface Envelope<T> { success: boolean; data: T; }

export const quranApi = {
  getAllSurahs: () =>
    apiClient.get<Envelope<SurahSummary[]>>("/quran/surahs").then((r) => r.data.data),

  getSurah: (surahNumber: number) =>
    apiClient.get<Envelope<SurahDetail>>(`/quran/surahs/${surahNumber}`).then((r) => r.data.data),

  getAyah: (surahNumber: number, numberInSurah: number) =>
    apiClient
      .get<Envelope<Ayah>>(`/quran/surahs/${surahNumber}/ayahs/${numberInSurah}`)
      .then((r) => r.data.data)
};
