import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { AuthLayout } from "@/components/AuthLayout";
import { authApi } from "@/api/authApi";
import { extractApiError } from "@/api/client";

type Step = "request" | "verify";

export function ForgotPasswordPage() {
  const navigate = useNavigate();
  const [step, setStep] = useState<Step>("request");

  const [email, setEmail] = useState("");
  const [code, setCode] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const [error, setError] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleRequestCode(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await authApi.forgotPassword(email);
      setStep("verify");
      setInfo("إذا كان هذا البريد مسجلاً لدينا، وصلك رمز مكوّن من 6 أرقام. صالح لمدة 10 دقائق.");
    } catch (err) {
      setError(extractApiError(err));
    } finally {
      setSubmitting(false);
    }
  }

  async function handleResendCode() {
    setError(null);
    setInfo(null);
    setSubmitting(true);
    try {
      await authApi.forgotPassword(email);
      setInfo("تم إرسال رمز جديد.");
    } catch (err) {
      setError(extractApiError(err));
    } finally {
      setSubmitting(false);
    }
  }

  async function handleVerifyAndReset(e: FormEvent) {
    e.preventDefault();
    setError(null);

    if (code.trim().length !== 6) {
      setError("الرمز مكوّن من 6 أرقام.");
      return;
    }
    if (newPassword.length < 8) {
      setError("كلمة المرور يجب ألا تقل عن 8 أحرف.");
      return;
    }
    if (!/\d/.test(newPassword)) {
      setError("كلمة المرور يجب أن تحتوي على رقم واحد على الأقل.");
      return;
    }
    if (newPassword !== confirmPassword) {
      setError("كلمتا المرور غير متطابقتين.");
      return;
    }

    setSubmitting(true);
    try {
      await authApi.resetPassword({ email, code: code.trim(), newPassword });
      navigate("/login", { replace: true });
    } catch (err) {
      setError(extractApiError(err));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <AuthLayout>
      {error && <div className="banner banner-error">{error}</div>}
      {info && !error && <div className="banner banner-success">{info}</div>}

      {step === "request" ? (
        <form onSubmit={handleRequestCode}>
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
          <button className="btn-primary" type="submit" disabled={submitting}>
            {submitting ? "جارٍ الإرسال..." : "إرسال رمز التحقق"}
          </button>
        </form>
      ) : (
        <form onSubmit={handleVerifyAndReset}>
          <div className="field">
            <label htmlFor="code">رمز التحقق (6 أرقام)</label>
            <input
              id="code"
              type="text"
              inputMode="numeric"
              maxLength={6}
              required
              value={code}
              onChange={(e) => setCode(e.target.value.replace(/\D/g, ""))}
              style={{ textAlign: "center", letterSpacing: "0.4em", direction: "ltr", fontSize: "1.2rem" }}
              placeholder="000000"
            />
          </div>
          <div className="field">
            <label htmlFor="newPassword">كلمة المرور الجديدة</label>
            <input
              id="newPassword"
              type="password"
              required
              autoComplete="new-password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
            />
          </div>
          <div className="field">
            <label htmlFor="confirmPassword">تأكيد كلمة المرور الجديدة</label>
            <input
              id="confirmPassword"
              type="password"
              required
              autoComplete="new-password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
            />
          </div>
          <button className="btn-primary" type="submit" disabled={submitting}>
            {submitting ? "جارٍ الحفظ..." : "تعيين كلمة المرور"}
          </button>
          <div style={{ textAlign: "center", marginTop: "16px" }}>
            <button type="button" className="btn-link" onClick={handleResendCode} disabled={submitting}>
              إعادة إرسال الرمز
            </button>
          </div>
        </form>
      )}

      <div className="auth-footer">
        <Link className="btn-link" to="/login">العودة لتسجيل الدخول</Link>
      </div>
    </AuthLayout>
  );
}
