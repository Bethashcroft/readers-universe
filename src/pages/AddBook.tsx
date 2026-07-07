import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useBooks } from "../context/BookContext";
import type { ShelfType, OfferType } from "../types/book";
import "../styles/forms.css";
import "./AddBook.css";

function AddBook() {
  const { addBook } = useBooks();
  const navigate = useNavigate();

  const [title, setTitle] = useState("");
  const [author, setAuthor] = useState("");
  const [shelf, setShelf] = useState<ShelfType>("tbr");
  const [offer, setOffer] = useState<OfferType>("none");
  const [rating, setRating] = useState("");
  const [error, setError] = useState("");

  const handleSubmit = async (e: React.SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError("");

    try {
      await addBook({
        title,
        author,
        coverUrl: `https://placehold.co/200x300/1a1430/a9a3cc?text=${encodeURIComponent(title)}`,
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
          <option value="currently-reading">Currently Reading</option>
          <option value="read">Read</option>
          <option value="tbr">To Be Read</option>
          <option value="dnf">Did Not Finish</option>
        </select>

        <label htmlFor="offer">Lending & Selling</label>
        <select
          id="offer"
          value={offer}
          onChange={(e) => setOffer(e.target.value as OfferType)}
        >
          <option value="none">Not offered</option>
          <option value="available-to-borrow">Available to Borrow</option>
          <option value="for-sale">For Sale</option>
          <option value="lent-out">Lent Out</option>
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
