import { useEffect, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { companionsApi, encouragementApi } from "@/api/companionsApi";
import { useAuth } from "@/context/AuthContext";
import type { Companion, ConnectionRequest, CompanionStatus } from "@/types/companion";
import { extractApiError } from "@/api/client";

export function CompanionsPage() {
  const { user } = useAuth();
  const [companions, setCompanions] = useState<Companion[]>([]);
  const [incoming, setIncoming] = useState<ConnectionRequest[]>([]);
  const [outgoing, setOutgoing] = useState<ConnectionRequest[]>([]);
  const [loading, setLoading] = useState(true);

  const [wirdIdInput, setWirdIdInput] = useState("");
  const [sendError, setSendError] = useState<string | null>(null);
  const [sendSuccess, setSendSuccess] = useState<string | null>(null);
  const [sending, setSending] = useState(false);

  const [expandedUserId, setExpandedUserId] = useState<string | null>(null);
  const [status, setStatus] = useState<CompanionStatus | null>(null);
  const [messages, setMessages] = useState<string[]>([]);
  const [encourageFeedback, setEncourageFeedback] = useState<string | null>(null);

  useEffect(() => {
    load();
    encouragementApi.getMessages().then(setMessages).catch(() => setMessages([]));
  }, []);

  function load() {
    setLoading(true);
    Promise.all([companionsApi.getCompanions(), companionsApi.getIncoming(), companionsApi.getOutgoing()])
      .then(([c, i, o]) => {
        setCompanions(c);
        setIncoming(i);
        setOutgoing(o);
      })
      .finally(() => setLoading(false));
  }

  async function handleSend(e: FormEvent) {
    e.preventDefault();
    setSendError(null);
    setSendSuccess(null);
    if (!wirdIdInput.trim()) {
      setSendError("الرجاء إدخال معرّف Wird.");
      return;
    }
    setSending(true);
    try {
      const result = await companionsApi.sendRequest(wirdIdInput.trim());
      setOutgoing((prev) => [result, ...prev]);
      setSendSuccess(`تم إرسال طلب الاتصال إلى ${result.displayName}.`);
      setWirdIdInput("");
    } catch (err) {
      setSendError(extractApiError(err));
    } finally {
      setSending(false);
    }
  }

  async function handleAccept(connectionId: number) {
    const companion = await companionsApi.accept(connectionId);
    setIncoming((prev) => prev.filter((r) => r.connectionId !== connectionId));
    setCompanions((prev) => [companion, ...prev]);
  }

  async function handleReject(connectionId: number) {
    await companionsApi.reject(connectionId);
    setIncoming((prev) => prev.filter((r) => r.connectionId !== connectionId));
  }

  async function handleRemove(connectionId: number) {
    await companionsApi.remove(connectionId);
    setCompanions((prev) => prev.filter((c) => c.connectionId !== connectionId));
    if (expandedUserId) setExpandedUserId(null);
  }

  async function toggleExpand(companionUserId: string) {
    if (expandedUserId === companionUserId) {
      setExpandedUserId(null);
      setStatus(null);
      return;
    }
    setExpandedUserId(companionUserId);
    setEncourageFeedback(null);
    try {
      const s = await companionsApi.getStatus(companionUserId);
      setStatus(s);
    } catch {
      setStatus(null);
    }
  }

  async function handleEncourage(companionUserId: string, message: string) {
    try {
      await companionsApi.sendEncouragement(companionUserId, message);
      setEncourageFeedback("تم إرسال التشجيع 🤍");
    } catch (err) {
      setEncourageFeedback(extractApiError(err));
    }
  }

  return (
    <div className="quran-shell">
      <header className="quran-header">
        <Link to="/" className="btn-link">الرئيسية</Link>
        <h1 className="quran-title">الرفقاء</h1>
        <Link to="/privacy-settings" className="btn-link">إعدادات الخصوصية</Link>
      </header>

      <div style={{ textAlign: "center", marginBottom: "24px" }}>
        <Link to="/groups" className="btn-link">مجموعاتي ←</Link>
      </div>

      <div className="companion-my-id">
        <div className="continue-label">معرّفك الخاص</div>
        <div className="wird-id-chip">{user?.wirdId}</div>
        <p style={{ color: "var(--color-ink-muted)", fontSize: "0.8rem", marginTop: "8px" }}>
          شاركه مع من تثق به عشان يضيفك كرفيق.
        </p>
      </div>

      <form onSubmit={handleSend} className="search-form">
        <input
          type="text"
          value={wirdIdInput}
          onChange={(e) => setWirdIdInput(e.target.value)}
          placeholder="WIRD-XXXXXX"
          className="search-input"
          style={{ direction: "ltr", textAlign: "center" }}
        />
        <button className="btn-primary search-submit" type="submit" disabled={sending}>
          {sending ? "..." : "إرسال طلب"}
        </button>
      </form>
      {sendError && <div className="banner banner-error">{sendError}</div>}
      {sendSuccess && <div className="banner banner-success">{sendSuccess}</div>}

      {!loading && incoming.length > 0 && (
        <section style={{ marginBottom: "32px" }}>
          <h2 className="search-section-title">طلبات واردة</h2>
          <div className="bookmark-list">
            {incoming.map((r) => (
              <div key={r.connectionId} className="bookmark-item">
                <div className="bookmark-item-header">
                  <span>{r.displayName}</span>
                  <span style={{ direction: "ltr" }}>{r.wirdId}</span>
                </div>
                <div className="bookmark-item-actions">
                  <button className="btn-link" onClick={() => handleAccept(r.connectionId)}>قبول</button>
                  <button className="btn-link" onClick={() => handleReject(r.connectionId)}>رفض</button>
                </div>
              </div>
            ))}
          </div>
        </section>
      )}

      {!loading && outgoing.length > 0 && (
        <section style={{ marginBottom: "32px" }}>
          <h2 className="search-section-title">طلبات مرسلة (بانتظار الرد)</h2>
          <div className="bookmark-list">
            {outgoing.map((r) => (
              <div key={r.connectionId} className="bookmark-item">
                <div className="bookmark-item-header">
                  <span>{r.displayName}</span>
                  <span style={{ direction: "ltr" }}>{r.wirdId}</span>
                </div>
              </div>
            ))}
          </div>
        </section>
      )}

      <section>
        <h2 className="search-section-title">رفقاؤك</h2>
        {!loading && companions.length === 0 && (
          <p className="quran-loading">لسه معندكش رفقاء. شارك معرّفك أو اطلب حد بمعرّفه.</p>
        )}
        <div className="bookmark-list">
          {companions.map((c) => (
            <div key={c.connectionId} className="bookmark-item">
              <div className="bookmark-item-header">
                <span>{c.displayName}</span>
                <span style={{ direction: "ltr" }}>{c.wirdId}</span>
              </div>
              <div className="bookmark-item-actions">
                <button className="btn-link" onClick={() => toggleExpand(c.userId)}>
                  {expandedUserId === c.userId ? "إخفاء" : "عرض الحالة"}
                </button>
                <button className="btn-link" onClick={() => handleRemove(c.connectionId)}>
                  إزالة
                </button>
              </div>

              {expandedUserId === c.userId && (
                <div className="companion-status-panel">
                  {status ? (
                    <>
                      {status.isCompletedToday !== null && (
                        <div className="companion-status-row">
                          <span>وِرد اليوم</span>
                          <span>{status.isCompletedToday ? "✓ تم الإنجاز" : "لم يُنجز بعد"}</span>
                        </div>
                      )}
                      {status.todayRangeSummary && (
                        <div className="companion-status-row">
                          <span>النطاق</span>
                          <span>{status.todayRangeSummary}</span>
                        </div>
                      )}
                      {status.currentStreak !== null && (
                        <div className="companion-status-row">
                          <span>🔥 التتابع الفردي</span>
                          <span>{status.currentStreak} يوم</span>
                        </div>
                      )}
                      {status.sharedStreak !== null && (
                        <div className="companion-status-row">
                          <span>🔥 التتابع المشترك</span>
                          <span>{status.sharedStreak} يوم معًا</span>
                        </div>
                      )}
                      {status.lastReadPositionSummary && (
                        <div className="companion-status-row">
                          <span>آخر قراءة</span>
                          <span>{status.lastReadPositionSummary}</span>
                        </div>
                      )}
                      {status.isCompletedToday === null &&
                        status.currentStreak === null &&
                        status.todayRangeSummary === null &&
                        status.lastReadPositionSummary === null && (
                          <p style={{ fontSize: "0.8rem", color: "var(--color-ink-muted)" }}>
                            هذا الرفيق مايشاركش نشاطه معاك حاليًا.
                          </p>
                        )}
                    </>
                  ) : (
                    <p style={{ fontSize: "0.8rem", color: "var(--color-ink-muted)" }}>جارٍ التحميل...</p>
                  )}

                  <div className="encourage-buttons">
                    {messages.map((m) => (
                      <button key={m} onClick={() => handleEncourage(c.userId, m)}>{m}</button>
                    ))}
                  </div>
                  {encourageFeedback && (
                    <p style={{ fontSize: "0.8rem", color: "var(--color-primary-strong)", marginTop: "8px" }}>
                      {encourageFeedback}
                    </p>
                  )}
                </div>
              )}
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}
