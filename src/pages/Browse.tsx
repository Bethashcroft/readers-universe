import { useState, useEffect } from "react";
import { browseBooks } from "../api/books";
import type { BookResponse } from "../api/books";
import { createBorrowRequest } from "../api/borrow";
import { useAuth } from "../context/AuthContext";
import "./Browse.css";

function Browse() {
  const { user } = useAuth();
  const [books, setBooks] = useState<BookResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [requestingBookId, setRequestingBookId] = useState<number | null>(null);
  const [message, setMessage] = useState("");
  const [sentRequests, setSentRequests] = useState<Set<number>>(new Set());
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    const fetchBooks = async () => {
      try {
        const data = await browseBooks();
        setBooks(data);
      } catch (err) {
        console.error("Failed to load browse books:", err);
      } finally {
        setLoading(false);
      }
    };

    fetchBooks();
  }, []);

  const handleRequest = async (bookId: number) => {
    setError("");
    setSubmitting(true);

    try {
      await createBorrowRequest({ bookId, message });
      setSentRequests((prev) => new Set(prev).add(bookId));
      setRequestingBookId(null);
      setMessage("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to send request");
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return <p>Loading books...</p>;
  }

  return (
    <div className="browse">
      <h1>Browse Nearby Books</h1>
      <p className="browse-subtitle">
        Books available to borrow or buy from readers near you
      </p>
      <div className="browse-banner">
        For your safety, always arrange exchanges through in-app messaging.
        Never share personal contact details.
      </div>
      {books.length === 0 && (
        <p className="empty-browse">No books available nearby right now.</p>
      )}
      <div className="browse-list">
        {books.map((book) => (
          <div key={book.id} className="browse-card">
            <img
              className="browse-cover"
              src={book.coverUrl}
              alt={`Cover of ${book.title}`}
            />
            <div className="browse-info">
              <h2>{book.title}</h2>
              <p className="browse-author">{book.author}</p>
              <span className={`browse-badge ${book.shelf}`}>
                {book.shelf === "for-sale" ? "For Sale" : "Available to Borrow"}
              </span>
              {!user ? (
                <p className="login-prompt">Log in to request this book</p>
              ) : sentRequests.has(book.id) ? (
                <p className="request-sent">Request sent!</p>
              ) : requestingBookId === book.id ? (
                <div className="request-form">
                  <textarea
                    placeholder="Add a message (optional)"
                    value={message}
                    onChange={(e) => setMessage(e.target.value)}
                    rows={3}
                  />
                  {error && <p className="form-error">{error}</p>}
                  <div className="request-actions">
                    <button
                      className="btn btn-primary"
                      onClick={() => handleRequest(book.id)}
                      disabled={submitting}
                    >
                      {submitting ? "Sending..." : "Send request"}
                    </button>
                    <button
                      className="btn btn-secondary"
                      onClick={() => {
                        setRequestingBookId(null);
                        setMessage("");
                      }}
                    >
                      Cancel
                    </button>
                  </div>
                </div>
              ) : (
                <button
                  className="btn btn-primary"
                  onClick={() => setRequestingBookId(book.id)}
                >
                  {book.shelf === "for-sale"
                    ? "Request to Buy"
                    : "Request to Borrow"}
                </button>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

export default Browse;
