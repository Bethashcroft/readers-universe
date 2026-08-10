import { request, requestVoid } from "./client";

export interface LibraryEntryResponse {
  id: number;
  bookId: number;
  alreadyOnShelves: boolean;
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

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
}

export function getMyBooks(options?: {
  shelf?: string;
  page?: number;
}): Promise<PagedResult<LibraryEntryResponse>> {
  const params = new URLSearchParams();
  if (options?.shelf) params.set("shelf", options.shelf);
  if (options?.page) params.set("page", String(options.page));
  const query = params.toString();

  return request(
    `/library${query ? `?${query}` : ""}`,
    "Failed to fetch books",
  );
}

export function getShelfCounts(): Promise<Record<string, number>> {
  return request("/library/shelf-counts", "Failed to fetch shelf counts");
}

export function browseBooks(options?: {
  page?: number;
  search?: string;
  offer?: string;
}): Promise<PagedResult<LibraryEntryResponse>> {
  const params = new URLSearchParams();
  if (options?.page) params.set("page", String(options.page));
  if (options?.search) params.set("search", options.search);
  if (options?.offer && options.offer !== "all")
    params.set("offer", options.offer);
  const query = params.toString();

  return request(
    `/books/browse${query ? `?${query}` : ""}`,
    "Failed to fetch books",
  );
}

export function getBook(id: number): Promise<BookDetailResponse> {
  return request(`/books/${id}`, "Failed to fetch book");
}

export function getUserBooks(
  username: string,
): Promise<LibraryEntryResponse[]> {
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
