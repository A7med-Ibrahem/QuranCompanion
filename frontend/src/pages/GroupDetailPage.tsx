import { useEffect, useState, type FormEvent } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { groupsApi } from "@/api/groupsApi";
import { useAuth } from "@/context/AuthContext";
import type { GroupDetail } from "@/types/group";
import { extractApiError } from "@/api/client";

function todayPlusDays(days: number): string {
  const d = new Date();
  d.setDate(d.getDate() + days);
  return d.toISOString().slice(0, 10);
}

export function GroupDetailPage() {
  const { groupId } = useParams<{ groupId: string }>();
  const id = Number(groupId);
  const navigate = useNavigate();
  const { user } = useAuth();

  const [group, setGroup] = useState<GroupDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [wirdIdInput, setWirdIdInput] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [adding, setAdding] = useState(false);

  const [showGoalForm, setShowGoalForm] = useState(false);
  const [goalDate, setGoalDate] = useState(todayPlusDays(30));
  const [goalError, setGoalError] = useState<string | null>(null);
  const [savingGoal, setSavingGoal] = useState(false);

  useEffect(() => {
    load();
  }, [id]);

  function load() {
    setLoading(true);
    groupsApi.getDetail(id).then(setGroup).finally(() => setLoading(false));
  }

  async function handleAddMember(e: FormEvent) {
    e.preventDefault();
    setError(null);
    if (!wirdIdInput.trim()) {
      setError("الرجاء إدخال معرّف Wird.");
      return;
    }
    setAdding(true);
    try {
      const updated = await groupsApi.addMember(id, wirdIdInput.trim());
      setGroup(updated);
      setWirdIdInput("");
    } catch (err) {
      setError(extractApiError(err));
    } finally {
      setAdding(false);
    }
  }

  async function handleRemove(memberUserId: string) {
    await groupsApi.removeMember(id, memberUserId);
    if (memberUserId === user?.id) {
      navigate("/groups", { replace: true });
    } else {
      load();
    }
  }

  async function handleDeleteGroup() {
    await groupsApi.deleteGroup(id);
    navigate("/groups", { replace: true });
  }

  async function handleSaveGoal(e: FormEvent) {
    e.preventDefault();
    setGoalError(null);
    setSavingGoal(true);
    try {
      const updated = await groupsApi.setGoal(id, goalDate);
      setGroup(updated);
      setShowGoalForm(false);
    } catch (err) {
      setGoalError(extractApiError(err));
    } finally {
      setSavingGoal(false);
    }
  }

  if (loading) return <div className="quran-shell"><p className="quran-loading">جارٍ التحميل...</p></div>;
  if (!group) return null;

  const isCreator = group.createdByUserId === user?.id;

  return (
    <div className="quran-shell">
      <header className="quran-header">
        <Link to="/groups" className="btn-link">مجموعاتي</Link>
        <h1 className="quran-title">{group.name}</h1>
        <span />
      </header>

      {group.sharedGoal && (
        <div className="wird-goal-note" style={{ display: "block", textAlign: "center", marginBottom: "24px" }}>
          🎯 الهدف المشترك: تخلصوا القرآن مع بعض بحلول {group.sharedGoal.targetCompletionDate}
          {" "}(متبقي {group.sharedGoal.daysRemaining} يوم)
        </div>
      )}

      {isCreator && !showGoalForm && (
        <div style={{ textAlign: "center", marginBottom: "24px" }}>
          <button className="btn-link" onClick={() => setShowGoalForm(true)}>
            {group.sharedGoal ? "تعديل الهدف المشترك" : "حدد هدفًا مشتركًا للمجموعة"}
          </button>
        </div>
      )}

      {isCreator && showGoalForm && (
        <form onSubmit={handleSaveGoal} style={{ marginBottom: "24px" }}>
          <div className="wird-options" style={{ flexDirection: "row", gap: "8px", marginBottom: "12px" }}>
            <button type="button" className="wird-option" onClick={() => setGoalDate(todayPlusDays(30))}>30 يوم</button>
            <button type="button" className="wird-option" onClick={() => setGoalDate(todayPlusDays(60))}>60 يوم</button>
            <button type="button" className="wird-option" onClick={() => setGoalDate(todayPlusDays(90))}>90 يوم</button>
          </div>
          <div className="field">
            <input
              type="date"
              min={todayPlusDays(1)}
              value={goalDate}
              onChange={(e) => setGoalDate(e.target.value)}
            />
          </div>
          {goalError && <div className="banner banner-error">{goalError}</div>}
          <div style={{ display: "flex", gap: "8px" }}>
            <button className="btn-primary" type="submit" disabled={savingGoal} style={{ flex: 1 }}>
              {savingGoal ? "..." : "حفظ الهدف"}
            </button>
            <button className="btn-link" type="button" onClick={() => setShowGoalForm(false)}>إلغاء</button>
          </div>
        </form>
      )}

      <form onSubmit={handleAddMember} className="search-form">
        <input
          type="text"
          value={wirdIdInput}
          onChange={(e) => setWirdIdInput(e.target.value)}
          placeholder="أضف رفيقًا بمعرّف WIRD-XXXXXX"
          className="search-input"
          style={{ direction: "ltr", textAlign: "center" }}
        />
        <button className="btn-primary search-submit" type="submit" disabled={adding}>
          {adding ? "..." : "إضافة"}
        </button>
      </form>
      {error && <div className="banner banner-error">{error}</div>}
      <p style={{ fontSize: "0.75rem", color: "var(--color-ink-muted)", marginTop: "-16px", marginBottom: "24px" }}>
        تقدر تضيف بس رفقاءك المتصلين بالفعل.
      </p>

      <div className="bookmark-list">
        {group.members.map((m) => (
          <div key={m.userId} className="bookmark-item">
            <div className="bookmark-item-header">
              <span>
                {m.displayName} {m.isCreator && <span style={{ fontSize: "0.7rem", color: "var(--color-accent)" }}>(المنشئ)</span>}
              </span>
              <span style={{ direction: "ltr" }}>{m.wirdId}</span>
            </div>
            <div className="companion-status-row">
              <span>وِرد اليوم</span>
              <span>
                {m.isCompletedToday === null ? "غير معروض" : m.isCompletedToday ? "✓ تم الإنجاز" : "لم يُنجز بعد"}
              </span>
            </div>
            {m.currentStreak !== null && (
              <div className="companion-status-row">
                <span>🔥 التتابع</span>
                <span>{m.currentStreak} يوم</span>
              </div>
            )}
            {m.overallProgressPercent !== null && (
              <div style={{ marginTop: "8px" }}>
                <div className="companion-status-row" style={{ marginBottom: "4px" }}>
                  <span>تقدّمه في القرآن كاملًا</span>
                  <span>{m.overallProgressPercent}٪</span>
                </div>
                <div className="progress-track">
                  <div className="progress-fill" style={{ width: `${m.overallProgressPercent}%` }} />
                </div>
              </div>
            )}
            {(isCreator || m.userId === user?.id) && (
              <div className="bookmark-item-actions">
                <button className="btn-link" onClick={() => handleRemove(m.userId)}>
                  {m.userId === user?.id ? "مغادرة المجموعة" : "إزالة من المجموعة"}
                </button>
              </div>
            )}
          </div>
        ))}
      </div>

      {isCreator && (
        <div style={{ textAlign: "center", marginTop: "32px" }}>
          <button className="btn-link" onClick={handleDeleteGroup}>حذف المجموعة نهائيًا</button>
        </div>
      )}
    </div>
  );
}
