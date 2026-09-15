import type { Ayah } from "@/types/quran";

/**
 * Groups a surah's ayahs into real Mushaf pages using the imported per-ayah
 * Page data. Returns null when the data isn't imported yet (all ayahs have
 * a null page) so callers can fall back to measured line pagination.
 */
export function groupAyahsByPage(ayahs: Ayah[]): Ayah[][] | null {
  if (ayahs.length === 0) return [];

  if (!ayahs.every((a) => a.page !== null)) return null;

  const pages: Ayah[][] = [];
  let current: Ayah[] = [];
  let currentPage = -1;

  for (const ayah of ayahs) {
    if (ayah.page !== currentPage) {
      if (current.length > 0) pages.push(current);
      current = [];
      currentPage = ayah.page!;
    }
    current.push(ayah);
  }
  if (current.length > 0) pages.push(current);

  return pages;
}