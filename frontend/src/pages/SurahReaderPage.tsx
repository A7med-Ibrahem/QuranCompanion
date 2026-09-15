import { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState, type TouchEvent } from "react";
import { useLocation, useNavigate, useParams, useSearchParams } from "react-router-dom";
import { quranApi } from "@/api/quranApi";
import { readingProgressApi } from "@/api/readingProgressApi";
import { bookmarksApi } from "@/api/bookmarksApi";
import { TafsirModal } from "@/components/TafsirModal";
import { paginateByLines } from "@/utils/paginateByLines";
import { groupAyahsByPage } from "@/utils/groupAyahsByPage";
import { ReaderTopBar, toArabicDigits } from "@/components/reader/ReaderTopBar";
import { ReaderToolbar, type ReaderAction } from "@/components/reader/ReaderToolbar";
import { IndexSheet } from "@/components/reader/IndexSheet";
import { JuzSheet } from "@/components/reader/JuzSheet";
import { PageJumpSheet } from "@/components/reader/PageJumpSheet";
import { DuasSheet } from "@/components/reader/DuasSheet";
import { SettingsSheet } from "@/components/reader/SettingsSheet";
import type { SurahDetail, Ayah } from "@/types/quran";
import { extractApiError } from "@/api/client";

const FONT_STEPS = [1.4, 1.7, 2.0, 2.4, 2.8]; // rem, applied to the whole page
const TARGET_LINES_PER_PAGE = 15; // fallback (measured) pagination; real Mushaf pages are used when imported
const SWIPE_THRESHOLD_PX = 60;
const MIN_FONT_STEP = 0;
const MAX_FONT_STEP = FONT_STEPS.length - 1;
const BASMALA = "بِسْمِ ٱللَّهِ ٱلرَّحْمَـٰنِ ٱلرَّحِيمِ";

type ReaderSheetKind = "index" | "juz" | "goto" | "duas" | "settings" | null;

