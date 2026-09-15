import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { quranApi } from "@/api/quranApi";
import { extractApiError } from "@/api/client";
import type { JuzStart } from "@/types/quran";
import { ReaderSheet } from "./ReaderSheet";
import { toArabicDigits } from "./ReaderTopBar";

interface JuzSheetProps {
  onClose: () => void;
}

/** Juz index (الأجزاء) - lists all 30 juz with their Mushaf start page, opens the surah at that page. */
export function JuzSheet({ onClose }: JuzSheetProps) {
  const navigate = useNavigate();
  const [juzs, setJuzs] = useState<JuzStart[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    quranApi
      .getJuzList()
      .then(setJuzs)
      .catch((err) => setError(extractApiError(err)));
  }, []);

  function openJuz(j: JuzStart) {
    onClose();
    navigate(`/quran/${j.surahNumber}?page=${j.startPage}`);
  }

  return (
    <ReaderSheet title="الأجزاء" onClose={onClose}>
      {error && <div className="banner banner-error">{error}</div>}
      {!error && !juzs && <p className="reader-sheet-hint">جارٍ التحميل...</p>}
      <div className="reader-sheet-list">
        {juzs?.map((j) => (
          <button key={j.juz} type="button" className="reader-sheet-item" onClick={() => openJuz(j)}>
            <span className="reader-sheet-item-num">الجزء {toArabicDigits(j.juz)}</span>
            <span className="reader-sheet-item-name">{j.surahArabicName}</span>
            <span className="reader-sheet-item-meta">ص {toArabicDigits(j.startPage)} · يبدأ {toArabicDigits(j.ayahNumber)}</span>
          </button>
        ))}
      </div>
    </ReaderSheet>
  );
}