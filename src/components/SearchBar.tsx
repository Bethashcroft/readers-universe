import { useState, useEffect, useRef, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { searchReaders } from "../api/readers";
import type { ReaderResponse } from "../api/readers";
import { searchBooks } from "../api/books";
import type { BookSummaryResponse } from "../api/books";
import { useClickOutside } from "../hooks/useClickOutside";
import { useDebouncedValue } from "../hooks/useDebouncedValue";
import Avatar from "./Avatar";
import BookCover from "./BookCover";
import "./SearchBar.css";

const Shown = 5;

const readerPath = (reader: ReaderResponse) => `/profile/${reader.userName}`;
const bookPath = (book: BookSummaryResponse) => `/book/${book.id}`;

function SearchBar() {
  const navigate = useNavigate();
  const [term, setTerm] = useState("");
  const query = useDebouncedValue(term.trim());
  const [readers, setReaders] = useState<ReaderResponse[]>([]);
  const [readerTotal, setReaderTotal] = useState(0);
  const [books, setBooks] = useState<BookSummaryResponse[]>([]);
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const close = useCallback(() => setOpen(false), []);

  useClickOutside(containerRef, open, close);

  useEffect(() => {
    if (!query) {
      return;
    }

    let active = true;

    const search = async () => {
      try {
        const [foundReaders, foundBooks] = await Promise.all([
          searchReaders({ q: query, page: 1 }),
          searchBooks({ q: query, page: 1 }),
        ]);
        if (!active) return;
        setReaders(foundReaders.items.slice(0, Shown));
        setReaderTotal(foundReaders.total);
        setBooks(foundBooks.items.slice(0, Shown));
        setOpen(true);
      } catch (err) {
        console.error("Search failed:", err);
      }
    };

    search();

    return () => {
      active = false;
    };
  }, [query]);

  const hasTerm = term.trim().length > 0;
  const visibleReaders = hasTerm ? readers : [];
  const visibleBooks = hasTerm ? books : [];
  const firstPath = visibleReaders[0]
    ? readerPath(visibleReaders[0])
    : visibleBooks[0]
      ? bookPath(visibleBooks[0])
      : null;

  const goTo = (path: string) => {
    setOpen(false);
    setTerm("");
    navigate(path);
  };

  return (
    <div className="search-bar" ref={containerRef}>
      <svg className="search-bar-icon" viewBox="0 0 20 20" aria-hidden="true">
        <circle cx="9" cy="9" r="6" />
        <path d="M13.5 13.5 L17 17" />
      </svg>
      <input
        type="search"
        className="search-bar-input"
        placeholder="Find readers or books"
        aria-label="Find readers or books"
        value={term}
        onChange={(e) => setTerm(e.target.value)}
        onFocus={() => hasTerm && setOpen(true)}
        onKeyDown={(e) => {
          if (e.key === "Enter" && firstPath) {
            goTo(firstPath);
          }
        }}
      />

      {open && hasTerm && (
        <div className="search-bar-panel">
          {visibleReaders.length === 0 && visibleBooks.length === 0 && (
            <p className="search-bar-empty">No readers or books found.</p>
          )}

          {visibleReaders.length > 0 && (
            <div className="search-bar-group">
              <span className="eyebrow search-bar-heading">Readers</span>
              {visibleReaders.map((reader) => (
                <button
                  key={reader.userName}
                  type="button"
                  className="search-bar-result"
                  onClick={() => goTo(readerPath(reader))}
                >
                  <Avatar
                    url={reader.avatarUrl}
                    name={reader.displayName}
                    size={32}
                  />
                  <span className="search-bar-names">
                    <span className="search-bar-name">
                      {reader.displayName}
                    </span>
                    <span className="search-bar-handle">@{reader.userName}</span>
                  </span>
                </button>
              ))}
              {readerTotal > visibleReaders.length && (
                <button
                  type="button"
                  className="search-bar-more"
                  onClick={() =>
                    goTo(`/readers?q=${encodeURIComponent(term.trim())}`)
                  }
                >
                  See all {readerTotal} readers
                </button>
              )}
            </div>
          )}

          {visibleBooks.length > 0 && (
            <div className="search-bar-group">
              <span className="eyebrow search-bar-heading">Books</span>
              {visibleBooks.map((book) => (
                <button
                  key={book.id}
                  type="button"
                  className="search-bar-result"
                  onClick={() => goTo(bookPath(book))}
                >
                  <BookCover
                    className="search-bar-cover"
                    src={book.coverUrl}
                    title={book.title}
                  />
                  <span className="search-bar-names">
                    <span className="search-bar-name">{book.title}</span>
                    <span className="search-bar-author">{book.author}</span>
                  </span>
                </button>
              ))}
            </div>
          )}

          <button
            type="button"
            className="search-bar-more"
            onClick={() =>
              goTo(`/add-book?q=${encodeURIComponent(term.trim())}`)
            }
          >
            Can't see it? Add a book
          </button>
        </div>
      )}
    </div>
  );
}

export default SearchBar;
