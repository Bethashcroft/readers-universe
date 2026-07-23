import { request, requestVoid } from "./client";

export interface BookResponse {
  id: number;
  title: string;
  author: string;
  coverUrl: string;
  shelf: string;
  offer: string;
  rating: number | null;
  userId: string;
  sellerVintedUrl: string;
  ownerName: string;
  ownerUserName: string;
}

export interface AddBookRequest {
  title: string;
  author: string;
  coverUrl: string;
  shelf: string;
  offer: string;
  rating: number | null;
}

export function getMyBooks(): Promise<BookResponse[]> {
  return request("/books", "Failed to fetch books");
}

export function browseBooks(): Promise<BookResponse[]> {
  return request("/books/browse", "Failed to fetch books");
}

export function getBook(id: number): Promise<BookResponse> {
  return request(`/books/${id}`, "Failed to fetch book");
}

export function getUserBooks(username: string): Promise<BookResponse[]> {
  return request(`/users/${username}/books`, "Failed to fetch books");
}

export function addBook(book: AddBookRequest): Promise<BookResponse> {
  return request("/books", "Failed to add book", {
    method: "POST",
    body: JSON.stringify(book),
  });
}

export function updateBook(
  id: number,
  book: AddBookRequest,
): Promise<BookResponse> {
  return request(`/books/${id}`, "Failed to update book", {
    method: "PUT",
    body: JSON.stringify(book),
  });
}

export function deleteBook(id: number): Promise<void> {
  return requestVoid(`/books/${id}`, "Failed to delete book", {
    method: "DELETE",
  });
}

export interface BookLookupResult {
  title: string;
  author: string;
  coverUrl: string;
}

export function lookupBook(isbn: string): Promise<BookLookupResult> {
  return request(
    `/books/lookup/${encodeURIComponent(isbn)}`,
    "No book found for that ISBN",
  );
}
