import { request } from "./client";
import type { PagedResult } from "./books";

export interface ReaderResponse {
  userName: string;
  displayName: string;
  bio: string;
  avatarUrl: string;
  joinedDate: string;
  bookCount: number;
}

export function searchReaders(options?: {
  q?: string;
  page?: number;
}): Promise<PagedResult<ReaderResponse>> {
  const params = new URLSearchParams();
  if (options?.q) params.set("q", options.q);
  if (options?.page) params.set("page", String(options.page));
  const query = params.toString();

  return request(
    `/users/search${query ? `?${query}` : ""}`,
    "Failed to search readers",
  );
}
