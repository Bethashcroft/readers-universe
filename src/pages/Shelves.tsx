import { useState, useEffect, useCallback } from "react";
import { getMyBooks, getShelfCounts } from "../api/books";
import type { LibraryEntryResponse } from "../api/books";
import BookCard from "../components/BookCard";
import CoverBackfill from "../components/CoverBackfill";
import Pager from "../components/Pager";
import SelectMenu from "../components/SelectMenu";
import ErrorState from "../components/ErrorState";
import { shelfLabels } from "../types/book";
import type { ShelfType } from "../types/book";
import { usePageTitle } from "../hooks/usePageTitle";
import "./Shelves.css";

function Shelves() {
  usePageTitle("My Shelves");

  const [activeShelf, setActiveShelf] = useState<ShelfType | "all">("all");
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [sort, setSort] = useState("added");
  const [page, setPage] = useState(1);
  const [books, setBooks] = useState<LibraryEntryResponse[]>([]);
  const [totalPages, setTotalPages] = useState(0);
  const [total, setTotal] = useState(0);
  const [counts, setCounts] = useState<Record<string, number>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError(false);

    try {
      const result = await getMyBooks({
        shelf: activeShelf === "all" ? undefined : activeShelf,
        search: debouncedSearch,
        sort,
        page,
      });
      setBooks(result.items);
      setTotalPages(result.totalPages);
      setTotal(result.total);
      setCounts(await getShelfCounts());
    } catch (err) {
      console.error("Failed to fetch books:", err);
      setError(true);
    } finally {
      setLoading(false);
    }
  }, [activeShelf, debouncedSearch, sort, page]);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(search);
      setPage(1);
    }, 300);

    return () => clearTimeout(timer);
  }, [search]);

  const chooseShelf = (shelf: ShelfType | "all") => {
    setActiveShelf(shelf);
    setPage(1);
  };

  const chooseSort = (next: string) => {
    setSort(next);
    setPage(1);
  };

  const changePage = (next: number) => {
    setPage(next);
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  if (error) {
    return (
      <ErrorState
        message="We couldn't load your shelves. Check your connection and try again."
        onRetry={load}
      />
    );
  }

  const allCount = Object.values(counts).reduce((sum, n) => sum + n, 0);

  return (
    <div className="shelves">
      <h1>My Shelves</h1>

      <div className="shelf-tabs">
        <button
          className={`shelf-tab ${activeShelf === "all" ? "active" : ""}`}
          onClick={() => chooseShelf("all")}
        >
          All
          {allCount > 0 && <span className="shelf-tab-count">{allCount}</span>}
        </button>
        {Object.entries(shelfLabels).map(([value, label]) => (
          <button
            key={value}
            className={`shelf-tab ${activeShelf === value ? "active" : ""}`}
            onClick={() => chooseShelf(value as ShelfType)}
          >
            {label}
            {counts[value] > 0 && (
              <span className="shelf-tab-count">{counts[value]}</span>
            )}
          </button>
        ))}
      </div>

      {allCount > 0 && (
        <div className="shelf-controls">
          <input
            type="text"
            className="shelf-search"
            placeholder="Search your shelves by title or author"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <SelectMenu
            label="Sort by"
            value={sort}
            onChange={chooseSort}
            options={[
              { value: "added", label: "Recently added" },
              { value: "title", label: "Title" },
              { value: "author", label: "Author" },
              { value: "rating", label: "My rating" },
            ]}
          />
        </div>
      )}

      {allCount > 0 && <CoverBackfill onFinished={load} />}

      {loading ? (
        <p>Loading your shelves...</p>
      ) : (
        <>
          <div className="book-grid">
            {books.map((book) => (
              <BookCard key={book.id} book={book} />
            ))}
          </div>

          {total === 0 && (
            <p className="empty-shelf">
              {debouncedSearch
                ? `Nothing on this shelf matches "${debouncedSearch}".`
                : "No books on this shelf yet."}
            </p>
          )}

          <Pager
            page={page}
            totalPages={totalPages}
            total={total}
            noun="book"
            onChange={changePage}
          />
        </>
      )}
    </div>
  );
}

export default Shelves;
