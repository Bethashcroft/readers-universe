import { useState } from "react";
import { useBooks } from "../context/useBooks";
import BookCard from "../components/BookCard";
import { shelfLabels } from "../types/book";
import type { ShelfType } from "../types/book";
import "./Shelves.css";

function Shelves() {
  const { books, loading } = useBooks();
  const [activeShelf, setActiveShelf] = useState<ShelfType | "all">("all");

  const filteredBooks =
    activeShelf === "all"
      ? books
      : books.filter((book) => book.shelf === activeShelf);

  if (loading) {
    return <p>Loading your shelves...</p>;
  }

  return (
    <div className="shelves">
      <h1>My Shelves</h1>

      <div className="shelf-tabs">
        <button
          className={`shelf-tab ${activeShelf === "all" ? "active" : ""}`}
          onClick={() => setActiveShelf("all")}
        >
          All
        </button>
        {Object.entries(shelfLabels).map(([value, label]) => (
          <button
            key={value}
            className={`shelf-tab ${activeShelf === value ? "active" : ""}`}
            onClick={() => setActiveShelf(value as ShelfType)}
          >
            {label}
          </button>
        ))}
      </div>

      <div className="book-grid">
        {filteredBooks.map((book) => (
          <BookCard key={book.id} book={book} />
        ))}
      </div>

      {filteredBooks.length === 0 && (
        <p className="empty-shelf">No books on this shelf yet.</p>
      )}
    </div>
  );
}

export default Shelves;
