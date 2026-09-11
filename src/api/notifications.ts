import { request } from "./client";

export type NotificationType =
  | "follow-requested"
  | "follow-approved"
  | "new-follower"
  | "trusted";

export interface NotificationResponse {
  id: number;
  type: NotificationType;
  isRead: boolean;
  date: string;
  actorUserName: string;
  actorDisplayName: string;
  actorAvatarUrl: string;
}

export function getNotifications(): Promise<NotificationResponse[]> {
  return request("/notifications", "Failed to load notifications");
}

export function getNotificationCount(): Promise<{ count: number }> {
  return request(
    "/notifications/unread-count",
    "Failed to load notification count",
  );
}

export function clearNotifications(): Promise<{ count: number }> {
  return request("/notifications", "Failed to clear notifications", {
    method: "DELETE",
  });
}

export function markNotificationsRead(): Promise<{ count: number }> {
  return request("/notifications/read", "Failed to mark notifications read", {
    method: "POST",
  });
}
