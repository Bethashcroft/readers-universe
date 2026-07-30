import { createContext, useContext } from "react";
import type {
  LibraryEntryResponse,
  AddToLibraryRequest,
  UpdateLibraryEntryRequest,
} from "../api/books";

export interface BookContextType {
  books: LibraryEntryResponse[];
  loading: boolean;
  error: boolean;
  addBook: (book: AddToLibraryRequest) => Promise<void>;
  updateBook: (
    id: number,
    changes: UpdateLibraryEntryRequest,
  ) => Promise<LibraryEntryResponse>;
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
