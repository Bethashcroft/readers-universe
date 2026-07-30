import React, { useState, useEffect, useCallback } from "react";
import { useParams, useNavigate, Link } from "react-router-dom";
import { useBooks } from "../context/useBooks";
import { useAuth } from "../context/useAuth";
import { getBook } from "../api/books";
import type { BookDetailResponse } from "../api/books";
import { createBorrowRequest } from "../api/borrow";
import VintedButton from "../components/VintedButton";
import { shelfLabels as allShelfLabels } from "../types/book";
import type { ShelfType } from "../types/book";
import {
  getReviewsForBook,
  addReview as addReviewApi,
  updateReview as updateReviewApi,
  deleteReview,
} from "../api/reviews";
import type { ReviewResponse } from "../api/reviews";
import { shelfLabels, offerLabels } from "../types/book";
import ErrorState from "../components/ErrorState";
import { usePageTitle } from "../hooks/usePageTitle";
import "./BookDetail.css";

function BookDetail() {
  const { addBook, updateBook, removeBook } = useBooks();
  const { user } = useAuth();
  const { id } = useParams();
  const navigate = useNavigate();

  const [book, setBook] = useState<BookDetailResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [reviews, setReviews] = useState<ReviewResponse[]>([]);
  const [rating, setRating] = useState("");
  const [text, setText] = useState("");
  const [error, setError] = useState("");
  const [shelf, setShelf] = useState("");
  const [offer, setOffer] = useState("");
  const [reviewSpoiler, setReviewSpoiler] = useState(false);
  const [editingReviewId, setEditingReviewId] = useState<number | null>(null);
  const [addShelf, setAddShelf] = useState<ShelfType>("tbr");
  const [adding, setAdding] = useState(false);
  const [requestingEntryId, setRequestingEntryId] = useState<number | null>(
    null,
  );
  const [requestMessage, setRequestMessage] = useState("");
  const [sentRequests, setSentRequests] = useState<Set<number>>(new Set());
  const [revealedReviews, setRevealedReviews] = useState<Set<number>>(
    new Set(),
  );

  usePageTitle(book ? book.title : "Book");

  const loadBook = useCallback(async () => {
    setLoading(true);
    setLoadError(false);
    try {
      const data = await getBook(Number(id));
      setBook(data);
      setShelf(data.myEntry?.shelf ?? "");
      setOffer(data.myEntry?.offer ?? "");
      setReviews(await getReviewsForBook(data.id));
    } catch (err) {
      console.error("Failed to load book:", err);
      setLoadError(true);
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    loadBook();
  }, [loadBook]);

  if (loading) {
    return <p>Loading book...</p>;
  }

  if (loadError) {
    return (
      <ErrorState
        message="We couldn't load this book. Check your connection and try again."
        onRetry={loadBook}
      />
    );
  }

  if (!book) {
    return <p>Book not found</p>;
  }

  const myEntry = book.myEntry;
  const myReview = reviews.find((r) => r.userId === user?.userId);
  const averageStars = book.averageRating ? Math.round(book.averageRating) : 0;

  const saveEntry = async (changes: { shelf?: string; offer?: string }) => {
    if (!myEntry) return;
    const updated = await updateBook(myEntry.id, {
      shelf: changes.shelf ?? shelf,
      offer: changes.offer ?? offer,
    });
    setBook({ ...book, myEntry: updated });
  };

  const handleShelfChange = async (newShelf: string) => {
    setShelf(newShelf);
    try {
      await saveEntry({ shelf: newShelf });
    } catch (err) {
      console.error("Failed to update shelf:", err);
      setShelf(myEntry?.shelf ?? "");
    }
  };

  const handleOfferChange = async (newOffer: string) => {
    setOffer(newOffer);
    try {
      await saveEntry({ offer: newOffer });
    } catch (err) {
      console.error("Failed to update offer:", err);
      setOffer(myEntry?.offer ?? "");
    }
  };

  const handleAddToShelves = async () => {
    setError("");
    setAdding(true);

    try {
      await addBook({
        bookId: book.id,
        title: book.title,
        author: book.author,
        coverUrl: book.coverUrl,
        isbn: book.isbn,
        shelf: addShelf,
        offer: "none",
        rating: null,
        reviewText: "",
        containsSpoiler: false,
      });
      await loadBook();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to add book");
    } finally {
      setAdding(false);
    }
  };

  const handleBorrowRequest = async (entryId: number) => {
    setError("");

    try {
      await createBorrowRequest({
        libraryEntryId: entryId,
        message: requestMessage,
      });
      setSentRequests((prev) => new Set(prev).add(entryId));
      setRequestingEntryId(null);
      setRequestMessage("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to send request");
    }
  };

  const handleRemove = async () => {
    if (!myEntry) return;

    if (!window.confirm(`Remove "${book.title}" from your shelves?`)) {
      return;
    }

    try {
      await removeBook(myEntry.id);
      navigate("/shelves");
    } catch (err) {
      console.error("Failed to remove book:", err);
    }
  };

  const startEditingReview = (review: ReviewResponse) => {
    setEditingReviewId(review.id);
    setRating(String(review.rating));
    setText(review.text);
    setReviewSpoiler(review.containsSpoiler);
    setError("");
  };

  const stopEditingReview = () => {
    setEditingReviewId(null);
    setRating("");
    setText("");
    setReviewSpoiler(false);
    setError("");
  };

  const handleReviewSubmit = async (e: React.SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError("");

    const details = {
      rating: Number(rating),
      text,
      containsSpoiler: reviewSpoiler,
    };

    try {
      const saved = editingReviewId
        ? await updateReviewApi(editingReviewId, details)
        : await addReviewApi({ ...details, bookId: book.id });

      setReviews((prev) =>
        editingReviewId
          ? prev.map((r) => (r.id === saved.id ? saved : r))
          : [...prev, saved],
      );
      stopEditingReview();
      if (myEntry) {
        setBook({ ...book, myEntry: { ...myEntry, rating: saved.rating } });
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save review");
    }
  };

  const handleDeleteReview = async (reviewId: number) => {
    try {
      await deleteReview(reviewId);
      setReviews((prev) => prev.filter((r) => r.id !== reviewId));
      if (myEntry) {
        setBook({ ...book, myEntry: { ...myEntry, rating: null } });
      }
    } catch (err) {
      console.error("Failed to delete review:", err);
    }
  };

  return (
    <div className="book-detail">
      <div className="book-detail-header">
        <img
          className="book-detail-cover"
          src={book.coverUrl}
          alt={`Cover of ${book.title}`}
        />
        <div className="book-detail-info">
          <h1>{book.title}</h1>
          <p className="book-detail-author">by {book.author}</p>

          {book.averageRating !== null ? (
            <p className="book-detail-rating">
              {"★".repeat(averageStars)}
              {"☆".repeat(5 - averageStars)}
              <span className="rating-meta">
                {book.averageRating} from {book.ratingCount}{" "}
                {book.ratingCount === 1 ? "rating" : "ratings"}
              </span>
            </p>
          ) : (
            <p className="rating-meta">No ratings yet</p>
          )}

          {myEntry ? (
            <>
              <div className="book-detail-controls">
                <label htmlFor="book-shelf">Shelf</label>
                <select
                  id="book-shelf"
                  value={shelf}
                  onChange={(e) => handleShelfChange(e.target.value)}
                >
                  {Object.entries(shelfLabels).map(([value, label]) => (
                    <option key={value} value={value}>
                      {label}
                    </option>
                  ))}
                </select>

                <label htmlFor="book-offer">Lending &amp; Selling</label>
                <select
                  id="book-offer"
                  value={offer}
                  onChange={(e) => handleOfferChange(e.target.value)}
                >
                  {Object.entries(offerLabels).map(([value, label]) => (
                    <option key={value} value={value}>
                      {label}
                    </option>
                  ))}
                </select>
              </div>

              {offer === "for-sale" && (
                <button
                  className="btn btn-secondary offer-clear"
                  onClick={() => handleOfferChange("none")}
                >
                  Sold / No Longer Selling
                </button>
              )}

              <button className="btn btn-secondary remove-entry" onClick={handleRemove}>
                Remove from My Shelves
              </button>
            </>
          ) : (
            <div className="add-to-shelves">
              <label htmlFor="add-shelf">Add this book to your shelves</label>
              <div className="add-to-shelves-row">
                <select
                  id="add-shelf"
                  value={addShelf}
                  onChange={(e) => setAddShelf(e.target.value as ShelfType)}
                >
                  {Object.entries(allShelfLabels).map(([value, label]) => (
                    <option key={value} value={value}>
                      {label}
                    </option>
                  ))}
                </select>
                <button
                  className="btn btn-primary"
                  onClick={handleAddToShelves}
                  disabled={adding}
                >
                  {adding ? "Adding..." : "Add to my shelves"}
                </button>
              </div>
              {error && <p className="form-error">{error}</p>}
            </div>
          )}
        </div>
      </div>

      {book.owners.length > 0 && (
        <section className="owners-section">
          <h2>Available from</h2>
          {book.owners.map((owner) => (
            <div key={owner.libraryEntryId} className="owner-card">
              <div className="owner-details">
                <Link to={`/profile/${owner.userName}`} className="owner-name">
                  {owner.displayName}
                </Link>
                <span className={`browse-badge ${owner.offer}`}>
                  {owner.offer === "for-sale" ? "For Sale" : "Available to Borrow"}
                </span>
              </div>

              {owner.offer === "for-sale" ? (
                owner.sellerVintedUrl ? (
                  <VintedButton
                    href={owner.sellerVintedUrl}
                    label="View on Vinted"
                  />
                ) : (
                  <p className="rating-meta">
                    This seller hasn't linked their Vinted yet.
                  </p>
                )
              ) : myEntry ? (
                <p className="rating-meta">Already on your shelves</p>
              ) : myReview ? (
                <p className="rating-meta">You've already rated this book</p>
              ) : sentRequests.has(owner.libraryEntryId) ? (
                <p className="request-sent">Request sent!</p>
              ) : requestingEntryId === owner.libraryEntryId ? (
                <div className="owner-request-form">
                  <textarea
                    placeholder="Add a message (optional)"
                    value={requestMessage}
                    onChange={(e) => setRequestMessage(e.target.value)}
                    rows={3}
                  />
                  <div className="owner-request-actions">
                    <button
                      className="btn btn-primary"
                      onClick={() => handleBorrowRequest(owner.libraryEntryId)}
                    >
                      Send request
                    </button>
                    <button
                      className="btn btn-secondary"
                      onClick={() => {
                        setRequestingEntryId(null);
                        setRequestMessage("");
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
                    setRequestingEntryId(owner.libraryEntryId);
                    setRequestMessage("");
                    setError("");
                  }}
                >
                  Request to Borrow
                </button>
              )}
            </div>
          ))}
        </section>
      )}

      <section className="reviews-section">
        <h2>Reviews ({reviews.length})</h2>

        {reviews.map((review) => (
          <div key={review.id} className="review-card">
            <div className="review-header">
              <span className="review-author">{review.userName}</span>
              <span className="review-rating">
                {" "}
                {"★".repeat(review.rating)}
                {"☆".repeat(5 - review.rating)}
              </span>
              <span className="review-date">
                {new Date(review.date).toLocaleDateString("en-GB", {
                  day: "numeric",
                  month: "short",
                  year: "numeric",
                })}
              </span>
            </div>
            {review.containsSpoiler && !revealedReviews.has(review.id) ? (
              <button
                type="button"
                className="review-spoiler-toggle"
                onClick={() =>
                  setRevealedReviews((prev) => new Set(prev).add(review.id))
                }
              >
                This review contains spoilers →
              </button>
            ) : (
              review.text && <p className="review-text">{review.text}</p>
            )}
            {user?.userId === review.userId && (
              <div className="review-actions">
                <button
                  className="review-edit"
                  onClick={() => startEditingReview(review)}
                >
                  Edit
                </button>
                <button
                  className="review-delete"
                  onClick={() => handleDeleteReview(review.id)}
                >
                  Delete
                </button>
              </div>
            )}
          </div>
        ))}

        {reviews.length === 0 && (
          <p className="no-reviews">No reviews yet. Be the first!</p>
        )}
      </section>

      {(!myReview || editingReviewId !== null) && (
        <section className="add-review-section">
          <h2>{editingReviewId ? "Edit your review" : "Rate and Review"}</h2>
          {error && <p className="form-error">{error}</p>}
          <form className="review-form" onSubmit={handleReviewSubmit}>
            <label htmlFor="review-rating">Rating</label>
            <select
              id="review-rating"
              value={rating}
              onChange={(e) => setRating(e.target.value)}
              required
            >
              <option value="">Select a rating</option>
              <option value="1">★☆☆☆☆</option>
              <option value="2">★★☆☆☆</option>
              <option value="3">★★★☆☆</option>
              <option value="4">★★★★☆</option>
              <option value="5">★★★★★</option>
            </select>

            <label htmlFor="review-text">Review (optional)</label>
            <textarea
              id="review-text"
              value={text}
              onChange={(e) => setText(e.target.value)}
              rows={4}
            />

            <label className="review-spoiler-check">
              <input
                type="checkbox"
                checked={reviewSpoiler}
                onChange={(e) => setReviewSpoiler(e.target.checked)}
              />
              This review contains spoilers
            </label>

            <div className="review-form-actions">
              <button type="submit">
                {editingReviewId ? "Save changes" : "Submit Review"}
              </button>
              {editingReviewId && (
                <button
                  type="button"
                  className="review-cancel"
                  onClick={stopEditingReview}
                >
                  Cancel
                </button>
              )}
            </div>
          </form>
        </section>
      )}
    </div>
  );
}

export default BookDetail;
