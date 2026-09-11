import { useTheme, type ThemeMode } from "@/context/ThemeContext";

const OPTIONS: { key: ThemeMode; label: string }[] = [
  { key: "light", label: "☀️ فاتح" },
  { key: "dark", label: "🌙 غامق" },
  { key: "system", label: "⚙️ حسب النظام" }
];

export function ThemeToggle() {
  const { theme, setTheme } = useTheme();

  return (
    <div className="theme-toggle">
      {OPTIONS.map((opt) => (
        <button
          key={opt.key}
          type="button"
          className={`theme-toggle-btn ${theme === opt.key ? "theme-toggle-btn-active" : ""}`}
          onClick={() => setTheme(opt.key)}
        >
          {opt.label}
        </button>
      ))}
    </div>
  );
}
