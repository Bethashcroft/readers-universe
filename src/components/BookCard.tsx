import { Link } from "react-router-dom";
import type { BookResponse } from "../api/books";
import "./BookCard.css";

interface BookCardProps {
  book: BookResponse;
}

const offerBadges: Record<string, string> = {
  "available-to-borrow": "Available to Borrow",
  "for-sale": "For Sale",
  "lent-out": "Lent Out",
};

function BookCard({ book }: BookCardProps) {
  return (
    <Link to={`/book/${book.id}`} className="book-card">
      <img
        className="book-cover"
        src={book.coverUrl}
        alt={`Cover of ${book.title}`}
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
        {offerBadges[book.offer] && (
          <span className={`book-offer-badge ${book.offer}`}>
            {offerBadges[book.offer]}
          </span>
        )}
      </div>
    </Link>
  );
}

export default BookCard;
