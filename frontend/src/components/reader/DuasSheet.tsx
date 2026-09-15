import { useNavigate } from "react-router-dom";
import { QURANI_DUAS } from "@/data/qurani-duas";
import { ReaderSheet } from "./ReaderSheet";
import { toArabicDigits } from "./ReaderTopBar";

interface DuasSheetProps {
  onClose: () => void;
}

/** Quranic duas - each entry jumps to its exact ayah in the reader. */
export function DuasSheet({ onClose }: DuasSheetProps) {
  const navigate = useNavigate();

  function openDua(surahNumber: number, ayahNumber: number) {
    onClose();
    navigate(`/quran/${surahNumber}?ayah=${ayahNumber}`);
  }

  return (
    <ReaderSheet title="الأدعية" onClose={onClose}>
      <p className="reader-sheet-hint">أدعية من القرآن الكريم. اضغط على أي دعاء للانتقال إلى موضعه.</p>
      <div className="reader-sheet-list">
        {QURANI_DUAS.map((dua) => (
          <button
            key={`${dua.surahNumber}:${dua.ayahNumber}`}
            type="button"
            className="reader-sheet-item reader-sheet-dua"
            onClick={() => openDua(dua.surahNumber, dua.ayahNumber)}
          >
            <span className="reader-sheet-item-name">{dua.title}</span>
            <span className="reader-sheet-dua-text">{dua.text}</span>
            <span className="reader-sheet-item-meta">
              سورة {toArabicDigits(dua.surahNumber)} · آية {toArabicDigits(dua.ayahNumber)}
            </span>
          </button>
        ))}
      </div>
    </ReaderSheet>
  );
}