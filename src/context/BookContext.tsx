import { useState, useEffect, useCallback } from "react";
import type { BookResponse, AddBookRequest } from "../api/books";
import {
  getMyBooks,
  addBook as addBookApi,
  updateBook as updateBookApi,
  deleteBook as deleteBookApi,
} from "../api/books";
import { useAuth } from "./useAuth";
import { BookContext } from "./useBooks";

export function BookProvider({ children }: { children: React.ReactNode }) {
  const [books, setBooks] = useState<BookResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const { user } = useAuth();

  const fetchBooks = useCallback(async () => {
    if (!user) {
      setBooks([]);
      setLoading(false);
      return;
    }

    try {
      setLoading(true);
      const data = await getMyBooks();
      setBooks(data);
    } catch (err) {
      console.error("Failed to fetch books:", err);
    } finally {
      setLoading(false);
    }
  }, [user]);

  useEffect(() => {
    fetchBooks();
  }, [fetchBooks]);

  const addBook = async (book: AddBookRequest) => {
    const newBook = await addBookApi(book);
    setBooks((prev) => [...prev, newBook]);
  };

  const updateBook = async (id: number, bookData: AddBookRequest) => {
    const updated = await updateBookApi(id, bookData);
    setBooks((prev) => prev.map((b) => (b.id === id ? updated : b)));
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
        addBook,
        updateBook,
        removeBook,
      }}
    >
      {children}
    </BookContext.Provider>
  );
}
