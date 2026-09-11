import { useState, type FormEvent } from "react";
import { useAuth } from "@/context/AuthContext";
import { authApi } from "@/api/authApi";
import { extractApiError } from "@/api/client";
import { ThemeToggle } from "@/components/ThemeToggle";

export function ProfilePage() {
  const { user, logout, refreshProfile } = useAuth();
  const [displayName, setDisplayName] = useState(user?.displayName ?? "");
  const [saved, setSaved] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  if (!user) return null;

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSaved(false);
    setSubmitting(true);
    try {
      await authApi.updateProfile({ displayName });
      await refreshProfile();
      setSaved(true);
    } catch (err) {
      setError(extractApiError(err));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="auth-shell">
      <div className="auth-card">
        <div className="brand">
          <div className="brand-mark">الملف الشخصي</div>
        </div>

        <div style={{ textAlign: "center", marginBottom: "24px" }}>
          <div className="wird-id-chip">{user.wirdId}</div>
          <p style={{ color: "var(--color-ink-muted)", fontSize: "0.8rem", marginTop: "8px" }}>
            هذا هو معرّفك الخاص — شاركه مع من تثق به للتواصل كرفقاء.
          </p>
        </div>

        {error && <div className="banner banner-error">{error}</div>}
        {saved && <div className="banner banner-success">تم حفظ التغييرات.</div>}

        <div className="continue-label" style={{ textAlign: "center" }}>مظهر التطبيق</div>
        <ThemeToggle />

        <form onSubmit={handleSubmit}>
          <div className="field">
            <label htmlFor="displayName">الاسم</label>
            <input
              id="displayName"
              type="text"
              required
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
            />
          </div>
          <div className="field">
            <label>البريد الإلكتروني</label>
            <input type="email" value={user.email} disabled />
          </div>
          <button className="btn-primary" type="submit" disabled={submitting}>
            {submitting ? "جارٍ الحفظ..." : "حفظ التغييرات"}
          </button>
        </form>

        <div className="auth-footer">
          <button className="btn-link" onClick={() => logout()}>تسجيل الخروج</button>
        </div>
      </div>
    </div>
  );
}
