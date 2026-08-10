import type {
  LibraryEntryResponse,
  AddToLibraryRequest,
  UpdateLibraryEntryRequest,
} from "../api/books";
import {
  addBook as addBookApi,
  updateBook as updateBookApi,
  deleteBook as deleteBookApi,
} from "../api/books";
import { BookContext } from "./useBooks";

export function BookProvider({ children }: { children: React.ReactNode }) {
  const addBook = async (book: AddToLibraryRequest) => {
    await addBookApi(book);
  };

  const updateBook = (
    id: number,
    changes: UpdateLibraryEntryRequest,
  ): Promise<LibraryEntryResponse> => updateBookApi(id, changes);

  const removeBook = async (id: number) => {
    await deleteBookApi(id);
  };

  return (
    <BookContext.Provider value={{ addBook, updateBook, removeBook }}>
      {children}
    </BookContext.Provider>
  );
}
