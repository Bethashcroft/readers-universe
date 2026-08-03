import { Link } from "react-router-dom";
import type { LibraryEntryResponse } from "../api/books";
import { offerLabels } from "../types/book";
import type { OfferType } from "../types/book";
import BookCover from "./BookCover";
import "./BookCard.css";

interface BookCardProps {
  book: LibraryEntryResponse;
}

function BookCard({ book }: BookCardProps) {
  return (
    <Link to={`/book/${book.bookId}`} className="book-card">
      <BookCover
        className="book-cover"
        src={book.coverUrl}
        title={book.title}
      />
      <div className="book-info">
        <h3 className="book-title">{book.title}</h3>
        <p className="book-author">{book.author}</p>
        {book.rating && (
          <p className="book-rating">
            {"★".repeat(book.rating)}
            {"☆".repeat(5 - book.rating)}
          </p>
        )}
        {book.offer !== "none" && (
          <span className={`book-offer-badge ${book.offer}`}>
            {offerLabels[book.offer as OfferType] ?? book.offer}
          </span>
        )}
      </div>
    </Link>
  );
}

export default BookCard;
