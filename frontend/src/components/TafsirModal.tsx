import { useEffect, useState } from "react";
import { tafsirApi } from "@/api/tafsirApi";
import { extractApiError } from "@/api/client";

interface Props {
  surahNumber: number;
  ayahNumber: number;
  ayahText: string;
  onClose: () => void;
}

export function TafsirModal({ surahNumber, ayahNumber, ayahText, onClose }: Props) {
  const [text, setText] = useState<string | null>(null);
  const [source, setSource] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    setError(null);
    tafsirApi
      .get(surahNumber, ayahNumber)
      .then((t) => {
        setText(t.text);
        setSource(t.sourceName);
      })
      .catch((err) => setError(extractApiError(err)))
      .finally(() => setLoading(false));
  }, [surahNumber, ayahNumber]);

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-card" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <span>التفسير</span>
          <button className="modal-close" onClick={onClose} aria-label="إغلاق">✕</button>
        </div>

        <div className="tafsir-ayah-quote">{ayahText}</div>

        {loading && <p className="quran-loading">جارٍ تحميل التفسير...</p>}
        {error && <div className="banner banner-error">{error}</div>}

        {!loading && !error && text && (
          <>
            <div className="tafsir-text">{text}</div>
            <div className="tafsir-source">المصدر: {source}</div>
          </>
        )}
      </div>
    </div>
  );
}
