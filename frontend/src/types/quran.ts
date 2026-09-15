export interface SurahSummary {
  number: number;
  arabicName: string;
  englishName: string;
  englishNameTranslation: string;
  numberOfAyahs: number;
  revelationType: "Meccan" | "Medinan";
  /** Mushaf page where this surah begins. Null until the pages/juz import runs. */
  startPage?: number | null;
  startJuz?: number | null;
}

export interface Ayah {
  surahNumber: number;
  numberInSurah: number;
  globalNumber: number;
  text: string;
  juz: number | null;
  page: number | null;
}

export interface SurahDetail {
  surah: SurahSummary;
  ayahs: Ayah[];
}

/** Where a Mushaf page begins - its first ayah plus the juz it lives in. */
export interface PagePosition {
  pageNumber: number;
  juz: number;
  surahNumber: number;
  ayahNumber: number;
}

/** Where a juz begins - first ayah plus the page it starts on. */
export interface JuzStart {
  juz: number;
  startPage: number;
  surahNumber: number;
  ayahNumber: number;
  surahArabicName: string;
}

export interface ReadingProgress {
  surahNumber: number;
  ayahNumber: number;
  surahArabicName: string;
  updatedAtUtc: string;
}
