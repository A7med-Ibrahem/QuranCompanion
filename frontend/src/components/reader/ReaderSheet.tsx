import type { ReactNode } from "react";

interface ReaderSheetProps {
  title: string;
  onClose: () => void;
  children: ReactNode;
}

/** Bottom sheet used by the reader overlays (index / juz / pages / duas / settings). */
export function ReaderSheet({ title, onClose, children }: ReaderSheetProps) {
  return (
    <div className="reader-sheet-backdrop" onClick={onClose}>
      <div className="reader-sheet" onClick={(e) => e.stopPropagation()} role="dialog" aria-modal="true" aria-label={title}>
        <div className="reader-sheet-header">
          <h2>{title}</h2>
          <button type="button" className="reader-sheet-close" onClick={onClose} aria-label="إغلاق">
            ✕
          </button>
        </div>
        <div className="reader-sheet-body">{children}</div>
      </div>
    </div>
  );
}