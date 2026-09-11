import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { wirdApi } from "@/api/wirdApi";
import { WIRD_TYPE_LABELS, type WirdType } from "@/types/wird";
import { extractApiError } from "@/api/client";

const OPTIONS: WirdType[] = ["OnePage", "FivePages", "TenPages", "QuarterJuz", "HalfJuz", "OneJuz", "Custom", "GoalBased"];

function todayPlusDays(days: number): string {
  const d = new Date();
  d.setDate(d.getDate() + days);
  return d.toISOString().slice(0, 10);
}

export function WirdSettingsPage() {
  const navigate = useNavigate();
  const [selected, setSelected] = useState<WirdType>("OnePage");
  const [customAyahs, setCustomAyahs] = useState(10);
  const [targetDate, setTargetDate] = useState(todayPlusDays(30));
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    wirdApi
      .getPlan()
      .then((plan) => {
        if (plan) {
          setSelected(plan.type);
          if (plan.customAyahsPerDay) setCustomAyahs(plan.customAyahsPerDay);
          if (plan.targetCompletionDate) setTargetDate(plan.targetCompletionDate);
        }
      })
      .finally(() => setLoading(false));
  }, []);

  async function handleSave() {
    setError(null);

    if (selected === "GoalBased" && !targetDate) {
      setError("اختر تاريخ الهدف.");
      return;
    }

    setSaving(true);
    try {
      await wirdApi.setPlan(
        selected,
        selected === "Custom" ? customAyahs : undefined,
        selected === "GoalBased" ? targetDate : undefined
      );
      navigate("/", { replace: true });
    } catch (err) {
      setError(extractApiError(err));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="auth-shell">
      <div className="auth-card">
        <div className="brand">
          <div className="brand-mark">وِردك اليومي</div>
          <div className="brand-tagline">اختر الكمية اللي تناسبك، تقدر تغيّرها في أي وقت.</div>
        </div>
        <div className="ornament"><span /></div>

        {error && <div className="banner banner-error">{error}</div>}

        {!loading && (
          <div className="wird-options">
            {OPTIONS.map((opt) => (
              <button
                key={opt}
                type="button"
                className={`wird-option ${selected === opt ? "wird-option-active" : ""}`}
                onClick={() => setSelected(opt)}
              >
                {WIRD_TYPE_LABELS[opt]}
              </button>
            ))}
          </div>
        )}

        {selected === "Custom" && (
          <div className="field" style={{ marginTop: "16px" }}>
            <label htmlFor="customAyahs">عدد الآيات يوميًا</label>
            <input
              id="customAyahs"
              type="number"
              min={1}
              value={customAyahs}
              onChange={(e) => setCustomAyahs(Number(e.target.value))}
            />
          </div>
        )}

        {selected === "GoalBased" && (
          <div style={{ marginTop: "16px" }}>
            <div className="wird-options" style={{ flexDirection: "row", gap: "8px", marginBottom: "12px" }}>
              <button type="button" className="wird-option" onClick={() => setTargetDate(todayPlusDays(30))}>
                30 يوم
              </button>
              <button type="button" className="wird-option" onClick={() => setTargetDate(todayPlusDays(60))}>
                60 يوم
              </button>
              <button type="button" className="wird-option" onClick={() => setTargetDate(todayPlusDays(90))}>
                90 يوم
              </button>
            </div>
            <div className="field">
              <label htmlFor="targetDate">أو اختر تاريخًا محددًا</label>
              <input
                id="targetDate"
                type="date"
                min={todayPlusDays(1)}
                value={targetDate}
                onChange={(e) => setTargetDate(e.target.value)}
              />
            </div>
            <p style={{ fontSize: "0.75rem", color: "var(--color-ink-muted)" }}>
              هنحسبلك تلقائيًا كام آية تقرا كل يوم عشان تخلّص القرآن بحلول التاريخ ده. لو فوّت يوم، الكمية هتزيد تلقائيًا في الأيام الجاية عشان تفضل على المسار.
            </p>
          </div>
        )}

        <button className="btn-primary" style={{ marginTop: "20px" }} onClick={handleSave} disabled={saving || loading}>
          {saving ? "جارٍ الحفظ..." : "حفظ الورد"}
        </button>

        <div className="auth-footer">
          <Link className="btn-link" to="/">العودة للرئيسية</Link>
        </div>
      </div>
    </div>
  );
}
