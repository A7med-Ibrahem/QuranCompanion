import { apiClient } from "./client";
import type { GroupSummary, GroupDetail } from "@/types/group";

interface Envelope<T> { success: boolean; data: T; }

export const groupsApi = {
  getMine: () =>
    apiClient.get<Envelope<GroupSummary[]>>("/groups").then((r) => r.data.data),

  create: (name: string) =>
    apiClient.post<Envelope<GroupSummary>>("/groups", { name }).then((r) => r.data.data),

  getDetail: (groupId: number) =>
    apiClient.get<Envelope<GroupDetail>>(`/groups/${groupId}`).then((r) => r.data.data),

  addMember: (groupId: number, wirdId: string) =>
    apiClient
      .post<Envelope<GroupDetail>>(`/groups/${groupId}/members`, { wirdId })
      .then((r) => r.data.data),

  removeMember: (groupId: number, memberUserId: string) =>
    apiClient.delete(`/groups/${groupId}/members/${memberUserId}`),

  deleteGroup: (groupId: number) => apiClient.delete(`/groups/${groupId}`),

  setGoal: (groupId: number, targetCompletionDate: string) =>
    apiClient
      .put<Envelope<GroupDetail>>(`/groups/${groupId}/goal`, { targetCompletionDate })
      .then((r) => r.data.data)
};
