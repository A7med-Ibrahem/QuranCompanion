export interface GroupSummary {
  id: number;
  name: string;
  memberCount: number;
  createdAtUtc: string;
}

export interface SharedGoal {
  targetCompletionDate: string; // "YYYY-MM-DD"
  daysRemaining: number;
  createdByUserId: string;
}

export interface GroupMemberStatus {
  userId: string;
  displayName: string;
  wirdId: string;
  isCreator: boolean;
  isCompletedToday: boolean | null;
  currentStreak: number | null;
  overallProgressPercent: number | null;
}

export interface GroupDetail {
  id: number;
  name: string;
  createdByUserId: string;
  sharedGoal: SharedGoal | null;
  members: GroupMemberStatus[];
}
