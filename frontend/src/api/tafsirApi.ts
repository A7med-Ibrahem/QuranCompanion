import { apiClient } from "./client";
import type { Tafsir } from "@/types/tafsir";

interface Envelope<T> { success: boolean; data: T; }

export const tafsirApi = {
  get: (surahNumber: number, ayahNumber: number) =>
    apiClient
      .get<Envelope<Tafsir>>(`/quran/surahs/${surahNumber}/ayahs/${ayahNumber}/tafsir`)
      .then((r) => r.data.data)
};
