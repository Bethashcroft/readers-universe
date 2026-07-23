import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useBooks } from "../context/useBooks";
import { lookupBook } from "../api/books";
import { shelfLabels, offerLabels } from "../types/book";
import type { ShelfType, OfferType } from "../types/book";
import { usePageTitle } from "../hooks/usePageTitle";
import "../styles/forms.css";
import "./AddBook.css";

type LookupStatus = "idle" | "looking" | "found" | "notfound";

function AddBook() {
  usePageTitle("Add a Book");
  const { addBook } = useBooks();
  const navigate = useNavigate();

  const [isbn, setIsbn] = useState("");
  const [lookupStatus, setLookupStatus] = useState<LookupStatus>("idle");
  const [title, setTitle] = useState("");
  const [author, setAuthor] = useState("");
  const [coverUrl, setCoverUrl] = useState("");
  const [shelf, setShelf] = useState<ShelfType>("tbr");
  const [offer, setOffer] = useState<OfferType>("none");
  const [rating, setRating] = useState("");
  const [error, setError] = useState("");

  const handleLookup = async () => {
    if (!isbn.trim()) return;
    setLookupStatus("looking");
    setError("");

    try {
      const result = await lookupBook(isbn.trim());
      setTitle(result.title);
      setAuthor(result.author);
      setCoverUrl(result.coverUrl);
      setLookupStatus("found");
    } catch {
      setLookupStatus("notfound");
    }
  };

  const handleSubmit = async (e: React.SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError("");

    try {
      await addBook({
        title,
        author,
        coverUrl:
          coverUrl ||
          `https://placehold.co/200x300/1a1430/a9a3cc?text=${encodeURIComponent(title)}`,
        shelf,
        offer,
        rating: rating ? Number(rating) : null,
      });
      navigate("/shelves");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to add book");
    }
  };

  return (
    <div className="auth-page">
      <form className="auth-form" onSubmit={handleSubmit}>
        <h1>Add a Book</h1>
        {error && <p className="form-error">{error}</p>}

        <label htmlFor="isbn">Have the book? Enter its ISBN to auto-fill</label>
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
            {lookupStatus === "looking" ? "Looking up…" : "Fill in the details"}
          </button>
        </div>
        {lookupStatus === "found" && (
          <p className="isbn-status found">Found it! Check the details below.</p>
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

        <label htmlFor="shelf">Shelf</label>
        <select
          id="shelf"
          value={shelf}
          onChange={(e) => setShelf(e.target.value as ShelfType)}
        >
          {Object.entries(shelfLabels).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>

        <label htmlFor="offer">Lending & Selling</label>
        <select
          id="offer"
          value={offer}
          onChange={(e) => setOffer(e.target.value as OfferType)}
        >
          {Object.entries(offerLabels).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>

        <label htmlFor="rating">Optional Rating</label>
        <select
          id="rating"
          value={rating}
          onChange={(e) => setRating(e.target.value)}
        >
          <option value="">No rating</option>
          <option value="1">★☆☆☆☆</option>
          <option value="2">★★☆☆☆</option>
          <option value="3">★★★☆☆</option>
          <option value="4">★★★★☆</option>
          <option value="5">★★★★★</option>
        </select>

        <button type="submit">Add book</button>
      </form>
    </div>
  );
}

export default AddBook;
