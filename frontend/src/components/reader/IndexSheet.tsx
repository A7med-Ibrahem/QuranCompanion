import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { quranApi } from "@/api/quranApi";
import { extractApiError } from "@/api/client";
import type { SurahSummary } from "@/types/quran";
import { ReaderSheet } from "./ReaderSheet";
import { toArabicDigits } from "./ReaderTopBar";

interface IndexSheetProps {
  onClose: () => void;
}

/** Surah index (فهرس) - goes straight into any surah, showing its page number when available. */
export function IndexSheet({ onClose }: IndexSheetProps) {
  const navigate = useNavigate();
  const [surahs, setSurahs] = useState<SurahSummary[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    quranApi
      .getAllSurahs()
      .then(setSurahs)
      .catch((err) => setError(extractApiError(err)));
  }, []);

  function openSurah(n: number) {
    onClose();
    navigate(`/quran/${n}`);
  }

  return (
    <ReaderSheet title="فهرس السور" onClose={onClose}>
      {error && <div className="banner banner-error">{error}</div>}
      {!error && !surahs && <p className="reader-sheet-hint">جارٍ التحميل...</p>}
      <div className="reader-sheet-list">
        {surahs?.map((s) => (
          <button key={s.number} type="button" className="reader-sheet-item" onClick={() => openSurah(s.number)}>
            <span className="reader-sheet-item-num">{toArabicDigits(s.number)}</span>
            <span className="reader-sheet-item-name">{s.arabicName}</span>
            <span className="reader-sheet-item-meta">
              {s.startPage ? `ص ${toArabicDigits(s.startPage)}` : `${toArabicDigits(s.numberOfAyahs)} آية`}
            </span>
          </button>
        ))}
      </div>
    </ReaderSheet>
  );
}