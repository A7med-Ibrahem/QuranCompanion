import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { privacyApi } from "@/api/companionsApi";
import type { PrivacySettings } from "@/types/companion";

const OPTIONS: { key: keyof PrivacySettings; label: string; hint: string }[] = [
  { key: "shareCompletionStatus", label: "حالة إنجاز الورد", hint: "هل أنجزت وِرد اليوم أم لا فقط" },
  { key: "shareWirdRange", label: "نطاق الورد بالتفصيل", hint: "الصفحات أو الآيات المحددة اللي قرأتها" },
  { key: "shareStreak", label: "التتابع (Streak)", hint: "عدد أيام الاستمرارية الفردي والمشترك" },
  { key: "shareReadingProgress", label: "آخر موضع قراءة", hint: "آخر سورة وآية وصلت لها" }
];

export function PrivacySettingsPage() {
  const [settings, setSettings] = useState<PrivacySettings | null>(null);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    privacyApi.get().then(setSettings);
  }, []);

  async function toggle(key: keyof PrivacySettings) {
    if (!settings) return;
    const updated = { ...settings, [key]: !settings[key] };
    setSettings(updated);
    setSaving(true);
    setSaved(false);
    try {
      await privacyApi.update(updated);
      setSaved(true);
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="auth-shell">
      <div className="auth-card">
        <div className="brand">
          <div className="brand-mark">إعدادات الخصوصية</div>
          <div className="brand-tagline">حدد اللي رفقاؤك يقدروا يشوفوه من نشاطك.</div>
        </div>
        <div className="ornament"><span /></div>

        {saved && !saving && <div className="banner banner-success">تم الحفظ.</div>}

        {settings &&
          OPTIONS.map((opt) => (
            <div key={opt.key} className="privacy-toggle-row">
              <div>
                <div style={{ fontWeight: 600 }}>{opt.label}</div>
                <div style={{ fontSize: "0.75rem", color: "var(--color-ink-muted)" }}>{opt.hint}</div>
              </div>
              <button
                className="btn-link"
                onClick={() => toggle(opt.key)}
                style={{ minWidth: "48px" }}
                disabled={saving}
              >
                {settings[opt.key] ? "✓ مفعّل" : "غير مفعّل"}
              </button>
            </div>
          ))}

        <div className="auth-footer">
          <Link className="btn-link" to="/companions">العودة للرفقاء</Link>
        </div>
      </div>
    </div>
  );
}
