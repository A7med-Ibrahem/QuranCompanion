import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { quranApi } from "@/api/quranApi";
import type { SurahSummary } from "@/types/quran";
import { extractApiError } from "@/api/client";

export function SurahListPage() {
  const [surahs, setSurahs] = useState<SurahSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    quranApi
      .getAllSurahs()
      .then(setSurahs)
      .catch((err) => setError(extractApiError(err)))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="quran-shell">
      <header className="quran-header">
        <Link to="/" className="btn-link">العودة للرئيسية</Link>
        <h1 className="quran-title">القرآن الكريم</h1>
        <Link to="/search" className="btn-link">بحث</Link>
      </header>

      {loading && <p className="quran-loading">جارٍ التحميل...</p>}
      {error && <div className="banner banner-error">{error}</div>}

      <div className="surah-grid">
        {surahs.map((s) => (
          <Link key={s.number} to={`/quran/${s.number}`} className="surah-card">
            <span className="surah-number">{s.number}</span>
            <span className="surah-name">{s.arabicName}</span>
            <span className="surah-meta">
              {s.englishName} · {s.numberOfAyahs} آية · {s.revelationType === "Meccan" ? "مكية" : "مدنية"}
            </span>
          </Link>
        ))}
      </div>
    </div>
  );
}
