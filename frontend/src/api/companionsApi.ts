import { apiClient } from "./client";
import type { Companion, ConnectionRequest, CompanionStatus, PrivacySettings, Encouragement } from "@/types/companion";

interface Envelope<T> { success: boolean; data: T; }

export const companionsApi = {
  getCompanions: () =>
    apiClient.get<Envelope<Companion[]>>("/companions").then((r) => r.data.data),

  getIncoming: () =>
    apiClient.get<Envelope<ConnectionRequest[]>>("/companions/requests/incoming").then((r) => r.data.data),

  getOutgoing: () =>
    apiClient.get<Envelope<ConnectionRequest[]>>("/companions/requests/outgoing").then((r) => r.data.data),

  sendRequest: (wirdId: string) =>
    apiClient
      .post<Envelope<ConnectionRequest>>("/companions/request", { wirdId })
      .then((r) => r.data.data),

  accept: (connectionId: number) =>
    apiClient.post<Envelope<Companion>>(`/companions/${connectionId}/accept`).then((r) => r.data.data),

  reject: (connectionId: number) => apiClient.post(`/companions/${connectionId}/reject`),

  remove: (connectionId: number) => apiClient.delete(`/companions/${connectionId}`),

  getStatus: (companionUserId: string) =>
    apiClient
      .get<Envelope<CompanionStatus>>(`/companions/${companionUserId}/status`)
      .then((r) => r.data.data),

  sendEncouragement: (companionUserId: string, message: string) =>
    apiClient
      .post<Envelope<Encouragement>>(`/companions/${companionUserId}/encourage`, { message })
      .then((r) => r.data.data)
};

export const privacyApi = {
  get: () => apiClient.get<Envelope<PrivacySettings>>("/privacy-settings").then((r) => r.data.data),
  update: (settings: PrivacySettings) =>
    apiClient.put<Envelope<PrivacySettings>>("/privacy-settings", settings).then((r) => r.data.data)
};

export const encouragementApi = {
  getMessages: () => apiClient.get<Envelope<string[]>>("/encouragements/messages").then((r) => r.data.data),
  getReceived: () => apiClient.get<Envelope<Encouragement[]>>("/encouragements/received").then((r) => r.data.data)
};
