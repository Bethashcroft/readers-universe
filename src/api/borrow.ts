import { request, requestVoid } from "./client";

export type BorrowStatus = "pending" | "accepted" | "declined" | "returned";

export interface BorrowRequestResponse {
  id: number;
  bookId: number;
  bookTitle: string;
  fromUserId: string;
  fromUserName: string;
  toUserId: string;
  toUserName: string;
  status: BorrowStatus;
  message: string;
  date: string;
}

export interface BorrowerResponse {
  requestId: number;
  displayName: string;
  userName: string;
}

export interface OfferedBookResponse {
  libraryEntryId: number;
  bookId: number;
  title: string;
  author: string;
  coverUrl: string;
  offer: "available-to-borrow" | "lent-out";
  borrower: BorrowerResponse | null;
}

export interface BorrowingResponse {
  offering: OfferedBookResponse[];
  borrowed: BorrowRequestResponse[];
  incoming: BorrowRequestResponse[];
  outgoing: BorrowRequestResponse[];
  history: BorrowRequestResponse[];
  limit: number;
}

export interface CreateBorrowRequest {
  libraryEntryId: number;
  message: string;
}

export function getBorrowing(): Promise<BorrowingResponse> {
  return request("/borrowing", "Failed to load your borrowing");
}

export function createBorrowRequest(
  data: CreateBorrowRequest,
): Promise<BorrowRequestResponse> {
  return request("/borrowrequests", "Failed to send borrow request", {
    method: "POST",
    body: JSON.stringify(data),
  });
}

export function getPendingRequestCount(): Promise<{ count: number }> {
  return request(
    "/borrowrequests/pending-count",
    "Failed to load pending request count",
  );
}

export function updateBorrowStatus(
  id: number,
  status: BorrowStatus,
): Promise<BorrowRequestResponse> {
  return request(`/borrowrequests/${id}`, "Failed to update borrow request", {
    method: "PUT",
    body: JSON.stringify({ status }),
  });
}

export function markReturned(id: number): Promise<BorrowRequestResponse> {
  return request(`/borrowrequests/${id}/return`, "Failed to mark as returned", {
    method: "POST",
  });
}

export function withdrawBorrowRequest(id: number): Promise<void> {
  return requestVoid(`/borrowrequests/${id}`, "Failed to withdraw request", {
    method: "DELETE",
  });
}