export function SurahReaderPage() {
  const { surahNumber } = useParams<{ surahNumber: string }>();
  const [searchParams] = useSearchParams();
  const location = useLocation();
  const navigate = useNavigate();
  const number = Number(surahNumber);

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
  const [controlsVisible, setControlsVisible] = useState(false);
  const [hintVisible, setHintVisible] = useState(true);
  const [sheet, setSheet] = useState<ReaderSheetKind>(null);

  const pageScrollRef = useRef<HTMLDivElement>(null);
  const touchStartX = useRef<number | null>(null);
  const lastSavedAyah = useRef<number | null>(null);
  const measureRef = useRef<HTMLDivElement>(null);
  const pendingAnchorAyah = useRef<number | null>(null);
  const pendingTargetPage = useRef<number | null>(null);
  const pendingLandOnLast = useRef(false);
  const pagesRef = useRef<Ayah[][]>([]);
  const pageIndexRef = useRef(0);
  const handledQueryRef = useRef("");
  pageIndexRef.current = pageIndex;
  pagesRef.current = pages;

  // Real Mushaf pages once the page/juz import has run; otherwise null and we
  // fall back to the 15-lines-per-page measurement.
  const groupedPages = useMemo(() => (detail ? groupAyahsByPage(detail.ayahs) : null), [detail]);
  const useRealPages = groupedPages !== null;

  useEffect(() => {
    setLoading(true);
    setError(null);
    setSheet(null);
    setControlsVisible(false);
    setHighlightAyah(null);
    pendingTargetPage.current = Number(searchParams.get("page") ?? "0") || null;
    pendingAnchorAyah.current = Number(searchParams.get("ayah") ?? "0") || null;
    pendingLandOnLast.current = (location.state as { landOn?: "last" } | null)?.landOn === "last";
    // The current query is handled by settle() below, so the ?ayah/?page effect
    // must ignore it (it exists for *future* query changes on the same surah).
    handledQueryRef.current = `${searchParams.get("ayah") ?? ""}|${searchParams.get("page") ?? ""}`;

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

    const hideHint = setTimeout(() => setHintVisible(false), 4000);
    return () => clearTimeout(hideHint);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [number]);

  function settle(computed: Ayah[][]) {
    if (computed.length === 0) {
      setPageIndex(0);
      pendingAnchorAyah.current = null;
      pendingTargetPage.current = null;
      pendingLandOnLast.current = false;
      return;
    }

    if (pendingAnchorAyah.current) {
      const idx = computed.findIndex((page) => page.some((a) => a.numberInSurah === pendingAnchorAyah.current));
      if (idx >= 0) {
        setPageIndex(idx);
        setHighlightAyah(pendingAnchorAyah.current);
        setTimeout(() => setHighlightAyah(null), 2500);
      }
    } else if (pendingTargetPage.current) {
      const idx = computed.findIndex((page) => page[0]?.page === pendingTargetPage.current);
      if (idx >= 0) setPageIndex(idx);
    } else if (pendingLandOnLast.current) {
      setPageIndex(computed.length - 1);
    } else {
      setPageIndex((prev) => Math.min(prev, Math.max(0, computed.length - 1)));
    }

    pendingAnchorAyah.current = null;
    pendingTargetPage.current = null;
    pendingLandOnLast.current = false;
  }

  // Real pages: no measuring needed - group by the imported Mushaf page numbers.
  // Fallback pages: measure live text and split into 15-line pages, re-measuring
  // when the font size or viewport changes (staying anchored on the same ayah).
  useLayoutEffect(() => {
    if (!detail) return;

    if (useRealPages) {
      const computed = groupedPages!;
      setPages(computed);
      settle(computed);
      return;
    }

    function repaginate() {
      if (!detail || !measureRef.current) return;
      // Unless a deep link is still pending, preserve the current spot (first
      // ayah of the visible page) across a font-size/viewport repagination.
      if (pendingAnchorAyah.current === null && pendingTargetPage.current === null && pagesRef.current.length > 0) {
        pendingAnchorAyah.current = pagesRef.current[pageIndexRef.current]?.[0]?.numberInSurah ?? null;
      }
      const computed = paginateByLines(detail.ayahs, measureRef.current, TARGET_LINES_PER_PAGE);
      setPages(computed);
      settle(computed);
    }

    repaginate();

    let resizeTimer: ReturnType<typeof setTimeout>;
    function onResize() {
      clearTimeout(resizeTimer);
      resizeTimer = setTimeout(repaginate, 200);
    }
    window.addEventListener("resize", onResize);
    return () => {
      window.removeEventListener("resize", onResize);
      clearTimeout(resizeTimer);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [detail, fontStep, useRealPages]);

  // Same-surah navigation via query params (?page=N / ?ayah=N) - e.g. from the
  // page-jump sheet or a dua, or a page jump that lands back in this surah.
  useEffect(() => {
    const query = `${searchParams.get("ayah") ?? ""}|${searchParams.get("page") ?? ""}`;
    if (query === handledQueryRef.current || pagesRef.current.length === 0) return;
    handledQueryRef.current = query;

    const ayah = Number(searchParams.get("ayah") ?? "0");
    const page = Number(searchParams.get("page") ?? "0");
    if (ayah > 0) {
      const idx = pagesRef.current.findIndex((p) => p.some((a) => a.numberInSurah === ayah));
      if (idx >= 0) {
        setPageIndex(idx);
        setHighlightAyah(ayah);
        setTimeout(() => setHighlightAyah(null), 2500);
        return;
      }
    }
    if (page > 0) {
      const idx = pagesRef.current.findIndex((p) => p[0]?.page === page);
      if (idx >= 0) setPageIndex(idx);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchParams, pages]);

  const totalPages = pages.length;
  const pageAyahs = pages[pageIndex] ?? [];
  const firstAyahOfPage = pageAyahs[0];
  const isSurahStart = firstAyahOfPage?.numberInSurah === 1;

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

  const goToPage = useCallback(
    (delta: 1 | -1) => {
      if (totalPages === 0) return;
      const next = pageIndex + delta;
      if (next >= 0 && next < totalPages) {
        setPageIndex(next);
        pageScrollRef.current?.scrollTo({ top: 0, behavior: "instant" as ScrollBehavior });
        return;
      }
      if (delta === 1 && number < 114) {
        navigate(`/quran/${number + 1}`);
      } else if (delta === -1 && number > 1) {
        navigate(`/quran/${number - 1}`, { state: { landOn: "last" } });
      }
    },
    [totalPages, pageIndex, number, navigate]
  );

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

  useEffect(() => {
    function onKey(e: KeyboardEvent) {
      if (e.key === "ArrowRight") {
        e.preventDefault();
        goToPage(1);
      } else if (e.key === "ArrowLeft") {
        e.preventDefault();
        goToPage(-1);
      }
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [goToPage]);

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

  function handleToolbarAction(action: ReaderAction) {
    switch (action) {
      case "home":
        navigate("/");
        break;
      case "index":
        setSheet("index");
        break;
      case "juz":
        setSheet("juz");
        break;
      case "bookmarks":
        navigate("/bookmarks");
        break;
      case "goto":
        setSheet("goto");
        break;
      case "back":
        if (window.history.length > 2) navigate(-1);
        else navigate("/quran");
        break;
      case "tafsir":
        if (firstAyahOfPage) {
          setTafsirAyah(firstAyahOfPage.numberInSurah);
          setControlsVisible(false);
        }
        break;
      case "duas":
        setSheet("duas");
        break;
      case "search":
        navigate("/search");
        break;
      case "settings":
        setSheet("settings");
        break;
    }
  }

  const pageNumberLabel = useRealPages
    ? firstAyahOfPage?.page
      ? `الصفحة ${toArabicDigits(firstAyahOfPage.page)}`
      : "الصفحة"
    : totalPages > 0
      ? `صفحة ${toArabicDigits(pageIndex + 1)} من ${toArabicDigits(totalPages)}`
      : "الصفحة";

  const juzLabel = firstAyahOfPage?.juz ? `الجزء ${toArabicDigits(firstAyahOfPage.juz)}` : "الجزء";

  return (
    <div className="reader-viewport">
      <div
        className="reader-page-area"
        ref={pageScrollRef}
        onClick={() => (sheet === null ? setControlsVisible((v) => !v) : undefined)}
        onTouchStart={handleTouchStart}
        onTouchEnd={handleTouchEnd}
      >
        {loading && <p className="reader-status">جارٍ التحميل...</p>}
        {error && (
          <div className="banner banner-error reader-status-banner">
            {error}
            <div style={{ marginTop: "8px", fontSize: "0.8rem" }}>
              (سور غير الفاتحة تحتاج استيراد نص القرآن الكامل أولًا - راجع تعليمات الاستيراد)
            </div>
          </div>
        )}

        {!loading && !error && detail && (
          <div className="reader-page-card">
            {isSurahStart && number !== 9 && <div className="reader-page-basmala">{BASMALA}</div>}
            <div className="reader-page-clip" style={{ fontSize: `${FONT_STEPS[fontStep]}rem` }}>
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
                      onClick={(e) => {
                        e.stopPropagation();
                        setOpenMenuAyah(openMenuAyah === a.numberInSurah ? null : a.numberInSurah);
                      }}
                      aria-label="خيارات الآية"
                    >
                      {toArabicDigits(a.numberInSurah)}
                    </button>
                    {openMenuAyah === a.numberInSurah && (
                      <div className="ayah-menu" onClick={(e) => e.stopPropagation()}>
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
          </div>
        )}

        {/* Hidden measuring clone - identical class/font metrics as the real page.
            Fallback pagination only; real Mushaf pages don't need this. */}
        {!useRealPages && !loading && detail && (
          <div
            ref={measureRef}
            className="reader-page-clip"
            style={{ fontSize: `${FONT_STEPS[fontStep]}rem`, visibility: "hidden", height: 0, overflow: "hidden" }}
          />
        )}

        {controlsVisible && totalPages > 0 && (
          <>
            {pageIndex > 0 && (
              <button
                type="button"
                className="reader-edge-btn reader-edge-prev"
                aria-label="الصفحة السابقة"
                onClick={(e) => { e.stopPropagation(); goToPage(-1); }}
              >
                ›
              </button>
            )}
            {pageIndex < totalPages - 1 && (
              <button
                type="button"
                className="reader-edge-btn reader-edge-next"
                aria-label="الصفحة التالية"
                onClick={(e) => { e.stopPropagation(); goToPage(1); }}
              >
                ‹
              </button>
            )}
          </>
        )}

        {totalPages > 1 && (
          <div className="reader-progress" onClick={(e) => e.stopPropagation()}>
            {Array.from({ length: totalPages }).map((_, i) => (
              <span
                key={i}
                className={`reader-progress-dot ${i === pageIndex ? "reader-progress-dot-active" : ""}`}
                onClick={() => {
                  setPageIndex(i);
                  pageScrollRef.current?.scrollTo({ top: 0 });
                }}
              />
            ))}
          </div>
        )}
      </div>

      {hintVisible && !loading && !error && !controlsVisible && (
        <div className="reader-tap-hint">اضغط في أي مكان لعرض أدوات القراءة</div>
      )}

      {controlsVisible && detail && (
        <div
          className="reader-overlay"
          onClick={(e) => {
            // Toggle only when tapping the empty space behind the bars - not when
            // a top-bar/toolbar button (or its label) itself was clicked.
            if (e.target === e.currentTarget) setControlsVisible(false);
          }}
        >
          <ReaderTopBar surahName={detail.surah.arabicName} pageText={pageNumberLabel} juzText={juzLabel} />
          <ReaderToolbar
            onAction={(a) => {
              setControlsVisible(false);
              handleToolbarAction(a);
            }}
          />
        </div>
      )}

      {sheet === "index" && <IndexSheet onClose={() => setSheet(null)} />}
      {sheet === "juz" && <JuzSheet onClose={() => setSheet(null)} />}
      {sheet === "goto" && <PageJumpSheet onClose={() => setSheet(null)} />}
      {sheet === "duas" && <DuasSheet onClose={() => setSheet(null)} />}
      {sheet === "settings" && (
        <SettingsSheet
          onClose={() => setSheet(null)}
          fontStep={fontStep}
          minFontStep={MIN_FONT_STEP}
          maxFontStep={MAX_FONT_STEP}
          onFontStepChange={(s) => setFontStep(s)}
        />
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