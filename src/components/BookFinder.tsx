import { useState, useEffect } from "react";
import { findBooks } from "../api/books";
import type { BookSearchResult } from "../api/books";
import { useDebouncedValue } from "../hooks/useDebouncedValue";
import BookCover from "./BookCover";
import "./BookFinder.css";

type BookFinderProps = {
  initialQuery?: string;
  onPick: (book: BookSearchResult) => void;
};

function BookFinder({ initialQuery = "", onPick }: BookFinderProps) {
  const [term, setTerm] = useState(initialQuery);
  const query = useDebouncedValue(term.trim());
  const [results, setResults] = useState<BookSearchResult[]>([]);
  const [searchedFor, setSearchedFor] = useState("");

  useEffect(() => {
    if (!query) {
      return;
    }

    let active = true;

    const search = async () => {
      try {
        const found = await findBooks(query);
        if (!active) return;
        setResults(found);
      } catch (err) {
        console.error("Book search failed:", err);
        if (!active) return;
        setResults([]);
      }
      setSearchedFor(query);
    };

    search();

    return () => {
      active = false;
    };
  }, [query]);

  const wanted = term.trim();
  const searching = wanted.length > 0 && searchedFor !== wanted;
  const finished = wanted.length > 0 && searchedFor === wanted;

  const pick = (book: BookSearchResult) => {
    setTerm("");
    onPick(book);
  };

  return (
    <div className="book-finder">
      <label htmlFor="book-finder">Search for the book</label>
      <input
        type="search"
        id="book-finder"
        placeholder="Title or author"
        value={term}
        onChange={(e) => setTerm(e.target.value)}
      />

      {searching && <p className="book-finder-status">Searching…</p>}

      {finished && results.length === 0 && (
        <p className="book-finder-status">
          Nothing found. Just type the details in below.
        </p>
      )}

      {finished && results.length > 0 && (
        <ul className="book-finder-results">
          {results.map((book) => (
            <li key={`${book.title}-${book.isbn}-${book.year}`}>
              <button
                type="button"
                className="book-finder-result"
                onClick={() => pick(book)}
              >
                <BookCover
                  className="book-finder-cover"
                  src={book.coverUrl}
                  title={book.title}
                />
                <span className="book-finder-names">
                  <span className="book-finder-title">{book.title}</span>
                  <span className="book-finder-author">
                    {book.author}
                    {book.year && ` (${book.year})`}
                  </span>
                </span>
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

export default BookFinder;
