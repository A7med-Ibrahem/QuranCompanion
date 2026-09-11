import type { Ayah } from "@/types/quran";

/**
 * Splits a surah's ayahs into pages that each take up roughly `targetLines`
 * lines of rendered text - the same idea as a printed Mushaf's fixed line
 * count per page (traditionally 15), just measured live in the browser
 * instead of using official page-break data we don't have imported.
 *
 * Works by actually rendering ayahs one at a time into a hidden measuring
 * element with the exact same markup/classes as the real reader, and
 * watching its height grow - this is the only reliable way to know where
 * justified, variable-width Arabic text will wrap, short of having real
 * Mushaf line-break data.
 */
export function paginateByLines(ayahs: Ayah[], measureEl: HTMLElement, targetLines: number): Ayah[][] {
  measureEl.innerHTML = "";

  const computed = window.getComputedStyle(measureEl);
  const lineHeightPx = parseFloat(computed.lineHeight);
  const targetHeightPx = (Number.isFinite(lineHeightPx) ? lineHeightPx : 40) * targetLines;

  const pages: Ayah[][] = [];
  let currentPage: Ayah[] = [];

  for (const ayah of ayahs) {
    const span = buildAyahSpan(ayah);
    measureEl.appendChild(span);

    if (measureEl.scrollHeight > targetHeightPx && currentPage.length > 0) {
      measureEl.removeChild(span);
      pages.push(currentPage);
      currentPage = [];
      measureEl.innerHTML = "";
      measureEl.appendChild(span);
      currentPage.push(ayah);
    } else {
      currentPage.push(ayah);
    }
  }

  if (currentPage.length > 0) pages.push(currentPage);
  measureEl.innerHTML = "";

  return pages.length > 0 ? pages : [ayahs];
}

function buildAyahSpan(ayah: Ayah): HTMLSpanElement {
  const span = document.createElement("span");
  span.className = "ayah-text";
  span.textContent = ayah.text;

  const marker = document.createElement("span");
  marker.className = "ayah-marker-wrap";
  const markerInner = document.createElement("span");
  markerInner.className = "ayah-marker";
  markerInner.textContent = String(ayah.numberInSurah);
  marker.appendChild(markerInner);
  span.appendChild(marker);

  return span;
}
