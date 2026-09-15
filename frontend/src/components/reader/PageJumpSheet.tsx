import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { quranApi } from "@/api/quranApi";
import { extractApiError } from "@/api/client";
import { ReaderSheet } from "./ReaderSheet";
import { toArabicDigits } from "./ReaderTopBar";

interface PageJumpSheetProps {
  onClose: () => void;
}

/** Jump to a specific Mushaf page (1-604). Relies on the verified page data. */
export function PageJumpSheet({ onClose }: PageJumpSheetProps) {
  const navigate = useNavigate();
  const [page, setPage] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);

    const n = Number(page);
    if (!Number.isInteger(n) || n < 1 || n > 604) {
      setError("أدخل رقم صفحة بين ١ و ٦٠٤.");
      return;
    }

    setSubmitting(true);
    try {
      const position = await quranApi.getPage(n);
      onClose();
      navigate(`/quran/${position.surahNumber}?page=${position.pageNumber}`);
    } catch (err) {
      setError(extractApiError(err));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <ReaderSheet title="الانتقال إلى صفحة" onClose={onClose}>
      <form onSubmit={handleSubmit} className="reader-jump-form">
        {error && <div className="banner banner-error">{error}</div>}
        <input
          type="text"
          inputMode="numeric"
          autoFocus
          placeholder="رقم الصفحة ١ - ٦٠٤"
          value={page}
          onChange={(e) => setPage(e.target.value.replace(/\D/g, ""))}
          className="reader-jump-input"
        />
        <button type="submit" className="btn-primary" disabled={submitting}>
          {submitting ? "جارٍ البحث..." : `انتقال إلى صفحة ${page ? toArabicDigits(page) : ""}`}
        </button>
      </form>
      <p className="reader-jump-hint">الصفحات مقسّمة وفق المصحف المعتمد (٦٠٤ صفحات).</p>
    </ReaderSheet>
  );
}