import { useEffect, useLayoutEffect, useRef, useState, type TouchEvent } from "react";
import { Link, useLocation, useNavigate, useParams, useSearchParams } from "react-router-dom";
import { quranApi } from "@/api/quranApi";
import { readingProgressApi } from "@/api/readingProgressApi";
import { bookmarksApi } from "@/api/bookmarksApi";
import { TafsirModal } from "@/components/TafsirModal";
import { paginateByLines } from "@/utils/paginateByLines";
import type { SurahDetail, Ayah } from "@/types/quran";
import { extractApiError } from "@/api/client";

const FONT_STEPS = [1.4, 1.7, 2.0, 2.4, 2.8]; // rem, applied to .ayah-text

// Matches the traditional 15-line-per-page Mushaf layout, measured live in
// the browser (see paginateByLines) rather than using official page-break
// data we don't have imported - see the README note on this.
const TARGET_LINES_PER_PAGE = 15;
const SWIPE_THRESHOLD_PX = 60;

interface NavState {
  landOn?: "last";
}

export function SurahReaderPage() {
  const { surahNumber } = useParams<{ surahNumber: string }>();
  const [searchParams] = useSearchParams();
  const location = useLocation();
  const navigate = useNavigate();
  const number = Number(surahNumber);
  const targetAyah = Number(searchParams.get("ayah") ?? "0");
  const landOnLast = (location.state as NavState | null)?.landOn === "last";

  const [detail, setDetail] = useState<SurahDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [fontStep, setFontStep] = useState(1);
  const [bookmarked, setBookmarked] = useState<Set<number>>(new Set());
  const [openMenuAyah, setOpenMenuAyah] = useState<number | null>(null);
  const [copiedAyah, setCopiedAyah] = useState<number | null>(null);
  const [tafsirAyah, setTafsirAyah] = useState<number | null>(null);
  const [pages, setPages] = useState<Ayah[][]>([]);
  const [pageIndex, setPageIndex] = useState(0);
  const [highlightAyah, setHighlightAyah] = useState<number | null>(null);

  const touchStartX = useRef<number | null>(null);
  const lastSavedAyah = useRef<number | null>(null);
  const measureRef = useRef<HTMLDivElement>(null);
  const pendingAnchorAyah = useRef<number | null>(null); // keeps your place across a repagination

  useEffect(() => {
    setLoading(true);
    setError(null);
    pendingAnchorAyah.current = targetAyah || null;

    quranApi
      .getSurah(number)
      .then(setDetail)
      .catch((err) => setError(extractApiError(err)))
      .finally(() => setLoading(false));

    bookmarksApi
      .getAll()
      .then((all) => {
        const mine = all.filter((b) => b.surahNumber === number).map((b) => b.ayahNumber);
        setBookmarked(new Set(mine));
      })
      .catch(() => {
        /* not critical - the reader still works without bookmark highlighting */
      });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [number]);

  // Re-measures pages whenever the surah loads, the font size changes, or the
  // viewport is resized (all of which change how much text fits per page).
  // Keeps you on the same ayah across a repagination instead of resetting to page 1.
  useLayoutEffect(() => {
    if (!detail || !measureRef.current) return;

    function repaginate() {
      if (!detail || !measureRef.current) return;
      const computed = paginateByLines(detail.ayahs, measureRef.current, TARGET_LINES_PER_PAGE);
      setPages(computed);

      const anchor = pendingAnchorAyah.current;
      if (anchor) {
        const idx = computed.findIndex((page) => page.some((a) => a.numberInSurah === anchor));
        setPageIndex(idx >= 0 ? idx : 0);
        if (targetAyah === anchor) {
          setHighlightAyah(anchor);
          setTimeout(() => setHighlightAyah(null), 2500);
        }
      } else if (landOnLast) {
        setPageIndex(Math.max(0, computed.length - 1));
      } else {
        setPageIndex((prev) => Math.min(prev, Math.max(0, computed.length - 1)));
      }
      pendingAnchorAyah.current = null;
    }

    repaginate();

    let resizeTimer: ReturnType<typeof setTimeout>;
    function onResize() {
      clearTimeout(resizeTimer);
      // Preserve the currently visible page's first ayah as the anchor across a resize.
      resizeTimer = setTimeout(() => {
        setPages((currentPages) => {
          const currentFirstAyah = currentPages[pageIndex]?.[0]?.numberInSurah;
          pendingAnchorAyah.current = currentFirstAyah ?? null;
          repaginate();
          return currentPages;
        });
      }, 200);
    }
    window.addEventListener("resize", onResize);
    return () => {
      window.removeEventListener("resize", onResize);
      clearTimeout(resizeTimer);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [detail, fontStep]);

  const totalPages = pages.length;
  const pageAyahs = pages[pageIndex] ?? [];

  // Saves "how far you've read" whenever the visible page changes - this is
  // what powers "Continue Reading" on the home page (spec section 4).
  useEffect(() => {
    if (!detail || pageAyahs.length === 0) return;
    const lastAyahOnPage = pageAyahs[pageAyahs.length - 1].numberInSurah;
    if (lastAyahOnPage === lastSavedAyah.current) return;
    lastSavedAyah.current = lastAyahOnPage;
    readingProgressApi.updateMine(number, lastAyahOnPage).catch(() => {
      /* best-effort - a missed save just means a slightly stale "Continue Reading" card */
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pageIndex, pages]);

  function goToPage(delta: 1 | -1) {
    if (totalPages === 0) return;
    const next = pageIndex + delta;
    if (next >= 0 && next < totalPages) {
      setPageIndex(next);
      window.scrollTo({ top: 0, behavior: "instant" as ScrollBehavior });
      return;
    }
    if (delta === 1 && number < 114) {
      navigate(`/quran/${number + 1}`);
    } else if (delta === -1 && number > 1) {
      navigate(`/quran/${number - 1}`, { state: { landOn: "last" } as NavState });
    }
  }

  function handleTouchStart(e: TouchEvent) {
    touchStartX.current = e.touches[0].clientX;
  }

  function handleTouchEnd(e: TouchEvent) {
    if (touchStartX.current === null) return;
    const deltaX = e.changedTouches[0].clientX - touchStartX.current;
    touchStartX.current = null;
    if (Math.abs(deltaX) < SWIPE_THRESHOLD_PX) return;
    // RTL reading: swipe left (finger moves left, deltaX negative) = next page.
    goToPage(deltaX < 0 ? 1 : -1);
  }

  async function toggleBookmark(ayahNumber: number) {
    const isBookmarked = bookmarked.has(ayahNumber);
    setOpenMenuAyah(null);
    try {
      if (isBookmarked) {
        await bookmarksApi.remove(number, ayahNumber);
        setBookmarked((prev) => {
          const next = new Set(prev);
          next.delete(ayahNumber);
          return next;
        });
      } else {
        await bookmarksApi.add(number, ayahNumber);
        setBookmarked((prev) => new Set(prev).add(ayahNumber));
      }
    } catch {
      /* silent - user can just try again from the action menu */
    }
  }

  async function copyAyah(ayahNumber: number, text: string) {
    setOpenMenuAyah(null);
    try {
      await navigator.clipboard.writeText(text);
      setCopiedAyah(ayahNumber);
      setTimeout(() => setCopiedAyah(null), 1500);
    } catch {
      /* clipboard permission denied - nothing we can do silently */
    }
  }

  function goToSurah(n: number) {
    if (n >= 1 && n <= 114) navigate(`/quran/${n}`);
  }

  return (
    <div className="quran-shell">
      <header className="quran-header">
        <Link to="/quran" className="btn-link">كل السور</Link>
        {detail && <h1 className="quran-title">{detail.surah.arabicName}</h1>}
        <div className="font-controls">
          <Link to="/search" className="btn-link" style={{ marginInlineEnd: "8px" }}>بحث</Link>
          <Link to="/bookmarks" className="btn-link" style={{ marginInlineEnd: "8px" }}>محفوظاتي</Link>
          <button aria-label="تصغير الخط" onClick={() => setFontStep((s) => Math.max(0, s - 1))}>أ-</button>
          <button aria-label="تكبير الخط" onClick={() => setFontStep((s) => Math.min(FONT_STEPS.length - 1, s + 1))}>أ+</button>
        </div>
      </header>

      {loading && <p className="quran-loading">جارٍ التحميل...</p>}
      {error && (
        <div className="banner banner-error">
          {error}
          <div style={{ marginTop: "8px", fontSize: "0.8rem" }}>
            (سور غير الفاتحة تحتاج استيراد نص القرآن الكامل أولًا - راجع تعليمات الاستيراد)
          </div>
        </div>
      )}

      {detail && (
        <>
          <div className="surah-meta-bar">
            {detail.surah.englishName} — {detail.surah.englishNameTranslation} ·{" "}
            {detail.surah.revelationType === "Meccan" ? "مكية" : "مدنية"} · {detail.surah.numberOfAyahs} آية
          </div>

          {totalPages > 0 && (
            <div className="page-indicator">صفحة {toArabicDigits(pageIndex + 1)} من {toArabicDigits(totalPages)}</div>
          )}

          {/* Hidden measuring clone - same classes/font-size as the real page below,
              used purely to figure out where 15 lines' worth of text ends. */}
          <div
            ref={measureRef}
            className="mushaf-page"
            aria-hidden="true"
            style={{
              fontSize: `${FONT_STEPS[fontStep]}rem`,
              visibility: "hidden",
              height: 0,
              overflow: "hidden",
              margin: 0,
              paddingTop: 0,
              paddingBottom: 0,
              border: "none",
              boxShadow: "none"
            }}
          />

          <div
            className="mushaf-page"
            style={{ fontSize: `${FONT_STEPS[fontStep]}rem` }}
            onTouchStart={handleTouchStart}
            onTouchEnd={handleTouchEnd}
          >
            {pageAyahs.map((a) => (
              <span
                key={a.numberInSurah}
                id={`ayah-${a.numberInSurah}`}
                className={`ayah-text ${highlightAyah === a.numberInSurah ? "ayah-highlight" : ""}`}
              >
                {a.text}
                <span className="ayah-marker-wrap">
                  <button
                    type="button"
                    className={`ayah-marker ayah-marker-btn ${bookmarked.has(a.numberInSurah) ? "ayah-marker-bookmarked" : ""}`}
                    onClick={() => setOpenMenuAyah(openMenuAyah === a.numberInSurah ? null : a.numberInSurah)}
                    aria-label="خيارات الآية"
                  >
                    {toArabicDigits(a.numberInSurah)}
                  </button>
                  {openMenuAyah === a.numberInSurah && (
                    <div className="ayah-menu">
                      <button onClick={() => copyAyah(a.numberInSurah, a.text)}>
                        {copiedAyah === a.numberInSurah ? "تم النسخ ✓" : "نسخ"}
                      </button>
                      <button onClick={() => toggleBookmark(a.numberInSurah)}>
                        {bookmarked.has(a.numberInSurah) ? "إزالة من المحفوظات" : "حفظ الآية"}
                      </button>
                      <button onClick={() => { setTafsirAyah(a.numberInSurah); setOpenMenuAyah(null); }}>
                        التفسير
                      </button>
                    </div>
                  )}
                </span>
              </span>
            ))}
          </div>

          <div className="page-nav">
            <button className="page-nav-btn" onClick={() => goToPage(-1)} disabled={pageIndex === 0 && number <= 1}>
              → السابقة
            </button>
            <div className="page-dots">
              {Array.from({ length: totalPages }).map((_, i) => (
                <span key={i} className={`page-dot ${i === pageIndex ? "page-dot-active" : ""}`} />
              ))}
            </div>
            <button
              className="page-nav-btn"
              onClick={() => goToPage(1)}
              disabled={pageIndex === totalPages - 1 && number >= 114}
            >
              التالية ←
            </button>
          </div>

          <div className="surah-nav">
            <button className="btn-link" disabled={number <= 1} onClick={() => goToSurah(number - 1)}>
              ← السورة السابقة
            </button>
            <button className="btn-link" disabled={number >= 114} onClick={() => goToSurah(number + 1)}>
              السورة التالية →
            </button>
          </div>
        </>
      )}

      {tafsirAyah !== null && detail && (
        <TafsirModal
          surahNumber={number}
          ayahNumber={tafsirAyah}
          ayahText={detail.ayahs.find((a) => a.numberInSurah === tafsirAyah)?.text ?? ""}
          onClose={() => setTafsirAyah(null)}
        />
      )}
    </div>
  );
}

function toArabicDigits(n: number): string {
  const digits = ["٠", "١", "٢", "٣", "٤", "٥", "٦", "٧", "٨", "٩"];
  return String(n).split("").map((d) => digits[Number(d)]).join("");
}
