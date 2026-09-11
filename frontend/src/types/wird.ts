export type WirdType =
  | "OnePage"
  | "FivePages"
  | "TenPages"
  | "QuarterJuz"
  | "HalfJuz"
  | "OneJuz"
  | "Custom"
  | "GoalBased";

export interface WirdPlan {
  type: WirdType;
  customAyahsPerDay: number | null;
  targetCompletionDate: string | null; // "YYYY-MM-DD"
  updatedAtUtc: string;
}

export interface TodayWird {
  type: WirdType;
  startSurah: number;
  startSurahName: string;
  startAyah: number;
  endSurah: number;
  endSurahName: string;
  endAyah: number;
  ayahCount: number;
  isCompletedToday: boolean;
  daysRemainingInGoal: number | null;
  targetCompletionDate: string | null;
}

export const WIRD_TYPE_LABELS: Record<WirdType, string> = {
  OnePage: "صفحة واحدة يوميًا",
  FivePages: "5 صفحات يوميًا",
  TenPages: "10 صفحات يوميًا",
  QuarterJuz: "ربع جزء يوميًا",
  HalfJuz: "نصف جزء يوميًا",
  OneJuz: "جزء كامل يوميًا",
  Custom: "عدد آيات مخصص",
  GoalBased: "خلّص القرآن بحلول تاريخ معيّن"
};
