import { useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { searchApi } from "@/api/searchApi";
import type { SearchResults } from "@/types/search";
import { extractApiError } from "@/api/client";

export function SearchPage() {
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<SearchResults | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [searched, setSearched] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!query.trim()) {
      setError("الرجاء إدخال كلمة للبحث.");
      return;
    }
    setError(null);
    setLoading(true);
    setSearched(true);
    try {
      const data = await searchApi.search(query.trim());
      setResults(data);
    } catch (err) {
      setError(extractApiError(err));
      setResults(null);
    } finally {
      setLoading(false);
    }
  }

  const totalResults = (results?.matchingSurahs.length ?? 0) + (results?.matchingAyahs.length ?? 0);

  return (
    <div className="quran-shell">
      <header className="quran-header">
        <Link to="/" className="btn-link">الرئيسية</Link>
        <h1 className="quran-title">البحث في القرآن</h1>
        <span />
      </header>

      <form onSubmit={handleSubmit} className="search-form">
        <input
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="ابحث باسم سورة أو كلمة من آية..."
          className="search-input"
          autoFocus
        />
        <button className="btn-primary search-submit" type="submit" disabled={loading}>
          {loading ? "..." : "بحث"}
        </button>
      </form>

      {error && <div className="banner banner-error">{error}</div>}

      {searched && !loading && !error && totalResults === 0 && (
        <p className="quran-loading">لا توجد نتائج مطابقة.</p>
      )}

      {results && results.matchingSurahs.length > 0 && (
        <section>
          <h2 className="search-section-title">السور</h2>
          <div className="surah-grid" style={{ marginBottom: "32px" }}>
            {results.matchingSurahs.map((s) => (
              <Link key={s.number} to={`/quran/${s.number}`} className="surah-card">
                <span className="surah-number">{s.number}</span>
                <span className="surah-name">{s.arabicName}</span>
                <span className="surah-meta">{s.englishName} · {s.numberOfAyahs} آية</span>
              </Link>
            ))}
          </div>
        </section>
      )}

      {results && results.matchingAyahs.length > 0 && (
        <section>
          <h2 className="search-section-title">الآيات</h2>
          <div className="bookmark-list">
            {results.matchingAyahs.map((r) => (
              <Link
                key={`${r.surahNumber}-${r.ayahNumber}`}
                to={`/quran/${r.surahNumber}?ayah=${r.ayahNumber}`}
                className="bookmark-item"
                style={{ textDecoration: "none", display: "block" }}
              >
                <div className="bookmark-item-header">
                  <span>سورة {r.surahArabicName} — آية {r.ayahNumber}</span>
                </div>
                <div className="bookmark-item-text">
                  <HighlightedAyah text={r.text} start={r.matchStart} length={r.matchLength} />
                </div>
              </Link>
            ))}
          </div>
        </section>
      )}
    </div>
  );
}

function HighlightedAyah({ text, start, length }: { text: string; start: number | null; length: number | null }) {
  if (start === null || length === null) return <>{text}</>;
  return (
    <>
      {text.slice(0, start)}
      <mark className="search-highlight">{text.slice(start, start + length)}</mark>
      {text.slice(start + length)}
    </>
  );
}
