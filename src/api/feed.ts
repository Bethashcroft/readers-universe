import { request } from "./client";

export type ActivityType =
  | "started-reading"
  | "finished"
  | "did-not-finish"
  | "wants-to-read"
  | "reviewed"
  | "offered"
  | "followed"
  | "progress";

export interface ActivityBook {
  id: number;
  title: string;
  author: string;
  coverUrl: string;
}

export interface ActivityUser {
  userName: string;
  displayName: string;
}

export interface ActivityResponse {
  id: number;
  type: ActivityType;
  date: string;
  rating: number | null;
  page: number | null;
  pageCount: number | null;
  userName: string;
  displayName: string;
  avatarUrl: string;
  book: ActivityBook | null;
  targetUser: ActivityUser | null;
}

export function getFeed(): Promise<ActivityResponse[]> {
  return request("/feed", "Failed to load your feed");
}

export function getUserActivity(
  username: string,
): Promise<ActivityResponse[]> {
  return request(`/users/${username}/activity`, "Failed to load activity");
}
