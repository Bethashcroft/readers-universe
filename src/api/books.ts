import { request, requestVoid } from "./client";

export interface LibraryEntryResponse {
  id: number;
  bookId: number;
  title: string;
  author: string;
  coverUrl: string;
  isbn: string;
  shelf: string;
  offer: string;
  rating: number | null;
  userId: string;
  ownerName: string;
  ownerUserName: string;
  sellerVintedUrl: string;
}

export interface BookOwnerResponse {
  libraryEntryId: number;
  userName: string;
  displayName: string;
  offer: string;
  sellerVintedUrl: string;
}

export interface BookDetailResponse {
  id: number;
  title: string;
  author: string;
  coverUrl: string;
  isbn: string;
  averageRating: number | null;
  ratingCount: number;
  myEntry: LibraryEntryResponse | null;
  owners: BookOwnerResponse[];
}

export interface AddToLibraryRequest {
  bookId?: number;
  title: string;
  author: string;
  coverUrl: string;
  isbn: string;
  shelf: string;
  offer: string;
  rating: number | null;
  reviewText: string;
  containsSpoiler: boolean;
}

export interface UpdateLibraryEntryRequest {
  shelf: string;
  offer: string;
}

export function getMyBooks(): Promise<LibraryEntryResponse[]> {
  return request("/library", "Failed to fetch books");
}

export function browseBooks(): Promise<LibraryEntryResponse[]> {
  return request("/books/browse", "Failed to fetch books");
}

export function getBook(id: number): Promise<BookDetailResponse> {
  return request(`/books/${id}`, "Failed to fetch book");
}

export function getUserBooks(username: string): Promise<LibraryEntryResponse[]> {
  return request(`/users/${username}/books`, "Failed to fetch books");
}

export function addBook(
  book: AddToLibraryRequest,
): Promise<LibraryEntryResponse> {
  return request("/library", "Failed to add book", {
    method: "POST",
    body: JSON.stringify(book),
  });
}

export function updateBook(
  id: number,
  changes: UpdateLibraryEntryRequest,
): Promise<LibraryEntryResponse> {
  return request(`/library/${id}`, "Failed to update book", {
    method: "PUT",
    body: JSON.stringify(changes),
  });
}

export function deleteBook(id: number): Promise<void> {
  return requestVoid(`/library/${id}`, "Failed to delete book", {
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
