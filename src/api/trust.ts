import { request } from "./client";

export interface TrustedReaderResponse {
  userName: string;
  displayName: string;
  avatarUrl: string;
  bio: string;
  date: string;
}

export function trustReader(username: string): Promise<{ trusted: boolean }> {
  return request(`/users/${username}/trust`, "Failed to add reader", {
    method: "POST",
  });
}

export function untrustReader(
  username: string,
): Promise<{ trusted: boolean }> {
  return request(`/users/${username}/trust`, "Failed to remove reader", {
    method: "DELETE",
  });
}

export function getTrustedReaders(): Promise<TrustedReaderResponse[]> {
  return request("/users/trusted", "Failed to load your Trusted Book Club");
}
