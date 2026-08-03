import { request, requestVoid } from "./client";

export interface ReviewResponse {
  id: number;
  rating: number | null;
  text: string;
  containsSpoiler: boolean;
  date: string;
  bookId: number;
  userId: string;
  userName: string;
}

export interface AddReviewRequest {
  rating: number | null;
  text: string;
  containsSpoiler: boolean;
  bookId: number;
}

export function getReviewsForBook(bookId: number): Promise<ReviewResponse[]> {
  return request(`/reviews/book/${bookId}`, "Failed to fetch reviews");
}

export function addReview(review: AddReviewRequest): Promise<ReviewResponse> {
  return request("/reviews", "Failed to add review", {
    method: "POST",
    body: JSON.stringify(review),
  });
}

export interface UpdateReviewRequest {
  rating: number | null;
  text: string;
  containsSpoiler: boolean;
}

export function updateReview(
  id: number,
  review: UpdateReviewRequest,
): Promise<ReviewResponse> {
  return request(`/reviews/${id}`, "Failed to update review", {
    method: "PUT",
    body: JSON.stringify(review),
  });
}

export function deleteReview(id: number): Promise<void> {
  return requestVoid(`/reviews/${id}`, "Failed to delete review", {
    method: "DELETE",
  });
}
