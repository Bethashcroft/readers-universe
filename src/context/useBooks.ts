import { createContext, useContext } from "react";
import type { BookResponse, AddBookRequest } from "../api/books";

export interface BookContextType {
  books: BookResponse[];
  loading: boolean;
  error: boolean;
  addBook: (book: AddBookRequest) => Promise<void>;
  updateBook: (id: number, book: AddBookRequest) => Promise<void>;
  removeBook: (id: number) => Promise<void>;
  refresh: () => void;
}

export const BookContext = createContext<BookContextType | null>(null);

export function useBooks() {
  const context = useContext(BookContext);

  if (!context) {
    throw new Error("useBooks must be used within a BookProvider");
  }

  return context;
}
