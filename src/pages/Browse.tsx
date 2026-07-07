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
  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState<
    "all" | "available-to-borrow" | "for-sale"
  >("all");

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

  const filteredBooks = books.filter((book) => {
    const query = search.toLowerCase();
    const matchesSearch =
      book.title.toLowerCase().includes(query) ||
      book.author.toLowerCase().includes(query);
    const matchesFilter = filter === "all" || book.offer === filter;
    return matchesSearch && matchesFilter;
  });

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

      <div className="browse-controls">
        <input
          type="text"
          className="browse-search"
          placeholder="Search by title or author"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <div className="browse-filters">
          <button
            className={`browse-filter ${filter === "all" ? "active" : ""}`}
            onClick={() => setFilter("all")}
          >
            All
          </button>
          <button
            className={`browse-filter ${
              filter === "available-to-borrow" ? "active" : ""
            }`}
            onClick={() => setFilter("available-to-borrow")}
          >
            To Borrow
          </button>
          <button
            className={`browse-filter ${filter === "for-sale" ? "active" : ""}`}
            onClick={() => setFilter("for-sale")}
          >
            For Sale
          </button>
        </div>
      </div>

      {books.length === 0 ? (
        <p className="empty-browse">No books available nearby right now.</p>
      ) : (
        filteredBooks.length === 0 && (
          <p className="empty-browse">No books match your search.</p>
        )
      )}
      <div className="browse-list">
        {filteredBooks.map((book) => (
          <div key={book.id} className="browse-card">
            <img
              className="browse-cover"
              src={book.coverUrl}
              alt={`Cover of ${book.title}`}
            />
            <div className="browse-info">
              <h2>{book.title}</h2>
              <p className="browse-author">{book.author}</p>
              <span className={`browse-badge ${book.offer}`}>
                {book.offer === "for-sale" ? "For Sale" : "Available to Borrow"}
              </span>
              {book.offer === "for-sale" ? (
                <div className="for-sale-actions">
                  {book.sellerVintedUrl ? (
                    <a
                      className="btn-vinted"
                      href={book.sellerVintedUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                    >
                      <img
                        className="vinted-logo"
                        src="/vinted-logo.png"
                        alt=""
                        aria-hidden="true"
                      />
                      View on Vinted
                    </a>
                  ) : (
                    <p className="no-vinted">
                      {book.userId === user?.userId
                        ? "Add your Vinted link in your profile to show a buy button here."
                        : "This seller hasn't linked their Vinted yet."}
                    </p>
                  )}
                  <p className="sale-disclaimer">
                    The Readers Universe isn't involved in sales — purchases are
                    made on Vinted, at your own risk.
                  </p>
                </div>
              ) : book.userId === user?.userId ? (
                <p className="own-book-label">Your book</p>
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
                  Request to Borrow
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
