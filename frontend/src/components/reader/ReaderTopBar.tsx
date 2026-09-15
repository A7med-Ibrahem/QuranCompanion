interface ReaderTopBarProps {
  surahName: string;
  pageText: string;
  juzText: string;
}

const STATIC_DIGITS = ["٠", "١", "٢", "٣", "٤", "٥", "٦", "٧", "٨", "٩"];
export function toArabicDigits(n: number | string): string {
  return String(n).split("").map((d) => STATIC_DIGITS[Number(d)] ?? d).join("");
}

/** Translucent top overlay: surah name (right), page number (center), juz (left). */
export function ReaderTopBar({ surahName, pageText, juzText }: ReaderTopBarProps) {
  return (
    <div className="reader-topbar">
      <div className="reader-topbar-surah" title={surahName}>{surahName}</div>
      <div className="reader-topbar-page">{pageText}</div>
      <div className="reader-topbar-juz" title={juzText}>{juzText}</div>
    </div>
  );
}