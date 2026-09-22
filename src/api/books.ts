import { request, requestVoid } from "./client";

export interface LibraryEntryResponse {
  id: number;
  bookId: number;
  alreadyOnShelves: boolean;
  canRequest: boolean;
  title: string;
  author: string;
  coverUrl: string;
  isbn: string;
  shelf: string;
  offer: string;
  format: string;
  page: number | null;
  pageCount: number | null;
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
  canRequest: boolean;
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
  format: string;
  rating: number | null;
  reviewText: string;
  containsSpoiler: boolean;
  pageCount?: number | null;
}

export interface UpdateLibraryEntryRequest {
  shelf: string;
  offer: string;
  format: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
}

export interface BookSummaryResponse {
  id: number;
  title: string;
  author: string;
  coverUrl: string;
}

export function searchBooks(options?: {
  q?: string;
  page?: number;
}): Promise<PagedResult<BookSummaryResponse>> {
  const params = new URLSearchParams();
  if (options?.q) params.set("q", options.q);
  if (options?.page) params.set("page", String(options.page));
  const query = params.toString();

  return request(
    `/books/search${query ? `?${query}` : ""}`,
    "Failed to search books",
  );
}

export function getMyBooks(options?: {
  shelf?: string;
  search?: string;
  sort?: string;
  offerable?: boolean;
  page?: number;
  pageSize?: number;
}): Promise<PagedResult<LibraryEntryResponse>> {
  const params = new URLSearchParams();
  if (options?.shelf) params.set("shelf", options.shelf);
  if (options?.search) params.set("search", options.search);
  if (options?.sort && options.sort !== "added")
    params.set("sort", options.sort);
  if (options?.offerable) params.set("offerable", "true");
  if (options?.page) params.set("page", String(options.page));
  if (options?.pageSize) params.set("pageSize", String(options.pageSize));
  const query = params.toString();

  return request(
    `/library${query ? `?${query}` : ""}`,
    "Failed to fetch books",
  );
}

export interface CoverBackfillResult {
  checked: number;
  fixed: number;
  alreadyFine: number;
  notFound: number;
  unverifiable: number;
  unreachable: number;
  nextAfterId: number;
  total: number;
  done: boolean;
  throttled: boolean;
}

export function refreshCovers(
  afterId: number,
  withTotal: boolean,
  signal?: AbortSignal,
): Promise<CoverBackfillResult> {
  return request(
    `/library/refresh-covers?afterId=${afterId}&withTotal=${withTotal}`,
    "Failed to look up covers",
    { method: "POST", signal },
  );
}

export function getShelfCounts(): Promise<Record<string, number>> {
  return request("/library/shelf-counts", "Failed to fetch shelf counts");
}

export function offerBook(id: number): Promise<void> {
  return requestVoid(`/library/${id}/offer`, "Failed to offer that book", {
    method: "POST",
  });
}

export function takeBackBook(id: number): Promise<void> {
  return requestVoid(`/library/${id}/offer`, "Failed to take that book back", {
    method: "DELETE",
  });
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

export function updateProgress(
  id: number,
  progress: { page: number | null; pageCount: number | null },
): Promise<LibraryEntryResponse> {
  return request(`/library/${id}/progress`, "Failed to save your progress", {
    method: "PUT",
    body: JSON.stringify(progress),
  });
}

export function lookupPageCount(id: number): Promise<LibraryEntryResponse> {
  return request(
    `/library/${id}/page-count`,
    "Failed to look up the page count",
    { method: "POST" },
  );
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
  pageCount: number | null;
}

export function lookupBook(isbn: string): Promise<BookLookupResult> {
  return request(
    `/books/lookup/${encodeURIComponent(isbn)}`,
    "No book found for that ISBN",
  );
}

export interface BookSearchResult {
  title: string;
  author: string;
  coverUrl: string;
  isbn: string;
  pageCount: number | null;
  year: number | null;
}

export function findBooks(q: string): Promise<BookSearchResult[]> {
  return request(
    `/books/lookup?q=${encodeURIComponent(q)}`,
    "Failed to search for books",
  );
}
