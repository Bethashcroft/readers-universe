import { request, requestVoid } from "./client";

export type BorrowStatus = "pending" | "accepted" | "declined";

export interface BorrowRequestResponse {
  id: number;
  bookId: number;
  bookTitle: string;
  fromUserId: string;
  fromUserName: string;
  toUserId: string;
  status: BorrowStatus;
  message: string;
  date: string;
}

export interface CreateBorrowRequest {
  bookId: number;
  message: string;
}

export function createBorrowRequest(
  data: CreateBorrowRequest,
): Promise<BorrowRequestResponse> {
  return request("/borrowrequests", "Failed to send borrow request", {
    method: "POST",
    body: JSON.stringify(data),
  });
}

export function getMyRequests(): Promise<BorrowRequestResponse[]> {
  return request("/borrowrequests", "Failed to fetch borrow requests");
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

export function withdrawBorrowRequest(id: number): Promise<void> {
  return requestVoid(`/borrowrequests/${id}`, "Failed to withdraw request", {
    method: "DELETE",
  });
}
