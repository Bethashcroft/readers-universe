import React, { useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useBooks } from "../context/useBooks";
import { lookupBook } from "../api/books";
import type { BookLookupResult, BookSearchResult } from "../api/books";
import {
  shelfChoices,
  offerChoices,
  formatChoices,
  ratingOptions,
  canOffer,
  cannotOfferMessage,
} from "../types/book";
import type { ShelfType, OfferType, FormatType } from "../types/book";
import SelectMenu from "../components/SelectMenu";
import { placeholderCover } from "../types/covers";
import { usePageTitle } from "../hooks/usePageTitle";
import ImportLibrary from "../components/ImportLibrary";
import BookFinder from "../components/BookFinder";
import "../styles/forms.css";
import "./AddBook.css";

type LookupStatus = "idle" | "looking" | "found" | "notfound";

function AddBook() {
  usePageTitle("Add a Book");
  const { addBook } = useBooks();
  const navigate = useNavigate();
  const [params] = useSearchParams();

  const [isbn, setIsbn] = useState("");
  const [lookupStatus, setLookupStatus] = useState<LookupStatus>("idle");
  const [title, setTitle] = useState("");
  const [author, setAuthor] = useState("");
  const [coverUrl, setCoverUrl] = useState("");
  const [shelf, setShelf] = useState<ShelfType>("tbr");
  const [offer, setOffer] = useState<OfferType>("none");
  const [format, setFormat] = useState<FormatType>("");
  const [rating, setRating] = useState("");
  const [reviewText, setReviewText] = useState("");
  const [containsSpoiler, setContainsSpoiler] = useState(false);
  const [error, setError] = useState("");
  const [lastLookup, setLastLookup] = useState<BookLookupResult | null>(null);
  const [showMore, setShowMore] = useState(false);

  const handleLookup = async () => {
    if (!isbn.trim()) return;
    setLookupStatus("looking");
    setError("");

    try {
      const result = await lookupBook(isbn.trim());
      setTitle(result.title);
      setAuthor(result.author);
      setCoverUrl(result.coverUrl);
      setLastLookup(result);
      setLookupStatus("found");
    } catch {
      const untouched =
        lastLookup &&
        title === lastLookup.title &&
        author === lastLookup.author;

      if (untouched) {
        setTitle("");
        setAuthor("");
        setCoverUrl("");
      }

      setLastLookup(null);
      setLookupStatus("notfound");
    }
  };

  const handleFormatChange = (chosen: string) => {
    setFormat(chosen as FormatType);

    if (!canOffer(chosen)) {
      setOffer("none");
    }
  };

  const handlePick = (book: BookSearchResult) => {
    setTitle(book.title);
    setAuthor(book.author);
    setCoverUrl(book.coverUrl);
    setIsbn(book.isbn);
    setLastLookup(book);
    setLookupStatus("found");
    setError("");
  };

  const handleSubmit = async (e: React.SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError("");

    try {
      await addBook({
        title,
        author,
        coverUrl: coverUrl || placeholderCover(title),
        isbn,
        shelf,
        offer,
        format,
        rating: rating ? Number(rating) : null,
        reviewText,
        containsSpoiler,
        pageCount: lastLookup?.pageCount ?? null,
      });
      navigate("/shelves");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to add book");
    }
  };

  return (
    <div className="auth-page add-book-page">
      <div className="add-book-inner">
        <form className="auth-form" onSubmit={handleSubmit}>
          <h1>Add a Book</h1>
          {error && <p className="form-error">{error}</p>}

          <BookFinder
            initialQuery={params.get("q") ?? ""}
            onPick={handlePick}
          />

          <label htmlFor="isbn">Or enter its ISBN to auto-fill</label>
          <div className="isbn-lookup">
            <input
              type="text"
              id="isbn"
              placeholder="e.g. 9780261103344"
              value={isbn}
              onChange={(e) => setIsbn(e.target.value)}
            />
            <button
              type="button"
              className="btn btn-secondary"
              onClick={handleLookup}
              disabled={lookupStatus === "looking" || !isbn.trim()}
            >
              {lookupStatus === "looking"
                ? "Looking up…"
                : "Fill in the details"}
            </button>
          </div>
          {lookupStatus === "found" && (
            <p className="isbn-status found">
              Found it! Check the details below.
            </p>
          )}
          {lookupStatus === "notfound" && (
            <p className="isbn-status notfound">
              No book found for that ISBN. Just type the details in below.
            </p>
          )}

          <label htmlFor="title">Title</label>
          <input
            type="text"
            id="title"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            required
          />

          <label htmlFor="author">Author</label>
          <input
            type="text"
            id="author"
            value={author}
            onChange={(e) => setAuthor(e.target.value)}
            required
          />

          <SelectMenu
            label="Shelf"
            value={shelf}
            options={shelfChoices}
            onChange={(chosen) => setShelf(chosen as ShelfType)}
            block
          />

          <SelectMenu
            label="Format"
            value={format}
            options={formatChoices}
            onChange={handleFormatChange}
            block
          />

          {canOffer(format) ? (
            <SelectMenu
              label="Lending & Selling"
              value={offer}
              options={offerChoices}
              onChange={(chosen) => setOffer(chosen as OfferType)}
              block
            />
          ) : (
            <>
              <span className="form-field-label">Lending & Selling</span>
              <p className="isbn-status notfound">{cannotOfferMessage}</p>
            </>
          )}

          <SelectMenu
            label="Optional Rating"
            value={rating}
            options={ratingOptions}
            onChange={setRating}
            block
          />

          <label htmlFor="reviewText">Review (optional)</label>
          <textarea
            id="reviewText"
            value={reviewText}
            onChange={(e) => setReviewText(e.target.value)}
            rows={4}
          />
          <label className="review-spoiler-check">
            <input
              type="checkbox"
              checked={containsSpoiler}
              onChange={(e) => setContainsSpoiler(e.target.checked)}
            />
            This review contains spoilers
          </label>

          <button type="submit">Add book</button>
        </form>

        <section className="add-book-more">
          <button
            type="button"
            className="add-book-more-toggle"
            aria-expanded={showMore}
            onClick={() => setShowMore((open) => !open)}
          >
            <h2>Bring your library with you</h2>
            <span className="add-book-more-hint">
              {showMore ? "Hide" : "Show"}
            </span>
          </button>
          {showMore && <ImportLibrary />}
        </section>
      </div>
    </div>
  );
}

export default AddBook;
