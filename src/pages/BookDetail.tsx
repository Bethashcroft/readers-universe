import React, { useState, useEffect, useCallback, useRef } from "react";
import { useParams, useNavigate, Link } from "react-router-dom";
import { useBooks } from "../context/useBooks";
import { useAuth } from "../context/useAuth";
import { getBook, lookupPageCount } from "../api/books";
import type { BookDetailResponse } from "../api/books";
import { createBorrowRequest, getBorrowing } from "../api/borrow";
import VintedButton from "../components/VintedButton";
import type { ShelfType } from "../types/book";
import {
  getReviewsForBook,
  addReview as addReviewApi,
  updateReview as updateReviewApi,
  deleteReview,
} from "../api/reviews";
import type { ReviewResponse } from "../api/reviews";
import {
  shelfChoices,
  offerChoices,
  formatChoices,
  ratingOptions,
  canOffer,
  formatLabel,
  cannotOfferMessage,
} from "../types/book";
import SelectMenu from "../components/SelectMenu";
import BookCover from "../components/BookCover";
import ReadingProgress from "../components/ReadingProgress";
import PageCount from "../components/PageCount";
import ReadingHistory from "../components/ReadingHistory";
import ErrorState from "../components/ErrorState";
import { usePageTitle } from "../hooks/usePageTitle";
import { formatDate, today } from "../utils/dates";
import "../styles/forms.css";
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
  const [format, setFormat] = useState("");
  const [offerError, setOfferError] = useState("");
  const [reviewSpoiler, setReviewSpoiler] = useState(false);
  const [editingReviewId, setEditingReviewId] = useState<number | null>(null);
  const [dnfNudge, setDnfNudge] = useState(false);
  const [lendNudge, setLendNudge] = useState(false);
  const reviewTextRef = useRef<HTMLTextAreaElement>(null);
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

      if (
        data.myEntry?.shelf === "currently-reading" &&
        data.myEntry.pageCount === null
      ) {
        data.myEntry = await lookupPageCount(data.myEntry.id);
      }

      setBook(data);
      setShelf(data.myEntry?.shelf ?? "");
      setOffer(data.myEntry?.offer ?? "");
      setFormat(data.myEntry?.format ?? "");
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

  const saveEntry = async (changes: {
    shelf?: string;
    offer?: string;
    format?: string;
  }) => {
    if (!myEntry) return;
    const updated = await updateBook(myEntry.id, {
      shelf: changes.shelf ?? shelf,
      offer: changes.offer ?? offer,
      format: changes.format ?? format,
      today: today(),
    });
    setBook({ ...book, myEntry: updated });
  };

  const hasLendingRoom = async () => {
    try {
      const { offering, limit } = await getBorrowing();
      return offering.length < limit;
    } catch {
      return false;
    }
  };

  const handleShelfChange = async (newShelf: string) => {
    const justFinished =
      newShelf === "read" &&
      myEntry?.shelf !== "read" &&
      offer === "none" &&
      canOffer(format);

    setShelf(newShelf);
    setLendNudge(false);
    try {
      await saveEntry({ shelf: newShelf });

      if (justFinished && (await hasLendingRoom())) {
        setLendNudge(true);
      }

      if (newShelf === "dnf" && !myReview) {
        setDnfNudge(true);
        reviewTextRef.current?.scrollIntoView({
          behavior: "smooth",
          block: "center",
        });
        reviewTextRef.current?.focus({ preventScroll: true });
      }
    } catch (err) {
      console.error("Failed to update shelf:", err);
      setShelf(myEntry?.shelf ?? "");
    }
  };

  const handleOfferChange = async (newOffer: string) => {
    setOffer(newOffer);
    setOfferError("");
    try {
      await saveEntry({ offer: newOffer });
    } catch (err) {
      setOfferError(
        err instanceof Error ? err.message : "Failed to update offer",
      );
      setOffer(myEntry?.offer ?? "");
    }
  };

  const lendIt = async () => {
    setLendNudge(false);
    await handleOfferChange("available-to-borrow");
  };

  const onLoan = offer === "lent-out";

  const handleFormatChange = async (newFormat: string) => {
    const newOffer = canOffer(newFormat) ? offer : "none";

    setFormat(newFormat);
    setOffer(newOffer);
    setOfferError("");

    try {
      await saveEntry({ format: newFormat, offer: newOffer });
    } catch (err) {
      setOfferError(
        err instanceof Error ? err.message : "Failed to update format",
      );
      setFormat(myEntry?.format ?? "");
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
        format: "",
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
    setRating(review.rating === null ? "" : String(review.rating));
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
    setDnfNudge(false);
  };

  const handleReviewSubmit = async (e: React.SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError("");

    if (rating === "" && !text.trim()) {
      setError("Add a rating or write a review");
      return;
    }

    const details = {
      rating: rating === "" ? null : Number(rating),
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
        <BookCover
          className="book-detail-cover"
          src={book.coverUrl}
          title={book.title}
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
                <SelectMenu
                  label="Shelf"
                  value={shelf}
                  options={shelfChoices}
                  onChange={handleShelfChange}
                  block
                />

                {onLoan ? (
                  <div className="book-offer-note">
                    <span className="form-field-label" id="book-format-label">
                      Format
                    </span>
                    <p className="rating-meta" aria-labelledby="book-format-label">
                      {formatLabel(format)}
                    </p>
                  </div>
                ) : (
                  <SelectMenu
                    label="Format"
                    value={format}
                    options={formatChoices}
                    onChange={handleFormatChange}
                    block
                  />
                )}

                {onLoan || !canOffer(format) ? (
                  <div className="book-offer-note">
                    <span className="form-field-label" id="book-offer-label">
                      Lending &amp; Selling
                    </span>
                    <p className="rating-meta" aria-labelledby="book-offer-label">
                      {onLoan ? (
                        <>
                          Out on loan. Mark it returned on{" "}
                          <Link to="/borrowing">Borrowing</Link> to change this.
                        </>
                      ) : (
                        cannotOfferMessage
                      )}
                    </p>
                  </div>
                ) : (
                  <SelectMenu
                    label="Lending & Selling"
                    value={offer}
                    options={offerChoices}
                    onChange={handleOfferChange}
                    block
                  />
                )}
              </div>

              {lendNudge && (
                <div className="lend-nudge" role="status">
                  <p>
                    <strong>Finished!</strong> If it's your own copy, you could
                    lend it to your Trusted Book Club.
                  </p>
                  <div className="lend-nudge-actions">
                    <button
                      type="button"
                      className="btn btn-primary"
                      onClick={lendIt}
                    >
                      Offer to borrow
                    </button>
                    <button
                      type="button"
                      className="btn btn-secondary"
                      onClick={() => setLendNudge(false)}
                    >
                      Not now
                    </button>
                  </div>
                </div>
              )}

              {offerError && <p className="form-error">{offerError}</p>}

              {shelf === "currently-reading" && (
                <ReadingProgress
                  key={`progress-${myEntry.id}`}
                  entry={myEntry}
                  onSaved={(updated) => setBook({ ...book, myEntry: updated })}
                />
              )}

              {shelf !== "currently-reading" && (
                <PageCount
                  key={`pages-${myEntry.id}`}
                  entry={myEntry}
                  onSaved={(updated) => setBook({ ...book, myEntry: updated })}
                />
              )}

              {myEntry.timesRead > 0 && (
                <ReadingHistory
                  key={`history-${myEntry.id}`}
                  entry={myEntry}
                  onChanged={(changes) =>
                    setBook({ ...book, myEntry: { ...myEntry, ...changes } })
                  }
                />
              )}

              {offer === "for-sale" && (
                <button
                  className="btn btn-secondary offer-clear"
                  onClick={() => handleOfferChange("none")}
                >
                  Sold / No Longer Selling
                </button>
              )}

              {offer !== "lent-out" && (
                <button
                  className="btn btn-secondary remove-entry"
                  onClick={handleRemove}
                >
                  Remove from My Shelves
                </button>
              )}
            </>
          ) : (
            <div className="add-to-shelves">
              <div className="add-to-shelves-row">
                <SelectMenu
                  label="Add this book to your shelves"
                  value={addShelf}
                  options={shelfChoices}
                  onChange={(chosen) => setAddShelf(chosen as ShelfType)}
                  block
                />
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
                  {owner.offer === "for-sale"
                    ? "For Sale"
                    : "Available to Borrow"}
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
              ) : !owner.canRequest ? (
                <p className="rating-meta">
                  Only {owner.displayName}'s Trusted Book Club can borrow this
                </p>
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
                {review.rating === null ? (
                  <span className="review-unrated">No rating</span>
                ) : (
                  <>
                    {"★".repeat(review.rating)}
                    {"☆".repeat(5 - review.rating)}
                  </>
                )}
              </span>
              <span className="review-date">{formatDate(review.date)}</span>
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
          <h2>
            {editingReviewId
              ? "Edit your review"
              : dnfNudge
                ? "Didn't finish? Say why"
                : "Rate and Review"}
          </h2>
          {dnfNudge && !editingReviewId && (
            <p className="review-nudge">
              Optional, but other readers will want to know. No stars needed.
            </p>
          )}
          {error && <p className="form-error">{error}</p>}
          <form className="review-form" onSubmit={handleReviewSubmit}>
            <SelectMenu
              label="Rating"
              value={rating}
              options={ratingOptions}
              onChange={setRating}
              block
            />

            <label htmlFor="review-text">Review (optional)</label>
            <textarea
              id="review-text"
              ref={reviewTextRef}
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
