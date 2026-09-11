import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { AuthLayout } from "@/components/AuthLayout";
import { GoogleSignInButton } from "@/components/GoogleSignInButton";
import { useAuth } from "@/context/AuthContext";
import { extractApiError } from "@/api/client";

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await login(email, password);
      navigate("/", { replace: true });
    } catch (err) {
      setError(extractApiError(err));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <AuthLayout>
      {error && <div className="banner banner-error">{error}</div>}
      <GoogleSignInButton />
      <div className="or-divider">أو بالبريد الإلكتروني</div>
      <form onSubmit={handleSubmit}>
        <div className="field">
          <label htmlFor="email">البريد الإلكتروني</label>
          <input
            id="email"
            type="email"
            required
            autoComplete="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
        </div>
        <div className="field">
          <label htmlFor="password">كلمة المرور</label>
          <input
            id="password"
            type="password"
            required
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </div>
        <div style={{ textAlign: "end", marginBottom: "16px" }}>
          <Link className="btn-link" to="/forgot-password">نسيت كلمة المرور؟</Link>
        </div>
        <button className="btn-primary" type="submit" disabled={submitting}>
          {submitting ? "جارٍ الدخول..." : "تسجيل الدخول"}
        </button>
      </form>
      <div className="auth-footer">
        ليس لديك حساب؟ <Link className="btn-link" to="/register">أنشئ حسابًا</Link>
      </div>
    </AuthLayout>
  );
}
