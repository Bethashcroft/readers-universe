import { useState, useEffect, useCallback, useRef } from "react";
import { Link } from "react-router-dom";
import { browseBooks } from "../api/books";
import type { LibraryEntryResponse } from "../api/books";
import { createBorrowRequest } from "../api/borrow";
import { useAuth } from "../context/useAuth";
import VintedButton from "../components/VintedButton";
import BookCover from "../components/BookCover";
import Pager from "../components/Pager";
import ErrorState from "../components/ErrorState";
import { usePageTitle } from "../hooks/usePageTitle";
import "./Browse.css";

function Browse() {
  usePageTitle("Browse Nearby");
  const { user } = useAuth();
  const [books, setBooks] = useState<LibraryEntryResponse[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [total, setTotal] = useState(0);
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [requestingBookId, setRequestingBookId] = useState<number | null>(null);
  const [message, setMessage] = useState("");
  const [sentRequests, setSentRequests] = useState<Set<number>>(new Set());
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState<
    "all" | "available-to-borrow" | "for-sale"
  >("all");

  const loadRequest = useRef(0);

  const loadBooks = useCallback(async () => {
    const ticket = ++loadRequest.current;
    setLoading(true);
    setLoadError(false);
    try {
      const result = await browseBooks({
        page,
        search: debouncedSearch,
        offer: filter,
      });
      if (ticket !== loadRequest.current) return;
      setBooks(result.items);
      setTotalPages(result.totalPages);
      setTotal(result.total);
    } catch (err) {
      if (ticket !== loadRequest.current) return;
      console.error("Failed to load browse books:", err);
      setLoadError(true);
    } finally {
      if (ticket === loadRequest.current) {
        setLoading(false);
      }
    }
  }, [page, debouncedSearch, filter]);

  useEffect(() => {
    loadBooks();
  }, [loadBooks]);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(search);
      setPage(1);
    }, 300);

    return () => clearTimeout(timer);
  }, [search]);

  const handleRequest = async (bookId: number) => {
    setError("");
    setSubmitting(true);

    try {
      await createBorrowRequest({ libraryEntryId: bookId, message });
      setSentRequests((prev) => new Set(prev).add(bookId));
      setRequestingBookId(null);
      setMessage("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to send request");
    } finally {
      setSubmitting(false);
    }
  };

  const chooseFilter = (next: typeof filter) => {
    setFilter(next);
    setPage(1);
  };

  const changePage = (next: number) => {
    setPage(next);
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  if (loadError) {
    return (
      <ErrorState
        message="We couldn't load nearby books. Check your connection and try again."
        onRetry={loadBooks}
      />
    );
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
            onClick={() => chooseFilter("all")}
          >
            All
          </button>
          <button
            className={`browse-filter ${
              filter === "available-to-borrow" ? "active" : ""
            }`}
            onClick={() => chooseFilter("available-to-borrow")}
          >
            To Borrow
          </button>
          <button
            className={`browse-filter ${filter === "for-sale" ? "active" : ""}`}
            onClick={() => chooseFilter("for-sale")}
          >
            For Sale
          </button>
        </div>
      </div>

      {loading && <p>Loading books...</p>}

      {!loading && total === 0 && (
        <p className="empty-browse">
          {debouncedSearch || filter !== "all"
            ? "No books match your search."
            : "No books available nearby right now."}
        </p>
      )}

      <div className="browse-list">
        {books.map((book) => (
          <div key={book.id} className="browse-card">
            <Link to={`/book/${book.bookId}`}>
              <BookCover
                className="browse-cover"
                src={book.coverUrl}
                title={book.title}
              />
            </Link>
            <div className="browse-info">
              <h2>
                <Link className="browse-title-link" to={`/book/${book.bookId}`}>
                  {book.title}
                </Link>
              </h2>
              <p className="browse-author">{book.author}</p>
              {book.userId !== user?.userId && (
                <Link
                  className="browse-owner"
                  to={`/profile/${book.ownerUserName}`}
                >
                  Offered by {book.ownerName}
                </Link>
              )}
              <span className={`browse-badge ${book.offer}`}>
                {book.offer === "for-sale" ? "For Sale" : "Available to Borrow"}
              </span>
              {book.offer === "for-sale" ? (
                <div className="for-sale-actions">
                  {book.sellerVintedUrl ? (
                    <VintedButton
                      href={book.sellerVintedUrl}
                      label="View on Vinted"
                    />
                  ) : (
                    <p className="no-vinted">
                      {book.userId === user?.userId
                        ? "Add your Vinted link in your profile to show a buy button here."
                        : "This seller hasn't linked their Vinted yet."}
                    </p>
                  )}
                  <p className="sale-disclaimer">
                    The Readers Universe isn't involved in sales. Purchases are
                    made on Vinted, at your own risk.
                  </p>
                </div>
              ) : book.userId === user?.userId ? (
                <p className="own-book-label">Your book</p>
              ) : book.alreadyOnShelves ? (
                <p className="own-book-label">Already on your shelves</p>
              ) : !book.canRequest ? (
                <p className="own-book-label club-only-label">
                  Only {book.ownerName}'s Trusted Book Club can borrow this
                </p>
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
                  onClick={() => {
                    setRequestingBookId(book.id);
                    setMessage("");
                    setError("");
                  }}
                >
                  Request to Borrow
                </button>
              )}
            </div>
          </div>
        ))}
      </div>

      <Pager
        page={page}
        totalPages={totalPages}
        total={total}
        noun="book"
        onChange={changePage}
      />
    </div>
  );
}

export default Browse;
