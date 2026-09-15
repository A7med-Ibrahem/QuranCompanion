import { apiClient } from "./client";
import type { SurahSummary, SurahDetail, Ayah, PagePosition, JuzStart } from "@/types/quran";

interface Envelope<T> { success: boolean; data: T; }

export const quranApi = {
  getAllSurahs: () =>
    apiClient.get<Envelope<SurahSummary[]>>("/quran/surahs").then((r) => r.data.data),

  getSurah: (surahNumber: number) =>
    apiClient.get<Envelope<SurahDetail>>(`/quran/surahs/${surahNumber}`).then((r) => r.data.data),

  getAyah: (surahNumber: number, numberInSurah: number) =>
    apiClient
      .get<Envelope<Ayah>>(`/quran/surahs/${surahNumber}/ayahs/${numberInSurah}`)
      .then((r) => r.data.data),

  /** First ayah of a Mushaf page (1-604). Throws if page data isn't imported. */
  getPage: (pageNumber: number) =>
    apiClient.get<Envelope<PagePosition>>(`/quran/page/${pageNumber}`).then((r) => r.data.data),

  /** All 30 juz with their Mushaf start-page + first ayah. */
  getJuzList: () =>
    apiClient.get<Envelope<JuzStart[]>>("/quran/juz").then((r) => r.data.data)
};
