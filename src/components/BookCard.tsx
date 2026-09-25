import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import type { LibraryEntryResponse } from "../api/books";
import { offerLabel, offerBadgeClass } from "../types/book";
import BookCover from "./BookCover";
import { percentThrough } from "../utils/progress";
import "./BookCard.css";

interface BookCardProps {
  book: LibraryEntryResponse;
  action?: ReactNode;
}

function BookCard({ book, action }: BookCardProps) {
  const percent =
    book.shelf === "currently-reading"
      ? percentThrough(book.page, book.pageCount)
      : null;

  return (
    <div className="book-card">
      <Link to={`/book/${book.bookId}`} className="book-card-link">
        <BookCover
          className="book-cover"
          src={book.coverUrl}
          title={book.title}
        />
        <div className="book-info">
          <h3 className="book-title">{book.title}</h3>
          <p className="book-author">{book.author}</p>
          {percent !== null && (
            <div className="book-progress" title={`${percent}% through`}>
              <div
                className="book-progress-fill"
                style={{ width: `${percent}%` }}
              />
            </div>
          )}
          {book.rating && (
            <p className="book-rating">
              {"★".repeat(book.rating)}
              {"☆".repeat(5 - book.rating)}
            </p>
          )}
          {book.offer !== "none" && (
            <span className={`book-offer-badge ${offerBadgeClass(book.offer)}`}>
              {offerLabel(book.offer)}
            </span>
          )}
        </div>
      </Link>
      {action && <div className="book-card-action">{action}</div>}
    </div>
  );
}

export default BookCard;
