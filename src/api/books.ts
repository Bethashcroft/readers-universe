import { BASE_URL, getHeaders, parseError } from "./client";

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
}

export interface AddBookRequest {
  title: string;
  author: string;
  coverUrl: string;
  shelf: string;
  offer: string;
  rating: number | null;
}

export async function getMyBooks(): Promise<BookResponse[]> {
  const response = await fetch(`${BASE_URL}/books`, {
    headers: getHeaders(),
  });

  if (!response.ok) {
    throw new Error(await parseError(response, "Failed to fetch books"));
  }

  return response.json();
}

export async function browseBooks(): Promise<BookResponse[]> {
  const response = await fetch(`${BASE_URL}/books/browse`);

  if (!response.ok) {
    throw new Error(await parseError(response, "Failed to fetch books"));
  }

  return response.json();
}

export async function getBook(id: number): Promise<BookResponse> {
  const response = await fetch(`${BASE_URL}/books/${id}`, {
    headers: getHeaders(),
  });

  if (!response.ok) {
    throw new Error(await parseError(response, "Failed to fetch book"));
  }

  return response.json();
}

export async function getUserBooks(username: string): Promise<BookResponse[]> {
  const response = await fetch(`${BASE_URL}/users/${username}/books`, {
    headers: getHeaders(),
  });

  if (!response.ok) {
    throw new Error(await parseError(response, "Failed to fetch books"));
  }

  return response.json();
}

export async function addBook(book: AddBookRequest): Promise<BookResponse> {
  const response = await fetch(`${BASE_URL}/books`, {
    method: "POST",
    headers: getHeaders(),
    body: JSON.stringify(book),
  });

  if (!response.ok) {
    throw new Error(await parseError(response, "Failed to add book"));
  }

  return response.json();
}

export async function updateBook(
  id: number,
  book: AddBookRequest,
): Promise<BookResponse> {
  const response = await fetch(`${BASE_URL}/books/${id}`, {
    method: "PUT",
    headers: getHeaders(),
    body: JSON.stringify(book),
  });

  if (!response.ok) {
    throw new Error(await parseError(response, "Failed to update book"));
  }

  return response.json();
}

export async function deleteBook(id: number): Promise<void> {
  const response = await fetch(`${BASE_URL}/books/${id}`, {
    method: "DELETE",
    headers: getHeaders(),
  });

  if (!response.ok) {
    throw new Error(await parseError(response, "Failed to delete book"));
  }
}
