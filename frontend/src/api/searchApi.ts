import { apiClient } from "./client";
import type { SearchResults } from "@/types/search";

interface Envelope<T> { success: boolean; data: T; }

export const searchApi = {
  search: (query: string) =>
    apiClient
      .get<Envelope<SearchResults>>("/quran/search", { params: { q: query } })
      .then((r) => r.data.data)
};
