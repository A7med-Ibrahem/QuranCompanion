export type ReaderIconName =
  | "home"
  | "index"
  | "juz"
  | "bookmark"
  | "goto"
  | "back"
  | "tafsir"
  | "duas"
  | "search"
  | "settings";

const PATHS: Record<ReaderIconName, React.ReactNode> = {
  home: (
    <>
      <path d="M3 10.5 12 3l9 7.5" />
      <path d="M5 9.5V21h14V9.5" />
      <path d="M9.5 21v-6h5v6" />
    </>
  ),
  index: (
    <>
      <path d="M4 4.5A2.5 2.5 0 0 1 6.5 2H20v19H6.5A2.5 2.5 0 0 0 4 23.5V4.5Z" transform="translate(0 -1)" />
      <path d="M4 18.5v5" />
    </>
  ),
  juz: (
    <>
      <path d="M12 3 3 6v12l9 3 9-3V6l-9-3Z" />
      <path d="M3 6l9 3 9-3" />
      <path d="M12 9v12" />
    </>
  ),
  bookmark: (
    <>
      <path d="M6 3h12v18l-6-4.5L6 21V3Z" />
    </>
  ),
  goto: (
    <>
      <rect x="3" y="4" width="14" height="16" rx="2" />
      <path d="M10 9v6" />
      <path d="M8 12h6" />
    </>
  ),
  back: (
    <>
      <path d="M19 12H5" />
      <path d="M12 5 5 12l7 7" />
    </>
  ),
  tafsir: (
    <>
      <path d="M12 5.5C10 3.5 7 3.5 5 4v14c2-.5 5-.5 7 1.5 2-2 5-2 7-1.5V4c-2-.5-5-.5-7 1.5Z" />
      <path d="M12 5.5v14" />
    </>
  ),
  duas: (
    <>
      <path d="M11 3c0-1-3-1-3 .5V14" transform="translate(1.5 -2)" />
      <path d="M7.5 2h1.5V4" transform="translate(0 0)" />
      <path d="M13 3c0-1 3-1 3 .5V14" transform="translate(-1.5 -2)" />
      <path d="M6.5 21.5a9.5 9.5 0 0 1 11 0" />
    </>
  ),
  search: (
    <>
      <circle cx="11" cy="11" r="6.5" />
      <path d="m20 20-4-4" />
    </>
  ),
  settings: (
    <>
      <circle cx="12" cy="12" r="3" />
      <path d="M19.4 15a1.7 1.7 0 0 0 .34 1.87l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.7 1.7 0 0 0-1.87-.34 1.7 1.7 0 0 0-1 1.55V21a2 2 0 1 1-4 0v-.09a1.7 1.7 0 0 0-1-1.55 1.7 1.7 0 0 0-1.87.34l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.7 1.7 0 0 0 .34-1.87 1.7 1.7 0 0 0-1.55-1H3a2 2 0 1 1 0-4h.09a1.7 1.7 0 0 0 1.55-1 1.7 1.7 0 0 0-.34-1.87l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.7 1.7 0 0 0 1.87.34h.08a1.7 1.7 0 0 0 1-1.55V3a2 2 0 1 1 4 0v.09a1.7 1.7 0 0 0 1 1.55 1.7 1.7 0 0 0 1.87-.34l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.7 1.7 0 0 0-.34 1.87v.08a1.7 1.7 0 0 0 1.55 1H21a2 2 0 1 1 0 4h-.09a1.7 1.7 0 0 0-1.51 1Z" />
    </>
  )
};

interface ReaderIconProps {
  name: ReaderIconName;
  size?: number;
}

export function ReaderIcon({ name, size = 22 }: ReaderIconProps) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.7}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      {PATHS[name]}
    </svg>
  );
}