import { request } from "./client";

export interface YearBook {
  bookId: number;
  title: string;
  author: string;
  coverUrl: string;
  pageCount: number | null;
  finishedDate: string;
  rating: number | null;
}

export interface YearInBooks {
  year: number;
  target: number | null;
  booksRead: number;
  pagesRead: number;
  booksWithoutPageCount: number;
  averageRating: number | null;
  rereads: number;
  months: number[];
  formats: Record<"physical" | "ebook" | "audiobook" | "unset", number>;
  longest: YearBook | null;
  shortest: YearBook | null;
  topAuthor: { name: string; books: number } | null;
  fiveStars: YearBook[];
  books: YearBook[];
}

export function getYearInBooks(year: number): Promise<YearInBooks> {
  return request(`/year-in-books/${year}`, "Failed to load your year in books");
}
