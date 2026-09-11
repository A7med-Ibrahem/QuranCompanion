import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import { readingProgressApi } from "@/api/readingProgressApi";
import { wirdApi } from "@/api/wirdApi";
import { encouragementApi } from "@/api/companionsApi";
import type { ReadingProgress } from "@/types/quran";
import type { TodayWird } from "@/types/wird";
import type { Encouragement } from "@/types/companion";

export function HomePage() {
  const { user } = useAuth();
  const [progress, setProgress] = useState<ReadingProgress | null>(null);
  const [loadedProgress, setLoadedProgress] = useState(false);
  const [today, setToday] = useState<TodayWird | null>(null);
  const [loadedWird, setLoadedWird] = useState(false);
  const [completing, setCompleting] = useState(false);
  const [streak, setStreak] = useState<number | null>(null);
  const [received, setReceived] = useState<Encouragement[]>([]);

  useEffect(() => {
    readingProgressApi
      .getMine()
      .then(setProgress)
      .catch(() => setProgress(null))
      .finally(() => setLoadedProgress(true));

    wirdApi
      .getToday()
      .then(setToday)
      .catch(() => setToday(null))
      .finally(() => setLoadedWird(true));

    wirdApi
      .getMyStreak()
      .then((r) => setStreak(r.currentStreak))
      .catch(() => setStreak(null));

    encouragementApi
      .getReceived()
      .then((all) => setReceived(all.slice(0, 5)))
      .catch(() => setReceived([]));
  }, []);

  const [completeMessage, setCompleteMessage] = useState<string | null>(null);

  async function handleComplete() {
    setCompleting(true);
    setCompleteMessage(null);
    try {
      const result = await wirdApi.completeToday();
      setToday(result);
      const r = await wirdApi.getMyStreak();
      setStreak(r.currentStreak);
    } catch {
      // Offline: the service worker has already queued this request and will
      // send it the moment the connection returns - nothing is lost.
      setCompleteMessage("تم حفظ إنجازك محليًا، وهيتزامن تلقائيًا لما يرجع الاتصال بالإنترنت.");
    } finally {
      setCompleting(false);
    }
  }

  return (
    <div className="auth-shell">
      <div className="auth-card">
        <div className="brand">
          <div className="brand-mark">أهلًا، {user?.displayName}</div>
          <div className="brand-tagline">اقرأ وِردك. تابع رحلتك.</div>
        </div>
        <div className="ornament"><span /></div>

        {loadedWird && !today && (
          <Link className="wird-card" to="/wird/settings" style={{ textDecoration: "none", display: "block" }}>
            <div className="wird-card-label">لسه معملتش وِرد يومي</div>
            <div className="wird-card-range">اضغط هنا عشان تحدد وِردك</div>
          </Link>
        )}

        {loadedWird && today && (
          <div className={`wird-card ${today.isCompletedToday ? "wird-card-done" : ""}`}>
            <div className="wird-card-label">{today.isCompletedToday ? "✓ تم إنجاز وِرد اليوم" : "وِرد اليوم"}</div>
            {streak !== null && streak > 0 && <div className="streak-badge">🔥 {streak} يوم</div>}
            <div className="wird-card-range">
              من سورة {today.startSurahName} آية {today.startAyah}
              {" "}إلى سورة {today.endSurahName} آية {today.endAyah}
              {" "}({today.ayahCount} آية)
            </div>
            {today.daysRemainingInGoal !== null && (
              <div className="wird-goal-note">
                🎯 متبقي {today.daysRemainingInGoal} يوم لتحقيق هدفك
                {today.targetCompletionDate && ` (${today.targetCompletionDate})`}
              </div>
            )}
            {!today.isCompletedToday && (
              <div className="wird-card-actions">
                <Link
                  className="btn-primary"
                  style={{ textDecoration: "none", flex: 1 }}
                  to={`/quran/${today.startSurah}?ayah=${today.startAyah}`}
                >
                  ابدأ القراءة
                </Link>
                <button className="btn-primary" style={{ flex: 1 }} onClick={handleComplete} disabled={completing}>
                  {completing ? "..." : "تم الإنجاز"}
                </button>
              </div>
            )}
            {completeMessage && (
              <p style={{ fontSize: "0.75rem", color: "var(--color-ink-muted)", marginTop: "8px" }}>{completeMessage}</p>
            )}
          </div>
        )}

        {loadedProgress && progress && (
          <Link
            className="continue-card"
            to={`/quran/${progress.surahNumber}?ayah=${progress.ayahNumber}`}
          >
            <div className="continue-label">متابعة القراءة</div>
            <div className="continue-position">
              سورة {progress.surahArabicName} — آية {progress.ayahNumber}
            </div>
          </Link>
        )}

        {received.length > 0 && (
          <div className="encouragement-feed">
            <div className="continue-label">رسائل وصلتك 🤍</div>
            {received.map((e) => (
              <div key={e.id} className="encouragement-item">
                <span className="encouragement-from">{e.fromDisplayName}</span>
                <span className="encouragement-message">{e.message}</span>
              </div>
            ))}
          </div>
        )}

        <div style={{ textAlign: "center", display: "flex", flexDirection: "column", gap: "12px", marginTop: "8px" }}>
          <Link className="btn-primary" style={{ textDecoration: "none", display: "block" }} to="/quran">
            افتح المصحف
          </Link>
          {today && <Link className="btn-link" to="/wird/settings">تعديل الورد اليومي</Link>}
          <Link className="btn-link" to="/companions">الرفقاء</Link>
          <Link className="btn-link" to="/profile">الذهاب إلى الملف الشخصي</Link>
        </div>
      </div>
    </div>
  );
}
