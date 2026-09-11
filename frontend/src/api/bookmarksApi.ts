import { apiClient } from "./client";
import type { Bookmark } from "@/types/bookmark";

interface Envelope<T> { success: boolean; data: T; }

export const bookmarksApi = {
  getAll: () =>
    apiClient.get<Envelope<Bookmark[]>>("/bookmarks").then((r) => r.data.data),

  add: (surahNumber: number, ayahNumber: number, note?: string) =>
    apiClient
      .post<Envelope<Bookmark>>("/bookmarks", { surahNumber, ayahNumber, note })
      .then((r) => r.data.data),

  remove: (surahNumber: number, ayahNumber: number) =>
    apiClient.delete(`/bookmarks/${surahNumber}/${ayahNumber}`),

  updateNote: (surahNumber: number, ayahNumber: number, note: string) =>
    apiClient
      .put<Envelope<Bookmark>>(`/bookmarks/${surahNumber}/${ayahNumber}/note`, { note })
      .then((r) => r.data.data)
};
