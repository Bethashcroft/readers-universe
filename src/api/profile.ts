import { getAuthHeaders, request } from "./client";
import type { FollowState } from "./follows";

export interface ProfileResponse {
  userName: string;
  displayName: string;
  bio: string;
  vintedUrl: string;
  avatarUrl: string;
  joinedDate: string;
  usernameChangeableOn: string | null;
  isPrivate: boolean;
  followState: FollowState;
  followerCount: number;
  followingCount: number;
  canView: boolean;
  trusted: boolean;
  trustsMe: boolean;
}

export interface UpdateProfileRequest {
  userName: string;
  displayName: string;
  bio: string;
  vintedUrl: string;
  isPrivate: boolean;
}

export function getUserProfile(username: string): Promise<ProfileResponse> {
  return request(`/users/${username}`, "Failed to fetch profile");
}

export function updateProfile(
  data: UpdateProfileRequest,
): Promise<ProfileResponse> {
  return request("/auth/profile", "Failed to update profile", {
    method: "PUT",
    body: JSON.stringify(data),
  });
}

export function uploadAvatar(file: File): Promise<ProfileResponse> {
  const body = new FormData();
  body.append("file", file);

  return request("/auth/profile/avatar", "Failed to upload photo", {
    method: "POST",
    headers: getAuthHeaders(),
    body,
  });
}
