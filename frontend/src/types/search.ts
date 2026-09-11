import type { SurahSummary } from "@/types/quran";

export interface AyahSearchResult {
  surahNumber: number;
  surahArabicName: string;
  ayahNumber: number;
  text: string;
  matchStart: number | null;
  matchLength: number | null;
}

export interface SearchResults {
  matchingSurahs: SurahSummary[];
  matchingAyahs: AyahSearchResult[];
}
