import { ReaderIcon, type ReaderIconName } from "./ReaderIcon";

export type ReaderAction =
  | "home"
  | "index"
  | "juz"
  | "bookmarks"
  | "goto"
  | "back"
  | "tafsir"
  | "duas"
  | "search"
  | "settings";

interface ToolbarButton {
  action: ReaderAction;
  icon: ReaderIconName;
  label: string;
}

// Row 1 (primary navigation) and row 2 (reading tools).
const ROW_1: ToolbarButton[] = [
  { action: "home", icon: "home", label: "الرئيسية" },
  { action: "index", icon: "index", label: "الفهرس" },
  { action: "juz", icon: "juz", label: "الأجزاء" },
  { action: "bookmarks", icon: "bookmark", label: "العلامات" },
  { action: "goto", icon: "goto", label: "الانتقال إلى صفحة" },
  { action: "back", icon: "back", label: "رجوع" }
];

const ROW_2: ToolbarButton[] = [
  { action: "tafsir", icon: "tafsir", label: "التفسير" },
  { action: "duas", icon: "duas", label: "الأدعية" },
  { action: "search", icon: "search", label: "البحث" },
  { action: "settings", icon: "settings", label: "الإعدادات" }
];

interface ReaderToolbarProps {
  onAction: (action: ReaderAction) => void;
}

function ToolbarRow({ buttons, onAction }: { buttons: ToolbarButton[]; onAction: (a: ReaderAction) => void }) {
  return (
    <div className="reader-toolbar-row">
      {buttons.map((b) => (
        <button key={b.action} type="button" className="reader-tool-btn" onClick={() => onAction(b.action)}>
          <ReaderIcon name={b.icon} size={20} />
          <span>{b.label}</span>
        </button>
      ))}
    </div>
  );
}

/** Floating bottom toolbar - two rows, every button wired to a real action. */
export function ReaderToolbar({ onAction }: ReaderToolbarProps) {
  return (
    <div className="reader-toolbar">
      <ToolbarRow buttons={ROW_1} onAction={onAction} />
      <ToolbarRow buttons={ROW_2} onAction={onAction} />
    </div>
  );
}