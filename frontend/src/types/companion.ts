export interface Companion {
  connectionId: number;
  userId: string;
  displayName: string;
  wirdId: string;
  connectedAtUtc: string;
}

export interface ConnectionRequest {
  connectionId: number;
  userId: string;
  displayName: string;
  wirdId: string;
  requestedAtUtc: string;
}

export interface CompanionStatus {
  companionUserId: string;
  displayName: string;
  isCompletedToday: boolean | null;
  currentStreak: number | null;
  sharedStreak: number | null;
  todayRangeSummary: string | null;
  lastReadPositionSummary: string | null;
}

export interface PrivacySettings {
  shareCompletionStatus: boolean;
  shareStreak: boolean;
  shareWirdRange: boolean;
  shareReadingProgress: boolean;
}

export interface Encouragement {
  id: number;
  fromUserId: string;
  fromDisplayName: string;
  message: string;
  createdAtUtc: string;
}
