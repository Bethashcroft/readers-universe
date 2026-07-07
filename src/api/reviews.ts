import { request, requestVoid } from "./client";

export interface ReviewResponse {
  id: number;
  rating: number;
  text: string;
  date: string;
  bookId: number;
  userId: string;
  userName: string;
}

export interface AddReviewRequest {
  rating: number;
  text: string;
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

export function deleteReview(id: number): Promise<void> {
  return requestVoid(`/reviews/${id}`, "Failed to delete review", {
    method: "DELETE",
  });
}
