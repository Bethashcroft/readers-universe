import { useState, useEffect, useCallback } from "react";
import type {
  LibraryEntryResponse,
  AddToLibraryRequest,
  UpdateLibraryEntryRequest,
} from "../api/books";
import {
  getMyBooks,
  addBook as addBookApi,
  updateBook as updateBookApi,
  deleteBook as deleteBookApi,
} from "../api/books";
import { useAuth } from "./useAuth";
import { BookContext } from "./useBooks";

export function BookProvider({ children }: { children: React.ReactNode }) {
  const [books, setBooks] = useState<LibraryEntryResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const { user } = useAuth();

  const fetchBooks = useCallback(async () => {
    if (!user) {
      setBooks([]);
      setLoading(false);
      setError(false);
      return;
    }

    try {
      setLoading(true);
      setError(false);
      const data = await getMyBooks();
      setBooks(data);
    } catch (err) {
      console.error("Failed to fetch books:", err);
      setError(true);
    } finally {
      setLoading(false);
    }
  }, [user]);

  useEffect(() => {
    fetchBooks();
  }, [fetchBooks]);

  const addBook = async (book: AddToLibraryRequest) => {
    const newBook = await addBookApi(book);
    setBooks((prev) => [...prev, newBook]);
  };

  const updateBook = async (
    id: number,
    changes: UpdateLibraryEntryRequest,
  ) => {
    const updated = await updateBookApi(id, changes);
    setBooks((prev) => prev.map((b) => (b.id === id ? updated : b)));
    return updated;
  };

  const removeBook = async (id: number) => {
    await deleteBookApi(id);
    setBooks((prev) => prev.filter((b) => b.id !== id));
  };

  return (
    <BookContext.Provider
      value={{
        books,
        loading,
        error,
        addBook,
        updateBook,
        removeBook,
        refresh: fetchBooks,
      }}
    >
      {children}
    </BookContext.Provider>
  );
}
