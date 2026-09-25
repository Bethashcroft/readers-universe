import { request } from "./client";
import type { FormatType } from "../types/book";

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
  format: FormatType;
  likeCount: number;
  likedByMe: boolean;
  commentCount: number;
  userName: string;
  displayName: string;
  avatarUrl: string;
  book: ActivityBook | null;
  targetUser: ActivityUser | null;
}

export interface LikesResponse {
  likeCount: number;
  likedByMe: boolean;
}

export function getFeed(): Promise<ActivityResponse[]> {
  return request("/feed", "Failed to load your feed");
}

export function likeActivity(id: number): Promise<LikesResponse> {
  return request(`/feed/${id}/like`, "Failed to like that", {
    method: "POST",
  });
}

export function unlikeActivity(id: number): Promise<LikesResponse> {
  return request(`/feed/${id}/like`, "Failed to unlike that", {
    method: "DELETE",
  });
}

export interface CommentResponse {
  id: number;
  text: string;
  date: string;
  editedDate: string | null;
  userName: string;
  displayName: string;
  avatarUrl: string;
  canEdit: boolean;
  canDelete: boolean;
}

export function getComments(id: number): Promise<CommentResponse[]> {
  return request(`/feed/${id}/comments`, "Failed to load comments");
}

export function addComment(
  id: number,
  text: string,
): Promise<CommentResponse[]> {
  return request(`/feed/${id}/comments`, "Failed to post your comment", {
    method: "POST",
    body: JSON.stringify({ text }),
  });
}

export function editComment(
  commentId: number,
  text: string,
): Promise<CommentResponse[]> {
  return request(`/feed/comments/${commentId}`, "Failed to save your comment", {
    method: "PUT",
    body: JSON.stringify({ text }),
  });
}

export function deleteComment(commentId: number): Promise<CommentResponse[]> {
  return request(
    `/feed/comments/${commentId}`,
    "Failed to delete that comment",
    { method: "DELETE" },
  );
}

export function getUserActivity(
  username: string,
): Promise<ActivityResponse[]> {
  return request(`/users/${username}/activity`, "Failed to load activity");
}
