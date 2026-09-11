export interface Bookmark {
  surahNumber: number;
  surahArabicName: string;
  ayahNumber: number;
  ayahText: string;
  note: string | null;
  createdAtUtc: string;
}
