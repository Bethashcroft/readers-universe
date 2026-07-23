import { getAuthHeaders, request } from "./client";

export interface ProfileResponse {
  userName: string;
  displayName: string;
  bio: string;
  vintedUrl: string;
  avatarUrl: string;
  joinedDate: string;
  usernameChangeableOn: string | null;
}

export interface UpdateProfileRequest {
  userName: string;
  displayName: string;
  bio: string;
  vintedUrl: string;
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
