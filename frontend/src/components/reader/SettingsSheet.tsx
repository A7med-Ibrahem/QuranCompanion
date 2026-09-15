import { useTheme, type ThemeMode } from "@/context/ThemeContext";
import { ReaderSheet } from "./ReaderSheet";
import { toArabicDigits } from "./ReaderTopBar";

interface SettingsSheetProps {
  onClose: () => void;
  fontStep: number;
  minFontStep: number;
  maxFontStep: number;
  onFontStepChange: (step: number) => void;
}

const THEME_OPTIONS: { value: ThemeMode; label: string }[] = [
  { value: "light", label: "فاتح" },
  { value: "dark", label: "داكن" },
  { value: "system", label: "النظام" }
];

/** Reader settings - font size and theme, both persisted. */
export function SettingsSheet({ onClose, fontStep, minFontStep, maxFontStep, onFontStepChange }: SettingsSheetProps) {
  const { theme, setTheme } = useTheme();

  return (
    <ReaderSheet title="الإعدادات" onClose={onClose}>
      <div className="reader-settings-group">
        <label className="reader-settings-label">حجم الخط</label>
        <div className="reader-settings-row">
          <button
            type="button"
            className="btn-secondary"
            onClick={() => onFontStepChange(Math.max(minFontStep, fontStep - 1))}
            disabled={fontStep <= minFontStep}
            aria-label="تصغير الخط"
          >
            أ-
          </button>
          <span className="reader-settings-value">الخط {toArabicDigits(fontStep + 1)}</span>
          <button
            type="button"
            className="btn-secondary"
            onClick={() => onFontStepChange(Math.min(maxFontStep, fontStep + 1))}
            disabled={fontStep >= maxFontStep}
            aria-label="تكبير الخط"
          >
            أ+
          </button>
        </div>
      </div>

      <div className="reader-settings-group">
        <label className="reader-settings-label">المظهر</label>
        <div className="reader-settings-segmented">
          {THEME_OPTIONS.map((opt) => (
            <button
              key={opt.value}
              type="button"
              className={theme === opt.value ? "reader-settings-seg-active" : ""}
              onClick={() => setTheme(opt.value)}
            >
              {opt.label}
            </button>
          ))}
        </div>
      </div>
    </ReaderSheet>
  );
}