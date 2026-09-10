import { request } from "./client";

export type FollowState = "none" | "requested" | "following" | "self";

interface FollowStateResponse {
  state: FollowState;
}

export interface FollowRequestResponse {
  userName: string;
  displayName: string;
  avatarUrl: string;
  requestedDate: string;
}

export function followReader(username: string): Promise<FollowStateResponse> {
  return request(`/users/${username}/follow`, "Failed to follow reader", {
    method: "POST",
  });
}

export function unfollowReader(username: string): Promise<FollowStateResponse> {
  return request(`/users/${username}/follow`, "Failed to unfollow reader", {
    method: "DELETE",
  });
}

export interface FollowListResponse {
  userName: string;
  displayName: string;
  avatarUrl: string;
  bio: string;
  followState: FollowState;
}

export function getFollowers(username: string): Promise<FollowListResponse[]> {
  return request(`/users/${username}/followers`, "Failed to load followers");
}

export function getFollowing(username: string): Promise<FollowListResponse[]> {
  return request(`/users/${username}/following`, "Failed to load following");
}

export function getFollowRequests(): Promise<FollowRequestResponse[]> {
  return request("/users/follow-requests", "Failed to load follow requests");
}

export function approveFollow(username: string): Promise<{ approved: boolean }> {
  return request(
    `/users/${username}/approve-follow`,
    "Failed to approve request",
    { method: "POST" },
  );
}

export function declineFollow(username: string): Promise<{ approved: boolean }> {
  return request(
    `/users/${username}/decline-follow`,
    "Failed to decline request",
    { method: "POST" },
  );
}
