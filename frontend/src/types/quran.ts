export interface SurahSummary {
  number: number;
  arabicName: string;
  englishName: string;
  englishNameTranslation: string;
  numberOfAyahs: number;
  revelationType: "Meccan" | "Medinan";
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

export interface ReadingProgress {
  surahNumber: number;
  ayahNumber: number;
  surahArabicName: string;
  updatedAtUtc: string;
}
