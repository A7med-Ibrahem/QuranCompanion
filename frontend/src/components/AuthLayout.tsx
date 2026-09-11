import type { ReactNode } from "react";

export function AuthLayout({ children }: { children: ReactNode }) {
  return (
    <div className="auth-shell">
      <div className="auth-card">
        <div className="brand">
          <div className="brand-mark">وِرْد</div>
          <div className="brand-tagline">اقرأ وِردك. تابع رحلتك. شجّع من تثق به.</div>
        </div>
        <div className="ornament"><span /></div>
        {children}
      </div>
    </div>
  );
}
