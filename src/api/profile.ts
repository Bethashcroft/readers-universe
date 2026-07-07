import { BASE_URL, getAuthHeaders, getHeaders, parseError } from "./client";

export interface ProfileResponse {
  userName: string;
  displayName: string;
  bio: string;
  vintedUrl: string;
  avatarUrl: string;
  joinedDate: string;
}

export interface UpdateProfileRequest {
  displayName: string;
  bio: string;
  vintedUrl: string;
}

export async function getUserProfile(
  username: string,
): Promise<ProfileResponse> {
  const response = await fetch(`${BASE_URL}/users/${username}`, {
    headers: getHeaders(),
  });

  if (!response.ok) {
    throw new Error(await parseError(response, "Failed to fetch profile"));
  }

  return response.json();
}

export async function updateProfile(
  data: UpdateProfileRequest,
): Promise<ProfileResponse> {
  const response = await fetch(`${BASE_URL}/auth/profile`, {
    method: "PUT",
    headers: getHeaders(),
    body: JSON.stringify(data),
  });

  if (!response.ok) {
    throw new Error(await parseError(response, "Failed to update profile"));
  }

  return response.json();
}

export async function uploadAvatar(file: File): Promise<ProfileResponse> {
  const body = new FormData();
  body.append("file", file);

  const response = await fetch(`${BASE_URL}/auth/profile/avatar`, {
    method: "POST",
    headers: getAuthHeaders(),
    body,
  });

  if (!response.ok) {
    throw new Error(await parseError(response, "Failed to upload photo"));
  }

  return response.json();
}
