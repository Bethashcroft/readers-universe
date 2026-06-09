const BASE_URL = "http://localhost:5128/api";

export type BorrowStatus = "pending" | "accepted" | "declined";

export interface BorrowRequestResponse {
  id: number;
  bookId: number;
  bookTitle: string;
  fromUserId: string;
  toUserId: string;
  status: BorrowStatus;
  message: string;
  date: string;
}

export interface CreateBorrowRequest {
  bookId: number;
  message: string;
}

function getHeaders(): Record<string, string> {
  const stored = localStorage.getItem("user");
  const token = stored ? JSON.parse(stored).token : null;

  const headers: Record<string, string> = {
    "Content-Type": "application/json",
  };

  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  return headers;
}

export async function createBorrowRequest(
  request: CreateBorrowRequest,
): Promise<BorrowRequestResponse> {
  const response = await fetch(`${BASE_URL}/borrowrequests`, {
    method: "POST",
    headers: getHeaders(),
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const error = await response.json().catch(() => null);
    throw new Error(error?.message || "Failed to send borrow request");
  }

  return response.json();
}

export async function getMyRequests(): Promise<BorrowRequestResponse[]> {
  const response = await fetch(`${BASE_URL}/borrowrequests`, {
    headers: getHeaders(),
  });

  if (!response.ok) {
    throw new Error("Failed to fetch borrow requests");
  }

  return response.json();
}

export async function updateBorrowStatus(
  id: number,
  status: BorrowStatus,
): Promise<BorrowRequestResponse> {
  const response = await fetch(`${BASE_URL}/borrowrequests/${id}`, {
    method: "PUT",
    headers: getHeaders(),
    body: JSON.stringify({ status }),
  });

  if (!response.ok) {
    throw new Error("Failed to update borrow request");
  }

  return response.json();
}
