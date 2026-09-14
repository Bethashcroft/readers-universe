import { useState, useEffect } from "react";
import { getMyBooks, offerBook } from "../api/books";
import type { LibraryEntryResponse } from "../api/books";
import { useDebouncedValue } from "../hooks/useDebouncedValue";
import Modal from "./Modal";
import BookCover from "./BookCover";
import "./OfferBookPicker.css";

type OfferBookPickerProps = {
  onClose: () => void;
  onOffered: () => void;
};

const pageSize = 100;

function OfferBookPicker({ onClose, onOffered }: OfferBookPickerProps) {
  const [search, setSearch] = useState("");
  const query = useDebouncedValue(search.trim());
  const [books, setBooks] = useState<LibraryEntryResponse[] | null>(null);
  const [more, setMore] = useState(false);
  const [offeringId, setOfferingId] = useState<number | null>(null);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;

    const load = async () => {
      try {
        const result = await getMyBooks({
          search: query,
          offerable: true,
          page: 1,
          pageSize,
        });
        if (!active) return;
        setBooks(result.items);
        setMore(result.totalPages > 1);
        setError("");
      } catch (err) {
        if (!active) return;
        setError(
          err instanceof Error ? err.message : "Failed to load your shelves",
        );
      }
    };

    load();

    return () => {
      active = false;
    };
  }, [query]);

  const offer = async (id: number) => {
    setOfferingId(id);
    setError("");

    try {
      await offerBook(id);
      onOffered();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to offer that book");
    } finally {
      setOfferingId(null);
    }
  };

  const stale = query !== search.trim();

  return (
    <Modal
      title="Offer a book"
      onClose={onClose}
      locked={offeringId !== null}
      className="picker-modal"
    >
      <input
        type="search"
        className="search-input"
        placeholder="Search your shelves"
        aria-label="Search your shelves"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
      />

      {error && <p className="form-error">{error}</p>}

      <div className="picker-list">
        {books === null || stale ? (
          <p className="picker-empty">Loading your shelves...</p>
        ) : books.length === 0 ? (
          <p className="picker-empty">
            {query
              ? "Nothing on your shelves matches that."
              : "Nothing left to offer."}
          </p>
        ) : (
          books.map((book) => (
            <div key={book.id} className="picker-row">
              <BookCover
                className="picker-cover"
                src={book.coverUrl}
                title={book.title}
              />
              <span className="picker-text">
                <span className="picker-title">{book.title}</span>
                <span className="picker-author">{book.author}</span>
              </span>
              <button
                type="button"
                className="btn btn-primary"
                onClick={() => offer(book.id)}
                disabled={offeringId !== null}
              >
                {offeringId === book.id ? "Offering..." : "Offer"}
              </button>
            </div>
          ))
        )}
      </div>

      {more && !stale && (
        <p className="picker-more">
          Showing the first {pageSize}. Search to find the rest.
        </p>
      )}
    </Modal>
  );
}

export default OfferBookPicker;
